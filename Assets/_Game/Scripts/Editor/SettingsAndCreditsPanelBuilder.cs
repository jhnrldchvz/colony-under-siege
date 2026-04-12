using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the Settings and Credits panels, wiring every MainMenuManager reference.
/// Run via:  Colony Under Siege → Rebuild Settings Panel
///           Colony Under Siege → Rebuild Credits Panel
/// </summary>
public static class SettingsAndCreditsPanelBuilder
{
    // ── Shared palette (matches MainMenuPanelBuilder) ─────────────────────────
    static readonly Color ColBg       = new Color(0.03f, 0.05f, 0.10f, 0.96f);
    static readonly Color ColCard     = new Color(0.06f, 0.09f, 0.17f, 1.00f);
    static readonly Color ColCardEdge = new Color(0.00f, 0.84f, 1.00f, 0.25f);
    static readonly Color ColCyan     = new Color(0.00f, 0.84f, 1.00f, 1.00f);
    static readonly Color ColCyanLine = new Color(0.00f, 0.84f, 1.00f, 0.55f);
    static readonly Color ColSection  = new Color(0.38f, 0.62f, 0.78f, 1.00f);
    static readonly Color ColSubtext  = new Color(0.60f, 0.70f, 0.85f, 1.00f);
    static readonly Color ColSliderBg = new Color(0.10f, 0.15f, 0.25f, 1.00f);
    static readonly Color ColFill     = new Color(0.00f, 0.84f, 1.00f, 1.00f);
    static readonly Color ColHandle   = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    static readonly Color ColBackBtn  = new Color(0.14f, 0.18f, 0.26f, 1.00f);
    static readonly Color ColBackHov  = new Color(0.22f, 0.26f, 0.36f, 1.00f);
    static readonly Color ColDivider  = new Color(0.12f, 0.17f, 0.27f, 1.00f);
    static readonly Color ColCredit1  = new Color(0.90f, 0.92f, 1.00f, 1.00f); // name
    static readonly Color ColCredit2  = new Color(0.42f, 0.62f, 0.80f, 1.00f); // role

    // ═════════════════════════════════════════════════════════════════════════
    // SETTINGS
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Settings Panel")]
    public static void BuildSettings()
    {
        MainMenuManager mgr = Object.FindFirstObjectByType<MainMenuManager>();
        if (!ValidateScene(mgr, out Canvas canvas)) return;

        if (mgr.settingsPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "settingsPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(mgr.settingsPanel);
        }

        Undo.RecordObject(mgr, "Build Settings Panel");

        // ── Full-screen dark overlay ─────────────────────────────────────────
        GameObject panel = NewGO("SettingsPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Settings Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColBg;

        // ── Centred card (760 × 600) ─────────────────────────────────────────
        GameObject card = NewGO("Card", panel.transform);
        CentreAnchor(card, 760, 600);
        card.AddComponent<Image>().color = ColCard;

        // Thin cyan top edge
        GameObject topEdge = NewGO("TopEdge", card.transform);
        var teRt = topEdge.GetComponent<RectTransform>();
        teRt.anchorMin = new Vector2(0f, 1f);
        teRt.anchorMax = new Vector2(1f, 1f);
        teRt.offsetMin = new Vector2(0f, -3f);
        teRt.offsetMax = Vector2.zero;
        topEdge.AddComponent<Image>().color = ColCyan;

        // ── Content column ───────────────────────────────────────────────────
        GameObject col = NewGO("ContentColumn", card.transform);
        FillParent(col);
        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(52, 52, 44, 44);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Title
        var title = MakeTMP("TitleText", col.transform,
            "SETTINGS", 38, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
        LE(title.gameObject, h: 50);

        Spacer(col.transform, 10);

        // Cyan rule
        var rule = NewGO("Rule", col.transform);
        rule.AddComponent<Image>().color = ColCyanLine;
        LE(rule, h: 2);

        Spacer(col.transform, 28);

        // ── MOUSE section ────────────────────────────────────────────────────
        SectionLabel(col.transform, "MOUSE");
        Spacer(col.transform, 12);

        Slider sensSlider = SliderRow(col.transform, "Sensitivity",
            out TextMeshProUGUI sensTxt, 0.5f, 10f, 2f);

        Spacer(col.transform, 24);
        Divider(col.transform);
        Spacer(col.transform, 24);

        // ── AUDIO section ────────────────────────────────────────────────────
        SectionLabel(col.transform, "AUDIO");
        Spacer(col.transform, 12);

        Slider masterSlider = SliderRow(col.transform, "Master Volume",
            out TextMeshProUGUI masterTxt, 0f, 1f, 1f);
        Spacer(col.transform, 10);

        Slider musicSlider  = SliderRow(col.transform, "Music Volume",
            out TextMeshProUGUI musicTxt, 0f, 1f, 1f);
        Spacer(col.transform, 10);

        Slider sfxSlider    = SliderRow(col.transform, "SFX Volume",
            out TextMeshProUGUI sfxTxt, 0f, 1f, 1f);

        Spacer(col.transform, 36);

        // ── Back button (right-aligned) ──────────────────────────────────────
        Button backBtn = ActionButton("BackButton", col.transform, "← BACK",
            ColBackBtn, ColBackHov, 180, 48, TextAlignmentOptions.Center);

        // ── Wire ─────────────────────────────────────────────────────────────
        mgr.settingsPanel          = panel;
        mgr.sensitivitySlider      = sensSlider;
        mgr.sensitivityValueText   = sensTxt;
        mgr.masterVolumeSlider     = masterSlider;
        mgr.masterVolumeValueText  = masterTxt;
        mgr.musicVolumeSlider      = musicSlider;
        mgr.musicVolumeValueText   = musicTxt;
        mgr.sfxVolumeSlider        = sfxSlider;
        mgr.sfxVolumeValueText     = sfxTxt;
        mgr.backFromSettingsButton = backBtn;

        panel.SetActive(false);
        EditorUtility.SetDirty(mgr);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Settings panel built and wired.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CREDITS
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Credits Panel")]
    public static void BuildCredits()
    {
        MainMenuManager mgr = Object.FindFirstObjectByType<MainMenuManager>();
        if (!ValidateScene(mgr, out Canvas canvas)) return;

        if (mgr.creditsPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "creditsPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(mgr.creditsPanel);
        }

        Undo.RecordObject(mgr, "Build Credits Panel");

        // ── Full-screen dark overlay ─────────────────────────────────────────
        GameObject panel = NewGO("CreditsPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Credits Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColBg;

        // ── Centred card (640 × 600) ─────────────────────────────────────────
        GameObject card = NewGO("Card", panel.transform);
        CentreAnchor(card, 640, 600);
        card.AddComponent<Image>().color = ColCard;

        // Thin cyan top edge
        GameObject topEdge = NewGO("TopEdge", card.transform);
        var teRt = topEdge.GetComponent<RectTransform>();
        teRt.anchorMin = new Vector2(0f, 1f);
        teRt.anchorMax = new Vector2(1f, 1f);
        teRt.offsetMin = new Vector2(0f, -3f);
        teRt.offsetMax = Vector2.zero;
        topEdge.AddComponent<Image>().color = ColCyan;

        // ── Content column ───────────────────────────────────────────────────
        GameObject col = NewGO("ContentColumn", card.transform);
        FillParent(col);
        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(52, 52, 44, 44);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Title
        var title = MakeTMP("TitleText", col.transform,
            "CREDITS", 38, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
        LE(title.gameObject, h: 50);

        Spacer(col.transform, 10);

        var rule = NewGO("Rule", col.transform);
        rule.AddComponent<Image>().color = ColCyanLine;
        LE(rule, h: 2);

        Spacer(col.transform, 30);

        // ── Credit rows ──────────────────────────────────────────────────────
        CreditRow(col.transform, "GAME DEVELOPER",      "Jhon Arol De Chavez");
        Spacer(col.transform, 6);
        CreditRow(col.transform, "LEVEL DESIGN",        "Jhon Arol De Chavez");
        Spacer(col.transform, 6);
        CreditRow(col.transform, "PROGRAMMING",         "Jhon Arol De Chavez");

        Spacer(col.transform, 20);
        Divider(col.transform);
        Spacer(col.transform, 20);

        CreditRow(col.transform, "3D ASSETS",           "Unity Asset Store");
        Spacer(col.transform, 6);
        CreditRow(col.transform, "SOUND EFFECTS",       "Unity Asset Store");
        Spacer(col.transform, 6);
        CreditRow(col.transform, "MUSIC",               "Unity Asset Store");

        Spacer(col.transform, 20);
        Divider(col.transform);
        Spacer(col.transform, 20);

        CreditRow(col.transform, "ENGINE",              "Unity 6");
        Spacer(col.transform, 6);
        CreditRow(col.transform, "BUILT WITH",          "Claude Code — Anthropic");

        Spacer(col.transform, 36);

        // ── Back button ──────────────────────────────────────────────────────
        Button backBtn = ActionButton("BackButton", col.transform, "← BACK",
            ColBackBtn, ColBackHov, 180, 48, TextAlignmentOptions.Center);

        // ── Wire ─────────────────────────────────────────────────────────────
        mgr.creditsPanel          = panel;
        mgr.backFromCreditsButton = backBtn;

        panel.SetActive(false);
        EditorUtility.SetDirty(mgr);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Credits panel built and wired.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Shared UI helpers
    // ═════════════════════════════════════════════════════════════════════════

    // ── Slider row:  [Label .............. Slider ............... Value] ─────

    static Slider SliderRow(Transform parent, string label,
        out TextMeshProUGUI valueTmp, float min, float max, float defaultVal)
    {
        GameObject row = NewGO(label.Replace(" ", "") + "Row", parent);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 16;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        LE(row, h: 40);

        // Label
        var lbl = MakeTMP(label + "Label", row.transform,
            label, 16, FontStyles.Normal, TextAlignmentOptions.Left, ColSubtext);
        lbl.raycastTarget = false;
        LE(lbl.gameObject, w: 160, h: 40);

        // Slider
        Slider slider = BuildSlider(label + "Slider", row.transform, min, max, defaultVal);
        LE(slider.gameObject, w: 310, h: 40);

        // Value text
        string display = label.Contains("Volume")
            ? Mathf.RoundToInt(defaultVal * 100f) + "%"
            : defaultVal.ToString("F1");

        valueTmp = MakeTMP(label + "Value", row.transform,
            display, 16, FontStyles.Bold, TextAlignmentOptions.Right, ColCyan);
        valueTmp.raycastTarget = false;
        LE(valueTmp.gameObject, w: 56, h: 40);

        return slider;
    }

    static Slider BuildSlider(string name, Transform parent, float min, float max, float val)
    {
        GameObject go = NewGO(name, parent);

        // Track background (full height, slim visual)
        GameObject bg = NewGO("Background", go.transform);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.4f);
        bgRt.anchorMax = new Vector2(1f, 0.6f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = ColSliderBg;

        // Fill area
        GameObject fillArea = NewGO("Fill Area", go.transform);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.4f);
        faRt.anchorMax = new Vector2(1f, 0.6f);
        faRt.offsetMin = new Vector2(0f, 0f);
        faRt.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = NewGO("Fill", fillArea.transform);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = ColFill;

        // Handle slide area
        GameObject handleArea = NewGO("Handle Slide Area", go.transform);
        var haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(10f, 0f);
        haRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = NewGO("Handle", handleArea.transform);
        var hRt = handle.GetComponent<RectTransform>();
        hRt.anchorMin        = new Vector2(0f, 0f);
        hRt.anchorMax        = new Vector2(0f, 1f);
        hRt.sizeDelta        = new Vector2(20f, 0f);
        hRt.anchoredPosition = Vector2.zero;
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = ColHandle;

        // Slider component
        Slider slider = go.AddComponent<Slider>();
        slider.fillRect     = fill.GetComponent<RectTransform>();
        slider.handleRect   = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImg;
        slider.direction    = Slider.Direction.LeftToRight;
        slider.minValue     = min;
        slider.maxValue     = max;
        slider.value        = val;

        ColorBlock cb          = ColorBlock.defaultColorBlock;
        cb.highlightedColor    = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.pressedColor        = ColCyan;
        slider.colors          = cb;

        return slider;
    }

    // ── Credit row:  ROLE (muted)  —  Name (bright) ──────────────────────────
    static void CreditRow(Transform parent, string role, string name)
    {
        GameObject row = NewGO(role.Replace(" ", "") + "Row", parent);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 0;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        LE(row, h: 30);

        var roleTmp = MakeTMP(role + "Role", row.transform,
            role, 13, FontStyles.Bold, TextAlignmentOptions.Left, ColCredit2);
        roleTmp.characterSpacing = 1f;
        LE(roleTmp.gameObject, w: 240, h: 30);

        var nameTmp = MakeTMP(role + "Name", row.transform,
            name, 15, FontStyles.Bold, TextAlignmentOptions.Left, ColCredit1);
        LE(nameTmp.gameObject, w: 290, h: 30);
    }

    // ── Section label ─────────────────────────────────────────────────────────
    static void SectionLabel(Transform parent, string text)
    {
        var lbl = MakeTMP(text + "Section", parent,
            text, 13, FontStyles.Bold, TextAlignmentOptions.Left, ColSection);
        lbl.characterSpacing = 2f;
        LE(lbl.gameObject, h: 22);
    }

    // ── Action button (Back etc.) ─────────────────────────────────────────────
    static Button ActionButton(string name, Transform parent, string label,
        Color bg, Color hover, float w, float h, TextAlignmentOptions align)
    {
        GameObject go = NewGO(name, parent);
        Image img     = go.AddComponent<Image>();
        img.color     = bg;

        Button btn    = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb      = ColorBlock.defaultColorBlock;
        cb.normalColor     = Color.white;
        cb.highlightedColor = new Color(
            hover.r / bg.r * 1.2f, hover.g / bg.g * 1.2f, hover.b / bg.b * 1.2f, 1f);
        cb.pressedColor    = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors         = cb;

        LE(go, w: w, h: h);

        var tmp = MakeTMP("Label", go.transform,
            label, 18, FontStyles.Bold, align, Color.white);
        FillParent(tmp.gameObject);

        return btn;
    }

    // ── Thin divider line ─────────────────────────────────────────────────────
    static void Divider(Transform parent)
    {
        var d = NewGO("Divider", parent);
        d.AddComponent<Image>().color = ColDivider;
        LE(d, h: 1);
    }

    // ── Spacer ────────────────────────────────────────────────────────────────
    static void Spacer(Transform parent, float h)
    {
        LE(NewGO("Spacer", parent), h: h);
    }

    // ── Centre-anchor a GO on screen ──────────────────────────────────────────
    static void CentreAnchor(GameObject go, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(w, h);
    }

    // ── Validation ───────────────────────────────────────────────────────────
    static bool ValidateScene(MainMenuManager mgr, out Canvas canvas)
    {
        canvas = null;
        if (mgr == null)
        {
            EditorUtility.DisplayDialog("Builder",
                "No MainMenuManager found in the active scene.", "OK");
            return false;
        }
        canvas = mgr.GetComponentInParent<Canvas>()
               ?? Object.FindFirstObjectByType<Canvas>();
        if (canvas != null) return true;
        EditorUtility.DisplayDialog("Builder", "No Canvas found in the active scene.", "OK");
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
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float size, FontStyles style, TextAlignmentOptions align, Color color)
    {
        var go  = NewGO(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.fontStyle     = style;
        tmp.alignment     = align;
        tmp.color         = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void LE(GameObject go, float w = -1, float h = -1)
    {
        var le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth  = w;
        if (h >= 0) le.preferredHeight = h;
    }
}
