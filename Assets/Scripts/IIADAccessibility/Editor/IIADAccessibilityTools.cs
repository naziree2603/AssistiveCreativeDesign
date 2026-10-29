using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > IIAD menu:
///   Migrate UAP Components   copies labels from UAP components into IIAD components, then removes UAP ones
///   Add Accessibility To Selection
///   Accessibility Report
/// Works without referencing any UAP type, so it keeps compiling after Assets/UAP is deleted.
/// </summary>
public static class IIADAccessibilityTools
{
    const string UapFolder = "Assets/UAP/";

    // =====================================================================
    // MIGRATE
    // =====================================================================

    [MenuItem("Tools/IIAD/Migrate UAP Components (open scenes + prefabs)", priority = 1)]
    static void MigrateMenu()
    {
        if (!EditorUtility.DisplayDialog("Migrate UAP to IIAD accessibility",
            "This will:\n\n" +
            "1. Add IIAD Accessible Element / Accessible Screen components using your existing UAP labels.\n" +
            "2. Remove every UAP component from the open scenes and from prefabs.\n\n" +
            "Back up or commit your project first. Scene changes can be undone with Ctrl+Z; prefab changes are saved straight away.",
            "Migrate", "Cancel")) return;

        var log = new StringBuilder();
        int added = 0, removed = 0, screens = 0;

        // 1. Prefabs first, so scene instances pick up the changes.
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith(UapFolder) || path.Contains("/TextMesh Pro/")) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var r = MigrateHierarchy(root, false, log);
                if (r.changed) { PrefabUtility.SaveAsPrefabAsset(root, path); log.AppendLine("Prefab updated: " + path); }
                added += r.added; removed += r.removed; screens += r.screens;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // 2. Open scenes.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            bool sceneChanged = false;
            foreach (var go in scene.GetRootGameObjects())
            {
                var r = MigrateHierarchy(go, true, log);
                sceneChanged |= r.changed; added += r.added; removed += r.removed; screens += r.screens;
            }
            if (sceneChanged) EditorSceneManager.MarkSceneDirty(scene);
        }

        string summary = $"IIAD migration: {added} accessible elements added, {screens} screens added, {removed} UAP components removed.";
        Debug.Log(summary + "\n" + log);
        EditorUtility.DisplayDialog("Migration finished", summary +
            "\n\nNext: save the scene, run Tools > IIAD > Accessibility Report, and test on a phone with TalkBack. " +
            "When everything works, delete the Assets/UAP folder.", "OK");
    }

    struct Result { public bool changed; public int added, removed, screens; }

    static bool IsUap(MonoBehaviour mb)
    {
        if (mb == null) return false;
        var script = MonoScript.FromMonoBehaviour(mb);
        string p = script != null ? AssetDatabase.GetAssetPath(script) : "";
        return p.StartsWith(UapFolder) || mb.GetType().Name.StartsWith("UAP_");
    }

    static Result MigrateHierarchy(GameObject root, bool inScene, StringBuilder log)
    {
        var res = new Result();
        var uap = root.GetComponentsInChildren<MonoBehaviour>(true).Where(IsUap).ToList();

        foreach (var c in uap)
        {
            string type = c.GetType().Name;
            var so = new SerializedObject(c);

            if (type == "AccessibleUIGroupRoot")
            {
                if (c.GetComponent<IIADScreen>() == null)
                {
                    var s = inScene ? Undo.AddComponent<IIADScreen>(c.gameObject) : c.gameObject.AddComponent<IIADScreen>();
                    string containerName = Str(so, "m_ContainerName");
                    s.title = string.IsNullOrWhiteSpace(containerName) ? Pretty(c.gameObject.name) : containerName;
                    s.isModal = Bool(so, "m_PopUp");
                    res.screens++; res.changed = true;
                }
            }
            else if (type.StartsWith("Accessible") && type != "AccessiblePluginToggle")
            {
                var refObj = Obj(so, "m_ReferenceElement") as GameObject;
                var target = refObj != null ? refObj : c.gameObject;
                if (target.GetComponent<IIADAccessibleElement>() == null)
                {
                    var e = inScene ? Undo.AddComponent<IIADAccessibleElement>(target) : target.AddComponent<IIADAccessibleElement>();
                    string text = Str(so, "m_Text");
                    string prefix = Str(so, "m_Prefix");
                    bool tryRead = Bool(so, "m_TryToReadLabel");
                    var nameLabel = Obj(so, "m_NameLabel") as GameObject;
                    TMP_Text labelText = nameLabel != null ? nameLabel.GetComponent<TMP_Text>() : null;

                    if (tryRead && labelText != null) e.labelText = labelText;
                    else if (!string.IsNullOrWhiteSpace(text)) e.label = (prefix + " " + text).Trim();
                    else if (labelText != null) e.labelText = labelText;

                    if (Bool(so, "m_CustomHint")) e.hint = Str(so, "m_Hint");
                    int order = Int(so, "m_ManualPositionOrder", -1);
                    e.focusOrder = order >= 0 ? order : -1;
                    e.focusFirst = Bool(so, "m_ForceStartHere");
                    if (type.StartsWith("AccessibleLabel")) e.role = IIADRole.Text;
                    res.added++; res.changed = true;
                    EditorUtility.SetDirty(e);
                }
            }

            // Remove the UAP component (skip parts of a prefab instance: the prefab itself was migrated).
            if (inScene && PrefabUtility.IsPartOfPrefabInstance(c) && !PrefabUtility.IsAddedComponentOverride(c))
                continue;
            log.AppendLine("Removed " + type + " from " + PathOf(c.transform));
            if (inScene) Undo.DestroyObjectImmediate(c); else Object.DestroyImmediate(c, true);
            res.removed++; res.changed = true;
        }
        return res;
    }

    // =====================================================================
    // ADD TO SELECTION
    // =====================================================================

    [MenuItem("Tools/IIAD/Add Accessibility To Selection", priority = 2)]
    static void AddToSelection()
    {
        int added = 0;
        foreach (var go in Selection.gameObjects)
        {
            string n = go.name.ToLowerInvariant();
            if (go.GetComponent<IIADScreen>() == null && (n.Contains("panel") || n.Contains("popup") || n.Contains("page")))
            {
                var s = Undo.AddComponent<IIADScreen>(go);
                s.title = Pretty(go.name);
                s.isModal = n.Contains("popup");
                added++;
            }
            foreach (var sel in go.GetComponentsInChildren<Selectable>(true))
            {
                if (sel.GetComponent<IIADAccessibleElement>() != null) continue;
                Undo.AddComponent<IIADAccessibleElement>(sel.gameObject);
                added++;
            }
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            {
                string tn = t.gameObject.name.ToLowerInvariant();
                if (!tn.Contains("title") && !tn.Contains("header") && !tn.Contains("heading")) continue;
                if (t.GetComponent<IIADAccessibleElement>() != null || t.GetComponentInParent<Selectable>() != null) continue;
                var e = Undo.AddComponent<IIADAccessibleElement>(t.gameObject);
                e.role = IIADRole.Header;
                added++;
            }
        }
        Debug.Log("IIAD: added " + added + " accessibility components. Check labels in the Inspector.");
    }

    [MenuItem("Tools/IIAD/Add Accessibility To Selection", true)]
    static bool AddToSelectionValidate() => Selection.gameObjects.Length > 0;

    // =====================================================================
    // REPORT
    // =====================================================================

    [MenuItem("Tools/IIAD/Accessibility Report (open scenes)", priority = 3)]
    static void Report()
    {
        int problems = 0;
        var objects = new List<GameObject>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var sc = SceneManager.GetSceneAt(i);
            if (sc.isLoaded) objects.AddRange(sc.GetRootGameObjects());
        }

        foreach (var root in objects)
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (IsUap(mb)) { problems++; Debug.LogWarning("Still uses UAP: " + PathOf(mb.transform), mb); }

            foreach (var sel in root.GetComponentsInChildren<Selectable>(true))
            {
                if (sel is Scrollbar) continue;
                if (sel.GetComponent<IIADAccessibleElement>() == null)
                { problems++; Debug.LogWarning("No accessible element (TalkBack cannot reach it): " + PathOf(sel.transform), sel); }

                var rt = (RectTransform)sel.transform;
                var canvas = sel.GetComponentInParent<CanvasScaler>(true);
                float minSize = 44f * (canvas != null && canvas.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize ? canvas.referenceResolution.x / 360f : 1f);
                if (rt.rect.width > 0 && (rt.rect.width < minSize || rt.rect.height < minSize))
                { problems++; Debug.LogWarning($"Touch target smaller than 44 dp ({rt.rect.width:0}x{rt.rect.height:0}): " + PathOf(sel.transform), sel); }
            }

            foreach (var e in root.GetComponentsInChildren<IIADAccessibleElement>(true))
            {
                string label = e.GetLabel();
                if (string.IsNullOrWhiteSpace(label) || label == e.gameObject.name)
                { problems++; Debug.LogWarning("Label missing (would read the object name '" + e.gameObject.name + "'): " + PathOf(e.transform), e); }
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if ((n.EndsWith("panel") || n.EndsWith("popup")) && t.GetComponent<IIADScreen>() == null && t.GetComponentInChildren<Selectable>(true) != null)
                { problems++; Debug.LogWarning("Panel without IIAD Accessible Screen (focus will not move when it opens): " + PathOf(t), t); }
            }
        }

        string msg = problems == 0 ? "IIAD Accessibility Report: no problems found." : $"IIAD Accessibility Report: {problems} items to fix. Click each warning in the Console to select the object.";
        Debug.Log(msg);
        EditorUtility.DisplayDialog("Accessibility Report", msg, "OK");
    }

    // =====================================================================
    // helpers
    // =====================================================================

    static string Str(SerializedObject so, string name) { var p = so.FindProperty(name); return p != null && p.propertyType == SerializedPropertyType.String ? p.stringValue : ""; }
    static bool Bool(SerializedObject so, string name) { var p = so.FindProperty(name); return p != null && p.propertyType == SerializedPropertyType.Boolean && p.boolValue; }
    static int Int(SerializedObject so, string name, int def) { var p = so.FindProperty(name); return p != null && p.propertyType == SerializedPropertyType.Integer ? p.intValue : def; }
    static Object Obj(SerializedObject so, string name) { var p = so.FindProperty(name); return p != null && p.propertyType == SerializedPropertyType.ObjectReference ? p.objectReferenceValue : null; }

    static string PathOf(Transform t)
    {
        var sb = new StringBuilder(t.name);
        while ((t = t.parent) != null) sb.Insert(0, t.name + "/");
        return sb.ToString();
    }

    /// <summary>"IdeaPromptPanel" -> "Idea prompt"</summary>
    static string Pretty(string name)
    {
        string n = name.Replace("_", " ");
        foreach (var w in new[] { "Panel", "panel", "Popup", "popup", "Page", "page" }) n = n.Replace(w, "");
        var sb = new StringBuilder();
        for (int i = 0; i < n.Length; i++)
        {
            char ch = n[i];
            if (i > 0 && char.IsUpper(ch) && char.IsLower(n[i - 1])) sb.Append(' ');
            sb.Append(sb.Length == 0 ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch));
        }
        return sb.ToString().Trim();
    }
}
