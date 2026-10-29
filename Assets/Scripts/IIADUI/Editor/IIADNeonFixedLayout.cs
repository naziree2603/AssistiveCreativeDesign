using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > IIAD > Neon UI: Fix All Panels (fixed layout)
///
/// The most reliable version. Every panel is laid out with FIXED positions (no stacked layout
/// groups, no scrolling), so what you see in the Scene view is exactly what the phone shows:
///   Header 190 | page (content shrinks evenly only if the phone is short) | Footer 210
/// Works on the original scene and on any earlier restyle: old wrappers are removed first and
/// every object is found again by name. Existing objects are re-used, so scripts keep working.
/// Also: card / button / field style, neon icons, neon logo, popups, list-card prefabs.
/// </summary>
public static partial class IIADNeonRestyle
{
    const float W_PAD = 44f;     // card inner padding
    const float CARD_GAP = 22f;  // gap between items inside a card

    [MenuItem("Tools/IIAD/Neon UI: Fix All Panels (fixed layout)", priority = 30)]
    static void RunFixed()
    {
        var container = FindInScene("ScreenContainer");
        if (container == null) { EditorUtility.DisplayDialog("Neon UI", "Open Assets/Scenes/UI/MainUI.unity first.", "OK"); return; }
        if (!EditorUtility.DisplayDialog("Neon UI: Fix All Panels",
            "Lays out every panel with fixed positions (no scrolling), and restyles cards, buttons, fields, icons and logo.\n\nOne Undo step (Ctrl+Z). Commit to Git first.", "Fix all panels", "Cancel")) return;

        log = new StringBuilder();
        EnsureSprites();
        font = FindFont();
        referenced = CollectReferenced(container.gameObject.scene);

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("IIAD Neon fixed layout");
        Undo.RegisterFullObjectHierarchyUndo(container.gameObject, "IIAD Neon fixed layout");

        // workspace frame art -> transparent, so every screen shares the same navy background
        var wsBg = container.Find("DesignWorkspace/MainSystemPanel/Background")?.GetComponent<Image>();
        if (wsBg != null) { Undo.RecordObject(wsBg, "IIAD"); wsBg.color = new Color(1, 1, 1, 0); }
        var sampleBg = container.Find("SamplePromptPopUp")?.GetComponent<Image>();
        if (sampleBg != null) { Undo.RecordObject(sampleBg, "IIAD"); sampleBg.sprite = null; sampleBg.color = new Color(Navy.r, Navy.g, Navy.b, 0.97f); }

        foreach (var (path, build) in Recipes())
        {
            var screen = container.Find(path) as RectTransform;
            if (screen == null) { log.AppendLine("! not found: " + path); continue; }
            try { build(screen); log.AppendLine("+ " + path); }
            catch (Exception e) { log.AppendLine("! " + path + ": " + e.Message); Debug.LogException(e); }
        }

        StylePopups(container.Find("PopUps"));
        FinalStylePass(container);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(container.gameObject.scene);

        IIADChallengePanelFix.Apply();
        StylePrefab("Assets/Data/Prefabs/ParticipantRankCard.prefab");
        StylePrefab("Assets/Data/Prefabs/SubmittedCard.prefab");
        IIADNeonLogo.ApplyToOpenScenes(true);
        IIADNeonIcons.ApplyToOpenScenes();

        Canvas.ForceUpdateCanvases();
        Debug.Log("IIAD Neon fixed layout done.\n" + log);
        EditorUtility.DisplayDialog("Neon UI", "All panels fixed. Press Play and check the whole loop, then save the scene.\nCtrl+Z undoes the layout.", "OK");
    }

    // =====================================================================================
    // RECIPES (one per panel). P = page builder. All sizes in canvas units (1080 x 1920).
    // =====================================================================================
    static IEnumerable<(string, Action<RectTransform>)> Recipes()
    {
        string[] wsLeft = { "Header/GooleeLogo", "Header/AppsLogo" };
        string[] wsRight = { "Header/AccessibilityButton", "Header/HomeButton" };
        string[] wsFoot = { "FooterNavigation/PreviousButton", "FooterNavigation/NextButton" };

        yield return ("WelcomeScreen", s => Panel(s, new string[0], new[] { "AccessibilityButton", "ExitAppButton" }, null, p =>
        {
            p.Skip(20);
            Pic(p, "Logo", 420, 420);
            Txt(p, "WelcomeText", 52, Tone.Title, TextAlignmentOptions.Center, 80);
            Pic(p, "LogoTitle", 760, 140);
            p.Skip(40);
            Btn(p, "LogInButton", K.Primary);
            Btn(p, "CreateNewAccountButton", K.Secondary);
        }));

        yield return ("LoginScreen", s => Panel(s, new[] { "BackButton" }, new[] { "AccessibilityButton" }, null, p =>
        {
            Pic(p, "Logo", 230, 230);
            CardAt(p, "LoginPanel", c =>
            {
                Txt(c, "LoginText", 60, Tone.Title, TextAlignmentOptions.Center, 86);
                Txt(c, "WelcomeText", 38, Tone.Muted, TextAlignmentOptions.Center, 52);
                Field(c, "Username"); Field(c, "Password");
                Btn(c, "ForgotPassword?", K.Link, 76);
                Txt(c, "MessageText", 36, Tone.Error, TextAlignmentOptions.Center, 50);
                Btn(c, "LoginButton", K.Primary);
                Txt(c, "Or", 34, Tone.Muted, TextAlignmentOptions.Center, 44);
                Btn(c, "CreateAccount", K.Secondary);
            });
        }));

        yield return ("RegisterScreen", s => Panel(s, new[] { "BackButton" }, new[] { "AccessibilityButton" }, null, p =>
        {
            CardAt(p, "RegisterPanel", c =>
            {
                Txt(c, "RegisterText", 60, Tone.Title, TextAlignmentOptions.Center, 86);
                Txt(c, "CreateText", 38, Tone.Muted, TextAlignmentOptions.Center, 52);
                Field(c, "Email"); Field(c, "Username"); Field(c, "Password"); Field(c, "ConfirmPassword");
                Txt(c, "MessageText", 36, Tone.Error, TextAlignmentOptions.Center, 50);
                Btn(c, "CreateAccount", K.Primary);
            });
        }));

        yield return ("MainDashboard", s => Panel(s, new string[0], new[] { "Header/AccessibilityButton", "Header/SettingsButton", "Header/ProfileButton", "Header/LogOutButton" }, null, p =>
        {
            Pic(p, "LogoImage", 250, 250);
            Pic(p, "LogoTitle", 760, 120);
            Txt(p, "UsernameWelcomeText", 56, Tone.Title, TextAlignmentOptions.Center, 80);
            p.Skip(10);
            BoxAt(p, "GroupButtons", b =>
            {
                Btn(b, "PracticeModeButton", K.Menu, MenuH); Btn(b, "ChallengeModeButton", K.Menu, MenuH);
                Btn(b, "SubmittedButton", K.Menu, MenuH); Btn(b, "ExitButton", K.MenuDanger, MenuH);
            }, 26);
        }));

        yield return ("ParticipantDetailsPanel", s => Panel(s, new[] { "BackButton" }, new string[0], null, p =>
        {
            CardAt(p, "ParticipantDetailsBg", c =>
            {
                TitleAt(c, "TitleBackground", 56, 86);
                Pic(c, "ProfileIcon", 120, 120);
                Field(c, "NameBox"); Field(c, "CategoryBox"); Field(c, "SubCategoryBox"); Field(c, "InstititionSchoolBox");
                Txt(c, "MessageText", 36, Tone.Error, TextAlignmentOptions.Center, 50);
                Btn(c, "SaveParticipantButton", K.Primary);
            });
        }));

        yield return ("SettingsPanel", s => Panel(s, null, null, new[] { "FooterNavigation/BackButton" }, p =>
        {
            TitleAt(p, "SettingTitle", 64, 110);
            p.Skip(10);
            BoxAt(p, "GroupButton", b =>
            {
                Btn(b, "HowToPlay", K.Menu, MenuH); Btn(b, "AboutButton", K.Menu, MenuH);
                Btn(b, "PrivacyButton", K.Menu, MenuH); Btn(b, "DeleteAccount", K.MenuDanger, MenuH);
            }, 26);
        }));

        yield return ("HowToPlayPanel", s => Panel(s, new string[0], new[] { "Header/HomeButton" }, new[] { "FooterNavigation/PreviousButton", "FooterNavigation/NextButton" }, p =>
        {
            CardAt(p, "Background", c =>
            {
                TitleAt(c, "HowToPlayTitle", 60, 96);
                Overlay(c, 1060, "Page 1", "Page 2", "Page 3", "Page 4");
                Txt(c, "page", 38, Tone.Muted, TextAlignmentOptions.Center, 56);
            });
        }));

        yield return ("SubmittedPanel", s => Panel(s, null, null, null, p =>
        {
            TitleAt(p, "titlle", 64, 110);
            Txt(p, "StatusText", 36, Tone.Muted, TextAlignmentOptions.Center, 50);
            Raw(p, "Scroll View", 1180);
            RowOf(p, BtnH, ("BackButton", K.Secondary), ("LeaderboardButton", K.Primary));
        }));

        yield return ("LeaderboardPanel", s => Panel(s, null, null, new[] { "FooterNavigation/BackButton" }, p =>
        {
            TitleAt(p, "TitleBackground", 64, 110);
            Txt(p, "ChallengeTitle", 44, Tone.Accent, TextAlignmentOptions.Center, 64);
            Txt(p, "StatusText", 36, Tone.Muted, TextAlignmentOptions.Center, 50);
            LeaderTable(p, "Panel Backgrounf", 1180);
        }));

        string WS = "DesignWorkspace/MainSystemPanel/Background/";

        yield return (WS + "IdeaInputPanel", s => Panel(s, wsLeft, wsRight, wsFoot, p =>
        {
            TitleAt(p, "TitleBackground", 60, 96);
            CardAt(p, "TipsBox", c =>
            {
                Hide(c, "TipsBox/Icon");
                Txt(c, "TipsBox/TipsLabel", 42, Tone.Title, TextAlignmentOptions.MidlineLeft, 56);
                Txt(c, "TipsBox/Detail", 34, Tone.Muted, TextAlignmentOptions.TopLeft, 120);
            });
            Txt(p, "YourIdeaLabel", 40, Tone.Title, TextAlignmentOptions.MidlineLeft, 52);
            Raw(p, "IdeaInput", 360);
            Btn(p, "OpenSampleIdea(PracticeOnly)", K.Secondary, 110);
            RowOf(p, 150, ("MicButton", K.Secondary), ("RepeatButton", K.Secondary));
            Txt(p, "MessageText", 34, Tone.Error, TextAlignmentOptions.Center, 46);
            Btn(p, "GeneratePosterButton", K.Primary, 160);
        }));

        yield return (WS + "OutputPanel", s => Panel(s, wsLeft, wsRight, wsFoot, p =>
        {
            TitleAt(p, "TitleBackground", 60, 96);
            CardAt(p, "OutputArea", c =>
            {
                Pic(c, "OriginalPosterImage", 760, 980);
                Btn(c, "FullScreenPoster", K.Secondary, 140);
            });
        }));

        yield return (WS + "DescriptionPanel", s => Panel(s, wsLeft, wsRight, wsFoot, p =>
        {
            TitleAt(p, "TitleBackground", 60, 96);
            CardAt(p, "OriginalPosterBox", c =>
            {
                Txt(c, "GeneratedPosterLabel", 40, Tone.Title, TextAlignmentOptions.MidlineLeft, 52);
                Pic(c, "LatesPosterDesc", 300, 420);
                Btn(c, "FullScreenPoster", K.Secondary, 130);
            });
            CardAt(p, "DescriptionBox", c =>
            {
                Txt(c, "PosterDescriptionLabel", 40, Tone.Title, TextAlignmentOptions.MidlineLeft, 52);
                Raw(c, "Scroll View", 380);
            });
            Txt(p, "MessageText", 34, Tone.Error, TextAlignmentOptions.Center, 46);
            Btn(p, "ReplayDescriptionVoiceButton", K.Secondary, 140);
        }));

        yield return (WS + "RevisionPanel", s => Panel(s, wsLeft, wsRight, wsFoot, p =>
        {
            TitleAt(p, "TitleBackground", 60, 96);
            CardAt(p, "LatestPosterArea", c =>
            {
                TitleAt(c, "LatestPosterArea/Label", 40, 56);
                Pic(c, "LatestPoster", 260, 360);
                Btn(c, "FullScreenPoster", K.Secondary, 120);
            });
            CardAt(p, "AttemptBox", c =>
            {
                Txt(c, "AttemptBox/Label", 38, Tone.Title, TextAlignmentOptions.MidlineLeft, 50);
                Hide(c, "AttempPanel/Icon");
                Txt(c, "AttemptRemainingText", 64, Tone.Score, TextAlignmentOptions.MidlineLeft, 80);
                Txt(c, "AttempPanel/Details", 32, Tone.Muted, TextAlignmentOptions.MidlineLeft, 44);
            });
            CardAt(p, "ReviseBox", c =>
            {
                TitleAt(c, "ReviseBox/LabelBg", 40, 56);
                Raw(c, "ReviseInputField", 230);
                RowOf(c, 140, ("ReviseBox/MicButton", K.Secondary), ("ReviseBox/RepeatButton", K.Secondary));
            });
            Btn(p, "ReviseDesignButton", K.Primary, 160);
        }));

        yield return (WS + "SubmissionPanel", s => Panel(s, wsLeft, wsRight, wsFoot, p =>
        {
            TitleAt(p, "TitleBackground", 60, 96);
            CardAt(p, "LatestPosterArea", c =>
            {
                TitleAt(c, "LatestPosterArea/Label", 40, 56);
                Pic(c, "LatestPoster", 240, 330);
                Btn(c, "FullScreenPoster", K.Secondary, 120);
            });
            CardAt(p, "ExplainBox", c =>
            {
                TitleAt(c, "ExplainBox/LabelBg", 40, 56);
                Raw(c, "SubmissionInput", 230);
                RowOf(c, 140, ("ExplainBox/MicButton", K.Secondary), ("ExplainBox/RepeatButton", K.Secondary));
            });
            CardAt(p, "TipsBox/Background", c =>
            {
                Txt(c, "TipsBox/Background/Label", 38, Tone.Title, TextAlignmentOptions.MidlineLeft, 50);
                Txt(c, "TipsBox/Background/Detail", 32, Tone.Muted, TextAlignmentOptions.TopLeft, 100);
                Hide(c, "•"); Hide(c, "• (1)");
            });
            Txt(p, "MessageText", 34, Tone.Error, TextAlignmentOptions.Center, 46);
            Btn(p, "SubmitPosterButton", K.Primary, 160);
        }));

        yield return (WS + "ScorePanel", s => Panel(s, wsLeft, wsRight, wsFoot, p =>
        {
            TitleAt(p, "TitleBackground", 60, 96);
            ScoreTotals(p, "TotalScorePanel");
            CardAt(p, "ScorebreakdownPanel", c =>
            {
                foreach (var r in new[] { "PromptQualityRow", "PosterMessageRow", "DesignQualityRow", "AccessibilityRow", "FinalDesignRow" }) ScoreLine(c, r);
            }, 14);
        }));

        yield return (WS + "FeedbackPanel", s => Panel(s, wsLeft, wsRight, wsFoot, p =>
        {
            TitleAt(p, "TitleBackground", 56, 120);
            FeedbackBlock(p, "FeedbackBox", "ReadFeedbackButton");
            FeedbackBlock(p, "ImprovementBox", "ReadImprovementButton");
        }));

        yield return (WS + "OriginalFullPosterPanel", s => Panel(s, null, null, null, p =>
        {
            Pic(p, "OriPosterImageFuul", 984, 1450);
            Btn(p, "BackButton", K.Secondary);
        }));
        yield return (WS + "LatestFullPosterPanel", s => Panel(s, null, null, null, p =>
        {
            Pic(p, "LatestPOsterFUUL", 984, 1450);
            Btn(p, "BackButton", K.Secondary);
        }));

        yield return ("SamplePromptPopUp", s => Panel(s, null, null, null, p =>
        {
            TitleAt(p, "Title", 60, 110);
            BoxAt(p, "Layout", b =>
            {
                Btn(b, "Sample 1 Button", K.Menu, 270); Btn(b, "Sample 2 Button", K.Menu, 270);
                Btn(b, "Sample 3 Button", K.Menu, 270); Btn(b, "Back", K.Secondary, BtnH);
            }, 24);
        }));
    }

    // =====================================================================================
    // PANEL
    // =====================================================================================
    static void Panel(RectTransform s, string[] left, string[] right, string[] footer, Action<Stack> build)
    {
        screenRoot = s;
        Unwrap(s);
        foreach (var w in s.GetComponentsInChildren<IIADFitToViewport>(true)) Undo.DestroyObjectImmediate(w);
        used = new HashSet<Transform>();
        map = new Dictionary<string, RectTransform>();
        foreach (var t in s.GetComponentsInChildren<RectTransform>(true))
            if (t != s) { string rp = RelPath(s, t); if (!map.ContainsKey(rp)) map[rp] = t; }

        RectTransform header = null, foot = null;
        bool wantHeader = (left != null && left.Length > 0) || (right != null && right.Length > 0);
        if (wantHeader)
        {
            header = s.Find("Header") as RectTransform ?? NewRect("Header", s);
            used.Add(header);
            BuildHeader(header, left ?? new string[0], right ?? new string[0]);
        }
        if (footer != null)
        {
            foot = s.Find("FooterNavigation") as RectTransform;
            if (foot != null) { used.Add(foot); BuildFooter(foot, footer); }
        }

        // page area + content (fixed positions inside), shrinks evenly if the phone is short
        var page = NewRect("IIAD_Page", s);
        page.SetSiblingIndex(header != null ? header.GetSiblingIndex() + 1 : 0);
        Stretch(page);
        page.offsetMin = new Vector2(0, foot != null ? FooterH : 40);
        page.offsetMax = new Vector2(0, -(header != null ? HeaderH : 70));
        var content = NewRect("Content", page);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        used.Add(page);

        var stack = new Stack(content, 6, Side, Gap, 6);
        build(stack);
        content.sizeDelta = new Vector2(0, stack.Height);

        var fit = Undo.AddComponent<IIADFitToViewport>(content.gameObject);
        fit.viewport = page; fit.minScale = 0.55f; fit.centerVertically = false;

        foreach (var c in Children(s))
            if (c != header && c != foot && c != page && !used.Contains(c)) Leftover(c);
    }

    // =====================================================================================
    // STACK: places items top to bottom with fixed positions
    // =====================================================================================
    class Stack
    {
        public RectTransform parent; public float y; public float side; public float gap; readonly float bottom;
        public Stack(RectTransform p, float top, float side, float gap, float bottom) { parent = p; y = -top; this.side = side; this.gap = gap; this.bottom = bottom; }
        public float Height => -y - gap + bottom;
        public void Skip(float h) { y -= h; }

        /// <summary>Full width (minus side padding); w > 0 = fixed width, centred.</summary>
        public RectTransform Put(RectTransform r, float h, float w = 0, bool stripGroups = true)
        {
            if (r == null) return null;
            Attach(r, parent, stripGroups);
            if (w <= 0)
            {
                r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1);
                r.sizeDelta = new Vector2(-2 * side, h); r.anchoredPosition = new Vector2(0, y);
            }
            else
            {
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1); r.pivot = new Vector2(0.5f, 1);
                r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(0, y);
            }
            y -= h + gap;
            return r;
        }
    }

    static void Attach(RectTransform r, RectTransform parent, bool stripGroups = true)
    {
        used.Add(r);
        if (r.parent != parent) Undo.SetTransformParent(r, parent, "IIAD");
        r.SetAsLastSibling();
        r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
        r.localPosition = new Vector3(r.localPosition.x, r.localPosition.y, 0);
        foreach (var c in r.GetComponents<LayoutElement>()) Undo.DestroyObjectImmediate(c);
        foreach (var c in r.GetComponents<ContentSizeFitter>()) Undo.DestroyObjectImmediate(c);
        foreach (var c in r.GetComponents<AspectRatioFitter>()) Undo.DestroyObjectImmediate(c);
        foreach (var c in r.GetComponents<IIADFitToViewport>()) Undo.DestroyObjectImmediate(c);
        if (stripGroups) foreach (var c in r.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(c);
    }

    // =====================================================================================
    // BUILDING BLOCKS
    // =====================================================================================
    static RectTransform Q(string name) => Get(name);   // exact path, else found by name

    static void Txt(Stack s, string name, float size, Tone tone, TextAlignmentOptions align, float h)
    {
        var r = Q(name); if (r == null) return;
        s.Put(r, h);
        var t = r.GetComponent<TMP_Text>();
        if (t != null)
        {
            StyleText(t, size, ToneColor(tone), tone == Tone.Title || tone == Tone.Score || tone == Tone.Accent ? FontStyles.Bold : FontStyles.Normal);
            t.alignment = align; t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = Mathf.Min(size, 26);
            t.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    static void TitleAt(Stack s, string name, float size, float h)
    {
        var r = Q(name); if (r == null) return;
        s.Put(r, h);
        var img = r.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = false; }
        foreach (var t in r.GetComponentsInChildren<TMP_Text>(true))
        {
            StyleText(t, size, White, FontStyles.Bold);
            t.alignment = size >= 56 ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
            t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = 30;
            if (t.transform != r) { var tr = (RectTransform)t.transform; Stretch(tr); }
            t.gameObject.SetActive(true);
        }
    }

    static void Btn(Stack s, string name, K kind, float h = BtnH)
    {
        var r = Q(name); if (r == null) return;
        s.Put(r, h, kind == K.Link ? 520 : 0);
        var b = r.GetComponent<Button>(); if (b != null) StyleButton(b, kind);
        if (kind == K.Link) { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(s.side, r.anchoredPosition.y); }
    }

    static void Raw(Stack s, string name, float h)
    {
        var r = Q(name); if (r == null) return;
        s.Put(r, h, 0, stripGroups: false);
    }

    /// <summary>Image kept at its own shape, centred in a slot of the given size.</summary>
    static void Pic(Stack s, string name, float maxW, float maxH)
    {
        var r = Q(name); if (r == null) return;
        var size = r.rect.size;
        var slot = NewRect("IIAD_Holder_" + r.name, s.parent);
        s.Put(slot, maxH);
        Attach(r, slot);
        Vector2 fit = new Vector2(maxW, maxH);
        if (size.x > 1 && size.y > 1) { float k = Mathf.Min(maxW / size.x, maxH / size.y); fit = size * k; }
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero; r.sizeDelta = fit;
        var img = r.GetComponent<Image>(); if (img != null) { img.preserveAspect = true; img.enabled = true; }
        r.gameObject.SetActive(true);
    }

    static void Hide(Stack s, string name)
    {
        var r = Q(name); if (r == null) return;
        used.Add(r);
        Undo.RecordObject(r.gameObject, "IIAD"); r.gameObject.SetActive(false);
    }

    /// <summary>Existing object becomes a dark card; children placed inside with fixed positions.</summary>
    static void CardAt(Stack s, string name, Action<Stack> inner, float gap = CARD_GAP) => Container(s, name, inner, gap, true);
    static void BoxAt(Stack s, string name, Action<Stack> inner, float gap = CARD_GAP) => Container(s, name, inner, gap, false);

    static void Container(Stack s, string name, Action<Stack> inner, float gap, bool card)
    {
        var r = Q(name); if (r == null) return;
        Attach(r, s.parent);
        r.gameObject.SetActive(true);
        var img = r.GetComponent<Image>();
        if (card)
        {
            if (img == null) img = Undo.AddComponent<Image>(r.gameObject);
            Undo.RecordObject(img, "IIAD");
            img.enabled = true; img.sprite = sCard; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.color = Cyan; img.preserveAspect = false;
        }
        else if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = false; }

        var st = new Stack(r, card ? 40 : 0, card ? W_PAD : 0, gap, card ? 44 : 0);
        inner(st);
        // children of this container that the recipe did not use
        foreach (var c in Children(r)) if (!used.Contains(c)) Leftover(c);
        s.Put(r, st.Height);
    }

    /// <summary>Label above a 132-high field (input or dropdown); eye button inside password fields.</summary>
    static void Field(Stack s, string name)
    {
        var g = Q(name); if (g == null) return;
        s.Put(g, 56 + 10 + FieldH);
        g.gameObject.SetActive(true);
        var gi = g.GetComponent<Image>(); if (gi != null) { Undo.RecordObject(gi, "IIAD"); gi.enabled = false; }

        Component field = g.GetComponentInChildren<TMP_InputField>(true);
        if (field == null) field = g.GetComponentInChildren<TMP_Dropdown>(true);
        var label = g.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(x => x.GetComponentInParent<TMP_InputField>(true) == null && x.GetComponentInParent<TMP_Dropdown>(true) == null);

        if (label != null)
        {
            var lr = (RectTransform)label.transform; Attach(lr, g);
            lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(1, 1); lr.pivot = new Vector2(0.5f, 1);
            lr.sizeDelta = new Vector2(0, 56); lr.anchoredPosition = Vector2.zero;
            StyleText(label, 38, White, FontStyles.Bold); label.alignment = TextAlignmentOptions.MidlineLeft;
            label.text = label.text.Replace(":", "").Trim(); label.enableAutoSizing = false;
            label.gameObject.SetActive(true);
        }
        if (field != null)
        {
            var fr = (RectTransform)field.transform; Attach(fr, g);
            fr.anchorMin = new Vector2(0, 1); fr.anchorMax = new Vector2(1, 1); fr.pivot = new Vector2(0.5f, 1);
            fr.sizeDelta = new Vector2(0, FieldH); fr.anchoredPosition = new Vector2(0, -66);
            fr.gameObject.SetActive(true);
            // eye button: inside the field, right side
            var eye = g.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.ToLowerInvariant().Contains("eye"));
            if (eye != null)
            {
                var er = (RectTransform)eye.transform; Attach(er, fr);
                er.anchorMin = er.anchorMax = new Vector2(1, 0.5f); er.pivot = new Vector2(1, 0.5f);
                er.anchoredPosition = new Vector2(-18, 0); er.sizeDelta = new Vector2(92, 92);
                var ei = eye.GetComponent<Image>(); if (ei != null) ei.preserveAspect = true;
                if (field is TMP_InputField inf && inf.textViewport != null) { Undo.RecordObject(inf.textViewport, "IIAD"); inf.textViewport.offsetMax = new Vector2(-124, inf.textViewport.offsetMax.y); }
            }
        }
        foreach (var c in Children(g)) if (!used.Contains(c)) Leftover(c);
    }

    /// <summary>Two or more buttons side by side, equal width.</summary>
    static void RowOf(Stack s, float h, params (string name, K kind)[] items)
    {
        var row = NewRect("IIAD_Row", s.parent);
        s.Put(row, h);
        int n = items.Length, i = 0;
        foreach (var (name, kind) in items)
        {
            var r = Q(name); if (r == null) { i++; continue; }
            Attach(r, row);
            r.anchorMin = new Vector2((float)i / n, 0); r.anchorMax = new Vector2((float)(i + 1) / n, 1); r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(i == 0 ? 0 : Gap / 2, 0); r.offsetMax = new Vector2(i == n - 1 ? 0 : -Gap / 2, 0);
            var b = r.GetComponent<Button>(); if (b != null) StyleButton(b, kind);
            i++;
        }
    }

    /// <summary>How-to-play pages: all in the same slot (the manager shows one at a time).</summary>
    static void Overlay(Stack s, float h, params string[] names)
    {
        var slot = NewRect("IIAD_Pages", s.parent);
        s.Put(slot, h);
        foreach (var n in names)
        {
            var r = Q(n); if (r == null) continue;
            Attach(r, slot); Stretch(r);
            foreach (var t in r.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleText(t, 36, White, FontStyles.Normal);
                t.alignment = TextAlignmentOptions.TopLeft; t.enableAutoSizing = true; t.fontSizeMax = 36; t.fontSizeMin = 24;
                var tr = (RectTransform)t.transform; Stretch(tr);
            }
        }
    }

    static void LeaderTable(Stack s, string name, float h)
    {
        var r = Q(name); if (r == null) return;
        float oldSide = s.side; s.side = -Side + 12;          // nearly full screen width, the list rows are wide
        s.Put(r, h, 0, stripGroups: false);
        s.side = oldSide;
        var img = r.GetComponent<Image>();
        if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = true; img.sprite = sCard; img.type = Image.Type.Sliced; img.color = Cyan; }

        var head = r.Find("Header") as RectTransform;
        if (head != null)
        {
            used.Add(head);
            head.anchorMin = new Vector2(0, 1); head.anchorMax = new Vector2(1, 1); head.pivot = new Vector2(0.5f, 1);
            head.sizeDelta = new Vector2(-24, 76); head.anchoredPosition = new Vector2(0, -12);
            var hi = head.GetComponent<Image>(); if (hi != null) { Undo.RecordObject(hi, "IIAD"); hi.color = new Color(0, 0, 0, 0); }
            foreach (var t in head.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleText(t, 28, Muted, FontStyles.Bold);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMax = 28; t.fontSizeMin = 18;
                t.alignment = TextAlignmentOptions.Center;
            }
        }
        var sv = r.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();
        if (sv != null)
        {
            var svr = (RectTransform)sv.transform; used.Add(svr);
            svr.anchorMin = Vector2.zero; svr.anchorMax = Vector2.one; svr.pivot = new Vector2(0.5f, 0.5f);
            svr.offsetMin = new Vector2(12, 16); svr.offsetMax = new Vector2(-12, -96);
        }
    }

    static void ScoreTotals(Stack s, string name)
    {
        var r = Q(name); if (r == null) return;
        s.Put(r, 380);
        var img = r.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = true; img.sprite = sCard; img.type = Image.Type.Sliced; img.color = Cyan; }
        var read = Q("ReadScoreButton"); var view = Q("ViewPosterButton"); var circle = Q("TotalScoreCircle");
        if (read != null) { Attach(read, r); read.anchorMin = read.anchorMax = read.pivot = new Vector2(0, 0.5f); read.anchoredPosition = new Vector2(32, 0); read.sizeDelta = new Vector2(230, 300); StyleButton(read.GetComponent<Button>(), K.Stacked); }
        if (view != null) { Attach(view, r); view.anchorMin = view.anchorMax = view.pivot = new Vector2(1, 0.5f); view.anchoredPosition = new Vector2(-32, 0); view.sizeDelta = new Vector2(230, 300); StyleButton(view.GetComponent<Button>(), K.Stacked); }
        if (circle != null)
        {
            Attach(circle, r);
            circle.anchorMin = circle.anchorMax = circle.pivot = new Vector2(0.5f, 0.5f); circle.anchoredPosition = Vector2.zero; circle.sizeDelta = new Vector2(320, 320);
            var ci = circle.GetComponent<Image>(); if (ci != null) { Undo.RecordObject(ci, "IIAD"); ci.sprite = sCircle; ci.type = Image.Type.Simple; ci.preserveAspect = true; ci.color = Yellow; }
            foreach (var t in circle.GetComponentsInChildren<TMP_Text>(true))
            {
                bool big = t.name.Contains("Marks");
                StyleText(t, big ? 110 : 34, big ? White : Muted, FontStyles.Bold); t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = false;
                var tr = (RectTransform)t.transform; tr.anchorMin = new Vector2(0, 0.5f); tr.anchorMax = new Vector2(1, 0.5f); tr.pivot = new Vector2(0.5f, 0.5f);
                if (big) { tr.anchoredPosition = new Vector2(0, 30); tr.sizeDelta = new Vector2(-40, 130); }
                else if (t.text.Contains("/")) { tr.anchoredPosition = new Vector2(0, -52); tr.sizeDelta = new Vector2(-40, 46); }
                else { tr.anchoredPosition = new Vector2(0, -96); tr.sizeDelta = new Vector2(-60, 40); t.fontSize = 26; }
            }
        }
    }

    static void ScoreLine(Stack s, string name)
    {
        var r = Q(name); if (r == null) return;
        s.Put(r, 108);
        var img = r.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = true; img.sprite = sField; img.type = Image.Type.Sliced; img.color = FieldEdge; }
        foreach (var c in Children(r))
        {
            used.Add(c);
            var t = c.GetComponent<TMP_Text>();
            foreach (var le in c.GetComponents<LayoutElement>()) Undo.DestroyObjectImmediate(le);
            if (c.name == "Icon") { c.anchorMin = c.anchorMax = c.pivot = new Vector2(0, 0.5f); c.anchoredPosition = new Vector2(20, 0); c.sizeDelta = new Vector2(76, 76); }
            else if (t != null && t.text.Contains("/"))
            {
                c.anchorMin = c.anchorMax = c.pivot = new Vector2(1, 0.5f); c.anchoredPosition = new Vector2(-24, 0); c.sizeDelta = new Vector2(200, 80);
                StyleText(t, 42, Yellow, FontStyles.Bold); t.alignment = TextAlignmentOptions.MidlineRight; t.textWrappingMode = TextWrappingModes.NoWrap;
            }
            else if (t != null)
            {
                c.anchorMin = new Vector2(0, 0); c.anchorMax = new Vector2(1, 1); c.pivot = new Vector2(0.5f, 0.5f);
                c.offsetMin = new Vector2(116, 6); c.offsetMax = new Vector2(-236, -6);
                StyleText(t, 36, White, FontStyles.Normal); t.alignment = TextAlignmentOptions.MidlineLeft;
                t.enableAutoSizing = true; t.fontSizeMax = 36; t.fontSizeMin = 24;
            }
        }
    }

    static void FeedbackBlock(Stack s, string name, string readButton)
    {
        var r = Q(name); if (r == null) return;
        Attach(r, s.parent);
        var img = r.GetComponent<Image>(); if (img != null) { Undo.RecordObject(img, "IIAD"); img.enabled = true; img.sprite = sCard; img.type = Image.Type.Sliced; img.color = Cyan; }

        var icon = r.Find("Icon") as RectTransform;
        var title = r.Find("Text (TMP)") as RectTransform;
        var read = Q(readButton);
        var sv = r.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();

        if (icon != null) { used.Add(icon); foreach (var le in icon.GetComponents<LayoutElement>()) Undo.DestroyObjectImmediate(le); icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0, 1); icon.anchoredPosition = new Vector2(W_PAD, -36); icon.sizeDelta = new Vector2(84, 84); }
        if (title != null)
        {
            used.Add(title); foreach (var le in title.GetComponents<LayoutElement>()) Undo.DestroyObjectImmediate(le);
            title.anchorMin = new Vector2(0, 1); title.anchorMax = new Vector2(1, 1); title.pivot = new Vector2(0.5f, 1);
            title.offsetMin = new Vector2(W_PAD + 104, 0); title.offsetMax = new Vector2(-(W_PAD + 140), 0);
            title.sizeDelta = new Vector2(title.sizeDelta.x, 84); title.anchoredPosition = new Vector2(title.anchoredPosition.x, -36);
            var t = title.GetComponent<TMP_Text>(); StyleText(t, 44, White, FontStyles.Bold); t.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (read != null)
        {
            Attach(read, r);
            read.anchorMin = read.anchorMax = read.pivot = new Vector2(1, 1); read.anchoredPosition = new Vector2(-W_PAD, -30); read.sizeDelta = new Vector2(IconBtn, IconBtn);
            StyleButton(read.GetComponent<Button>(), K.Icon);
        }
        if (sv != null)
        {
            var svr = (RectTransform)sv.transform; used.Add(svr);
            foreach (var le in svr.GetComponents<LayoutElement>()) Undo.DestroyObjectImmediate(le);
            svr.anchorMin = new Vector2(0, 1); svr.anchorMax = new Vector2(1, 1); svr.pivot = new Vector2(0.5f, 1);
            svr.offsetMin = new Vector2(W_PAD, 0); svr.offsetMax = new Vector2(-W_PAD, 0);
            svr.sizeDelta = new Vector2(svr.sizeDelta.x, 420); svr.anchoredPosition = new Vector2(svr.anchoredPosition.x, -184);
            foreach (var t in sv.GetComponentsInChildren<TMP_Text>(true)) { StyleText(t, 36, White, FontStyles.Normal); t.alignment = TextAlignmentOptions.TopLeft; }
        }
        foreach (var lg in r.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(lg);
        foreach (var c in Children(r)) if (!used.Contains(c)) Leftover(c);
        s.Put(r, 184 + 420 + 40);
    }
}
