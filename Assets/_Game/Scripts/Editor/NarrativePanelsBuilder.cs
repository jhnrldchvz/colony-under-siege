using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the Storyboard and Instruction panels for any stage scene,
/// auto-wiring every UIManager reference.
///
/// Run via:  Colony Under Siege → Rebuild Storyboard Panel
///           Colony Under Siege → Rebuild Instruction Panel
/// </summary>
public static class NarrativePanelsBuilder
{
    // ── Palette ───────────────────────────────────────────────────────────────
    static readonly Color ColBlack       = new Color(0.00f, 0.00f, 0.00f, 1.00f);
    static readonly Color ColBgDark      = new Color(0.02f, 0.03f, 0.06f, 1.00f);
    static readonly Color ColCard        = new Color(0.05f, 0.08f, 0.15f, 1.00f);
    static readonly Color ColCyan        = new Color(0.00f, 0.84f, 1.00f, 1.00f);
    static readonly Color ColCyanDim     = new Color(0.00f, 0.84f, 1.00f, 0.45f);
    static readonly Color ColCyanLine    = new Color(0.00f, 0.84f, 1.00f, 0.55f);
    static readonly Color ColSubtext     = new Color(0.75f, 0.82f, 0.92f, 1.00f);
    static readonly Color ColBottomBar   = new Color(0.02f, 0.04f, 0.08f, 0.92f);
    static readonly Color ColTopHud      = new Color(0.00f, 0.00f, 0.00f, 0.55f);
    static readonly Color ColCounter     = new Color(0.42f, 0.58f, 0.76f, 1.00f);
    static readonly Color ColBtnNav      = new Color(0.10f, 0.15f, 0.25f, 1.00f);
    static readonly Color ColBtnContinue = new Color(0.00f, 0.55f, 0.70f, 1.00f);
    static readonly Color ColBtnStart    = new Color(0.00f, 0.60f, 0.38f, 1.00f);
    static readonly Color ColDivider     = new Color(0.10f, 0.15f, 0.22f, 1.00f);
    static readonly Color ColImageBg     = new Color(0.06f, 0.09f, 0.16f, 1.00f);

    // ═════════════════════════════════════════════════════════════════════════
    // STORYBOARD PANEL  — cinematic full-screen style
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Storyboard Panel")]
    public static void BuildStoryboard()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (!Validate(ui, out Canvas canvas)) return;

        // Destroy UIManager-referenced panel
        if (ui.storyboardPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "storyboardPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(ui.storyboardPanel);
            ui.storyboardPanel = null;
        }

        // Also destroy any orphaned panels with the same name (not referenced by UIManager)
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go != null && go.name == "StoryboardPanel")
                Undo.DestroyObjectImmediate(go);
        }

        Undo.RecordObject(ui, "Build Storyboard Panel");

        // ── Root (full screen) ───────────────────────────────────────────────
        // This Image is the permanent black backdrop. It is OUTSIDE SbContentLayer
        // so it stays fully opaque while slide cross-fades run on SbContentLayer.
        GameObject panel = NewGO("StoryboardPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Storyboard Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColBlack;

        // ── SbContentLayer — everything that fades during slide transitions ───
        // UIManager puts its CanvasGroup here (via Find("SbContentLayer")).
        // The panel root's black Image sits outside this layer and never fades,
        // so the game world never shows through during cross-fades.
        GameObject layer = NewGO("SbContentLayer", panel.transform);
        FillParent(layer);
        // CanvasGroup is added at runtime by UIManager (GetOrAddCG logic).
        // We pre-add it here so the initial alpha can be set correctly.
        layer.AddComponent<CanvasGroup>();

        // ── SbImage — fills 100% of content layer (behind overlays) ──────────
        // Always kept active. When no sprite is assigned it shows ColImageBg tint.
        GameObject imgGo = NewGO("SbImage", layer.transform);
        FillParent(imgGo);
        Image sbImg = imgGo.AddComponent<Image>();
        sbImg.color          = ColImageBg;   // placeholder tint until sprite assigned
        sbImg.preserveAspect = false;        // fill the whole screen — no letterboxing

        // ── Top HUD bar (floats over image) ───────────────────────────────────
        GameObject topHud = NewGO("TopHUD", layer.transform);
        var thRt = topHud.GetComponent<RectTransform>();
        thRt.anchorMin = new Vector2(0f, 1f);
        thRt.anchorMax = new Vector2(1f, 1f);
        thRt.offsetMin = new Vector2(0f, -56f);
        thRt.offsetMax = Vector2.zero;
        topHud.AddComponent<Image>().color = ColTopHud;

        // "INTEL / LORE" label — top left
        GameObject hudLabel = NewGO("HudLabel", topHud.transform);
        var hlRt = hudLabel.GetComponent<RectTransform>();
        hlRt.anchorMin = new Vector2(0f, 0f);
        hlRt.anchorMax = new Vector2(0f, 1f);
        hlRt.offsetMin = new Vector2(28f, 0f);
        hlRt.offsetMax = new Vector2(280f, 0f);
        var hudTmp = hudLabel.AddComponent<TextMeshProUGUI>();
        hudTmp.text             = "I N T E L  /  L O R E";
        hudTmp.fontSize         = 12;
        hudTmp.fontStyle        = FontStyles.Bold;
        hudTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        hudTmp.color            = ColCyanDim;
        hudTmp.raycastTarget    = false;

        // Counter — top right
        GameObject counterGo = NewGO("SbCounterText", topHud.transform);
        var cRt = counterGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 0f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.offsetMin = new Vector2(-170f, 0f);
        cRt.offsetMax = new Vector2(-28f,  0f);
        var counterTmp = counterGo.AddComponent<TextMeshProUGUI>();
        counterTmp.text          = "1 / 3";
        counterTmp.fontSize      = 14;
        counterTmp.fontStyle     = FontStyles.Bold;
        counterTmp.alignment     = TextAlignmentOptions.MidlineRight;
        counterTmp.color         = ColCounter;
        counterTmp.raycastTarget = false;

        // ── Bottom overlay (floats over image, dark gradient strip) ───────────
        GameObject bottomOverlay = NewGO("BottomOverlay", layer.transform);
        var boRt = bottomOverlay.GetComponent<RectTransform>();
        boRt.anchorMin = new Vector2(0f, 0f);
        boRt.anchorMax = new Vector2(1f, 0f);
        boRt.offsetMin = Vector2.zero;
        boRt.offsetMax = new Vector2(0f, 220f);
        bottomOverlay.AddComponent<Image>().color = ColBottomBar;

        // Cyan accent line — very top edge of the bottom overlay
        GameObject topAccent = NewGO("TopAccent", bottomOverlay.transform);
        var taRt = topAccent.GetComponent<RectTransform>();
        taRt.anchorMin = new Vector2(0f, 1f);
        taRt.anchorMax = new Vector2(1f, 1f);
        taRt.offsetMin = new Vector2(0f, -2f);
        taRt.offsetMax = Vector2.zero;
        topAccent.AddComponent<Image>().color = ColCyanLine;

        // Left text block (title + body)
        GameObject textBlock = NewGO("TextBlock", bottomOverlay.transform);
        var tbRt = textBlock.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 0f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.offsetMin = new Vector2(52f,  16f);
        tbRt.offsetMax = new Vector2(-440f, -16f);  // leave room for buttons on right

        var vlg = textBlock.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(0, 0, 8, 8);
        vlg.spacing               = 6;
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var titleTmp = MakeTMP("SbTitleText", textBlock.transform,
            "NARRATOR", 13, FontStyles.Bold, TextAlignmentOptions.Left, ColCyan);
        titleTmp.characterSpacing = 3f;
        LE(titleTmp.gameObject, h: 20);

        Spacer(textBlock.transform, 4);

        var bodyTmp = MakeTMP("SbBodyText", textBlock.transform,
            "Your story begins here...",
            17, FontStyles.Italic, TextAlignmentOptions.Left, Color.white);
        bodyTmp.enableWordWrapping = true;
        LE(bodyTmp.gameObject, h: 80);

        // ── Nav buttons (bottom-right corner) ─────────────────────────────────
        GameObject navRow = NewGO("NavButtons", layer.transform);
        var navRt = navRow.GetComponent<RectTransform>();
        navRt.anchorMin        = new Vector2(1f, 0f);
        navRt.anchorMax        = new Vector2(1f, 0f);
        navRt.pivot            = new Vector2(1f, 0f);
        navRt.anchoredPosition = new Vector2(-28f, 20f);
        navRt.sizeDelta        = new Vector2(410f, 52f);

        var navHlg = navRow.AddComponent<HorizontalLayoutGroup>();
        navHlg.spacing               = 10;
        navHlg.childAlignment        = TextAnchor.MiddleRight;
        navHlg.childControlWidth     = false;
        navHlg.childControlHeight    = true;
        navHlg.childForceExpandWidth  = false;
        navHlg.childForceExpandHeight = true;

        Button prevBtn     = NavBtn(navRow.transform, "SbPrevButton",     "‹  PREV",     ColBtnNav,      118);
        Button nextBtn     = NavBtn(navRow.transform, "SbNextButton",     "NEXT  ›",     ColBtnNav,      118);
        Button continueBtn = NavBtn(navRow.transform, "SbContinueButton", "CONTINUE  →", ColBtnContinue, 154);

        // ── Wire UIManager ────────────────────────────────────────────────────
        ui.storyboardPanel  = panel;
        ui.sbImage          = sbImg;
        ui.sbTitleText      = titleTmp;
        ui.sbBodyText       = bodyTmp;
        ui.sbCounterText    = counterTmp;
        ui.sbPrevButton     = prevBtn;
        ui.sbNextButton     = nextBtn;
        ui.sbContinueButton = continueBtn;

        panel.SetActive(false);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Storyboard panel built and wired. SAVE THE SCENE before pressing Play.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // INSTRUCTION PANEL  — mission briefing card style
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Instruction Panel")]
    public static void BuildInstruction()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (!Validate(ui, out Canvas canvas)) return;

        if (ui.instructionPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "instructionPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(ui.instructionPanel);
        }

        Undo.RecordObject(ui, "Build Instruction Panel");

        // ── Full-screen dark overlay ──────────────────────────────────────────
        GameObject panel = NewGO("InstructionPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Instruction Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.09f, 0.96f);

        // ── Centred briefing card (920 × 640) ─────────────────────────────────
        GameObject card = NewGO("BriefingCard", panel.transform);
        CentreAnchor(card, 920, 640);
        card.AddComponent<Image>().color = ColCard;
        TopEdge(card.transform, ColCyan, 3f);

        // Left accent bar
        LeftEdge(card.transform, ColCyan, 4f);

        // ── Content column ────────────────────────────────────────────────────
        GameObject col = NewGO("ContentColumn", card.transform);
        FillParent(col);
        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(36, 36, 28, 28);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Header row (title + counter) ──────────────────────────────────────
        GameObject headerRow = NewGO("HeaderRow", col.transform);
        var hHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
        hHlg.spacing               = 0;
        hHlg.childAlignment        = TextAnchor.MiddleLeft;
        hHlg.childControlWidth     = false;
        hHlg.childControlHeight    = true;
        hHlg.childForceExpandWidth  = false;
        hHlg.childForceExpandHeight = true;
        LE(headerRow, h: 36);

        // Left side: header tag
        var tagGo = NewGO("Tag", headerRow.transform);
        tagGo.AddComponent<Image>().color = ColCyan;
        LE(tagGo, w: 4, h: 36);

        var tagSpacer = NewGO("TagSpacer", headerRow.transform);
        LE(tagSpacer, w: 12);

        var headerLabel = MakeTMP("HeaderLabel", headerRow.transform,
            "M I S S I O N   B R I E F I N G",
            14, FontStyles.Bold, TextAlignmentOptions.Left, ColCyan);
        headerLabel.characterSpacing = 1f;
        LE(headerLabel.gameObject, w: 500, h: 36);

        // Right side: counter (flexible spacer pushes it right)
        var flexSpacer = NewGO("FlexSpacer", headerRow.transform);
        var fsLE = flexSpacer.AddComponent<LayoutElement>();
        fsLE.flexibleWidth = 1;

        var counterTmp = MakeTMP("SlideCounterText", headerRow.transform,
            "1 / 2", 14, FontStyles.Bold, TextAlignmentOptions.Right, ColCounter);
        LE(counterTmp.gameObject, w: 80, h: 36);

        Spacer(col.transform, 14);
        HRule(col.transform, ColCyanLine, 2f);
        Spacer(col.transform, 16);

        // ── Slide image ───────────────────────────────────────────────────────
        // UIManager calls slideImage.gameObject.SetActive(hasImg) — must be
        // a direct child of ContentColumn (no intermediate container that hides)
        var slideImgTmp = MakeTMP("SlideImagePlaceholder", col.transform,
            "", 0, FontStyles.Normal, TextAlignmentOptions.Center, Color.clear);
        LE(slideImgTmp.gameObject, h: 0); // zero height when no image shown

        GameObject slideImgGo = NewGO("SlideImage", col.transform);
        var siRt = slideImgGo.GetComponent<RectTransform>();
        siRt.pivot = new Vector2(0.5f, 0.5f);
        Image slideImg = slideImgGo.AddComponent<Image>();
        slideImg.color          = ColImageBg;
        slideImg.preserveAspect = true;
        LE(slideImgGo, h: 300);

        Spacer(col.transform, 16);

        // ── Body text ─────────────────────────────────────────────────────────
        var bodyTmp = MakeTMP("SlideBodyText", col.transform,
            "Mission objectives will appear here. Read carefully before engaging.",
            17, FontStyles.Normal, TextAlignmentOptions.Left, ColSubtext);
        bodyTmp.enableWordWrapping = true;
        LE(bodyTmp.gameObject, h: 90);

        Spacer(col.transform, 14);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 16);

        // ── Button row ────────────────────────────────────────────────────────
        GameObject btnRow = NewGO("ButtonRow", col.transform);
        var bHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        bHlg.spacing               = 12;
        bHlg.childAlignment        = TextAnchor.MiddleRight;
        bHlg.childControlWidth     = false;
        bHlg.childControlHeight    = true;
        bHlg.childForceExpandWidth  = false;
        bHlg.childForceExpandHeight = true;
        LE(btnRow, h: 50);

        // Flex push buttons to right
        var bFlex = NewGO("FlexSpacer", btnRow.transform);
        bFlex.AddComponent<LayoutElement>().flexibleWidth = 1;

        Button prevBtn  = InstrBtn(btnRow.transform, "SlidePrevButton",  "‹  BACK",          ColBtnNav,   130);
        Button nextBtn  = InstrBtn(btnRow.transform, "SlideNextButton",  "NEXT  ›",          ColBtnNav,   130);
        Button startBtn = InstrBtn(btnRow.transform, "SlideStartButton", "▶  START MISSION", ColBtnStart, 210);

        // ── Wire UIManager ────────────────────────────────────────────────────
        ui.instructionPanel  = panel;
        ui.slideImage        = slideImg;
        ui.slideBodyText     = bodyTmp;
        ui.slideCounterText  = counterTmp;
        ui.slidePrevButton   = prevBtn;
        ui.slideNextButton   = nextBtn;
        ui.slideStartButton  = startBtn;

        panel.SetActive(false);
        EditorUtility.SetDirty(ui);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Instruction panel built and wired.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — layout
    // ═════════════════════════════════════════════════════════════════════════

    static void CinematicBar(Transform parent, string name, Vector2 anchor, float height)
    {
        GameObject bar = NewGO(name, parent);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchor.y);
        rt.anchorMax = new Vector2(1f, anchor.y);
        rt.pivot     = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
        bar.AddComponent<Image>().color = ColBlack;
    }

    static void ProgressDots(Transform parent, int count)
    {
        GameObject row = NewGO("ProgressDots", parent);
        var rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 78f);
        rt.sizeDelta = new Vector2(count * 24f, 8f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing             = 8;
        hlg.childAlignment      = TextAnchor.MiddleCenter;
        hlg.childControlWidth   = false;
        hlg.childControlHeight  = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < count; i++)
        {
            GameObject dot = NewGO($"Dot{i}", row.transform);
            dot.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 8f);
            dot.AddComponent<Image>().color = i == 0 ? ColCyan : ColCyanDim;
        }
    }

    static void TopEdge(Transform parent, Color color, float height)
    {
        GameObject e = NewGO("TopEdge", parent);
        var rt = e.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, -height);
        rt.offsetMax = Vector2.zero;
        e.AddComponent<Image>().color = color;
    }

    static void LeftEdge(Transform parent, Color color, float width)
    {
        GameObject e = NewGO("LeftEdge", parent);
        var rt = e.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(width, 0f);
        e.AddComponent<Image>().color = color;
    }

    static void HRule(Transform parent, Color color, float height)
    {
        var r = NewGO("Rule", parent);
        r.AddComponent<Image>().color = color;
        LE(r, h: height);
    }

    static Button NavBtn(Transform parent, string name, string label, Color bg, float width)
    {
        GameObject go = NewGO(name, parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 0f);

        Image img = go.AddComponent<Image>();
        img.color = bg;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = bg;
        cb.highlightedColor = new Color(
            Mathf.Clamp01(bg.r + 0.20f),
            Mathf.Clamp01(bg.g + 0.20f),
            Mathf.Clamp01(bg.b + 0.20f), 1f);
        cb.pressedColor  = new Color(bg.r * 0.75f, bg.g * 0.75f, bg.b * 0.75f, 1f);
        cb.fadeDuration  = 0.08f;
        btn.colors = cb;

        var lbl = MakeTMP("Label", go.transform,
            label, 16, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        FillParent(lbl.gameObject);

        return btn;
    }

    static Button InstrBtn(Transform parent, string name, string label, Color bg, float width)
    {
        GameObject go = NewGO(name, parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 0f);

        Image img = go.AddComponent<Image>();
        img.color = bg;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = bg;
        cb.highlightedColor = new Color(
            Mathf.Clamp01(bg.r + 0.18f),
            Mathf.Clamp01(bg.g + 0.18f),
            Mathf.Clamp01(bg.b + 0.18f), 1f);
        cb.pressedColor = new Color(bg.r * 0.75f, bg.g * 0.75f, bg.b * 0.75f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        var lbl = MakeTMP("Label", go.transform,
            label, 17, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        FillParent(lbl.gameObject);

        return btn;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Micro helpers
    // ═════════════════════════════════════════════════════════════════════════

    static bool Validate(UIManager ui, out Canvas canvas)
    {
        canvas = null;
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Builder",
                "No UIManager found in the active scene.\nOpen a stage scene first.", "OK");
            return false;
        }

        // Always use the ROOT canvas so the panel fills the whole screen.
        // GetComponentInParent can return a constrained nested canvas, which would
        // make the panel tiny. rootCanvas walks up to the top-level canvas.
        Canvas nearest = ui.GetComponentInParent<Canvas>()
                      ?? Object.FindFirstObjectByType<Canvas>();
        canvas = nearest != null ? nearest.rootCanvas : null;

        if (canvas != null) return true;
        EditorUtility.DisplayDialog("Builder", "No Canvas found in scene.", "OK");
        return false;
    }

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void FillParent(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void CentreAnchor(GameObject go, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float size, FontStyles style, TextAlignmentOptions align, Color color)
    {
        var go  = NewGO(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.alignment = align; tmp.color = color; tmp.raycastTarget = false;
        return tmp;
    }

    static void Spacer(Transform parent, float h) => LE(NewGO("Spacer", parent), h: h);

    static void LE(GameObject go, float w = -1, float h = -1)
    {
        var le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth  = w;
        if (h >= 0) le.preferredHeight = h;
    }
}
