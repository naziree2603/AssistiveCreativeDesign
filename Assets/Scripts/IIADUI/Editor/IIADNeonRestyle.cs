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
/// Tools > IIAD > Restyle MainUI (Dark Neon)
///
/// Rebuilds the layout and button design of every screen under Canvas/ScreenContainer:
///   - Header bar at the top (back on the left, accessibility / home / settings on the right)
///   - Scrollable content column in the middle, ordered top to bottom as before
///   - Footer bar pinned to the bottom with full-width Previous / Next buttons
///   - Login, Register, Challenge and Participant forms: label above a large field
///   - Rounded buttons: yellow main action, cyan outline secondary, coral danger, round icon buttons
///   - Dark rounded cards with a thin cyan edge instead of the old frame images
///   - Lexend font, 48 dp minimum touch size, larger text
///
/// Everything is one Undo step (Ctrl+Z). Only references by object are used in your scripts
/// (no transform.Find), so moving objects into the new containers keeps them working.
/// </summary>
public static class IIADNeonRestyle
{
    // ------------------------------------------------------------------ palette
    static readonly Color Navy = Hex("#0B1630");
    static readonly Color Cyan = Hex("#3CC8FF");
    static readonly Color FieldEdge = Hex("#7DB7E8");
    static readonly Color Yellow = Hex("#FFC233");
    static readonly Color Coral = Hex("#FF6B5B");
    static readonly Color White = Color.white;
    static readonly Color Muted = Hex("#A9BCD9");

    // ------------------------------------------------------------------ sizes (canvas units, 1080 x 1920 reference, ~3 units = 1 dp)
    const float HeaderH = 190f;
    const float FooterH = 210f;
    const float SidePad = 48f;
    const float Gap = 32f;
    const float ButtonH = 156f;     // 52 dp
    const float IconBtn = 132f;     // 44 dp
    const float FieldH = 132f;
    const float TitleH = 120f;

    const string SpriteFolder = "Assets/Arts/NEW UI/IIAD Neon";
    const string ContentName = "IIAD_Content";

    static Sprite sBtnFill, sBtnOutline, sCard, sField, sCircle;
    static TMP_FontAsset font;
    static StringBuilder log;

    // ==================================================================
    [MenuItem("Tools/IIAD/Restyle MainUI (Dark Neon)", priority = 40)]
    static void Run()
    {
        var container = FindInScene("ScreenContainer");
        if (container == null)
        {
            EditorUtility.DisplayDialog("Restyle MainUI", "Open Assets/Scenes/UI/MainUI.unity first (no 'ScreenContainer' found).", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Restyle MainUI (Dark Neon)",
            "This rebuilds the layout and button design of every screen in ScreenContainer.\n\n" +
            "It is one Undo step (Ctrl+Z). Commit to Git first so you can also roll back with 'git checkout'.",
            "Restyle", "Cancel")) return;

        log = new StringBuilder();
        EnsureSprites();
        font = FindFont();

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("IIAD Neon Restyle");
        Undo.RegisterFullObjectHierarchyUndo(container.gameObject, "IIAD Neon Restyle");

        // 1. Layout: each screen gets Header / scroll Content / Footer
        foreach (var screen in Screens(container))
        {
            try { RestructureScreen(screen); }
            catch (System.Exception e) { log.AppendLine("! " + screen.name + ": " + e.Message); }
        }

        // 2. Visual style for everything (screens, popups, sample prompts)
        StyleTree(container);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(container.gameObject.scene);
        Canvas.ForceUpdateCanvases();

        Debug.Log("IIAD Neon restyle finished.\n" + log);
        EditorUtility.DisplayDialog("Restyle finished",
            "Done. Press Play and check each screen. Details are in the Console.\n\nCtrl+Z undoes everything. Save the scene to keep it.", "OK");
    }

    // ==================================================================
    // SCREENS
    // ==================================================================

    static IEnumerable<RectTransform> Screens(Transform container)
    {
        foreach (Transform t in container)
        {
            if (t.name == "PopUps") continue;
            if (t.name == "DesignWorkspace")
            {
                var bg = t.Find("MainSystemPanel/Background");
                if (bg != null) foreach (Transform p in bg) if (p is RectTransform r) yield return r;
                continue;
            }
            if (t is RectTransform rt) yield return rt;
        }
    }

    static void RestructureScreen(RectTransform screen)
    {
        if (screen.Find(ContentName) != null) { log.AppendLine("= " + screen.name + " already restructured, restyle only"); return; }

        // ---- header
        RectTransform header = screen.Find("Header") as RectTransform;
        var looseIcons = Children(screen).Where(c => IsHeaderButtonName(c.name) && c.GetComponent<Button>() != null).ToList();
        if (header == null && looseIcons.Count > 0)
        {
            header = NewRect("Header", screen);
            header.SetSiblingIndex(0);
        }
        if (header != null)
        {
            foreach (var b in looseIcons) Undo.SetTransformParent(b, header, "IIAD");
            LayoutHeader(header);
        }

        // ---- footer
        var footer = screen.Find("FooterNavigation") as RectTransform;
        if (footer != null) LayoutFooter(footer);

        // ---- content
        var items = Children(screen)
            .Where(c => c != header && c != footer && !IsFullStretch(c))
            .ToList();
        if (items.Count == 0) return;

        // Remember sizes and visual order before moving anything
        var size = new Dictionary<RectTransform, Vector2>();
        var center = new Dictionary<RectTransform, Vector2>();
        foreach (var c in items) { size[c] = c.rect.size; center[c] = (Vector2)screen.InverseTransformPoint(WorldCenter(c)); }
        items.Sort((a, b) =>
        {
            float dy = center[b].y - center[a].y;
            if (Mathf.Abs(dy) > 40f) return dy > 0 ? 1 : -1;
            return center[a].x.CompareTo(center[b].x);
        });

        var content = BuildScroll(screen, header != null ? HeaderH : 60f, footer != null ? FooterH : 0f);

        // Group items that sat side by side into rows
        int i = 0;
        while (i < items.Count)
        {
            var row = new List<RectTransform> { items[i] };
            int j = i + 1;
            while (j < items.Count && Mathf.Abs(center[items[j]].y - center[items[i]].y) <= 40f
                   && size[items[j]].x < 700f && size[items[i]].x < 700f) { row.Add(items[j]); j++; }

            if (row.Count == 1)
            {
                MoveInto(row[0], content, size[row[0]]);
            }
            else
            {
                var r = NewRect("Row", content);
                float w = row.Sum(x => size[x].x) + Gap * (row.Count - 1);
                float h = row.Max(x => size[x].y);
                r.sizeDelta = new Vector2(Mathf.Min(w, 1080f - SidePad * 2), h);
                var hl = Undo.AddComponent<HorizontalLayoutGroup>(r.gameObject);
                hl.spacing = Gap; hl.childAlignment = TextAnchor.MiddleCenter;
                hl.childControlWidth = false; hl.childControlHeight = false;
                hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
                foreach (var x in row) MoveInto(x, r, size[x]);
            }
            i = j;
        }

        // Forms: label above a large field
        foreach (Transform c in content.GetComponentsInChildren<Transform>(true))
            if (IsFormCard(c.name) && c is RectTransform card) RestructureForm(card);

        log.AppendLine("+ " + screen.name + ": " + items.Count + " items in a column" + (header ? ", header" : "") + (footer ? ", footer" : ""));
    }

    static RectTransform BuildScroll(RectTransform screen, float top, float bottom)
    {
        var scroll = NewRect(ContentName, screen);
        int idx = 0;
        if (screen.Find("Header") != null) idx = screen.Find("Header").GetSiblingIndex() + 1;
        scroll.SetSiblingIndex(idx);
        scroll.anchorMin = Vector2.zero; scroll.anchorMax = Vector2.one;
        scroll.offsetMin = new Vector2(0, bottom); scroll.offsetMax = new Vector2(0, -top);

        var hit = Undo.AddComponent<Image>(scroll.gameObject);    // lets you drag anywhere to scroll
        hit.color = new Color(0, 0, 0, 0);

        var viewport = NewRect("Viewport", scroll);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        Undo.AddComponent<RectMask2D>(viewport.gameObject);

        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var vl = Undo.AddComponent<VerticalLayoutGroup>(content.gameObject);
        vl.padding = new RectOffset((int)SidePad, (int)SidePad, 24, 64);
        vl.spacing = Gap; vl.childAlignment = TextAnchor.UpperCenter;
        vl.childControlWidth = false; vl.childControlHeight = false;
        vl.childForceExpandWidth = false; vl.childForceExpandHeight = false;
        var fit = Undo.AddComponent<ContentSizeFitter>(content.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = Undo.AddComponent<ScrollRect>(scroll.gameObject);
        sr.viewport = viewport; sr.content = content;
        sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40f;
        return content;
    }

    static void MoveInto(RectTransform item, RectTransform parent, Vector2 size)
    {
        Undo.SetTransformParent(item, parent, "IIAD");
        item.anchorMin = item.anchorMax = new Vector2(0.5f, 1f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.localScale = Vector3.one;
        item.localRotation = Quaternion.identity;
        item.sizeDelta = new Vector2(Mathf.Min(size.x, 1080f - SidePad), size.y);
    }

    // ------------------------------------------------------------------ header
    static void LayoutHeader(RectTransform header)
    {
        header.anchorMin = new Vector2(0, 1); header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0, HeaderH);

        var old = header.GetComponent<LayoutGroup>();
        if (old != null) Undo.DestroyObjectImmediate(old);
        var hl = Undo.AddComponent<HorizontalLayoutGroup>(header.gameObject);
        hl.padding = new RectOffset(36, 36, 40, 18);
        hl.spacing = 24; hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true; hl.childControlHeight = false;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;

        RectTransform spacer = header.Find("Spacer") as RectTransform;
        if (spacer == null) { spacer = NewRect("Spacer", header); }
        var sle = GetOrAdd<LayoutElement>(spacer.gameObject);
        sle.flexibleWidth = 1; sle.minWidth = 0;

        foreach (var c in Children(header))
        {
            if (c == spacer) continue;
            string n = c.name.ToLowerInvariant();
            Vector2 s;
            if (n.Contains("appslogo")) s = FitHeight(c, 112f);
            else if (n.Contains("logo")) s = FitHeight(c, 96f);
            else if (c.GetComponent<Button>() != null) s = new Vector2(IconBtn, IconBtn);
            else s = c.rect.size;
            c.sizeDelta = s;
            var le = GetOrAdd<LayoutElement>(c.gameObject);
            le.minWidth = le.preferredWidth = s.x; le.flexibleWidth = 0;
            var img = c.GetComponent<Image>(); if (img != null && n.Contains("logo")) img.preserveAspect = true;
        }

        // Order: back, logos, spacer, then actions on the right
        var ordered = Children(header).OrderBy(HeaderOrder).ToList();
        for (int k = 0; k < ordered.Count; k++) ordered[k].SetSiblingIndex(k);
    }

    static int HeaderOrder(RectTransform t)
    {
        string n = t.name.ToLowerInvariant();
        if (n.Contains("back")) return 0;
        if (n.Contains("gooleelogo")) return 1;
        if (n.Contains("logo")) return 2;
        if (n == "spacer") return 3;
        if (n.Contains("accessib")) return 4;
        if (n.Contains("setting")) return 5;
        if (n.Contains("profile")) return 6;
        if (n.Contains("home")) return 7;
        if (n.Contains("logout")) return 8;
        if (n.Contains("exit")) return 9;
        return 3;
    }

    static bool IsHeaderButtonName(string name)
    {
        string n = name.ToLowerInvariant();
        return n == "backbutton" || n.Contains("accessibilitybutton") || n.Contains("homebutton") || n.Contains("exitappbutton")
            || n.Contains("settingsbutton") || n.Contains("logoutbutton") || n.Contains("profilebutton");
    }

    // ------------------------------------------------------------------ footer
    static void LayoutFooter(RectTransform footer)
    {
        footer.anchorMin = new Vector2(0, 0); footer.anchorMax = new Vector2(1, 0);
        footer.pivot = new Vector2(0.5f, 0);
        footer.anchoredPosition = Vector2.zero;
        footer.sizeDelta = new Vector2(0, FooterH);
        var old = footer.GetComponent<LayoutGroup>();
        if (old != null) Undo.DestroyObjectImmediate(old);
        var hl = Undo.AddComponent<HorizontalLayoutGroup>(footer.gameObject);
        hl.padding = new RectOffset((int)SidePad, (int)SidePad, 27, 27);
        hl.spacing = Gap; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = true; hl.childControlHeight = true;
        hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
        // Previous on the left, Next on the right
        var prev = footer.Find("PreviousButton"); if (prev != null) prev.SetAsFirstSibling();
        var next = footer.Find("NextButton"); if (next != null) next.SetAsLastSibling();
    }

    // ------------------------------------------------------------------ forms
    static bool IsFormCard(string n) => n == "LoginPanel" || n == "RegisterPanel" || n == "ChallengeBackground" || n == "ParticipantDetailsBg";

    static void RestructureForm(RectTransform card)
    {
        if (card.GetComponent<VerticalLayoutGroup>() != null) return;

        var kids = Children(card).ToList();
        var y = kids.ToDictionary(k => k, k => card.InverseTransformPoint(WorldCenter(k)).y);
        kids.Sort((a, b) => y[b].CompareTo(y[a]));
        for (int k = 0; k < kids.Count; k++) kids[k].SetSiblingIndex(k);

        var vl = Undo.AddComponent<VerticalLayoutGroup>(card.gameObject);
        vl.padding = new RectOffset(64, 64, 56, 64);
        vl.spacing = 28; vl.childAlignment = TextAnchor.UpperCenter;
        vl.childControlWidth = true; vl.childControlHeight = false;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
        var fit = Undo.AddComponent<ContentSizeFitter>(card.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var k in kids)
        {
            var field = (Component)k.GetComponentInChildren<TMP_InputField>(true) ?? k.GetComponentInChildren<TMP_Dropdown>(true);
            bool isGroup = field != null && field.transform != k;
            if (isGroup) RestructureFieldGroup(k, field);
            else if (k.GetComponent<TMP_InputField>() != null || k.GetComponent<TMP_Dropdown>() != null) k.sizeDelta = new Vector2(k.sizeDelta.x, FieldH);
            else if (k.GetComponent<Button>() != null) k.sizeDelta = new Vector2(k.sizeDelta.x, k.GetComponent<Image>() == null ? 96f : ButtonH);
            else if (k.GetComponent<Image>() != null && k.GetComponentInChildren<TMP_Text>() == null) { var img = k.GetComponent<Image>(); img.preserveAspect = true; }
            else if (k.GetComponent<TMP_Text>() is TMP_Text t && string.IsNullOrWhiteSpace(t.text) && k.name.ToLower().Contains("message")) k.sizeDelta = new Vector2(k.sizeDelta.x, 64);
        }
        log.AppendLine("  form " + card.name + ": label-above-field column");
    }

    static void RestructureFieldGroup(RectTransform group, Component field)
    {
        var fieldRT = (RectTransform)field.transform;

        // Decorative icons inside field groups are hidden (they are not tappable and crowd the column)
        foreach (var c in Children(group))
            if (c != fieldRT && c.GetComponent<TMP_Text>() == null && c.GetComponent<Button>() == null && c.GetComponentInChildren<Selectable>(true) == null)
            { Undo.RecordObject(c.gameObject, "IIAD"); c.gameObject.SetActive(false); log.AppendLine("  hid decoration " + group.name + "/" + c.name); }

        // Eye button goes inside the password field, on the right
        foreach (var c in Children(group).ToList())
        {
            if (!c.name.ToLower().Contains("eye")) continue;
            Undo.SetTransformParent(c, fieldRT, "IIAD");
            c.anchorMin = c.anchorMax = new Vector2(1, 0.5f); c.pivot = new Vector2(1, 0.5f);
            c.anchoredPosition = new Vector2(-16, 0); c.sizeDelta = new Vector2(100, 100);
            var img = c.GetComponent<Image>(); if (img != null) img.preserveAspect = true;
            if (field is TMP_InputField inf && inf.textViewport != null)
            { Undo.RecordObject(inf.textViewport, "IIAD"); inf.textViewport.offsetMax = new Vector2(-124, inf.textViewport.offsetMax.y); }
        }

        var label = group.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.GetComponentInParent<TMP_InputField>() == null && t.GetComponentInParent<TMP_Dropdown>() == null);
        if (label != null)
        {
            label.transform.SetAsFirstSibling();
            ((RectTransform)label.transform).sizeDelta = new Vector2(0, 56);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.text = label.text.Replace(":", "").Trim();
        }
        fieldRT.SetSiblingIndex(label != null ? 1 : 0);
        fieldRT.sizeDelta = new Vector2(fieldRT.sizeDelta.x, FieldH);

        var vl = Undo.AddComponent<VerticalLayoutGroup>(group.gameObject);
        vl.spacing = 12; vl.childAlignment = TextAnchor.UpperLeft;
        vl.childControlWidth = true; vl.childControlHeight = false;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
        group.sizeDelta = new Vector2(group.sizeDelta.x, (label != null ? 56 + 12 : 0) + FieldH);
    }

    // ==================================================================
    // STYLE
    // ==================================================================

    static void StyleTree(Transform root)
    {
        int buttons = 0, cards = 0, fields = 0, titles = 0;

        foreach (var b in root.GetComponentsInChildren<Button>(true)) { StyleButton(b); buttons++; }
        foreach (var f in root.GetComponentsInChildren<TMP_InputField>(true)) { StyleField(f.GetComponent<Image>(), f.textComponent, f.placeholder as TMP_Text); f.caretColor = Cyan; f.customCaretColor = true; f.selectionColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f); fields++; }
        foreach (var d in root.GetComponentsInChildren<TMP_Dropdown>(true)) { StyleDropdown(d); fields++; }

        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.GetComponent<Selectable>() != null || img.GetComponentInParent<Button>(true) != null) continue;
            if (img.GetComponentInParent<TMP_InputField>(true) != null || img.GetComponentInParent<TMP_Dropdown>(true) != null) continue;
            if (img.GetComponent<Scrollbar>() != null || img.GetComponentInParent<Scrollbar>(true) != null) continue;
            string n = img.name.ToLowerInvariant();
            if (img.sprite == null || n.Contains("logo") || n.Contains("icon") || n.Contains("poster") || n.Contains("circle") || n.Contains("handle") || n == "viewport" || n == "image") continue;
            if (IsFullStretch((RectTransform)img.transform) && img.transform.childCount > 0 && img.transform.parent != null && img.transform.parent.name != "PopUps" && !n.Contains("box")) continue; // background art

            if (IsTitle(img))
            {
                Undo.RecordObject(img, "IIAD"); img.enabled = false;
                var rt = (RectTransform)img.transform;
                rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 800f), TitleH);
                var le = img.GetComponent<LayoutElement>(); if (le != null) le.preferredHeight = TitleH;
                foreach (var t in img.GetComponentsInChildren<TMP_Text>(true))
                {
                    StyleText(t, 64, White, FontStyles.Bold);
                    t.alignment = TextAlignmentOptions.Center;
                    var trt = (RectTransform)t.transform;
                    trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
                }
                titles++;
                continue;
            }
            if (img.transform.childCount == 0) continue;     // single decorative image
            Undo.RecordObject(img, "IIAD");
            img.sprite = sCard; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f;
            img.color = new Color(Cyan.r, Cyan.g, Cyan.b, 1f);
            cards++;
        }

        // Plain texts: readable font, light colour on the dark background, never tiny
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.GetComponentInParent<Button>(true) != null || t.GetComponentInParent<TMP_InputField>(true) != null || t.GetComponentInParent<TMP_Dropdown>(true) != null) continue;
            Undo.RecordObject(t, "IIAD");
            if (font != null) t.font = font;
            Color.RGBToHSV(t.color, out _, out _, out float v);
            if (v < 0.45f) t.color = White;
            if (!t.enableAutoSizing && t.fontSize < 34) t.fontSize = 34;
        }

        log.AppendLine($"Styled {buttons} buttons, {fields} fields, {cards} cards, {titles} titles.");
    }

    enum Kind { Primary, Secondary, Danger, Icon, Link, Eye, Menu }

    static Kind Classify(Button b)
    {
        string n = b.name.ToLowerInvariant();
        string parent = b.transform.parent != null ? b.transform.parent.name.ToLowerInvariant() : "";
        var img = b.GetComponent<Image>();
        bool hasText = b.GetComponentsInChildren<TMP_Text>(true).Any(t => !string.IsNullOrWhiteSpace(t.text) && t.GetComponentInParent<TMP_InputField>(true) == null);

        if (n.Contains("eye")) return Kind.Eye;
        if (img == null && hasText) return Kind.Link;
        if (!hasText) return Kind.Icon;
        if (n.Contains("delete") || n == "exitbutton") return Kind.Danger;
        if (parent.Contains("groupbutton") || parent == "layout") return Kind.Menu;     // dashboard, settings, sample prompts
        if (n.Contains("login") || n.Contains("generate") || n.Contains("revisedesign") || n.Contains("submitposter") ||
            n.Contains("joinevent") || n.Contains("saveparticipant") || n == "nextbutton" || n == "yesbutton" ||
            (n == "createaccount" && parent.Contains("register"))) return Kind.Primary;
        return Kind.Secondary;
    }

    static void StyleButton(Button b)
    {
        var kind = Classify(b);
        var rt = (RectTransform)b.transform;
        var img = b.GetComponent<Image>();
        Undo.RecordObject(b, "IIAD");
        if (img != null) Undo.RecordObject(img, "IIAD");
        Undo.RecordObject(rt, "IIAD");

        var cb = b.colors;
        cb.normalColor = Color.white; cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        cb.pressedColor = new Color(0.74f, 0.74f, 0.74f); cb.selectedColor = Color.white;
        cb.disabledColor = new Color(1f, 1f, 1f, 0.4f); cb.fadeDuration = 0.08f;
        b.colors = cb; b.transition = Selectable.Transition.ColorTint;

        bool layoutControlsHeight = rt.parent != null && rt.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() is HorizontalOrVerticalLayoutGroup g && g.childControlHeight;

        switch (kind)
        {
            case Kind.Eye:
                if (img != null) img.preserveAspect = true;
                if (rt.sizeDelta.x < 96) rt.sizeDelta = new Vector2(100, 100);
                return;

            case Kind.Link:
                foreach (var t in Labels(b)) { StyleText(t, 40, Cyan, FontStyles.Bold | FontStyles.Underline); }
                if (rt.sizeDelta.y < 96) rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 420), 96);
                return;

            case Kind.Icon:
                if (img != null && img.sprite != sCircle)
                {
                    // keep the old icon artwork in a child, put a round button behind it
                    var iconRT = NewRect("IIAD_Icon", rt);
                    iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
                    iconRT.offsetMin = new Vector2(26, 26); iconRT.offsetMax = new Vector2(-26, -26);
                    var icon = Undo.AddComponent<Image>(iconRT.gameObject);
                    icon.sprite = img.sprite; icon.preserveAspect = true; icon.raycastTarget = false; icon.color = Color.white;
                    img.sprite = sCircle; img.type = Image.Type.Simple; img.preserveAspect = true;
                    img.color = b.name.ToLowerInvariant().Contains("logout") || b.name.ToLowerInvariant().Contains("exit") ? Coral : Cyan;
                }
                if (!layoutControlsHeight) rt.sizeDelta = new Vector2(Mathf.Max(IconBtn, rt.sizeDelta.x > 180 ? rt.sizeDelta.x : IconBtn), Mathf.Max(IconBtn, rt.sizeDelta.y > 180 ? rt.sizeDelta.y : IconBtn));
                return;
        }

        Color fill = kind == Kind.Primary ? Yellow : kind == Kind.Danger ? Coral : Cyan;
        Color fg = kind == Kind.Primary ? Navy : kind == Kind.Danger ? Coral : White;
        if (img != null)
        {
            img.sprite = kind == Kind.Primary ? sBtnFill : sBtnOutline;
            img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.preserveAspect = false;
            img.color = fill;
        }
        if (!layoutControlsHeight && rt.sizeDelta.y < ButtonH && rt.anchorMin.y == rt.anchorMax.y)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, ButtonH);

        var labels = Labels(b).ToList();
        bool hasIcon = Children(rt).Any(c => c.name.ToLowerInvariant().Contains("icon"));
        for (int i = 0; i < labels.Count; i++)
        {
            var t = labels[i];
            bool secondLine = kind == Kind.Menu && i > 0;
            StyleText(t, secondLine ? 38 : 48, secondLine ? Muted : fg, secondLine ? FontStyles.Normal : FontStyles.Bold);
            t.enableAutoSizing = true; t.fontSizeMax = secondLine ? 38 : 50; t.fontSizeMin = 30;
            if (hasIcon || (kind == Kind.Menu && labels.Count > 1)) t.alignment = TextAlignmentOptions.MidlineLeft;
            else t.alignment = TextAlignmentOptions.Center;
        }
    }

    static IEnumerable<TMP_Text> Labels(Button b) =>
        b.GetComponentsInChildren<TMP_Text>(true).Where(t => t.GetComponentInParent<TMP_InputField>(true) == null);

    static void StyleField(Image img, TMP_Text text, TMP_Text placeholder)
    {
        if (img != null)
        {
            Undo.RecordObject(img, "IIAD");
            img.sprite = sField; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.color = FieldEdge;
            var rt = (RectTransform)img.transform;
            bool controlled = rt.parent != null && rt.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() is HorizontalOrVerticalLayoutGroup g && g.childControlHeight;
            if (!controlled && rt.sizeDelta.y < FieldH && rt.anchorMin.y == rt.anchorMax.y) rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 600), FieldH);
        }
        if (text != null) { StyleText(text, 44, White, FontStyles.Normal); text.enableAutoSizing = false; }
        if (placeholder != null) { StyleText(placeholder, 44, new Color(Muted.r, Muted.g, Muted.b, 0.75f), FontStyles.Italic); placeholder.enableAutoSizing = false; }
    }

    static void StyleDropdown(TMP_Dropdown d)
    {
        StyleField(d.GetComponent<Image>(), d.captionText, d.placeholder as TMP_Text);
        if (d.itemText != null) StyleText(d.itemText, 44, White, FontStyles.Normal);
        if (d.template != null)
        {
            var timg = d.template.GetComponent<Image>();
            if (timg != null) { Undo.RecordObject(timg, "IIAD"); timg.sprite = sCard; timg.type = Image.Type.Sliced; timg.color = Cyan; }
            foreach (var t in d.template.GetComponentsInChildren<Toggle>(true))
            {
                var bg = t.targetGraphic as Image;
                if (bg != null) { Undo.RecordObject(bg, "IIAD"); bg.color = new Color(0.07f, 0.14f, 0.29f, 1f); }
                var tr = (RectTransform)t.transform; Undo.RecordObject(tr, "IIAD");
                tr.sizeDelta = new Vector2(tr.sizeDelta.x, Mathf.Max(tr.sizeDelta.y, 120));
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

    static bool IsTitle(Image img)
    {
        string n = img.name.ToLowerInvariant();
        return (n.Contains("title") || n == "titlle") && img.GetComponentInChildren<TMP_Text>(true) != null && !n.Contains("logo");
    }

    // ==================================================================
    // SPRITES (generated once into Assets/Arts/NEW UI/IIAD Neon)
    // ==================================================================

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

    /// <summary>White rounded rectangle. ring>0: white edge with a dark inner fill (so one tint colours the edge and darkens the fill).</summary>
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
                    float outside = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0);
                    float d = outside - radius;                         // signed distance, <0 inside
                    float a = Mathf.Clamp01(0.5f - d);
                    Color c;
                    if (ring <= 0) c = new Color(1, 1, 1, a);
                    else
                    {
                        float inner = Mathf.Clamp01(0.5f - (d + ring)); // 1 inside the ring
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
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = 100;
            imp.spriteBorder = new Vector4(border, border, border, border);
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static TMP_FontAsset FindFont()
    {
        foreach (var guid in AssetDatabase.FindAssets("Lexend t:TMP_FontAsset"))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        log.AppendLine("Lexend TMP font asset not found; fonts left as they were.");
        return null;
    }

    // ==================================================================
    // helpers
    // ==================================================================

    static Transform FindInScene(string name)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    static IEnumerable<RectTransform> Children(Transform t)
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
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return rt;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    static bool IsFullStretch(RectTransform r) =>
        r.anchorMin.x <= 0.001f && r.anchorMin.y <= 0.001f && r.anchorMax.x >= 0.999f && r.anchorMax.y >= 0.999f;

    static Vector3 WorldCenter(RectTransform r)
    {
        var c = new Vector3[4]; r.GetWorldCorners(c);
        return (c[0] + c[2]) * 0.5f;
    }

    static Vector2 FitHeight(RectTransform r, float h)
    {
        var s = r.rect.size;
        if (s.y <= 0) return new Vector2(h, h);
        return new Vector2(s.x * h / s.y, h);
    }

    static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
}
