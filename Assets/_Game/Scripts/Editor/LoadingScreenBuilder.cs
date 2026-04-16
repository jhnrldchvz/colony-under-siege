using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// Builds the LoadingManager GameObject + Canvas hierarchy in the currently open scene.
/// Run once from the menu:  Colony Under Siege › Build Loading Screen
///
/// After building:
///   1. Wire the Inspector fields on the LoadingManager component (already auto-wired).
///   2. Move the LoadingManager GameObject into the Main Menu scene if it isn't already there.
///   3. Save the scene.
/// </summary>
public static class LoadingScreenBuilder
{
    private const string MenuPath = "Colony Under Siege/Build Loading Screen";

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(MenuPath)]
    public static void Build()
    {
        // Remove any existing LoadingManager roots to avoid duplicates
        foreach (var go in Object.FindObjectsByType<LoadingManager>(FindObjectsSortMode.None))
            Undo.DestroyObjectImmediate(go.gameObject);

        // ── Root: LoadingManager GameObject ─────────────────────────────────
        GameObject root = new GameObject("LoadingManager");
        Undo.RegisterCreatedObjectUndo(root, "Build Loading Screen");

        LoadingManager mgr = root.AddComponent<LoadingManager>();

        // ── Canvas ───────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("LoadingCanvas");
        canvasGO.transform.SetParent(root.transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1920, 1080);
        scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight     = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasGroup cg = canvasGO.AddComponent<CanvasGroup>();
        cg.alpha          = 0f;
        cg.blocksRaycasts = true;

        // ── Black background ─────────────────────────────────────────────────
        Image bg = MakeImage(canvasGO.transform, "Background", Color.black);
        FillParent(bg.rectTransform);

        // ── Stage name ───────────────────────────────────────────────────────
        TMP_Text stageName = MakeText(canvasGO.transform, "StageNameText",
            "STAGE 1", 52, FontStyles.Bold, TextAlignmentOptions.Center);
        SetAnchors(stageName.rectTransform,
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.72f));

        // ── Tip text ─────────────────────────────────────────────────────────
        TMP_Text tip = MakeText(canvasGO.transform, "TipText",
            "Tip: Eliminate all enemies to clear a wave.", 22,
            FontStyles.Normal, TextAlignmentOptions.Center);
        tip.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        SetAnchors(tip.rectTransform,
            new Vector2(0.15f, 0.46f), new Vector2(0.85f, 0.54f));

        // ── Progress bar container ────────────────────────────────────────────
        GameObject barContainer = new GameObject("ProgressBarContainer");
        barContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform barContRT = barContainer.AddComponent<RectTransform>();
        SetAnchors(barContRT, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.43f));

        // Track (dark grey)
        Image track = MakeImage(barContainer.transform, "Track", new Color(0.12f, 0.12f, 0.12f));
        FillParent(track.rectTransform);

        // Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barContainer.transform, false);
        Image fill = fillGO.AddComponent<Image>();
        fill.color      = new Color(0.2f, 0.7f, 1f);   // cyan-blue
        fill.type       = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0f;
        RectTransform fillRT = fill.rectTransform;
        FillParent(fillRT);

        // ── Percent label ─────────────────────────────────────────────────────
        TMP_Text pct = MakeText(canvasGO.transform, "PercentText",
            "0%", 18, FontStyles.Normal, TextAlignmentOptions.Right);
        pct.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        SetAnchors(pct.rectTransform,
            new Vector2(0.85f, 0.43f), new Vector2(0.9f, 0.48f));

        // ── LOADING... animated label ─────────────────────────────────────────
        TMP_Text loading = MakeText(canvasGO.transform, "LoadingLabel",
            "LOADING", 20, FontStyles.Normal, TextAlignmentOptions.Left);
        loading.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        SetAnchors(loading.rectTransform,
            new Vector2(0.1f, 0.43f), new Vector2(0.4f, 0.48f));

        // ── Wire Inspector fields ─────────────────────────────────────────────
        mgr.loadingCanvas   = canvas;
        mgr.canvasGroup     = cg;
        mgr.progressBarFill = fill;
        mgr.stageNameText   = stageName;
        mgr.loadingLabel    = loading;
        mgr.tipText         = tip;
        mgr.percentText     = pct;

        EditorSceneManager.MarkSceneDirty(root.scene);

        Selection.activeGameObject = root;
        Debug.Log("[LoadingScreenBuilder] LoadingManager built. Save the scene and configure stageNames / tips in the Inspector.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Image MakeImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TMP_Text MakeText(Transform parent, string name, string text,
        float size, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text           = text;
        t.fontSize       = size;
        t.fontStyle      = style;
        t.alignment      = align;
        t.color          = Color.white;
        t.enableWordWrapping = true;
        return t;
    }

    private static void FillParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
