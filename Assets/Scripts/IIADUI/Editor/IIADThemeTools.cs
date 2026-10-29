using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Tools > IIAD theme helpers.</summary>
public static class IIADThemeTools
{
    const string Folder = "Assets/Resources";

    [MenuItem("Tools/IIAD/Create Default Themes", priority = 20)]
    static void CreateThemes()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Resources");

        var light = ScriptableObject.CreateInstance<IIADTheme>();   // defaults are the light palette
        var dark = ScriptableObject.CreateInstance<IIADTheme>();
        dark.background = IIADTheme.Hex("#0E1116");
        dark.surface = IIADTheme.Hex("#171B23");
        dark.softSurface = IIADTheme.Hex("#1D222B");
        dark.text = IIADTheme.Hex("#F1F2EE");
        dark.mutedText = IIADTheme.Hex("#A5ABB6");
        dark.line = IIADTheme.Hex("#2C323D");
        dark.ink = IIADTheme.Hex("#F1F2EE");
        dark.onInk = IIADTheme.Hex("#14181F");
        dark.success = IIADTheme.Hex("#3CC79A");
        dark.danger = IIADTheme.Hex("#FF8A70");
        dark.focus = IIADTheme.Hex("#7FA6FF");

        // Use a readable font that is already in the project until Atkinson Hyperlegible is added.
        var lexend = FindFont("Lexend");
        light.bodyFont = dark.bodyFont = lexend;
        light.displayFont = dark.displayFont = lexend;

        Save(light, Folder + "/IIAD Theme.asset");        // loaded automatically at runtime
        Save(dark, Folder + "/IIAD Theme Dark.asset");
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<IIADTheme>(Folder + "/IIAD Theme.asset");
        Debug.Log("IIAD: created 'IIAD Theme' (light, active by default) and 'IIAD Theme Dark' in Assets/Resources." +
                  (lexend == null ? " No Lexend TMP font asset found: set Body Font and Display Font on the theme." : ""));
    }

    static void Save(IIADTheme t, string path)
    {
        if (File.Exists(path)) { Debug.Log("IIAD: kept existing " + path); return; }
        AssetDatabase.CreateAsset(t, path);
    }

    static TMP_FontAsset FindFont(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:TMP_FontAsset"))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }

    [MenuItem("Tools/IIAD/Auto-Assign Theme Roles To Selection", priority = 21)]
    static void AutoAssign()
    {
        int n = 0;
        foreach (var root in Selection.gameObjects)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<IIADThemeApplier>() != null) continue;
                var a = Undo.AddComponent<IIADThemeApplier>(b.gameObject);
                string name = b.name.ToLowerInvariant();
                a.role = name.Contains("delete") ? IIADThemeApplier.Role.DangerButton
                       : name.Contains("mic") ? IIADThemeApplier.Role.DarkButton
                       : name.Contains("generate") || name.Contains("next") || name.Contains("submit") || name.Contains("login") || name.Contains("signin") || name.Contains("start")
                           ? IIADThemeApplier.Role.PrimaryButton
                           : IIADThemeApplier.Role.SecondaryButton;
                a.Apply(); n++;
            }
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.GetComponent<IIADThemeApplier>() != null || t.GetComponentInParent<Button>() != null || t.GetComponentInParent<TMP_InputField>() != null) continue;
                string name = t.name.ToLowerInvariant();
                var a = Undo.AddComponent<IIADThemeApplier>(t.gameObject);
                a.role = name.Contains("title") ? IIADThemeApplier.Role.Title
                       : name.Contains("header") || name.Contains("heading") ? IIADThemeApplier.Role.Heading
                       : name.Contains("status") || name.Contains("hint") || name.Contains("caption") ? IIADThemeApplier.Role.Caption
                       : name.Contains("error") ? IIADThemeApplier.Role.ErrorText
                       : IIADThemeApplier.Role.Body;
                a.Apply(); n++;
            }
            foreach (var f in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (f.GetComponent<IIADThemeApplier>() != null) continue;
                var a = Undo.AddComponent<IIADThemeApplier>(f.gameObject);
                a.role = IIADThemeApplier.Role.SoftSurface; a.Apply(); n++;
            }
        }
        Debug.Log("IIAD: added " + n + " theme appliers. Review the roles in the Inspector.");
    }

    [MenuItem("Tools/IIAD/Auto-Assign Theme Roles To Selection", true)]
    static bool AutoAssignValidate() => Selection.gameObjects.Length > 0;
}
