using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a screen's content on one page with no scrolling: if the content is taller than the
/// visible area (short phones, tablets, odd Game-view sizes), it is scaled down evenly to fit.
/// Put it on the scroll "Content" object. Runs in the Editor too, so the Scene view matches.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("IIAD/Fit To Viewport (no scroll)")]
public class IIADFitToViewport : MonoBehaviour
{
    [Tooltip("The visible area. Defaults to the parent.")]
    public RectTransform viewport;

    [Tooltip("Never shrink below this (keeps text readable).")]
    [Range(0.4f, 1f)] public float minScale = 0.6f;

    [Tooltip("Centre the content vertically when there is spare room.")]
    public bool centerVertically = true;

    RectTransform rt;

    void OnEnable()
    {
        rt = (RectTransform)transform;
        if (viewport == null) viewport = transform.parent as RectTransform;
        var sr = GetComponentInParent<ScrollRect>();
        if (sr != null) { sr.vertical = false; sr.horizontal = false; }
    }

    void LateUpdate()
    {
        if (rt == null || viewport == null) return;
        float contentH = LayoutUtility.GetPreferredHeight(rt);
        if (contentH <= 0) contentH = rt.rect.height;
        float viewH = viewport.rect.height;
        if (contentH <= 0 || viewH <= 0) return;

        float s = Mathf.Clamp(viewH / contentH, minScale, 1f);
        if (!Mathf.Approximately(rt.localScale.x, s)) rt.localScale = new Vector3(s, s, 1f);

        float y = 0f;
        if (centerVertically) y = -Mathf.Max(0f, (viewH - contentH * s) * 0.5f);
        if (!Mathf.Approximately(rt.anchoredPosition.y, y) || rt.anchoredPosition.x != 0f) rt.anchoredPosition = new Vector2(0f, y);
    }
}
