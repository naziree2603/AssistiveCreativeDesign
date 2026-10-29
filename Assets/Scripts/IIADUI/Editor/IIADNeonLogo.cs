using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > IIAD > Apply Neon Logo
/// Swaps the old app logo (shield badge) and the old "AI ASSISTIVE DESIGN" title art for the
/// neon versions, everywhere they are used: open scenes and prefabs. One Undo step for the scene.
/// The Goolee company logo is not touched.
/// </summary>
public static class IIADNeonLogo
{
    const string NewLogo = "Assets/Arts/NEW UI/IIAD Neon/iiad_app_logo.png";
    const string NewWordmark = "Assets/Arts/NEW UI/IIAD Neon/iiad_app_wordmark.png";
    const string OldLogoGuid = "476e1871df3002f4abb03844f11441e6";       // Arts/Sprites/6425bfc3-...png
    const string OldWordmarkGuid = "4f345171a3d621a499a1c5c6c318cff0";   // Arts/NEW UI/ChatGPT Image Aug 16 ... 03_18_48 PM.png

    [MenuItem("Tools/IIAD/Apply Neon Logo", priority = 41)]
    static void Menu()
    {
        int n = ApplyToOpenScenes(true);
        EditorUtility.DisplayDialog("Neon logo", n > 0
            ? $"Replaced the logo on {n} images (scene and prefabs). Save the scene to keep it. Ctrl+Z undoes the scene changes."
            : "No old logo images found in the open scene or prefabs.", "OK");
    }

    /// <summary>Returns how many images were changed.</summary>
    public static int ApplyToOpenScenes(bool includePrefabs)
    {
        var logo = LoadSprite(NewLogo, 1024);
        var word = LoadSprite(NewWordmark, 2048);
        if (logo == null || word == null) { Debug.LogWarning("IIAD: neon logo files are missing in Assets/Arts/NEW UI/IIAD Neon."); return 0; }

        int count = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                    if (Swap(img, logo, word, true)) { count++; changed = true; }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
        }

        if (includePrefabs)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/UAP/") || path.Contains("/TextMesh Pro/")) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                try
                {
                    foreach (var img in root.GetComponentsInChildren<Image>(true))
                        if (Swap(img, logo, word, false)) { count++; changed = true; }
                    if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        Debug.Log("IIAD: neon logo applied to " + count + " images.");
        return count;
    }

    static bool Swap(Image img, Sprite logo, Sprite word, bool undo)
    {
        if (img == null || img.sprite == null) return false;
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(img.sprite));
        Sprite target = guid == OldLogoGuid ? logo : guid == OldWordmarkGuid ? word : null;
        if (target == null) return false;
        if (undo) Undo.RecordObject(img, "IIAD Neon Logo");
        img.sprite = target;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = Color.white;
        return true;
    }

    static Sprite LoadSprite(string path, int maxSize)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;
        if (imp.textureType != TextureImporterType.Sprite || imp.maxTextureSize != maxSize)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;                 // stays smooth when shown small
            imp.filterMode = FilterMode.Trilinear;
            imp.maxTextureSize = maxSize;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
