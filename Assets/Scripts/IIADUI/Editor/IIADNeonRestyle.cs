using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > IIAD > Build Neon UI (all screens)
///
/// Rebuilds every screen of MainUI to match the IIAD Neon preview, using an explicit recipe
/// per screen (not guesswork):
///   Header   190 high: back on the left, round icon buttons on the right
///   Content  one scrolling column, 48 side margin, dark cards with a cyan edge
///   Footer   210 high: full-width Previous / Next (Next is yellow)
/// Buttons: yellow = main action, cyan outline = secondary, coral = exit / delete,
/// round 44 dp icon buttons, 52 dp minimum height, Lexend font.
///
/// Your existing objects are re-used and moved, never re-created, so every script reference and
/// OnClick keeps working. One Undo step (Ctrl+Z). Run on the original scene (git checkout first if
/// you saved an earlier restyle).
/// </summary>
public static partial class IIADNeonRestyle
{
    // ---------------------------------------------------------------- palette
    static readonly Color Navy = Hex("#0B1630");
    static readonly Color Cyan = Hex("#3CC8FF");
    static readonly Color FieldEdge = Hex("#7DB7E8");
    static readonly Color Yellow = Hex("#FFC233");
    static readonly Color Coral = Hex("#FF6B5B");
    static readonly Color White = Color.white;
    static readonly Color Muted = Hex("#A9BCD9");

    // ---------------------------------------------------------------- sizes (1080 x 1920 reference, ~3 units = 1 dp)
    const float HeaderH = 190f, FooterH = 210f, Side = 48f, Gap = 30f;
    const float BtnH = 156f, IconBtn = 132f, FieldH = 132f, TitleH = 110f, MenuH = 176f;
    const string SpriteFolder = "Assets/Arts/NEW UI/IIAD Neon";
    const string ContentName = "IIAD_Content";

    static Sprite sBtnFill, sBtnOutline, sCard, sField, sCircle;
    static TMP_FontAsset font;
    static StringBuilder log;
    static Dictionary<string, RectTransform> map;      // original paths of the screen being built
    static RectTransform screenRoot;
    static HashSet<Transform> used;
    static HashSet<GameObject> referenced;              // objects your scripts point to

    enum K { Primary, Secondary, Danger, Menu, MenuDanger, Icon, IconDanger, Link, Eye, Stacked }
    enum Tone { Title, Body, Muted, Accent, Error, Score }

    // =================================================================== MENU
    [MenuItem("Tools/IIAD/Build Neon UI (all screens)", priority = 40)]
    static void Run()
    {
        var container = FindInScene("ScreenContainer");
        if (container == null) { EditorUtility.DisplayDialog("Build Neon UI", "Open Assets/Scenes/UI/MainUI.unity first.", "OK"); return; }
        if (!EditorUtility.DisplayDialog("Build Neon UI (all screens)",
            "Rebuilds every screen, popup and list card to the IIAD Neon design. Pages fit the screen with no scrolling. Works on the original scene or on an earlier restyle.\n\nOne Undo step (Ctrl+Z). Commit to Git first.", "Build", "Cancel")) return;

        log = new StringBuilder();
        EnsureSprites();
        font = FindFont();
        referenced = CollectReferenced(container.gameObject.scene);

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("IIAD Neon UI");
        Undo.RegisterFullObjectHierarchyUndo(container.gameObject, "IIAD Neon UI");

        BuildAllScreens(container);
        StylePopups(container.Find("PopUps"));
        FinalStylePass(container);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(container.gameObject.scene);

        StylePrefab("Assets/Data/Prefabs/ParticipantRankCard.prefab");
        StylePrefab("Assets/Data/Prefabs/SubmittedCard.prefab");
        IIADNeonLogo.ApplyToOpenScenes(true);
        IIADNeonIcons.ApplyToOpenScenes();
        IIADChallengePanelFix.Apply();   // join-event screen uses its own fixed layout

        Canvas.ForceUpdateCanvases();
        Debug.Log("IIAD Neon UI built.\n" + log);
        EditorUtility.DisplayDialog("Neon UI built",
            "Done. Press Play and go through the whole loop. The Console lists anything that needs a look.\n\nCtrl+Z undoes everything. Save the scene to keep it.", "OK");
    }

    // =================================================================== RECIPES
    const string WS = "DesignWorkspace/MainSystemPanel/Background/";

    static void BuildAllScreens(Transform c)
    {
        // ---------- Splash: centre logo only
        Screen(c, "SplashScreen", null, null, new N[] { Gapper(260), H("Logo", 560, 560), T("LoadingIndicator", 44, Tone.Muted, TextAlignmentOptions.Center, 80) }, null, header: false);

        // ---------- Welcome
        Screen(c, "WelcomeScreen", new string[0], new[] { "AccessibilityButton", "ExitAppButton" }, new N[] {
            Gapper(40),
            H("Logo", 440, 440),
            T("WelcomeText", 52, Tone.Title, TextAlignmentOptions.Center, 80),
            H("LogoTitle", 640, 210),
            Gapper(40),
            B("LogInButton", K.Primary),
            B("CreateNewAccountButton", K.Secondary),
        }, null);

        // ---------- Login
        Screen(c, "LoginScreen", new[] { "BackButton" }, new[] { "AccessibilityButton" }, new N[] {
            H("Logo", 260, 260),
            Card("LoginPanel",
                T("LoginPanel/LoginText", 64, Tone.Title, TextAlignmentOptions.Center, 90),
                T("LoginPanel/WelcomeText", 40, Tone.Muted, TextAlignmentOptions.Center, 56),
                F("LoginPanel/Username"),
                F("LoginPanel/Password"),
                B("LoginPanel/ForgotPassword?", K.Link, 96),
                T("LoginPanel/MessageText", 38, Tone.Error, TextAlignmentOptions.Center, 50),
                B("LoginPanel/LoginButton", K.Primary),
                T("LoginPanel/Or", 36, Tone.Muted, TextAlignmentOptions.Center, 50),
                B("LoginPanel/CreateAccount", K.Secondary)),
        }, null);

        // ---------- Register
        Screen(c, "RegisterScreen", new[] { "BackButton" }, new[] { "AccessibilityButton" }, new N[] {
            H("Logo", 200, 200),
            Card("RegisterPanel",
                T("RegisterPanel/RegisterText", 64, Tone.Title, TextAlignmentOptions.Center, 90),
                T("RegisterPanel/CreateText", 40, Tone.Muted, TextAlignmentOptions.Center, 56),
                F("RegisterPanel/Email"),
                F("RegisterPanel/Username"),
                F("RegisterPanel/Password"),
                F("RegisterPanel/ConfirmPassword"),
                T("RegisterPanel/MessageText", 38, Tone.Error, TextAlignmentOptions.Center, 50),
                B("RegisterPanel/CreateAccount", K.Primary)),
        }, null);

        // ---------- Main dashboard
        Screen(c, "MainDashboard", new string[0], new[] { "Header/AccessibilityButton", "Header/SettingsButton", "Header/ProfileButton", "Header/LogOutButton" }, new N[] {
            H("Logo/LogoImage", 260, 260),
            H("Logo/LogoTitle", 760, 116),
            T("WelcomeSection/UsernameWelcomeText", 60, Tone.Title, TextAlignmentOptions.Center, 90),
            Menu("GroupButtons",
                B("GroupButtons/PracticeModeButton", K.Menu, MenuH),
                B("GroupButtons/ChallengeModeButton", K.Menu, MenuH),
                B("GroupButtons/SubmittedButton", K.Menu, MenuH),
                B("GroupButtons/ExitButton", K.MenuDanger, MenuH)),
        }, null);

        // ---------- Challenge (join event)
        Screen(c, "ChallengePanel", new[] { "BackButton" }, new string[0], new N[] {
            Card("ChallengeBackground",
                T("ChallengeBackground/ChallengeText", 64, Tone.Title, TextAlignmentOptions.Center, 90),
                F("ChallengeBackground/SelectEvent"),
                F("ChallengeBackground/EnterCode"),
                T("ChallengeBackground/MessageText", 38, Tone.Error, TextAlignmentOptions.Center, 50),
                B("ChallengeBackground/JoinEventButton", K.Primary)),
        }, null);

        // ---------- Participant details
        Screen(c, "ParticipantDetailsPanel", new[] { "BackButton" }, new string[0], new N[] {
            Card("ParticipantDetailsBg",
                Title("ParticipantDetailsBg/TitleBackground"),
                H("ParticipantDetailsBg/ProfileIcon", 140, 140),
                F("ParticipantDetailsBg/NameBox"),
                F("ParticipantDetailsBg/CategoryBox"),
                F("ParticipantDetailsBg/SubCategoryBox"),
                F("ParticipantDetailsBg/InstititionSchoolBox"),
                T("ParticipantDetailsBg/MessageText", 38, Tone.Error, TextAlignmentOptions.Center, 50),
                B("ParticipantDetailsBg/SaveParticipantButton", K.Primary)),
        }, null);

        // ---------- Settings
        Screen(c, "SettingsPanel", new string[0], new string[0], new N[] {
            Title("Background/SettingTitle"),
            Menu("Background/GroupButton",
                B("Background/GroupButton/HowToPlay", K.Menu, MenuH),
                B("Background/GroupButton/AboutButton", K.Menu, MenuH),
                B("Background/GroupButton/PrivacyButton", K.Menu, MenuH),
                B("Background/GroupButton/DeleteAccount", K.MenuDanger, MenuH)),
            Hide("Background"),
        }, new[] { "FooterNavigation/BackButton" });

        // ---------- How to play
        Screen(c, "HowToPlayPanel", new string[0], new[] { "Header/HomeButton" }, new N[] {
            Card("Background",
                Title("Background/HowToPlayTitle"),
                Pages(1080, "Background/Page 1", "Background/Page 2", "Background/Page 3", "Background/Page 4"),
                T("Background/page", 40, Tone.Muted, TextAlignmentOptions.Center, 60)),
        }, new[] { "FooterNavigation/PreviousButton", "FooterNavigation/NextButton" });

        // ---------- Submitted designs
        Screen(c, "SubmittedPanel", new string[0], new string[0], new N[] {
            Title("bg/titlle"),
            T("bg/StatusText", 38, Tone.Muted, TextAlignmentOptions.Center, 50),
            H("bg/Scroll View", 900, 1150),
            Row(BtnH, B("bg/BackButton", K.Secondary), B("bg/LeaderboardButton", K.Primary)),
            Hide("bg"),
        }, null, header: false);

        // ---------- Leaderboard
        Screen(c, "LeaderboardPanel", new string[0], new string[0], new N[] {
            Title("TitleBackground"),
            T("ChallengeTitle", 46, Tone.Accent, TextAlignmentOptions.Center, 66),
            T("StatusText", 38, Tone.Muted, TextAlignmentOptions.Center, 50),
            H("Panel Backgrounf", 1080, 1250, keepAspect: false),
        }, new[] { "FooterNavigation/BackButton" }, header: false);

        // ---------- Design workspace: shared header
        string[] wsLeft = { "Header/GooleeLogo", "Header/AppsLogo" };
        string[] wsRight = { "Header/AccessibilityButton", "Header/HomeButton" };
        string[] wsFooter = { "FooterNavigation/PreviousButton", "FooterNavigation/NextButton" };

        // Step 1: Idea
        Screen(c, WS + "IdeaInputPanel", wsLeft, wsRight, new N[] {
            Title("TitleBackground"),
            Card("TipsBox",
                Hide("TipsBox/Icon"),
                T("TipsBox/TipsLabel", 44, Tone.Title, TextAlignmentOptions.MidlineLeft, 60),
                T("TipsBox/Detail", 38, Tone.Muted, TextAlignmentOptions.TopLeft, 90)),
            Box("IdeaInputArea",
                T("IdeaInputArea/YourIdeaLabel", 40, Tone.Title, TextAlignmentOptions.MidlineLeft, 56),
                In("IdeaInputArea/IdeaInput", 420),
                B("IdeaInputArea/OpenSampleIdea(PracticeOnly)", K.Secondary, 120)),
            Row(BtnH, B("MicButton", K.Secondary), B("RepeatButton", K.Secondary)),
            T("MessageText", 38, Tone.Error, TextAlignmentOptions.Center, 50),
            B("GeneratePosterButton", K.Primary, 170),
        }, wsFooter);

        // Step 2a: Output
        Screen(c, WS + "OutputPanel", wsLeft, wsRight, new N[] {
            Title("TitleBackground"),
            Card("OutputArea",
                H("OutputArea/OriginalPosterImage", 820, 1000),
                B("OutputArea/FullScreenPoster", K.Secondary)),
        }, wsFooter);

        // Step 2b: Description
        Screen(c, WS + "DescriptionPanel", wsLeft, wsRight, new N[] {
            Title("TitleBackground"),
            Card("OriginalPosterBox",
                T("OriginalPosterBox/GeneratedPosterLabel", 40, Tone.Title, TextAlignmentOptions.MidlineLeft, 56),
                H("OriginalPosterBox/LatesPosterDesc", 820, 640),
                B("OriginalPosterBox/FullScreenPoster", K.Secondary)),
            Card("DescriptionBox",
                T("DescriptionBox/PosterDescriptionLabel", 40, Tone.Title, TextAlignmentOptions.MidlineLeft, 56),
                R("DescriptionBox/Scroll View", 620)),
            T("MessageText", 38, Tone.Error, TextAlignmentOptions.Center, 50),
            B("ReplayDescriptionVoiceButton", K.Secondary),
        }, wsFooter);

        // Step 2c: Revision
        Screen(c, WS + "RevisionPanel", wsLeft, wsRight, new N[] {
            Title("TitleBackground"),
            Card("Background/LatestPosterArea",
                Title("Background/LatestPosterArea/Label", 44),
                H("Background/LatestPosterArea/LatestPoster", 820, 640),
                B("Background/LatestPosterArea/FullScreenPoster", K.Secondary)),
            Card("Background/AttemptBox",
                T("Background/AttemptBox/Label", 40, Tone.Title, TextAlignmentOptions.MidlineLeft, 56),
                Box("Background/AttemptBox/AttempPanel",
                    Hide("Background/AttemptBox/AttempPanel/Icon"),
                    T("Background/AttemptBox/AttempPanel/AttemptRemainingText", 72, Tone.Score, TextAlignmentOptions.MidlineLeft, 96),
                    T("Background/AttemptBox/AttempPanel/Details", 36, Tone.Muted, TextAlignmentOptions.MidlineLeft, 50))),
            Card("Background/ReviseBox",
                Title("Background/ReviseBox/LabelBg", 44),
                In("Background/ReviseBox/ReviseInputField", 340),
                Row(BtnH, B("Background/ReviseBox/MicButton", K.Secondary), B("Background/ReviseBox/RepeatButton", K.Secondary))),
            B("ReviseDesignButton", K.Primary, 170),
            Hide("Background"),
        }, wsFooter);

        // Step 3: Explain + submit
        Screen(c, WS + "SubmissionPanel", wsLeft, wsRight, new N[] {
            Title("TitleBackground"),
            Card("Background/LatestPosterArea",
                Title("Background/LatestPosterArea/Label", 44),
                H("Background/LatestPosterArea/LatestPoster", 820, 560),
                B("Background/LatestPosterArea/FullScreenPoster", K.Secondary)),
            Card("Background/ExplainBox",
                Title("Background/ExplainBox/LabelBg", 44),
                In("Background/ExplainBox/SubmissionInput", 340),
                Row(BtnH, B("Background/ExplainBox/MicButton", K.Secondary), B("Background/ExplainBox/RepeatButton", K.Secondary))),
            Card("Background/TipsBox/Background",
                T("Background/TipsBox/Background/Label", 44, Tone.Title, TextAlignmentOptions.MidlineLeft, 60),
                T("Background/TipsBox/Background/Detail", 38, Tone.Muted, TextAlignmentOptions.TopLeft, 160),
                Hide("Background/TipsBox/Background/\u2022"),
                Hide("Background/TipsBox/Background/\u2022 (1)")),
            T("MessageText", 38, Tone.Error, TextAlignmentOptions.Center, 50),
            B("SubmitPosterButton", K.Primary, 170),
            Hide("Background"),
        }, wsFooter);

        // Step 4a: Score
        Screen(c, WS + "ScorePanel", wsLeft, wsRight, new N[] {
            Title("TitleBackground"),
            ScoreTop("TotalScorePanel"),
            Card("ScorebreakdownPanel",
                ScoreRow("ScorebreakdownPanel/PromptQualityRow"),
                ScoreRow("ScorebreakdownPanel/PosterMessageRow"),
                ScoreRow("ScorebreakdownPanel/DesignQualityRow"),
                ScoreRow("ScorebreakdownPanel/AccessibilityRow"),
                ScoreRow("ScorebreakdownPanel/FinalDesignRow")),
        }, wsFooter);

        // Step 4b: Feedback
        Screen(c, WS + "FeedbackPanel", wsLeft, wsRight, new N[] {
            Title("TitleBackground"),
            FeedbackCard("FeedbackBox", "ReadFeedbackButton"),
            FeedbackCard("ImprovementBox", "ReadImprovementButton"),
        }, wsFooter);

        // Full-screen posters
        Screen(c, WS + "OriginalFullPosterPanel", null, null, new N[] { H("OriPosterImageFuul", 984, 1450), B("BackButton", K.Secondary) }, null, header: false);
        Screen(c, WS + "LatestFullPosterPanel", null, null, new N[] { H("LatestPOsterFUUL", 984, 1450), B("BackButton", K.Secondary) }, null, header: false);

        // Sample prompts (practice only)
        Screen(c, "SamplePromptPopUp", null, null, new N[] {
            Title("Title"),
            Menu("Layout",
                B("Layout/Sample 1 Button", K.Menu, 300),
                B("Layout/Sample 2 Button", K.Menu, 300),
                B("Layout/Sample 3 Button", K.Menu, 300),
                B("Layout/Back", K.Secondary, BtnH)),
        }, null, header: false);
    }

    // =================================================================== SCREEN BUILDER
    static void Screen(Transform container, string path, string[] left, string[] right, N[] content, string[] footer, bool header = true)
    {
        var s = container.Find(path) as RectTransform;
        if (s == null) { log.AppendLine("! screen not found: " + path); return; }
        screenRoot = s;
        Unwrap(s);
        used = new HashSet<Transform>();
        map = new Dictionary<string, RectTransform>();
        foreach (var t in s.GetComponentsInChildren<RectTransform>(true))
            if (t != s) { string p = RelPath(s, t); if (!map.ContainsKey(p)) map[p] = t; }

        try
        {
            // ---- header
            RectTransform headerRT = null;
            bool hasHeader = header && ((left != null && left.Length > 0) || (right != null && right.Length > 0) || s.Find("Header") != null);
            if (hasHeader)
            {
                headerRT = s.Find("Header") as RectTransform ?? NewRect("Header", s);
                used.Add(headerRT);
                BuildHeader(headerRT, left ?? new string[0], right ?? new string[0]);
            }

            // ---- footer
            RectTransform footerRT = null;
            if (footer != null)
            {
                footerRT = s.Find("FooterNavigation") as RectTransform;
                if (footerRT != null) { used.Add(footerRT); BuildFooter(footerRT, footer); }
            }

            // ---- content
            var column = BuildScroll(s, headerRT != null ? HeaderH : 70f, footerRT != null ? FooterH : 0f);
            foreach (var n in content) n.Build(column);

            // ---- leftovers directly under the screen
            foreach (var child in Children(s))
            {
                if (child == headerRT || child == footerRT || child.name == ContentName || used.Contains(child)) continue;
                Leftover(child);
            }
            log.AppendLine("+ " + path);
        }
        catch (System.Exception e) { log.AppendLine("! " + path + ": " + e.Message); Debug.LogException(e); }
    }

    /// <summary>Page area between header and footer. No scrolling: the content shrinks evenly to fit.</summary>
    static RectTransform BuildScroll(RectTransform screen, float top, float bottom)
    {
        var area = NewRect(ContentName, screen);
        var hdr = screen.Find("Header");
        area.SetSiblingIndex(hdr != null ? hdr.GetSiblingIndex() + 1 : 0);
        Stretch(area); area.offsetMin = new Vector2(0, bottom); area.offsetMax = new Vector2(0, -top);

        var content = NewRect("Content", area);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var vl = Undo.AddComponent<VerticalLayoutGroup>(content.gameObject);
        vl.padding = new RectOffset((int)Side, (int)Side, 12, 24);
        Column(vl, Gap);
        var fit = Undo.AddComponent<ContentSizeFitter>(content.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var f = Undo.AddComponent<IIADFitToViewport>(content.gameObject);
        f.viewport = area; f.minScale = 0.55f; f.centerVertically = true;
        return content;
    }

    /// <summary>
    /// Undoes the containers made by earlier restyles (scroll views, rows, holders), putting every
    /// original object back directly under the screen so the recipe can place it again.
    /// </summary>
    static void Unwrap(RectTransform screen)
    {
        // a) page containers: IIAD_Content (+ Viewport) -> move their content up
        foreach (var page in Children(screen).Where(c => c.name == ContentName || c.name == "IIAD_Page").ToList())
        {
            var inner = page.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Content" && t != page);
            var source = inner != null ? inner : page;
            foreach (var c in Children(source).ToList()) { Undo.SetTransformParent(c, screen, "IIAD"); c.localScale = Vector3.one; }
            Undo.DestroyObjectImmediate(page.gameObject);
        }
        // b) helper wrappers anywhere in the screen: move children up, delete wrapper
        bool again = true;
        while (again)
        {
            again = false;
            foreach (var w in screen.GetComponentsInChildren<RectTransform>(true))
            {
                if (w == screen || !IsWrapper(w)) continue;
                var parent = w.parent; int idx = w.GetSiblingIndex();
                foreach (var c in Children(w).ToList()) { Undo.SetTransformParent(c, parent, "IIAD"); c.SetSiblingIndex(idx++); c.localScale = Vector3.one; }
                Undo.DestroyObjectImmediate(w.gameObject);
                again = true; break;
            }
        }
        // c) leftover header spacer from the first tool
        var hdr = screen.Find("Header");
        if (hdr != null) foreach (var sp in Children(hdr).Where(c => c.name == "Spacer").ToList()) Undo.DestroyObjectImmediate(sp.gameObject);
    }

    static bool IsWrapper(RectTransform t)
    {
        string n = t.name;
        if (n.StartsWith("IIAD_Holder") || n == "IIAD_Row" || n == "IIAD_Pages" || n == "IIAD_Gap") return true;
        // the first tool made plain "Row" objects with only a HorizontalLayoutGroup
        if (n == "Row" && t.GetComponent<HorizontalLayoutGroup>() != null && t.GetComponents<Component>().Length <= 3) return true;
        return false;
    }

    static void BuildHeader(RectTransform h, string[] left, string[] right)
    {
        h.anchorMin = new Vector2(0, 1); h.anchorMax = new Vector2(1, 1); h.pivot = new Vector2(0.5f, 1);
        h.anchoredPosition = Vector2.zero; h.sizeDelta = new Vector2(0, HeaderH);
        foreach (var lg in h.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(lg);
        var hl = Undo.AddComponent<HorizontalLayoutGroup>(h.gameObject);
        hl.padding = new RectOffset(36, 36, 44, 14); hl.spacing = 22; hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;

        var order = new List<RectTransform>();
        foreach (var p in left) { var t = Get(p); if (t != null) order.Add(t); }
        var spacer = NewRect("Spacer", h); Pref(spacer, 0, 1, flexW: 1);
        order.Add(spacer);
        foreach (var p in right) { var t = Get(p); if (t != null) order.Add(t); }

        foreach (var t in order)
        {
            if (t == spacer) continue;
            Undo.SetTransformParent(t, h, "IIAD"); used.Add(t);
            string n = t.name.ToLowerInvariant();
            var btn = t.GetComponent<Button>();
            if (btn != null)
            {
                bool danger = n.Contains("logout") || n.Contains("exit");
                StyleButton(btn, danger ? K.IconDanger : K.Icon);
                Pref(t, IconBtn, IconBtn);
            }
            else
            {
                var img = t.GetComponent<Image>(); if (img != null) img.preserveAspect = true;
                float hh = n.Contains("appslogo") ? 108 : 92;
                var sz = t.rect.size; float w = sz.y > 0 ? sz.x * hh / sz.y : hh;
                Pref(t, w, hh);
            }
        }
        for (int i = 0; i < order.Count; i++) order[i].SetSiblingIndex(i);
        // anything else that lived in the header stays but is hidden from layout
        foreach (var c in Children(h)) if (!order.Contains(c)) Leftover(c);
    }

    static void BuildFooter(RectTransform f, string[] buttons)
    {
        f.anchorMin = new Vector2(0, 0); f.anchorMax = new Vector2(1, 0); f.pivot = new Vector2(0.5f, 0);
        f.anchoredPosition = Vector2.zero; f.sizeDelta = new Vector2(0, FooterH);
        foreach (var lg in f.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(lg);
        var hl = Undo.AddComponent<HorizontalLayoutGroup>(f.gameObject);
        hl.padding = new RectOffset((int)Side, (int)Side, 27, 27); hl.spacing = Gap; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
        int i = 0;
        foreach (var p in buttons)
        {
            var t = Get(p); if (t == null) continue;
            used.Add(t); t.SetSiblingIndex(i++);
            var b = t.GetComponent<Button>();
            if (b != null) StyleButton(b, t.name.ToLowerInvariant().Contains("next") ? K.Primary : K.Secondary);
            Pref(t, 0, BtnH, flexW: 1);
        }
    }

    // =================================================================== NODES
    abstract class N { public abstract void Build(RectTransform parent); }

    static RectTransform Get(string path)
    {
        if (map.TryGetValue(path, out var t) && t != null) return t;
        var found = FindByName(path);
        if (found != null) return found;
        log.AppendLine("  ? " + screenRoot.name + ": '" + path + "' not found (skipped)");
        return null;
    }

    /// <summary>Finds an object by its own name when an earlier restyle moved it; prefers the longest matching path ending.</summary>
    static RectTransform FindByName(string path)
    {
        var parts = path.Split('/');
        string last = parts[parts.Length - 1];
        RectTransform best = null; int bestScore = -1;
        foreach (var kv in map)
        {
            if (kv.Value == null || kv.Value.name != last) continue;
            var kp = kv.Key.Split('/');
            int score = 0;
            for (int i = 1; i <= Mathf.Min(parts.Length, kp.Length); i++) { if (parts[parts.Length - i] == kp[kp.Length - i]) score++; else break; }
            // also count matching parent names anywhere in the path (object moved into a new container)
            score = score * 10 + parts.Count(pn => kp.Contains(pn));
            if (score > bestScore) { bestScore = score; best = kv.Value; }
        }
        return best;
    }

    static RectTransform Place(string path, RectTransform parent)
    {
        var t = Get(path); if (t == null) return null;
        used.Add(t);
        if (t.parent != parent) Undo.SetTransformParent(t, parent, "IIAD");
        t.SetAsLastSibling();
        t.localScale = Vector3.one; t.localRotation = Quaternion.identity;
        return t;
    }

    // ---- text
    class TN : N
    {
        public string p; public float size, h; public Tone tone; public TextAlignmentOptions align;
        public override void Build(RectTransform parent)
        {
            var t = Place(p, parent); if (t == null) return;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                StyleText(tmp, size, ToneColor(tone), tone == Tone.Title || tone == Tone.Score || tone == Tone.Accent ? FontStyles.Bold : FontStyles.Normal);
                tmp.alignment = align; tmp.enableAutoSizing = false; tmp.overflowMode = TextOverflowModes.Overflow;
            }
            Pref(t, 0, h, flexW: 1, minOnly: true);
        }
    }
    static N T(string p, float size, Tone tone, TextAlignmentOptions align, float h) => new TN { p = p, size = size, tone = tone, align = align, h = h };

    // ---- title (image badge with text inside, or plain text)
    class TitleN : N
    {
        public string p; public float size;
        public override void Build(RectTransform parent)
        {
            var t = Place(p, parent); if (t == null) return;
            var img = t.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = false; }
            foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleText(tmp, size, White, FontStyles.Bold);
                tmp.alignment = size >= 60 ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
                tmp.enableAutoSizing = true; tmp.fontSizeMax = size; tmp.fontSizeMin = 36;
                if (tmp.transform != t) { var r = (RectTransform)tmp.transform; Stretch(r); }
            }
            Pref(t, 0, size >= 60 ? TitleH : 64, flexW: 1);
        }
    }
    static N Title(string p, float size = 64) => new TitleN { p = p, size = size };

    // ---- button
    class BN : N
    {
        public string p; public K k; public float h;
        public override void Build(RectTransform parent)
        {
            var t = Place(p, parent); if (t == null) return;
            var b = t.GetComponent<Button>(); if (b != null) StyleButton(b, k);
            Pref(t, 0, h, flexW: 1);
        }
    }
    static BN B(string p, K k, float h = BtnH) => new BN { p = p, k = k, h = h };

    // ---- field group (label + input/dropdown)
    class FN : N
    {
        public string p;
        public override void Build(RectTransform parent)
        {
            var g = Place(p, parent); if (g == null) return;
            Component field = g.GetComponentInChildren<TMP_InputField>(true);
            if (field == null) field = g.GetComponentInChildren<TMP_Dropdown>(true);
            if (field == null) { Pref(g, 0, FieldH, flexW: 1); return; }
            var fRT = (RectTransform)field.transform;

            var label = g.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(x => x.GetComponentInParent<TMP_InputField>(true) == null && x.GetComponentInParent<TMP_Dropdown>(true) == null);
            foreach (var c in Children(g).ToList())
            {
                if (c == fRT || (label != null && c == label.transform)) continue;
                if (c.name.ToLowerInvariant().Contains("eye"))
                {   // eye button sits inside the password box, right side
                    Undo.SetTransformParent(c, fRT, "IIAD");
                    c.anchorMin = c.anchorMax = new Vector2(1, 0.5f); c.pivot = new Vector2(1, 0.5f);
                    c.anchoredPosition = new Vector2(-14, 0); c.sizeDelta = new Vector2(104, 104);
                    var le = c.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(c.gameObject); le.ignoreLayout = true;
                    var img = c.GetComponent<Image>(); if (img != null) img.preserveAspect = true;
                    if (field is TMP_InputField inf && inf.textViewport != null) { Undo.RecordObject(inf.textViewport, "IIAD"); inf.textViewport.offsetMax = new Vector2(-126, inf.textViewport.offsetMax.y); }
                    continue;
                }
                Leftover(c);
            }
            if (label != null)
            {
                label.transform.SetAsFirstSibling();
                StyleText(label, 40, White, FontStyles.Bold); label.alignment = TextAlignmentOptions.MidlineLeft;
                label.text = label.text.Replace(":", "").Trim();
                Pref((RectTransform)label.transform, 0, 56, flexW: 1);
            }
            fRT.SetSiblingIndex(label != null ? 1 : 0);
            Pref(fRT, 0, FieldH, flexW: 1);
            foreach (var lg in g.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(lg);
            var vl = Undo.AddComponent<VerticalLayoutGroup>(g.gameObject); Column(vl, 12);
            var gi = g.GetComponent<Image>(); if (gi != null) { Undo.RecordObject(gi, "IIAD"); gi.enabled = false; }
            Pref(g, 0, -1, flexW: 1);
        }
    }
    static N F(string p) => new FN { p = p };

    // ---- big text input
    class InN : N
    {
        public string p; public float h;
        public override void Build(RectTransform parent) { var t = Place(p, parent); if (t != null) Pref(t, 0, h, flexW: 1); }
    }
    static N In(string p, float h) => new InN { p = p, h = h };

    // ---- raw element at full width (scroll views)
    static N R(string p, float h) => new InN { p = p, h = h };

    // ---- holder: element kept at its own aspect, centred, inside a full-width slot
    class HN : N
    {
        public string p; public float w, h; public bool keepAspect = true;
        public override void Build(RectTransform parent)
        {
            var src = Get(p); if (src == null) return;
            var size = src.rect.size;                       // measure before moving
            var holder = NewRect("IIAD_Holder_" + src.name, parent);
            Pref(holder, 0, h, flexW: 1);
            var t = Place(p, holder);
            Vector2 fit = new Vector2(w, h);
            if (keepAspect && size.x > 1 && size.y > 1)
            {
                float k = Mathf.Min(w / size.x, h / size.y);
                fit = size * k;
            }
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
            t.anchoredPosition = Vector2.zero; t.sizeDelta = fit;
            var le = t.GetComponent<LayoutElement>(); if (le != null) le.ignoreLayout = true;
            var img = t.GetComponent<Image>(); if (img != null) img.preserveAspect = true;
        }
    }
    static N H(string p, float w, float h, bool keepAspect = true) => new HN { p = p, w = w, h = h, keepAspect = keepAspect };

    // ---- card: existing object becomes a dark rounded card with a cyan edge, children in a column
    class CardN : N
    {
        public string p; public N[] kids; public bool transparent;
        public override void Build(RectTransform parent)
        {
            var t = Place(p, parent); if (t == null) return;
            var img = t.GetComponent<Image>();
            if (transparent) { if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = false; } }
            else
            {
                if (img == null) img = Undo.AddComponent<Image>(t.gameObject);
                Undo.RecordObject(img, "IIAD");
                img.enabled = true; img.sprite = sCard; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.color = Cyan; img.preserveAspect = false;
            }
            foreach (var lg in t.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(lg);
            foreach (var cf in t.GetComponents<ContentSizeFitter>()) Undo.DestroyObjectImmediate(cf);
            var vl = Undo.AddComponent<VerticalLayoutGroup>(t.gameObject);
            vl.padding = transparent ? new RectOffset(0, 0, 0, 0) : new RectOffset(44, 44, 40, 44);
            Column(vl, 24);
            foreach (var k in kids) k.Build(t);
            foreach (var c in Children(t)) if (!used.Contains(c) && !c.name.StartsWith("IIAD_")) Leftover(c);
            Pref(t, 0, -1, flexW: 1);
        }
    }
    static N Card(string p, params N[] kids) => new CardN { p = p, kids = kids };
    static N Box(string p, params N[] kids) => new CardN { p = p, kids = kids, transparent = true };
    static N Menu(string p, params N[] kids) => new CardN { p = p, kids = kids, transparent = true };

    // ---- row of items side by side
    class RowN : N
    {
        public float h; public N[] kids;
        public override void Build(RectTransform parent)
        {
            var r = NewRect("IIAD_Row", parent);
            var hl = Undo.AddComponent<HorizontalLayoutGroup>(r.gameObject);
            hl.spacing = Gap; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            foreach (var k in kids) k.Build(r);
            Pref(r, 0, h, flexW: 1);
        }
    }
    static N Row(float h, params N[] kids) => new RowN { h = h, kids = kids };

    // ---- empty space
    class GapN : N { public float h; public override void Build(RectTransform parent) { var g = NewRect("IIAD_Gap", parent); Pref(g, 0, h, flexW: 1); } }
    static N Gapper(float h) => new GapN { h = h };

    // ---- hide decoration
    class HideN : N
    {
        public string p;
        public override void Build(RectTransform parent)
        {
            if (!map.TryGetValue(p, out var t) || t == null) t = FindByName(p);
            if (t == null) return;
            used.Add(t);
            // never hide something a script uses or anything containing controls / text that is still needed
            if (t.GetComponentsInChildren<Selectable>(true).Any(s => !used.Contains(s.transform)) && !t.GetComponentsInChildren<Selectable>(true).All(s => used.Contains(s.transform)))
            { Leftover(t); return; }
            if (referenced.Contains(t.gameObject) && t.GetComponentsInChildren<Transform>(true).Count() == 1) { Leftover(t); return; }
            Undo.RecordObject(t.gameObject, "IIAD");
            if (t.GetComponentsInChildren<Transform>(true).Any(x => x != t && used.Contains(x)))
            {   // container emptied by the recipe: just hide its own artwork
                var img = t.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = false; }
                var le = t.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(t.gameObject); le.ignoreLayout = true;
                return;
            }
            t.gameObject.SetActive(false);
        }
    }
    static N Hide(string p) => new HideN { p = p };

    // ---- How-to-play pages stacked in one slot (the manager switches them)
    class PagesN : N
    {
        public float h; public string[] pages;
        public override void Build(RectTransform parent)
        {
            var slot = NewRect("IIAD_Pages", parent); Pref(slot, 0, h, flexW: 1);
            foreach (var p in pages)
            {
                var t = Place(p, slot); if (t == null) continue;
                Stretch(t);
                foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                {
                    StyleText(tmp, 40, White, FontStyles.Normal); tmp.alignment = TextAlignmentOptions.TopLeft;
                    var r = (RectTransform)tmp.transform; Stretch(r);
                }
            }
        }
    }
    static N Pages(float h, params string[] pages) => new PagesN { h = h, pages = pages };

    // ---- Score: read button | big circle | view poster button
    class ScoreTopN : N
    {
        public string p;
        public override void Build(RectTransform parent)
        {
            var t = Place(p, parent); if (t == null) return;
            var img = t.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.sprite = sCard; img.type = Image.Type.Sliced; img.color = Cyan; }
            foreach (var lg in t.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(lg);
            var hl = Undo.AddComponent<HorizontalLayoutGroup>(t.gameObject);
            hl.padding = new RectOffset(32, 32, 32, 32); hl.spacing = 24; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;

            var read = Place(p + "/ReadScoreButton", t);
            var circle = Place(p + "/TotalScoreCircle", t);
            var view = Place(p + "/ViewPosterButton", t);
            if (circle != null) circle.SetSiblingIndex(1);
            if (view != null) view.SetAsLastSibling();
            foreach (var b in new[] { read, view })
            {
                if (b == null) continue;
                StyleButton(b.GetComponent<Button>(), K.Stacked);
                Pref(b, 240, 330);
            }
            if (circle != null)
            {
                var ci = circle.GetComponent<Image>();
                if (ci != null) { Undo.RecordObject(ci, "IIAD"); ci.sprite = sCircle; ci.type = Image.Type.Simple; ci.preserveAspect = true; ci.color = Yellow; }
                Pref(circle, 360, 360);
                foreach (var tmp in circle.GetComponentsInChildren<TMP_Text>(true))
                {
                    bool big = tmp.name.Contains("Marks");
                    StyleText(tmp, big ? 120 : 38, big ? White : Muted, big ? FontStyles.Bold : FontStyles.Bold);
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
            Pref(t, 0, 410, flexW: 1);
        }
    }
    static N ScoreTop(string p) => new ScoreTopN { p = p };

    class ScoreRowN : N
    {
        public string p;
        public override void Build(RectTransform parent)
        {
            var r = Place(p, parent); if (r == null) return;
            var img = r.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.sprite = sField; img.type = Image.Type.Sliced; img.color = FieldEdge; }
            foreach (var lg in r.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(lg);
            var hl = Undo.AddComponent<HorizontalLayoutGroup>(r.gameObject);
            hl.padding = new RectOffset(24, 28, 10, 10); hl.spacing = 20; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
            foreach (var c in Children(r))
            {
                string n = c.name.ToLowerInvariant(); used.Add(c);
                var tmp = c.GetComponent<TMP_Text>();
                if (n == "icon") { var i = c.GetComponent<Image>(); if (i != null) i.preserveAspect = true; Pref(c, 80, 80); c.SetAsFirstSibling(); }
                else if (tmp != null && tmp.text.Contains("/")) { StyleText(tmp, 46, Yellow, FontStyles.Bold); tmp.alignment = TextAlignmentOptions.MidlineRight; Pref(c, 210, 90); c.SetAsLastSibling(); }
                else if (tmp != null) { StyleText(tmp, 40, White, FontStyles.Normal); tmp.alignment = TextAlignmentOptions.MidlineLeft; tmp.enableAutoSizing = true; tmp.fontSizeMax = 40; tmp.fontSizeMin = 30; Pref(c, 0, 90, flexW: 1); }
            }
            Pref(r, 0, 120, flexW: 1);
        }
    }
    static N ScoreRow(string p) => new ScoreRowN { p = p };

    // ---- Feedback / Improvement card: [icon] Title ........ (read)   + scrolling text
    class FeedbackN : N
    {
        public string p, readBtn;
        public override void Build(RectTransform parent)
        {
            var card = Place(p, parent); if (card == null) return;
            var img = card.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.sprite = sCard; img.type = Image.Type.Sliced; img.color = Cyan; }
            foreach (var lg in card.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(lg);
            var vl = Undo.AddComponent<VerticalLayoutGroup>(card.gameObject); vl.padding = new RectOffset(44, 44, 36, 40); Column(vl, 20);

            var head = NewRect("IIAD_Row", card);
            var hl = Undo.AddComponent<HorizontalLayoutGroup>(head.gameObject);
            hl.spacing = 20; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
            Pref(head, 0, IconBtn, flexW: 1);

            var icon = Place(p + "/Icon", head); if (icon != null) { var i = icon.GetComponent<Image>(); if (i != null) i.preserveAspect = true; Pref(icon, 80, 80); }
            var title = Place(p + "/Text (TMP)", head); if (title != null) { var tt = title.GetComponent<TMP_Text>(); StyleText(tt, 48, White, FontStyles.Bold); tt.alignment = TextAlignmentOptions.MidlineLeft; Pref(title, 0, 80, flexW: 1); }
            var read = Place(p + "/" + readBtn, head); if (read != null) { StyleButton(read.GetComponent<Button>(), K.Icon); Pref(read, IconBtn, IconBtn); }

            var sv = Place(p + "/ScrollViewFeedback", card);
            if (sv != null)
            {
                Pref(sv, 0, 420, flexW: 1);
                foreach (var tmp in sv.GetComponentsInChildren<TMP_Text>(true)) { StyleText(tmp, 40, White, FontStyles.Normal); tmp.alignment = TextAlignmentOptions.TopLeft; }
            }
            foreach (var c in Children(card)) if (!used.Contains(c) && c != head) Leftover(c);
            Pref(card, 0, -1, flexW: 1);
        }
    }
    static N FeedbackCard(string p, string readBtn) => new FeedbackN { p = p, readBtn = readBtn };

    // =================================================================== POPUPS
    static void StylePopups(Transform popups)
    {
        if (popups == null) return;
        foreach (Transform pop in popups)
        {
            var box = pop.Find("Box") as RectTransform;
            if (box == null) continue;
            // rerun: take Yes / No back out of the row made last time
            foreach (var oldRow in Children(box).Where(c => c.name == "IIAD_Row").ToList())
            {
                foreach (var c in Children(oldRow).ToList()) Undo.SetTransformParent(c, pop, "IIAD");
                Undo.DestroyObjectImmediate(oldRow.gameObject);
            }
            var dim = pop.GetComponent<Image>();
            if (dim != null && dim.sprite == null) { Undo.RecordObject(dim, "IIAD"); dim.color = new Color(0.02f, 0.04f, 0.1f, 0.78f); }

            var bi = box.GetComponent<Image>(); if (bi != null) { Undo.RecordObject(bi, "IIAD"); bi.sprite = sCard; bi.type = Image.Type.Sliced; bi.color = Cyan; }
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f); box.anchoredPosition = Vector2.zero;

            var yes = pop.Find("YesButton") as RectTransform; var no = pop.Find("NoButton") as RectTransform;
            bool hasButtons = yes != null || no != null;
            box.sizeDelta = new Vector2(940, hasButtons ? 760 : 620);

            foreach (var tmp in box.GetComponentsInChildren<TMP_Text>(true))
            {
                bool title = tmp.name == "Title";
                StyleText(tmp, title ? 58 : 44, title ? White : Muted, title ? FontStyles.Bold : FontStyles.Normal);
                tmp.alignment = TextAlignmentOptions.Center;
                var r = (RectTransform)tmp.transform;
                if (title) { r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1); r.anchoredPosition = new Vector2(0, -56); r.sizeDelta = new Vector2(-88, 100); }
                else if (hasButtons) { r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1); r.offsetMin = new Vector2(48, 240); r.offsetMax = new Vector2(-48, -170); }
                else { r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0.5f); r.offsetMin = new Vector2(48, 60); r.offsetMax = new Vector2(-48, 0); }
            }
            var spinner = box.Find("Image") as RectTransform;
            if (spinner != null) { spinner.anchorMin = spinner.anchorMax = new Vector2(0.5f, 0.72f); spinner.anchoredPosition = Vector2.zero; spinner.sizeDelta = new Vector2(220, 220); var si = spinner.GetComponent<Image>(); if (si != null) si.preserveAspect = true; }

            if (hasButtons)
            {
                var row = NewRect("IIAD_Row", box);
                row.anchorMin = new Vector2(0, 0); row.anchorMax = new Vector2(1, 0); row.pivot = new Vector2(0.5f, 0);
                row.anchoredPosition = new Vector2(0, 48); row.sizeDelta = new Vector2(-96, BtnH);
                var hl = Undo.AddComponent<HorizontalLayoutGroup>(row.gameObject);
                hl.spacing = Gap; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
                if (no != null) { Undo.SetTransformParent(no, row, "IIAD"); StyleButton(no.GetComponent<Button>(), K.Secondary); }
                if (yes != null) { Undo.SetTransformParent(yes, row, "IIAD"); StyleButton(yes.GetComponent<Button>(), K.Primary); }
            }
            log.AppendLine("+ popup " + pop.name);
        }
    }

    // =================================================================== STYLE HELPERS
    static void StyleButton(Button b, K kind)
    {
        if (b == null) return;
        var rt = (RectTransform)b.transform;
        var img = b.GetComponent<Image>();
        Undo.RecordObject(b, "IIAD"); if (img != null) Undo.RecordObject(img, "IIAD");

        var cb = b.colors;
        cb.normalColor = Color.white; cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        cb.pressedColor = new Color(0.74f, 0.74f, 0.74f); cb.selectedColor = Color.white;
        cb.disabledColor = new Color(1f, 1f, 1f, 0.4f); cb.fadeDuration = 0.08f;
        b.colors = cb; b.transition = Selectable.Transition.ColorTint;

        var labels = b.GetComponentsInChildren<TMP_Text>(true).Where(t => t.GetComponentInParent<TMP_InputField>(true) == null).ToList();
        var icon = Children(rt).FirstOrDefault(c => c.name.ToLowerInvariant().Contains("icon") && c.GetComponent<Image>() != null);

        switch (kind)
        {
            case K.Eye: if (img != null) img.preserveAspect = true; return;

            case K.Link:
                foreach (var t in labels) { StyleText(t, 40, Cyan, FontStyles.Bold | FontStyles.Underline); t.alignment = TextAlignmentOptions.MidlineLeft; }
                return;

            case K.Icon:
            case K.IconDanger:
                if (img != null && img.sprite != sCircle)
                {
                    var iconRT = NewRect("IIAD_Icon", rt); Stretch(iconRT);
                    iconRT.offsetMin = new Vector2(28, 28); iconRT.offsetMax = new Vector2(-28, -28);
                    var ic = Undo.AddComponent<Image>(iconRT.gameObject);
                    ic.sprite = img.sprite; ic.preserveAspect = true; ic.raycastTarget = false;
                    img.sprite = sCircle; img.type = Image.Type.Simple; img.preserveAspect = true;
                }
                if (img != null) img.color = kind == K.IconDanger ? Coral : Cyan;
                return;
        }

        bool primary = kind == K.Primary;
        bool danger = kind == K.Danger || kind == K.MenuDanger;
        if (img != null)
        {
            img.enabled = true;
            img.sprite = primary ? sBtnFill : sBtnOutline; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.preserveAspect = false;
            img.color = primary ? Yellow : danger ? Coral : Cyan;
        }
        Color fg = primary ? Navy : danger ? Coral : White;

        if (kind == K.Stacked)
        {   // icon on top, label below (score panel)
            if (icon != null) { icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1); icon.pivot = new Vector2(0.5f, 1); icon.anchoredPosition = new Vector2(0, -40); icon.sizeDelta = new Vector2(120, 120); icon.GetComponent<Image>().preserveAspect = true; }
            foreach (var t in labels) { StyleText(t, 36, fg, FontStyles.Bold); t.alignment = TextAlignmentOptions.Center; var r = (RectTransform)t.transform; r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0); r.anchoredPosition = new Vector2(0, 30); r.sizeDelta = new Vector2(-24, 110); }
            return;
        }

        bool menu = kind == K.Menu || kind == K.MenuDanger;
        float iconSize = menu ? 96 : 68, iconX = menu ? 32 : 28;
        if (icon != null)
        {
            icon.anchorMin = icon.anchorMax = new Vector2(0, 0.5f); icon.pivot = new Vector2(0, 0.5f);
            icon.anchoredPosition = new Vector2(iconX, 0); icon.sizeDelta = new Vector2(iconSize, iconSize);
            var ii = icon.GetComponent<Image>(); ii.preserveAspect = true;
        }
        float textLeft = icon != null ? iconX + iconSize + 24 : 24;

        for (int i = 0; i < labels.Count; i++)
        {
            var t = labels[i];
            bool second = menu && labels.Count > 1 && i > 0;
            StyleText(t, second ? 36 : 48, second ? Muted : fg, second ? FontStyles.Normal : FontStyles.Bold);
            t.enableAutoSizing = true; t.fontSizeMax = second ? 36 : 50; t.fontSizeMin = 30;
            var r = (RectTransform)t.transform;
            if (labels.Count == 1)
            {
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f);
                r.offsetMin = new Vector2(textLeft, 8); r.offsetMax = new Vector2(-24, -8);
                t.alignment = icon != null || menu ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            }
            else
            {   // two-line card (sample prompts): title on top, text under it
                r.anchorMin = new Vector2(0, i == 0 ? 0.62f : 0f); r.anchorMax = new Vector2(1, i == 0 ? 1f : 0.62f);
                r.offsetMin = new Vector2(textLeft, i == 0 ? 0 : 24); r.offsetMax = new Vector2(-32, i == 0 ? -24 : 0);
                t.alignment = i == 0 ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.TopLeft;
            }
        }
    }

    static void StyleText(TMP_Text t, float size, Color c, FontStyles style)
    {
        if (t == null) return;
        Undo.RecordObject(t, "IIAD");
        if (font != null) t.font = font;
        t.fontSize = size; t.color = c; t.fontStyle = style;
        t.textWrappingMode = TextWrappingModes.Normal;
    }

    static Color ToneColor(Tone t) => t == Tone.Muted ? Muted : t == Tone.Accent ? Cyan : t == Tone.Error ? Coral : t == Tone.Score ? Yellow : White;

    /// <summary>Inputs, dropdowns and any text the recipes did not reach.</summary>
    static void FinalStylePass(Transform root)
    {
        foreach (var f in root.GetComponentsInChildren<TMP_InputField>(true))
        {
            var img = f.GetComponent<Image>();
            if (img != null) { Undo.RecordObject(img, "IIAD"); img.sprite = sField; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.color = FieldEdge; }
            Undo.RecordObject(f, "IIAD");
            if (f.textComponent != null) { StyleText(f.textComponent, 44, White, FontStyles.Normal); f.textComponent.enableAutoSizing = false; }
            if (f.placeholder is TMP_Text ph) { StyleText(ph, 42, new Color(Muted.r, Muted.g, Muted.b, 0.75f), FontStyles.Italic); ph.enableAutoSizing = false; }
            f.customCaretColor = true; f.caretColor = Cyan; f.caretWidth = 3;
            f.selectionColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
            if (f.textViewport != null && f.textViewport.offsetMin.x < 20) { Undo.RecordObject(f.textViewport, "IIAD"); f.textViewport.offsetMin = new Vector2(28, Mathf.Max(f.textViewport.offsetMin.y, 12)); if (f.textViewport.offsetMax.x > -20) f.textViewport.offsetMax = new Vector2(-28, Mathf.Min(f.textViewport.offsetMax.y, -12)); }
        }
        foreach (var d in root.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            var img = d.GetComponent<Image>();
            if (img != null) { Undo.RecordObject(img, "IIAD"); img.sprite = sField; img.type = Image.Type.Sliced; img.color = FieldEdge; }
            if (d.captionText != null) { StyleText(d.captionText, 44, White, FontStyles.Normal); var r = d.captionText.rectTransform; r.offsetMin = new Vector2(28, r.offsetMin.y); r.offsetMax = new Vector2(-90, r.offsetMax.y); }
            if (d.itemText != null) StyleText(d.itemText, 44, White, FontStyles.Normal);
            var arrow = d.transform.Find("Arrow") as RectTransform;
            if (arrow != null) { arrow.sizeDelta = new Vector2(48, 48); arrow.anchoredPosition = new Vector2(-40, 0); var ai = arrow.GetComponent<Image>(); if (ai != null) ai.color = Cyan; }
            if (d.template != null)
            {
                var ti = d.template.GetComponent<Image>(); if (ti != null) { Undo.RecordObject(ti, "IIAD"); ti.sprite = sCard; ti.type = Image.Type.Sliced; ti.color = Cyan; }
                foreach (var tog in d.template.GetComponentsInChildren<Toggle>(true))
                {
                    if (tog.targetGraphic is Image bg) { Undo.RecordObject(bg, "IIAD"); bg.color = new Color(0.05f, 0.11f, 0.22f, 1f); }
                    var tr = (RectTransform)tog.transform; Undo.RecordObject(tr, "IIAD"); tr.sizeDelta = new Vector2(tr.sizeDelta.x, Mathf.Max(tr.sizeDelta.y, 120));
                }
                var content = d.template.Find("Viewport/Content") as RectTransform;
                if (content != null) { Undo.RecordObject(content, "IIAD"); content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(content.sizeDelta.y, 128)); }
            }
        }
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (font != null && t.font != font) { Undo.RecordObject(t, "IIAD"); t.font = font; }
            Color.RGBToHSV(t.color, out _, out _, out float v);
            if (v < 0.45f && t.GetComponentInParent<Button>(true) == null) { Undo.RecordObject(t, "IIAD"); t.color = White; }
            if (!t.enableAutoSizing && t.fontSize < 32) { Undo.RecordObject(t, "IIAD"); t.fontSize = 32; }
        }
    }

    static void StylePrefab(string path)
    {
        if (!File.Exists(path)) return;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var img = root.GetComponent<Image>();
            if (img != null) { img.sprite = ((RectTransform)root.transform).sizeDelta.y < 120 ? sField : sCard; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.color = Cyan; }
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (font != null) t.font = font;
                if (t.GetComponentInParent<Button>(true) != null) { t.color = Navy; t.fontStyle = FontStyles.Bold; continue; }
                bool key = t.name.Contains("Key") || t.name.Contains("rank");
                t.color = key ? Muted : White;
                if (t.name.ToLowerInvariant().Contains("score")) { t.color = Yellow; t.fontStyle = FontStyles.Bold; }
                if (t.fontSize < 32 && !t.enableAutoSizing) t.fontSize = 32;
            }
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                var bi = b.GetComponent<Image>();
                if (bi != null) { bi.sprite = sBtnFill; bi.type = Image.Type.Sliced; bi.color = Yellow; }
                var rt = (RectTransform)b.transform; if (rt.sizeDelta.y < 110) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 110);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            log.AppendLine("+ prefab " + path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // =================================================================== LEFTOVERS
    /// <summary>Something the recipe did not place. Decoration is hidden; anything a script uses stays, out of the layout.</summary>
    static void Leftover(RectTransform t)
    {
        if (t == null || used.Contains(t)) return;
        used.Add(t);
        bool important = referenced.Contains(t.gameObject)
                      || t.GetComponentsInChildren<Selectable>(true).Length > 0
                      || t.GetComponentsInChildren<RawImage>(true).Length > 0
                      || t.GetComponentsInChildren<TMP_Text>(true).Any(x => referenced.Contains(x.gameObject));
        if (!important)
        {
            if (t.gameObject.activeSelf) { Undo.RecordObject(t.gameObject, "IIAD"); t.gameObject.SetActive(false); }
            log.AppendLine("  - hid decoration " + RelPath(screenRoot, t));
            return;
        }
        var le = t.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(t.gameObject);
        le.ignoreLayout = true;
        log.AppendLine("  ~ kept in place (used by a script, not in recipe): " + RelPath(screenRoot, t));
    }

    /// <summary>Every GameObject that a script field points to (directly or through one of its components).</summary>
    static HashSet<GameObject> CollectReferenced(Scene scene)
    {
        var set = new HashSet<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string ns = mb.GetType().Namespace ?? "";
                if (ns.StartsWith("UnityEngine") || ns.StartsWith("TMPro")) continue;   // UI components reference their own children
                var so = new SerializedObject(mb);
                var it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
                    if (it.objectReferenceValue is GameObject g) set.Add(g);
                    else if (it.objectReferenceValue is Component c) set.Add(c.gameObject);
                }
            }
        return set;
    }

    // =================================================================== SPRITES
    static void EnsureSprites()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Arts/NEW UI")) AssetDatabase.CreateFolder("Assets/Arts", "NEW UI");
        if (!AssetDatabase.IsValidFolder(SpriteFolder)) AssetDatabase.CreateFolder("Assets/Arts/NEW UI", "IIAD Neon");
        sBtnFill = MakeSprite("iiad_button_fill", 128, 44, 0, 1f, 1f, true);
        sBtnOutline = MakeSprite("iiad_button_outline", 128, 44, 5, 0.18f, 1f, true);
        sCard = MakeSprite("iiad_card", 128, 40, 3, 0.13f, 0.94f, true);
        sField = MakeSprite("iiad_field", 128, 30, 3, 0.10f, 1f, true);
        sCircle = MakeSprite("iiad_circle", 128, 64, 5, 0.18f, 1f, false);
    }

    static Sprite MakeSprite(string name, int size, float radius, float ring, float innerGray, float innerAlpha, bool sliced)
    {
        string path = SpriteFolder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float d = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - radius;
                    float a = Mathf.Clamp01(0.5f - d);
                    Color c;
                    if (ring <= 0) c = new Color(1, 1, 1, a);
                    else
                    {
                        float inner = Mathf.Clamp01(0.5f - (d + ring));
                        float g = Mathf.Lerp(1f, innerGray, inner);
                        c = new Color(g, g, g, a * Mathf.Lerp(1f, innerAlpha, inner));
                    }
                    tex.SetPixel(x, y, c);
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        float border = sliced ? radius + 2 : 0;
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteBorder.x != border)
        {
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed; imp.spritePixelsPerUnit = 100;
            imp.spriteBorder = new Vector4(border, border, border, border);
            var st = new TextureImporterSettings(); imp.ReadTextureSettings(st); st.spriteMeshType = SpriteMeshType.FullRect; imp.SetTextureSettings(st);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static TMP_FontAsset FindFont()
    {
        foreach (var guid in AssetDatabase.FindAssets("Lexend t:TMP_FontAsset"))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        log.AppendLine("Lexend TMP font asset not found; fonts unchanged.");
        return null;
    }

    // =================================================================== UTIL
    static void Column(VerticalLayoutGroup vl, float spacing)
    {
        vl.spacing = spacing; vl.childAlignment = TextAnchor.UpperCenter;
        vl.childControlWidth = true; vl.childControlHeight = true;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
    }

    /// <summary>Sets the size the parent layout gives this item. h = -1 lets its own layout decide.</summary>
    static void Pref(RectTransform t, float w, float h, float flexW = 0, bool minOnly = false)
    {
        var le = t.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(t.gameObject);
        Undo.RecordObject(le, "IIAD");
        le.ignoreLayout = false;
        if (w > 0) { le.minWidth = w; le.preferredWidth = w; } else { le.minWidth = -1; le.preferredWidth = -1; }
        le.flexibleWidth = flexW > 0 ? flexW : (w > 0 ? 0 : -1);
        if (h > 0) { le.minHeight = h; le.preferredHeight = minOnly ? -1 : h; } else { le.minHeight = -1; le.preferredHeight = -1; }
        le.flexibleHeight = -1;
    }

    static Transform FindInScene(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    static List<RectTransform> Children(Transform t)
    {
        var list = new List<RectTransform>();
        foreach (Transform c in t) if (c is RectTransform r) list.Add(r);
        return list;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "IIAD");
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false); rt.localScale = Vector3.one;
        return rt;
    }

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; r.pivot = new Vector2(0.5f, 0.5f); }

    static string RelPath(Transform root, Transform t)
    {
        var parts = new List<string>();
        while (t != null && t != root) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }

    static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
}
