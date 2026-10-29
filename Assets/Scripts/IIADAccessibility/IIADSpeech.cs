using System;
using UnityEngine;
using UnityEngine.Accessibility;

/// <summary>
/// One place for everything the app says out loud. Replaces UAP_AccessibilityManager.Say
/// and the UAP AndroidTTS plugin.
///
/// Where speech goes:
///   1. TalkBack / VoiceOver is on  -> a native screen-reader announcement
///      (Unity 6 Screen Reader API, AssistiveSupport.notificationDispatcher).
///   2. Voice Guide is on           -> Android's built-in TextToSpeech (no plugin).
///   3. Neither                     -> silent (logged in the Editor).
/// </summary>
public static class IIADSpeech
{
    const string PrefVoiceGuide = "IIAD_VoiceGuide";
    const string PrefRate = "Accessibility_Speech_Rate";   // same key SpeechManager already uses (0..100)
    const string PrefAnnounce = "IIAD_AnnounceStatus";

    static bool initialised;
    static bool voiceGuide;
    static bool announceStatus = true;
    static string lastSpoken = "";

    // Speech can be requested from Firebase / network callbacks on other threads.
    // Android JNI and the screen reader must be called on Unity's main thread, so those requests are queued.
    static int mainThreadId = -1;
    static readonly System.Collections.Concurrent.ConcurrentQueue<Action> mainThreadQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void CaptureMainThread() { mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId; initialised = false; }

    static bool OffMainThread(Action a)
    {
        if (mainThreadId < 0 || System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId) return false;
        mainThreadQueue.Enqueue(a);
        return true;
    }

    /// <summary>Runs speech requested from background threads. Called every frame by IIADAccessibilityManager.</summary>
    public static void DrainMainThreadQueue()
    {
        int guard = 32;
        while (guard-- > 0 && mainThreadQueue.TryDequeue(out var a))
        {
            try { a(); } catch (Exception e) { Debug.LogWarning("IIADSpeech: " + e.Message); }
        }
    }

    /// <summary>Raised when Voice Guide is switched on or off.</summary>
    public static event Action<bool> VoiceGuideChanged;

    /// <summary>The text most recently sent to speech. Used by "repeat".</summary>
    public static string LastSpoken => lastSpoken;

    // ---------------------------------------------------------------------
    // State
    // ---------------------------------------------------------------------

    /// <summary>True when the phone's own screen reader (TalkBack / VoiceOver) is running.</summary>
    public static bool IsScreenReaderOn
    {
        get
        {
            try { return AssistiveSupport.isScreenReaderEnabled; }
            catch { return false; }
        }
    }

    /// <summary>The app's own self-voicing mode for users who do not run TalkBack.</summary>
    public static bool VoiceGuideEnabled
    {
        get { EnsureInit(); return voiceGuide; }
        set
        {
            EnsureInit();
            if (voiceGuide == value) return;
            voiceGuide = value;
            PlayerPrefs.SetInt(PrefVoiceGuide, value ? 1 : 0);
            PlayerPrefs.Save();
            if (!value) NativeTTS.Stop();
            VoiceGuideChanged?.Invoke(value);
        }
    }

    /// <summary>Voice Guide is in charge of gestures and speech (Voice Guide on, TalkBack off).</summary>
    public static bool IsVoiceGuideActive => VoiceGuideEnabled && !IsScreenReaderOn;

    /// <summary>Anything will be heard: TalkBack or Voice Guide.</summary>
    public static bool IsAnyOutputOn => IsScreenReaderOn || VoiceGuideEnabled;

    /// <summary>User setting: speak status changes such as "Generating poster". Default on.</summary>
    public static bool AnnounceStatus
    {
        get { EnsureInit(); return announceStatus; }
        set { EnsureInit(); announceStatus = value; PlayerPrefs.SetInt(PrefAnnounce, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>Speech rate 0..100, 50 = normal. Applies to Voice Guide only; TalkBack uses the phone's rate.</summary>
    public static float Rate
    {
        get => PlayerPrefs.GetFloat(PrefRate, 50f);
        set
        {
            float r = Mathf.Clamp(value, 0f, 100f);
            PlayerPrefs.SetFloat(PrefRate, r);
            if (!OffMainThread(() => NativeTTS.SetRate(RateToAndroid(r)))) NativeTTS.SetRate(RateToAndroid(r));
        }
    }

    public static bool IsSpeaking => NativeTTS.IsSpeaking;

    // ---------------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------------

    /// <summary>Call once at startup (IIADAccessibilityManager does this for you).</summary>
    public static void Initialize(string languageTag = "en-US")
    {
        EnsureInit();
        NativeTTS.Init(languageTag, RateToAndroid(Rate));
    }

    public static void Shutdown() => NativeTTS.Shutdown();

    public static void SetLanguage(string languageTag) => NativeTTS.SetLanguage(languageTag);

    static void EnsureInit()
    {
        if (initialised) return;
        initialised = true;
        voiceGuide = PlayerPrefs.GetInt(PrefVoiceGuide, 0) == 1;
        announceStatus = PlayerPrefs.GetInt(PrefAnnounce, 1) == 1;
    }

    /// <summary>0..100 slider to Android rate: 0 = 0.5x, 50 = 1x (normal), 100 = 2x.</summary>
    static float RateToAndroid(float r)
    {
        r = Mathf.Clamp(r, 0f, 100f);
        return r <= 50f ? Mathf.Lerp(0.5f, 1f, r / 50f) : Mathf.Lerp(1f, 2f, (r - 50f) / 50f);
    }

    // ---------------------------------------------------------------------
    // Speaking
    // ---------------------------------------------------------------------

    /// <summary>
    /// Say something now. interrupt=true stops what is being said first (Voice Guide only;
    /// TalkBack queues announcements itself).
    /// </summary>
    public static void Say(string text, bool interrupt = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (OffMainThread(() => Say(text, interrupt))) return;
        text = Clean(text);
        lastSpoken = text;

        if (IsScreenReaderOn)
        {
            try { AssistiveSupport.notificationDispatcher.SendAnnouncement(text); }
            catch (Exception e) { Debug.LogWarning("IIADSpeech: announcement failed: " + e.Message); }
            return;
        }

        if (VoiceGuideEnabled)
        {
            NativeTTS.Speak(text, interrupt);
            return;
        }

#if UNITY_EDITOR
        Debug.Log("[IIAD speech, output off] " + text);
#endif
    }

    /// <summary>
    /// Speak through the Voice Guide engine even when Voice Guide is off. Only for the few
    /// messages that must be heard to turn it on ("Voice Guide off", first-launch hint).
    /// </summary>
    public static void SayDirect(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (OffMainThread(() => SayDirect(text))) return;
        if (IsScreenReaderOn) { Say(text); return; }
        lastSpoken = Clean(text);
        NativeTTS.Speak(lastSpoken, true);
    }

    /// <summary>Status updates (loading, ready, error). Respects the "Announce status changes" setting.</summary>
    public static void Status(string text)
    {
        if (!AnnounceStatus) return;
        Say(text, false);
    }

    /// <summary>Stop Voice Guide speech. TalkBack is stopped by the user with its own gesture.</summary>
    public static void Stop() { if (OffMainThread(Stop)) return; NativeTTS.Stop(); }

    /// <summary>Say the last thing again.</summary>
    public static void Repeat() { if (!string.IsNullOrEmpty(lastSpoken)) Say(lastSpoken); }

    /// <summary>Removes rich-text tags so they are not read out ("&lt;b&gt;").</summary>
    public static string Clean(string s)
    {
        if (string.IsNullOrEmpty(s) || s.IndexOf('<') < 0) return s.Trim();
        var sb = new System.Text.StringBuilder(s.Length);
        bool inTag = false;
        foreach (char c in s)
        {
            if (c == '<') { inTag = true; continue; }
            if (c == '>') { inTag = false; continue; }
            if (!inTag) sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    // =====================================================================
    // Android TextToSpeech, called directly through JNI. No .jar needed.
    // =====================================================================
    static class NativeTTS
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject tts;
        static volatile bool ready;
        static string pendingLanguage = "en-US";
        static float pendingRate = 1f;
        static string queuedText;
        static IntPtr speakMethod = IntPtr.Zero;
        static int utterance;

        class InitListener : AndroidJavaProxy
        {
            public InitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
            // Called on the Android UI thread.
            void onInit(int status)
            {
                ready = status == 0; // TextToSpeech.SUCCESS
                if (!ready) Debug.LogWarning("IIADSpeech: Android TextToSpeech failed to start, status " + status);
            }
        }

        public static void Init(string lang, float rate)
        {
            pendingLanguage = lang; pendingRate = rate;
            if (tts != null) return;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener());
                }
            }
            catch (Exception e) { Debug.LogWarning("IIADSpeech: could not create TextToSpeech: " + e.Message); }
        }

        static bool EnsureReady()
        {
            if (tts == null) Init(pendingLanguage, pendingRate);
            if (tts == null || !ready) return false;
            if (speakMethod == IntPtr.Zero)
            {
                ApplyLanguage(); ApplyRate();
                speakMethod = AndroidJNIHelper.GetMethodID(tts.GetRawClass(), "speak",
                    "(Ljava/lang/CharSequence;ILandroid/os/Bundle;Ljava/lang/String;)I");
            }
            return speakMethod != IntPtr.Zero;
        }

        public static void Speak(string text, bool interrupt)
        {
            if (!EnsureReady()) { queuedText = text; IIADSpeechPump.Ensure(); return; }
            IntPtr jText = AndroidJNI.NewStringUTF(text);
            IntPtr jId = AndroidJNI.NewStringUTF("iiad" + (++utterance));
            try
            {
                var args = new jvalue[4];
                args[0].l = jText;
                args[1].i = interrupt ? 0 : 1;      // QUEUE_FLUSH : QUEUE_ADD
                args[2].l = IntPtr.Zero;           // no params bundle
                args[3].l = jId;
                AndroidJNI.CallIntMethod(tts.GetRawObject(), speakMethod, args);
            }
            catch (Exception e) { Debug.LogWarning("IIADSpeech: speak failed: " + e.Message); }
            finally { AndroidJNI.DeleteLocalRef(jText); AndroidJNI.DeleteLocalRef(jId); }
        }

        /// <summary>Speaks text that was requested before the engine finished starting.</summary>
        public static void FlushQueued()
        {
            if (queuedText != null && EnsureReady()) { var t = queuedText; queuedText = null; Speak(t, true); }
        }
        public static bool HasQueued => queuedText != null;

        public static void Stop() { try { if (tts != null && ready) tts.Call<int>("stop"); } catch { } queuedText = null; }

        public static bool IsSpeaking { get { try { return tts != null && ready && tts.Call<bool>("isSpeaking"); } catch { return false; } } }

        public static void SetRate(float r) { pendingRate = r; if (tts != null && ready) ApplyRate(); }
        public static void SetLanguage(string tag) { pendingLanguage = tag; if (tts != null && ready) ApplyLanguage(); }

        static void ApplyRate() { try { tts.Call<int>("setSpeechRate", pendingRate); } catch { } }
        static void ApplyLanguage()
        {
            try
            {
                using (var locale = new AndroidJavaClass("java.util.Locale"))
                using (var l = locale.CallStatic<AndroidJavaObject>("forLanguageTag", pendingLanguage))
                    tts.Call<int>("setLanguage", l);
            }
            catch { }
        }

        public static void Shutdown()
        {
            try { if (tts != null) { tts.Call("stop"); tts.Call("shutdown"); tts.Dispose(); } } catch { }
            tts = null; ready = false; speakMethod = IntPtr.Zero;
        }
#else
        public static void Init(string lang, float rate) { }
        public static void Speak(string text, bool interrupt) { Debug.Log("[IIAD Voice Guide] " + text); }
        public static void FlushQueued() { }
        public static bool HasQueued => false;
        public static void Stop() { }
        public static bool IsSpeaking => false;
        public static void SetRate(float r) { }
        public static void SetLanguage(string tag) { }
        public static void Shutdown() { }
#endif
    }

    /// <summary>Internal: lets the queued first sentence play once the engine is ready.</summary>
    internal static void PumpQueue() => NativeTTS.FlushQueued();
    internal static bool HasQueuedSpeech => NativeTTS.HasQueued;
}

/// <summary>Tiny helper that waits for Android TTS to finish starting, then speaks the queued text.</summary>
internal class IIADSpeechPump : MonoBehaviour
{
    static IIADSpeechPump instance;
    float giveUpAt;

    public static void Ensure()
    {
        if (instance != null) { instance.giveUpAt = Time.unscaledTime + 5f; return; }
        var go = new GameObject("IIAD Speech Pump") { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(go);
        instance = go.AddComponent<IIADSpeechPump>();
        instance.giveUpAt = Time.unscaledTime + 5f;
    }

    void Update()
    {
        IIADSpeech.PumpQueue();
        if (!IIADSpeech.HasQueuedSpeech || Time.unscaledTime > giveUpAt) { instance = null; Destroy(gameObject); }
    }
}
