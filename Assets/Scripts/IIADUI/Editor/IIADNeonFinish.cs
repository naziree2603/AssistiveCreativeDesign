using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > IIAD > Finish Neon UI (icons + no-scroll menus)
/// Works on the scene as it is now (no revert needed):
///   1. Main menu, Welcome, Settings, Splash and Sample prompts fit on one page with no scrolling
///      (content shrinks evenly on short screens), and the main menu logo area is made smaller.
///   2. Every old icon is replaced with the neon line icons.
/// One Undo step for the layout part.
/// </summary>
public static class IIADNeonFinish
{
    static readonly string[] NoScrollScreens = { "MainDashboard", "WelcomeScreen", "SettingsPanel", "SplashScreen", "SamplePromptPopUp" };
    const float MenuButtonH = 170f, MenuGap = 26f;

    [MenuItem("Tools/IIAD/Finish Neon UI (icons + no-scroll menus)", priority = 39)]
    static void Run()
    {
        var container = Find("ScreenContainer");
        if (container == null) { EditorUtility.DisplayDialog("Finish Neon UI", "Open Assets/Scenes/UI/MainUI.unity first.", "OK"); return; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.RegisterFullObjectHierarchyUndo(container.gameObject, "IIAD Finish");

        int fitted = 0;
        foreach (var name in NoScrollScreens)
        {
            var screen = container.Find(name);
            if (screen == null) continue;
            if (MakeNoScroll(screen)) fitted++;
        }
        TidyMainMenu(container.Find("MainDashboard"));

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(container.gameObject.scene);

        int icons = IIADNeonIcons.ApplyToOpenScenes();
        Canvas.ForceUpdateCanvases();
        EditorUtility.DisplayDialog("Finish Neon UI",
            $"{fitted} screens now fit without scrolling.\n{icons} icons replaced.\n\nSave the scene to keep it.", "OK");
    }

    /// <summary>Turns off scrolling and adds the fit-to-screen helper. Used by Build Neon UI too.</summary>
    public static bool MakeNoScroll(Transform screen)
    {
        var content = screen.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Content" && t.parent != null && t.parent.name == "Viewport" && t.parent.parent != null && t.parent.parent.name == "IIAD_Content");
        if (content == null) return false;
        var sr = content.GetComponentInParent<ScrollRect>(true);
        if (sr != null) { Undo.RecordObject(sr, "IIAD"); sr.vertical = false; sr.horizontal = false; }
        var fit = content.GetComponent<IIADFitToViewport>();
        if (fit == null) fit = Undo.AddComponent<IIADFitToViewport>(content.gameObject);
        fit.viewport = content.parent as RectTransform;
        return true;
    }

    /// <summary>Smaller logo block and evenly sized menu buttons, for both layout versions.</summary>
    static void TidyMainMenu(Transform dash)
    {
        if (dash == null) return;

        // First tool version: "Logo" container with the badge and title placed freely inside
        var logo = dash.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Logo" && t.Find("LogoImage") != null);
        if (logo != null && logo.GetComponentInParent<LayoutGroup>() != null && logo.GetComponent<LayoutGroup>() == null)
        {
            logo.sizeDelta = new Vector2(980, 440);
            Place(logo.Find("LogoImage") as RectTransform, new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(260, 260));
            Place(logo.Find("LogoTitle") as RectTransform, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(780, 140));
            var le = logo.GetComponent<LayoutElement>(); if (le != null) { le.preferredHeight = 440; le.minHeight = 440; }
        }

        // Menu buttons
        var groupRT = dash.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "GroupButtons");
        if (groupRT == null) return;
        var vl = groupRT.GetComponent<VerticalLayoutGroup>();
        if (vl != null) { vl.spacing = MenuGap; vl.padding = new RectOffset(0, 0, 0, 0); vl.childAlignment = TextAnchor.UpperCenter; }
        int active = 0;
        foreach (RectTransform b in groupRT)
        {
            if (b.GetComponent<Button>() == null) continue;
            b.sizeDelta = new Vector2(Mathf.Max(b.sizeDelta.x, 880), MenuButtonH);
            var le = b.GetComponent<LayoutElement>(); if (le != null) { le.preferredHeight = MenuButtonH; le.minHeight = MenuButtonH; }
            if (b.gameObject.activeSelf) active++;
        }
        bool parentControls = groupRT.parent != null && groupRT.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() is HorizontalOrVerticalLayoutGroup p && p.childControlHeight;
        if (!parentControls) groupRT.sizeDelta = new Vector2(Mathf.Max(groupRT.sizeDelta.x, 900), active * MenuButtonH + Mathf.Max(0, active - 1) * MenuGap);
    }

    static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        if (r == null) return;
        r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, anchor.y);
        r.anchoredPosition = pos; r.sizeDelta = size;
        var img = r.GetComponent<Image>(); if (img != null) img.preserveAspect = true;
    }

    static Transform Find(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
