using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > IIAD > Fix Challenge Panel
/// Rebuilds the "join an event" screen with fixed positions (no layout groups, no scrolling):
///
///   [<-]                                   header
///   ┌──────────── card ────────────┐
///   │         JOIN A CHALLENGE      │
///   │ Pick your event, then type    │
///   │ the code from your organiser. │
///   │ EVENT                         │
///   │ [ Kempen Kitar Semula   ▾ ]   │
///   │ EVENT CODE                    │
///   │ [ Enter event code        ]   │
///   │        (error message)        │
///   │ [        JOIN EVENT        ]  │  yellow
///   └───────────────────────────────┘
///
/// Re-uses the existing dropdown, input field, button and message text, so CompetitionManager keeps working.
/// Works on the original scene and on any earlier restyle. One Undo step.
/// </summary>
public static class IIADChallengePanelFix
{
    static readonly Color Navy = new Color32(0x0B, 0x16, 0x30, 0xFF);
    static readonly Color Cyan = new Color32(0x3C, 0xC8, 0xFF, 0xFF);
    static readonly Color FieldEdge = new Color32(0x7D, 0xB7, 0xE8, 0xFF);
    static readonly Color Yellow = new Color32(0xFF, 0xC2, 0x33, 0xFF);
    static readonly Color Coral = new Color32(0xFF, 0x6B, 0x5B, 0xFF);
    static readonly Color Muted = new Color32(0xA9, 0xBC, 0xD9, 0xFF);
    const string Neon = "Assets/Arts/NEW UI/IIAD Neon/";

    const float CardW = 960f, Pad = 56f, LabelH = 56f, FieldH = 132f, BtnH = 156f;

    [MenuItem("Tools/IIAD/Fix Challenge Panel", priority = 45)]
    static void Menu()
    {
        if (Apply()) EditorUtility.DisplayDialog("Challenge panel", "Challenge panel rebuilt. Save the scene to keep it (Ctrl+Z undoes it).", "OK");
        else EditorUtility.DisplayDialog("Challenge panel", "Open MainUI first (ChallengePanel / ChallengeBackground not found).", "OK");
    }

    public static bool Apply()
    {
        var panel = Find("ChallengePanel") as RectTransform;
        if (panel == null) return false;
        var card = panel.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "ChallengeBackground");
        if (card == null) return false;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "IIAD Challenge Panel");

        var font = AssetDatabase.FindAssets("Lexend t:TMP_FontAsset").Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
        var sCard = Sprite(Neon + "iiad_card.png");
        var sField = Sprite(Neon + "iiad_field.png");
        var sFill = Sprite(Neon + "iiad_button_fill.png");

        // ---- 1. card back directly under the panel; drop any scroll / layout wrappers
        if (card.parent != panel) Undo.SetTransformParent(card, panel, "IIAD");
        foreach (var wrap in Children(panel).Where(c => c.name == "IIAD_Content").ToList()) Undo.DestroyObjectImmediate(wrap.gameObject);
        StripLayout(card);
        card.localScale = Vector3.one;
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f); card.pivot = new Vector2(0.5f, 1f);
        card.anchoredPosition = new Vector2(0, -250); // below the 190 header
        var cardImg = card.GetComponent<Image>();
        if (cardImg != null && sCard != null) { cardImg.sprite = sCard; cardImg.type = Image.Type.Sliced; cardImg.pixelsPerUnitMultiplier = 1; cardImg.color = Cyan; cardImg.enabled = true; }

        // ---- 2. find the pieces wherever an earlier restyle left them
        var title = Child<TMP_Text>(card, "ChallengeText");
        var selectGroup = Rect(card, "SelectEvent");
        var codeGroup = Rect(card, "EnterCode");
        var dropdown = card.GetComponentInChildren<TMP_Dropdown>(true);
        var input = card.GetComponentsInChildren<TMP_InputField>(true).FirstOrDefault();
        var message = Child<TMP_Text>(card, "MessageText");
        var join = Rect(card, "JoinEventButton");
        var eventLabel = Child<TMP_Text>(card, "SelectEventLabel");
        var codeLabel = Child<TMP_Text>(card, "EnterCodeLabel");

        // put the groups and loose items directly in the card
        foreach (var t in new Transform[] { title ? title.transform : null, selectGroup, codeGroup, message ? message.transform : null, join })
            if (t != null && t.parent != card) Undo.SetTransformParent(t, card, "IIAD");
        foreach (var g in new[] { selectGroup, codeGroup }) if (g != null) StripLayout(g);

        // hide decorative icons inside the groups
        foreach (var n in new[] { "SelectEvenIcon", "EnterCodeIcon" })
        { var ic = Rect(card, n); if (ic != null) ic.gameObject.SetActive(false); }

        // ---- 3. fixed positions, top to bottom
        float y = -48;
        if (title != null)
        {
            title.text = "JOIN A CHALLENGE";
            Text(title, font, 60, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            Top((RectTransform)title.transform, y, 96); y -= 96 + 8;
        }
        var sub = Rect(card, "IIAD_Subtitle");
        if (sub == null)
        {
            var go = new GameObject("IIAD_Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "IIAD");
            sub = (RectTransform)go.transform; sub.SetParent(card, false);
        }
        var subText = sub.GetComponent<TMP_Text>();
        subText.text = "Pick your event, then type the code from your organiser.";
        Text(subText, font, 38, Muted, FontStyles.Normal, TextAlignmentOptions.Center);
        subText.raycastTarget = false;
        Top(sub, y, 104); y -= 104 + 28;

        if (selectGroup != null)
        {
            Top(selectGroup, y, LabelH + 12 + FieldH);
            if (eventLabel != null) { if (eventLabel.transform.parent != selectGroup) Undo.SetTransformParent(eventLabel.transform, selectGroup, "IIAD"); eventLabel.text = "EVENT"; Text(eventLabel, font, 40, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft); Top((RectTransform)eventLabel.transform, 0, LabelH, 0); }
            if (dropdown != null) { if (dropdown.transform.parent != selectGroup) Undo.SetTransformParent(dropdown.transform, selectGroup, "IIAD"); Top((RectTransform)dropdown.transform, -(LabelH + 12), FieldH, 0); StyleDropdown(dropdown, sField, sCard, font); }
            y -= LabelH + 12 + FieldH + 36;
        }
        if (codeGroup != null)
        {
            Top(codeGroup, y, LabelH + 12 + FieldH);
            if (codeLabel != null) { if (codeLabel.transform.parent != codeGroup) Undo.SetTransformParent(codeLabel.transform, codeGroup, "IIAD"); codeLabel.text = "EVENT CODE"; Text(codeLabel, font, 40, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft); Top((RectTransform)codeLabel.transform, 0, LabelH, 0); }
            if (input != null) { if (input.transform.parent != codeGroup) Undo.SetTransformParent(input.transform, codeGroup, "IIAD"); Top((RectTransform)input.transform, -(LabelH + 12), FieldH, 0); StyleInput(input, sField, font); }
            y -= LabelH + 12 + FieldH + 20;
        }
        if (message != null)
        {
            Text(message, font, 36, Coral, FontStyles.Normal, TextAlignmentOptions.Center);
            Top((RectTransform)message.transform, y, 70); y -= 70 + 16;
        }
        if (join != null)
        {
            Top(join, y, BtnH); y -= BtnH;
            var bi = join.GetComponent<Image>();
            if (bi != null && sFill != null) { bi.sprite = sFill; bi.type = Image.Type.Sliced; bi.color = Yellow; bi.enabled = true; }
            foreach (var t in join.GetComponentsInChildren<TMP_Text>(true))
            {
                Text(t, font, 48, Navy, FontStyles.Bold, TextAlignmentOptions.Center);
                var r = (RectTransform)t.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            }
        }
        card.sizeDelta = new Vector2(CardW, -y + Pad);

        // anything else left in the card that is not part of the design: out of the way
        foreach (var c in Children(card))
            if (c != sub && c != selectGroup && c != codeGroup && c != join && (title == null || c != title.transform) && (message == null || c != message.transform))
                if (c.GetComponentInChildren<Selectable>(true) == null && c.GetComponentInChildren<TMP_Text>(true) == null) c.gameObject.SetActive(false);

        // ---- 4. labels for TalkBack / Voice Guide
        Label(dropdown ? dropdown.gameObject : null, "Event", "Double-tap to choose an event");
        Label(input ? input.gameObject : null, "Event code", "Double-tap to type the code from your organiser");
        Label(join ? join.gameObject : null, "Join event", "");

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Canvas.ForceUpdateCanvases();
        return true;
    }

    // ------------------------------------------------------------------ styling
    static void StyleDropdown(TMP_Dropdown d, Sprite field, Sprite card, TMP_FontAsset font)
    {
        Undo.RecordObject(d, "IIAD");
        var img = d.GetComponent<Image>(); if (img != null && field != null) { img.sprite = field; img.type = Image.Type.Sliced; img.color = FieldEdge; }
        if (d.captionText != null)
        {
            Text(d.captionText, font, 44, Color.white, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            var r = d.captionText.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(32, 8); r.offsetMax = new Vector2(-100, -8);
        }
        var arrow = d.transform.Find("Arrow") as RectTransform;
        if (arrow != null)
        {
            arrow.anchorMin = arrow.anchorMax = new Vector2(1, 0.5f); arrow.pivot = new Vector2(1, 0.5f);
            arrow.anchoredPosition = new Vector2(-32, 0); arrow.sizeDelta = new Vector2(44, 44);
            var ai = arrow.GetComponent<Image>(); if (ai != null) ai.color = Cyan;
        }
        if (d.template != null)
        {
            var t = d.template;
            t.anchorMin = new Vector2(0, 0); t.anchorMax = new Vector2(1, 0); t.pivot = new Vector2(0.5f, 1);
            t.anchoredPosition = new Vector2(0, -8); t.sizeDelta = new Vector2(0, 480);
            var ti = t.GetComponent<Image>(); if (ti != null && card != null) { ti.sprite = card; ti.type = Image.Type.Sliced; ti.color = Cyan; }
            var item = t.GetComponentInChildren<Toggle>(true);
            if (item != null)
            {
                var ir = (RectTransform)item.transform; ir.sizeDelta = new Vector2(ir.sizeDelta.x, 120);
                if (item.targetGraphic is Image bg) bg.color = new Color32(0x10, 0x21, 0x3F, 0xFF);
                var content = ir.parent as RectTransform; if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, 128);
            }
            if (d.itemText != null)
            {
                Text(d.itemText, font, 42, Color.white, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
                var r = d.itemText.rectTransform; r.offsetMin = new Vector2(80, r.offsetMin.y);
            }
        }
    }

    static void StyleInput(TMP_InputField f, Sprite field, TMP_FontAsset font)
    {
        Undo.RecordObject(f, "IIAD");
        var img = f.GetComponent<Image>(); if (img != null && field != null) { img.sprite = field; img.type = Image.Type.Sliced; img.color = FieldEdge; }
        if (f.textViewport != null) { var r = f.textViewport; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(32, 10); r.offsetMax = new Vector2(-32, -10); }
        if (f.textComponent != null) { Text(f.textComponent, font, 44, Color.white, FontStyles.Normal, TextAlignmentOptions.MidlineLeft); f.textComponent.enableAutoSizing = false; }
        if (f.placeholder is TMP_Text ph) { ph.text = "Enter event code"; Text(ph, font, 42, new Color(Muted.r, Muted.g, Muted.b, 0.75f), FontStyles.Italic, TextAlignmentOptions.MidlineLeft); }
        f.customCaretColor = true; f.caretColor = Cyan; f.caretWidth = 3;
        f.selectionColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
        f.lineType = TMP_InputField.LineType.SingleLine;
    }

    static void Text(TMP_Text t, TMP_FontAsset font, float size, Color c, FontStyles style, TextAlignmentOptions align)
    {
        if (t == null) return;
        Undo.RecordObject(t, "IIAD");
        if (font != null) t.font = font;
        t.fontSize = size; t.enableAutoSizing = false; t.color = c; t.fontStyle = style; t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.gameObject.SetActive(true);
    }

    static void Label(GameObject go, string label, string hint)
    {
        if (go == null) return;
        var e = go.GetComponent<IIADAccessibleElement>() ?? Undo.AddComponent<IIADAccessibleElement>(go);
        e.label = label; e.hint = hint;
    }

    // ------------------------------------------------------------------ layout helpers
    /// <summary>Anchor to the top of the parent, full width minus side padding.</summary>
    static void Top(RectTransform r, float y, float h, float side = Pad)
    {
        r.gameObject.SetActive(true);
        r.localScale = Vector3.one;
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1);
        r.offsetMin = new Vector2(side, 0); r.offsetMax = new Vector2(-side, 0);
        r.anchoredPosition = new Vector2(0, y); r.sizeDelta = new Vector2(-side * 2, h);
        var le = r.GetComponent<LayoutElement>(); if (le != null) Undo.DestroyObjectImmediate(le);
    }

    static void StripLayout(Transform t)
    {
        foreach (var c in t.GetComponents<LayoutGroup>()) Undo.DestroyObjectImmediate(c);
        foreach (var c in t.GetComponents<ContentSizeFitter>()) Undo.DestroyObjectImmediate(c);
        foreach (var c in t.GetComponents<LayoutElement>()) Undo.DestroyObjectImmediate(c);
    }

    static RectTransform Rect(Transform root, string name) => root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == name);
    static T Child<T>(Transform root, string name) where T : Component { var r = Rect(root, name); return r != null ? r.GetComponent<T>() : null; }
    static System.Collections.Generic.List<RectTransform> Children(Transform t) { var l = new System.Collections.Generic.List<RectTransform>(); foreach (Transform c in t) if (c is RectTransform r) l.Add(r); return l; }

    static Sprite Sprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    static Transform Find(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
