using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > IIAD > Apply Neon Icons
/// Replaces the old colourful icons with neon line icons that match the buttons:
/// a dark round badge with a cyan ring (coral for exit / log out / delete) and a line glyph.
/// Icons are chosen by the object's name (AccessibilityButton, PracticeModeButton, MicButton...).
/// Also sets the eye / eye-off sprites used by the password show/hide scripts. One Undo step.
/// </summary>
public static class IIADNeonIcons
{
    const string Folder = "Assets/Arts/NEW UI/IIAD Neon/Icons/";
    const string CirclePath = "Assets/Arts/NEW UI/IIAD Neon/iiad_circle.png";
    static readonly Color Cyan = new Color32(0x3C, 0xC8, 0xFF, 0xFF);
    static readonly Color Coral = new Color32(0xFF, 0x6B, 0x5B, 0xFF);

    // object name (lower case, contains) -> icon file, danger?
    // checked in order: specific names first ("feedbackbox" must not match "back")
    static readonly (string key, string icon, bool danger)[] Rules =
    {
        ("readfeedback", "speaker", false), ("readimprovement", "speaker", false), ("readscore", "speaker", false),
        ("replay", "speaker", false), ("repeat", "speaker", false), ("viewposter", "poster", false), ("fullscreen", "poster", false),
        ("feedbackbox", "feedback", false), ("improvementbox", "improve", false),
        ("accessibilityrow", "accessibility", false), ("row", "star", false),
        ("accessibility", "accessibility", false), ("setting", "settings", false), ("profile", "profile", false),
        ("logout", "logout", true), ("exitapp", "exit", true), ("exitbutton", "exit", true),
        ("backbutton", "back", false), ("homebutton", "home", false),
        ("practice", "practice", false), ("challengemode", "challenge", false), ("submittedbutton", "submitted", false),
        ("howtoplay", "howto", false), ("about", "about", false), ("privacy", "privacy", false), ("delete", "delete", true),
        ("mic", "mic", false), ("tips", "tips", false), ("attemp", "improve", false),
        ("selectev", "event", false), ("entercode", "code", false), ("eye", "eye", false),
    };

    [MenuItem("Tools/IIAD/Apply Neon Icons", priority = 42)]
    static void Menu()
    {
        int n = ApplyToOpenScenes();
        EditorUtility.DisplayDialog("Neon icons", $"Updated {n} icons. Save the scene to keep them. Ctrl+Z undoes this.", "OK");
    }

    public static int ApplyToOpenScenes()
    {
        // keep the existing circle sprite as it is (buttons already point to it; re-importing would break them)
        var circle = AssetDatabase.LoadAllAssetsAtPath(CirclePath).OfType<Sprite>().FirstOrDefault();
        if (circle == null) { Debug.LogWarning("IIAD: run Build Neon UI once first (it creates iiad_circle.png)."); return 0; }
        var cache = new Dictionary<string, Sprite>();
        Sprite Glyph(string n) { if (!cache.TryGetValue(n, out var s)) cache[n] = s = Load(Folder + "iiad_icon_" + n + ".png", 256); return s; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        int count = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                // 1. Buttons whose own image is the icon (header buttons, read buttons, eye)
                foreach (var b in root.GetComponentsInChildren<Button>(true))
                {
                    var bn = b.name.ToLowerInvariant();
                    var rule = Match(bn);
                    if (rule == null) continue;
                    var img = b.GetComponent<Image>();
                    if (img == null) continue;
                    bool hasText = b.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => !string.IsNullOrWhiteSpace(t.text));
                    var glyphSprite = Glyph(rule.Value.icon);
                    if (glyphSprite == null) continue;

                    if (bn.Contains("eye"))
                    {
                        Undo.RecordObject(img, "IIAD Icons");
                        img.sprite = glyphSprite; img.color = Cyan; img.preserveAspect = true; img.type = Image.Type.Simple;
                        count++; continue;
                    }
                    if (hasText) continue;   // text buttons get their icon through an "Icon" child (step 2)

                    Undo.RecordObject(img, "IIAD Icons");
                    var holder = b.transform.Find("IIAD_Icon") as RectTransform;
                    if (holder == null)
                    {
                        var go = new GameObject("IIAD_Icon", typeof(RectTransform), typeof(Image));
                        Undo.RegisterCreatedObjectUndo(go, "IIAD Icons");
                        holder = (RectTransform)go.transform; holder.SetParent(b.transform, false);
                        holder.anchorMin = Vector2.zero; holder.anchorMax = Vector2.one;
                        holder.offsetMin = new Vector2(30, 30); holder.offsetMax = new Vector2(-30, -30);
                    }
                    img.sprite = circle; img.type = Image.Type.Simple; img.preserveAspect = true;
                    img.color = rule.Value.danger ? Coral : Cyan;
                    SetGlyph(holder.GetComponent<Image>(), glyphSprite, rule.Value.danger ? Coral : Color.white);
                    count++;
                }

                // 2. "Icon" images inside buttons, cards and score rows
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                {
                    string n = img.name.ToLowerInvariant();
                    if (!(n == "icon" || n.EndsWith("icon")) || n == "iiad_icon") continue;
                    if (img.GetComponentInParent<TMPro.TMP_InputField>(true) != null) continue;
                    var owner = n == "icon" && img.transform.parent != null ? img.transform.parent.name.ToLowerInvariant() : n;
                    var rule = Match(owner);
                    if (rule == null) continue;
                    var glyphSprite = Glyph(rule.Value.icon);
                    if (glyphSprite == null) continue;
                    Color c = rule.Value.danger ? Coral : Cyan;

                    Undo.RecordObject(img, "IIAD Icons");
                    img.sprite = circle; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = c;
                    var g = img.transform.Find("IIAD_Glyph") as RectTransform;
                    if (g == null)
                    {
                        var go = new GameObject("IIAD_Glyph", typeof(RectTransform), typeof(Image));
                        Undo.RegisterCreatedObjectUndo(go, "IIAD Icons");
                        g = (RectTransform)go.transform; g.SetParent(img.transform, false);
                        g.anchorMin = new Vector2(0.22f, 0.22f); g.anchorMax = new Vector2(0.78f, 0.78f);
                        g.offsetMin = g.offsetMax = Vector2.zero;
                    }
                    SetGlyph(g.GetComponent<Image>(), glyphSprite, c);
                    count++;
                }

                // 3. Password show / hide scripts swap the eye sprite at runtime
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    var so = new SerializedObject(mb);
                    bool changed = false;
                    changed |= SetSprite(so, "eyeOpenSprite", Glyph("eye"));
                    changed |= SetSprite(so, "eyeClosedSprite", Glyph("eyeoff"));
                    changed |= SetSprite(so, "showSprite", Glyph("eye"));
                    changed |= SetSprite(so, "hideSprite", Glyph("eyeoff"));
                    if (changed) { so.ApplyModifiedProperties(); count++; }
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }
        Undo.CollapseUndoOperations(group);
        Debug.Log("IIAD: neon icons applied to " + count + " items.");
        return count;
    }

    static (string key, string icon, bool danger)? Match(string name)
    {
        foreach (var r in Rules) if (name.Contains(r.key)) return r;
        return null;
    }

    static void SetGlyph(Image g, Sprite s, Color c)
    {
        Undo.RecordObject(g, "IIAD Icons");
        g.sprite = s; g.color = c; g.preserveAspect = true; g.raycastTarget = false; g.type = Image.Type.Simple;
    }

    static bool SetSprite(SerializedObject so, string field, Sprite s)
    {
        var p = so.FindProperty(field);
        if (p == null || p.propertyType != SerializedPropertyType.ObjectReference || s == null) return false;
        if (p.objectReferenceValue == s) return false;
        p.objectReferenceValue = s;
        return true;
    }

    /// <summary>Imports a PNG as ONE sprite (the project otherwise auto-slices new PNGs into pieces).</summary>
    public static Sprite Load(string path, int maxSize)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;
        var st = new TextureImporterSettings();
        imp.ReadTextureSettings(st);
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || st.spriteMeshType != SpriteMeshType.FullRect || imp.maxTextureSize != maxSize)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Trilinear;
            imp.maxTextureSize = maxSize;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteMode = (int)SpriteImportMode.Single;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
