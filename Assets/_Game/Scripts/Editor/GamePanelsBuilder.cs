using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds Pause, Game Over, and Win panels for any stage scene,
/// auto-wiring every UIManager reference.
///
/// Run via:  Colony Under Siege → Rebuild Pause Panel
///           Colony Under Siege → Rebuild Game Over Panel
///           Colony Under Siege → Rebuild Win Panel
/// </summary>
public static class GamePanelsBuilder
{
    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color ColBg          = new Color(0.02f, 0.04f, 0.09f, 0.92f);
    static readonly Color ColCard        = new Color(0.06f, 0.09f, 0.17f, 1.00f);
    static readonly Color ColCyan        = new Color(0.00f, 0.84f, 1.00f, 1.00f);
    static readonly Color ColCyanLine    = new Color(0.00f, 0.84f, 1.00f, 0.55f);
    static readonly Color ColGreen       = new Color(0.00f, 0.85f, 0.45f, 1.00f);
    static readonly Color ColGreenDim    = new Color(0.00f, 0.85f, 0.45f, 0.55f);
    static readonly Color ColRed         = new Color(0.92f, 0.15f, 0.15f, 1.00f);
    static readonly Color ColRedDim      = new Color(0.92f, 0.15f, 0.15f, 0.55f);
    static readonly Color ColRedBg       = new Color(0.10f, 0.02f, 0.02f, 0.96f);
    static readonly Color ColSection     = new Color(0.38f, 0.62f, 0.78f, 1.00f);
    static readonly Color ColSubtext     = new Color(0.60f, 0.70f, 0.85f, 1.00f);
    static readonly Color ColStatLabel   = new Color(0.42f, 0.58f, 0.76f, 1.00f);
    static readonly Color ColStatValue   = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    static readonly Color ColSliderBg    = new Color(0.10f, 0.15f, 0.25f, 1.00f);
    static readonly Color ColDivider     = new Color(0.12f, 0.17f, 0.27f, 1.00f);
    static readonly Color ColBtnCyan     = new Color(0.00f, 0.60f, 0.75f, 1.00f);
    static readonly Color ColBtnDark     = new Color(0.14f, 0.18f, 0.26f, 1.00f);
    static readonly Color ColBtnRed      = new Color(0.55f, 0.10f, 0.10f, 1.00f);
    static readonly Color ColBtnGreen    = new Color(0.05f, 0.52f, 0.30f, 1.00f);

    // ═════════════════════════════════════════════════════════════════════════
    // PAUSE
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Pause Panel")]
    public static void BuildPause()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (!Validate(ui, out Canvas canvas)) return;

        if (ui.pausePanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "pausePanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(ui.pausePanel);
        }

        Undo.RecordObject(ui, "Build Pause Panel");

        // ── Full-screen semi-transparent overlay ─────────────────────────────
        GameObject panel = NewGO("PausePanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Pause Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColBg;

        // Dim overlay (separate GO so UIManager can reference it)
        GameObject overlay = NewGO("PauseOverlay", panel.transform);
        FillParent(overlay);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        // ── Centred card (700 × 660) ─────────────────────────────────────────
        GameObject card = NewGO("Card", panel.transform);
        CentreAnchor(card, 700, 660);
        card.AddComponent<Image>().color = ColCard;
        TopEdge(card.transform, ColCyan);

        // ── Content column ───────────────────────────────────────────────────
        GameObject col = ContentColumn(card.transform, 52, 52, 40, 36);

        MakeTitle(col.transform, "PAUSED", Color.white);
        Spacer(col.transform, 10);
        Rule(col.transform, ColCyanLine);
        Spacer(col.transform, 22);

        // ── MOUSE ────────────────────────────────────────────────────────────
        SectionLabel(col.transform, "MOUSE");
        Spacer(col.transform, 10);
        Slider sensSlider = SliderRow(col.transform, "Sensitivity",
            out TextMeshProUGUI sensTxt, 0.5f, 10f, 2f);

        Spacer(col.transform, 18);
        Divider(col.transform);
        Spacer(col.transform, 18);

        // ── AUDIO ────────────────────────────────────────────────────────────
        SectionLabel(col.transform, "AUDIO");
        Spacer(col.transform, 10);
        Slider masterSlider = SliderRow(col.transform, "Master Volume",
            out TextMeshProUGUI masterTxt, 0f, 1f, 1f);
        Spacer(col.transform, 8);
        Slider musicSlider  = SliderRow(col.transform, "Music Volume",
            out TextMeshProUGUI musicTxt, 0f, 1f, 1f);
        Spacer(col.transform, 8);
        Slider sfxSlider    = SliderRow(col.transform, "SFX Volume",
            out TextMeshProUGUI sfxTxt, 0f, 1f, 1f);

        Spacer(col.transform, 24);
        Divider(col.transform);
        Spacer(col.transform, 22);

        // ── Button row ───────────────────────────────────────────────────────
        Button resumeBtn  = default, restartBtn = default, quitBtn = default;
        ButtonRow(col.transform, 580, 52, 16, row =>
        {
            resumeBtn  = Btn(row, "ResumeButton",  "▶  RESUME",  ColBtnCyan, 240);
            restartBtn = Btn(row, "RestartButton", "↺  RESTART", ColBtnDark, 150);
            quitBtn    = Btn(row, "QuitButton",    "✕  QUIT",    ColBtnRed,  150);
        });

        // ── Wire ─────────────────────────────────────────────────────────────
        ui.pausePanel            = panel;
        ui.pauseOverlay          = overlay;
        ui.resumeButton          = resumeBtn;
        ui.pauseRestartButton    = restartBtn;
        ui.quitButton            = quitBtn;
        ui.sensitivitySlider     = sensSlider;
        ui.sensitivityValueText  = sensTxt;
        ui.masterVolumeSlider    = masterSlider;
        ui.masterVolumeValueText = masterTxt;
        ui.musicVolumeSlider     = musicSlider;
        ui.musicVolumeValueText  = musicTxt;
        ui.sfxVolumeSlider       = sfxSlider;
        ui.sfxVolumeValueText    = sfxTxt;

        panel.SetActive(false);
        EditorUtility.SetDirty(ui);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Pause panel built and wired.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GAME OVER
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Game Over Panel")]
    public static void BuildGameOver()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (!Validate(ui, out Canvas canvas)) return;

        if (ui.gameOverPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "gameOverPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(ui.gameOverPanel);
        }

        Undo.RecordObject(ui, "Build Game Over Panel");

        // ── Full-screen dark-red overlay ─────────────────────────────────────
        GameObject panel = NewGO("GameOverPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Game Over Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColRedBg;

        // ── Centred card (620 × 380) ─────────────────────────────────────────
        GameObject card = NewGO("Card", panel.transform);
        CentreAnchor(card, 620, 380);
        card.AddComponent<Image>().color = new Color(0.10f, 0.04f, 0.04f, 1f);
        TopEdge(card.transform, ColRed);

        // Left red accent bar
        GameObject leftBar = NewGO("LeftAccent", card.transform);
        var lbRt = leftBar.GetComponent<RectTransform>();
        lbRt.anchorMin = new Vector2(0f, 0f);
        lbRt.anchorMax = new Vector2(0f, 1f);
        lbRt.offsetMin = Vector2.zero;
        lbRt.offsetMax = new Vector2(4f, 0f);
        leftBar.AddComponent<Image>().color = ColRed;

        // ── Content ──────────────────────────────────────────────────────────
        GameObject col = ContentColumn(card.transform, 52, 40, 44, 44);

        Rule(col.transform, ColRedDim);
        Spacer(col.transform, 22);

        var headline = MakeTMP("HeadlineText", col.transform,
            "MISSION FAILED", 48, FontStyles.Bold, TextAlignmentOptions.Center, ColRed);
        LE(headline.gameObject, h: 58);

        Spacer(col.transform, 12);

        var msgTmp = MakeTMP("GameOverMessageText", col.transform,
            "The colony has fallen. Try again, soldier.",
            18, FontStyles.Italic, TextAlignmentOptions.Center, ColSubtext);
        msgTmp.enableWordWrapping = true;
        LE(msgTmp.gameObject, h: 50);

        Spacer(col.transform, 30);
        Rule(col.transform, ColRedDim);
        Spacer(col.transform, 24);

        Button restartBtn = default;
        ButtonRow(col.transform, 540, 52, 16, row =>
        {
            restartBtn = Btn(row, "RestartButton", "↺  TRY AGAIN", ColBtnRed, 260);
        });

        // ── Wire ─────────────────────────────────────────────────────────────
        ui.gameOverPanel          = panel;
        ui.gameOverMessageText    = msgTmp;
        ui.gameOverRestartButton  = restartBtn;

        panel.SetActive(false);
        EditorUtility.SetDirty(ui);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Game Over panel built and wired.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // WIN
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Win Panel")]
    public static void BuildWin()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (!Validate(ui, out Canvas canvas)) return;

        if (ui.winPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "winPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(ui.winPanel);
        }

        Undo.RecordObject(ui, "Build Win Panel");

        // ── Full-screen overlay ──────────────────────────────────────────────
        GameObject panel = NewGO("WinPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Win Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = new Color(0.02f, 0.06f, 0.04f, 0.95f);

        // ── Centred card (700 × 580) ─────────────────────────────────────────
        GameObject card = NewGO("Card", panel.transform);
        CentreAnchor(card, 700, 580);
        card.AddComponent<Image>().color = new Color(0.05f, 0.10f, 0.08f, 1f);
        TopEdge(card.transform, ColGreen);

        // Left green accent bar
        GameObject leftBar = NewGO("LeftAccent", card.transform);
        var lbRt = leftBar.GetComponent<RectTransform>();
        lbRt.anchorMin = new Vector2(0f, 0f);
        lbRt.anchorMax = new Vector2(0f, 1f);
        lbRt.offsetMin = Vector2.zero;
        lbRt.offsetMax = new Vector2(4f, 0f);
        leftBar.AddComponent<Image>().color = ColGreen;

        // ── Content ──────────────────────────────────────────────────────────
        GameObject col = ContentColumn(card.transform, 52, 40, 40, 36);

        var headline = MakeTMP("HeadlineText", col.transform,
            "MISSION COMPLETE", 36, FontStyles.Bold, TextAlignmentOptions.Center, ColGreen);
        LE(headline.gameObject, h: 46);

        Spacer(col.transform, 6);

        var msgTmp = MakeTMP("WinMessageText", col.transform,
            "Outstanding work, soldier. The colony is safe.",
            16, FontStyles.Italic, TextAlignmentOptions.Center, ColSubtext);
        LE(msgTmp.gameObject, h: 26);

        Spacer(col.transform, 14);
        Rule(col.transform, ColGreenDim);
        Spacer(col.transform, 16);

        // ── Stats grid (2 columns) ────────────────────────────────────────────
        GameObject grid = NewGO("StatsGrid", col.transform);
        var glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(285, 66);
        glg.spacing         = new Vector2(18, 10);
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.UpperLeft;
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        LE(grid, h: 156); // 2 rows × 66 + 1 gap × 10 + small buffer

        TextMeshProUGUI gradeTmp, scoreTmp, killsTmp, accTmp, timeTmp,
                        killPtsTmp, accBonusTmp, timeBonusTmp;

        StatBlock(grid.transform, "GRADE",       "--",    out gradeTmp);
        StatBlock(grid.transform, "SCORE",       "0",     out scoreTmp);
        StatBlock(grid.transform, "KILLS",       "0",     out killsTmp);
        StatBlock(grid.transform, "ACCURACY",    "0%",    out accTmp);
        StatBlock(grid.transform, "TIME",        "0:00",  out timeTmp);
        StatBlock(grid.transform, "KILL PTS",    "0",     out killPtsTmp);
        StatBlock(grid.transform, "ACC BONUS",   "+0",    out accBonusTmp);
        StatBlock(grid.transform, "TIME BONUS",  "+0",    out timeBonusTmp);

        Spacer(col.transform, 14);
        Rule(col.transform, ColGreenDim);
        Spacer(col.transform, 18);

        // ── Button row ────────────────────────────────────────────────────────
        Button restartBtn = default, nextBtn = default, menuBtn = default;
        ButtonRow(col.transform, 596, 50, 14, row =>
        {
            restartBtn = Btn(row, "RestartButton",   "↺ RESTART",    ColBtnDark,  160);
            nextBtn    = Btn(row, "NextLevelButton", "▶ NEXT STAGE", ColBtnGreen, 200);
            menuBtn    = Btn(row, "MainMenuButton",  "⌂ MAIN MENU",  ColBtnDark,  160);
        });

        // ── Wire ─────────────────────────────────────────────────────────────
        ui.winPanel          = panel;
        ui.winMessageText    = msgTmp;
        ui.winRestartButton  = restartBtn;
        ui.nextLevelButton   = nextBtn;
        ui.mainMenuButton    = menuBtn;
        ui.gradeText         = gradeTmp;
        ui.scoreText         = scoreTmp;
        ui.killsText         = killsTmp;
        ui.accuracyText      = accTmp;
        ui.timeText          = timeTmp;
        ui.killPointsText    = killPtsTmp;
        ui.accuracyBonusText = accBonusTmp;
        ui.timeBonusText     = timeBonusTmp;

        panel.SetActive(false);
        EditorUtility.SetDirty(ui);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Win panel built and wired.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Shared UI helpers
    // ═════════════════════════════════════════════════════════════════════════

    static void StatBlock(Transform parent, string label, string defVal,
        out TextMeshProUGUI valueTmp)
    {
        GameObject block = NewGO(label.Replace(" ", "") + "Block", parent);
        block.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.10f, 1f);

        var vlg = block.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(14, 10, 10, 6);
        vlg.spacing               = 2;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var lbl = MakeTMP(label + "Label", block.transform,
            label, 11, FontStyles.Bold, TextAlignmentOptions.Left, ColStatLabel);
        lbl.characterSpacing = 1.5f;
        LE(lbl.gameObject, h: 18);

        valueTmp = MakeTMP(label + "Value", block.transform,
            defVal, 26, FontStyles.Bold, TextAlignmentOptions.Left, ColStatValue);
        LE(valueTmp.gameObject, h: 34);
    }

    static Slider SliderRow(Transform parent, string label,
        out TextMeshProUGUI valueTmp, float min, float max, float def)
    {
        GameObject row = NewGO(label.Replace(" ", "") + "Row", parent);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 14;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        LE(row, h: 38);

        var lbl = MakeTMP(label + "Lbl", row.transform,
            label, 15, FontStyles.Normal, TextAlignmentOptions.Left, ColSubtext);
        LE(lbl.gameObject, w: 155, h: 38);

        Slider slider = BuildSlider(label + "Slider", row.transform, min, max, def);
        LE(slider.gameObject, w: 310, h: 38);

        string display = label.Contains("Volume")
            ? Mathf.RoundToInt(def * 100f) + "%" : def.ToString("F1");
        valueTmp = MakeTMP(label + "Val", row.transform,
            display, 15, FontStyles.Bold, TextAlignmentOptions.Right, ColCyan);
        LE(valueTmp.gameObject, w: 54, h: 38);

        return slider;
    }

    static Slider BuildSlider(string name, Transform parent, float min, float max, float val)
    {
        GameObject go = NewGO(name, parent);

        GameObject bg = NewGO("Background", go.transform);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.4f); bgRt.anchorMax = new Vector2(1f, 0.6f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = ColSliderBg;

        GameObject fillArea = NewGO("Fill Area", go.transform);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.4f); faRt.anchorMax = new Vector2(1f, 0.6f);
        faRt.offsetMin = new Vector2(0f, 0f);   faRt.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = NewGO("Fill", fillArea.transform);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = ColCyan;

        GameObject handleArea = NewGO("Handle Slide Area", go.transform);
        var haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(10f, 0f); haRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = NewGO("Handle", handleArea.transform);
        var hRt = handle.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 0f); hRt.anchorMax = new Vector2(0f, 1f);
        hRt.sizeDelta = new Vector2(20f, 0f); hRt.anchoredPosition = Vector2.zero;
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect      = fill.GetComponent<RectTransform>();
        slider.handleRect    = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = min;
        slider.maxValue      = max;
        slider.value         = val;

        return slider;
    }

    static void ButtonRow(Transform parent, float width, float height, float spacing,
        System.Action<Transform> populate)
    {
        GameObject row = NewGO("ButtonRow", parent);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = spacing;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        LE(row, w: width, h: height);
        populate(row.transform);
    }

    static Button Btn(Transform parent, string name, string label, Color bg, float width)
    {
        GameObject go = NewGO(name, parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 0f);

        Image img = go.AddComponent<Image>();
        img.color = bg;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.highlightedColor = new Color(
            Mathf.Clamp01(bg.r + 0.18f),
            Mathf.Clamp01(bg.g + 0.18f),
            Mathf.Clamp01(bg.b + 0.18f), 1f);
        cb.pressedColor = new Color(bg.r * 0.7f, bg.g * 0.7f, bg.b * 0.7f, 1f);
        btn.colors = cb;

        var lbl = MakeTMP("Label", go.transform,
            label, 17, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        FillParent(lbl.gameObject);

        return btn;
    }

    static void SectionLabel(Transform parent, string text)
    {
        var lbl = MakeTMP(text + "Section", parent,
            text, 12, FontStyles.Bold, TextAlignmentOptions.Left, ColSection);
        lbl.characterSpacing = 2f;
        LE(lbl.gameObject, h: 20);
    }

    static void MakeTitle(Transform parent, string text, Color color)
    {
        var t = MakeTMP("TitleText", parent,
            text, 36, FontStyles.Bold, TextAlignmentOptions.Left, color);
        LE(t.gameObject, h: 48);
    }

    static void TopEdge(Transform parent, Color color)
    {
        GameObject e = NewGO("TopEdge", parent);
        var rt = e.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, -3f); rt.offsetMax = Vector2.zero;
        e.AddComponent<Image>().color = color;
    }

    static GameObject ContentColumn(Transform parent,
        int padL, int padR, int padT, int padB)
    {
        GameObject col = NewGO("ContentColumn", parent);
        FillParent(col);
        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(padL, padR, padT, padB);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        return col;
    }

    static void Rule(Transform parent, Color color)
    {
        var r = NewGO("Rule", parent);
        r.AddComponent<Image>().color = color;
        LE(r, h: 2);
    }

    static void Divider(Transform parent)
    {
        var d = NewGO("Divider", parent);
        d.AddComponent<Image>().color = ColDivider;
        LE(d, h: 1);
    }

    static void Spacer(Transform parent, float h)
        => LE(NewGO("Spacer", parent), h: h);

    // ── Validation ───────────────────────────────────────────────────────────

    static bool Validate(UIManager ui, out Canvas canvas)
    {
        canvas = null;
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Builder",
                "No UIManager found in the active scene.\n" +
                "Open a stage scene first.", "OK");
            return false;
        }
        canvas = ui.GetComponentInParent<Canvas>()
               ?? Object.FindFirstObjectByType<Canvas>();
        if (canvas != null) return true;
        EditorUtility.DisplayDialog("Builder", "No Canvas found in scene.", "OK");
        return false;
    }

    // ── Micro helpers ─────────────────────────────────────────────────────────

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void FillParent(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
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

    static void LE(GameObject go, float w = -1, float h = -1)
    {
        var le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth  = w;
        if (h >= 0) le.preferredHeight = h;
    }
}
