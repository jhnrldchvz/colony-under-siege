using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the Settings and Credits panels for the main menu scene,
/// auto-wiring every MainMenuManager reference.
///
/// Run via:  Colony Under Siege → Rebuild Settings Panel (Main Menu)
///           Colony Under Siege → Rebuild Credits Panel (Main Menu)
/// </summary>
public static class MainMenuSubPanelsBuilder
{
    // ── Palette ───────────────────────────────────────────────────────────────
    static readonly Color ColOverlay      = new Color(0.02f, 0.04f, 0.09f, 0.96f);
    static readonly Color ColCard         = new Color(0.05f, 0.08f, 0.15f, 1.00f);
    static readonly Color ColCardDark     = new Color(0.03f, 0.05f, 0.10f, 1.00f);
    static readonly Color ColCyan         = new Color(0.00f, 0.84f, 1.00f, 1.00f);
    static readonly Color ColCyanDim      = new Color(0.00f, 0.84f, 1.00f, 0.18f);
    static readonly Color ColCyanLine     = new Color(0.00f, 0.84f, 1.00f, 0.55f);
    static readonly Color ColSubtext      = new Color(0.65f, 0.75f, 0.90f, 1.00f);
    static readonly Color ColMuted        = new Color(0.40f, 0.50f, 0.65f, 1.00f);
    static readonly Color ColSliderTrack  = new Color(0.10f, 0.16f, 0.28f, 1.00f);
    static readonly Color ColSliderFill   = new Color(0.00f, 0.70f, 0.90f, 1.00f);
    static readonly Color ColSliderHandle = new Color(0.00f, 0.84f, 1.00f, 1.00f);
    static readonly Color ColBtnBack      = new Color(0.10f, 0.15f, 0.26f, 1.00f);
    static readonly Color ColBtnBackHover = new Color(0.15f, 0.23f, 0.38f, 1.00f);
    static readonly Color ColDivider      = new Color(0.12f, 0.18f, 0.28f, 1.00f);
    static readonly Color ColSectionLabel = new Color(0.00f, 0.84f, 1.00f, 0.70f);
    static readonly Color ColValueText    = new Color(0.00f, 0.84f, 1.00f, 1.00f);
    static readonly Color ColCreditsRole  = new Color(0.55f, 0.65f, 0.82f, 1.00f);
    static readonly Color ColCreditsName  = new Color(0.90f, 0.95f, 1.00f, 1.00f);
    static readonly Color ColCreditsDim   = new Color(0.35f, 0.45f, 0.60f, 1.00f);

    // ═════════════════════════════════════════════════════════════════════════
    // SETTINGS PANEL — main menu
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Settings Panel (Main Menu)")]
    public static void BuildSettings()
    {
        MainMenuManager mgr = Object.FindFirstObjectByType<MainMenuManager>();
        if (!ValidateMgr(mgr, out Canvas canvas)) return;

        if (mgr.settingsPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "settingsPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(mgr.settingsPanel);
            mgr.settingsPanel = null;
        }

        Undo.RecordObject(mgr, "Build Settings Panel");

        // ── Full-screen overlay ───────────────────────────────────────────────
        GameObject panel = NewGO("SettingsPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Settings Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColOverlay;

        // ── Centred card (700 × 560) ──────────────────────────────────────────
        GameObject card = NewGO("SettingsCard", panel.transform);
        CentreAnchor(card, 700, 560);
        card.AddComponent<Image>().color = ColCard;

        // Top cyan edge
        TopEdge(card.transform, ColCyan, 3f);
        // Left cyan accent bar
        LeftEdge(card.transform, ColCyan, 4f);

        // ── Content column ────────────────────────────────────────────────────
        GameObject col = NewGO("ContentColumn", card.transform);
        FillParent(col);
        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(40, 40, 32, 32);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Header ────────────────────────────────────────────────────────────
        var headerRow = NewGO("HeaderRow", col.transform);
        var hHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
        hHlg.spacing               = 14;
        hHlg.childAlignment        = TextAnchor.MiddleLeft;
        hHlg.childControlWidth     = false;
        hHlg.childControlHeight    = true;
        hHlg.childForceExpandWidth  = false;
        hHlg.childForceExpandHeight = true;
        LE(headerRow, h: 44);

        var tagBar = NewGO("TagBar", headerRow.transform);
        tagBar.AddComponent<Image>().color = ColCyan;
        LE(tagBar, w: 4, h: 44);

        var headerTmp = MakeTMP("HeaderTitle", headerRow.transform,
            "S E T T I N G S",
            18, FontStyles.Bold, TextAlignmentOptions.Left, ColCyan);
        headerTmp.characterSpacing = 2f;
        LE(headerTmp.gameObject, w: 400, h: 44);

        Spacer(col.transform, 12);
        HRule(col.transform, ColCyanLine, 1f);
        Spacer(col.transform, 24);

        // ── Mouse section ─────────────────────────────────────────────────────
        SectionLabel(col.transform, "  MOUSE");
        Spacer(col.transform, 10);

        Slider senSlider; TextMeshProUGUI senValueTmp;
        SliderRow(col.transform, "Mouse Sensitivity", 0.5f, 10f, 2f,
            "SensitivitySlider", "SensitivityValueText",
            out senSlider, out senValueTmp);

        Spacer(col.transform, 20);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 18);

        // ── Audio section ─────────────────────────────────────────────────────
        SectionLabel(col.transform, "  AUDIO");
        Spacer(col.transform, 10);

        Slider masterSlider; TextMeshProUGUI masterValueTmp;
        SliderRow(col.transform, "Master Volume", 0f, 1f, 1f,
            "MasterVolumeSlider", "MasterVolumeValueText",
            out masterSlider, out masterValueTmp);

        Spacer(col.transform, 8);

        Slider musicSlider; TextMeshProUGUI musicValueTmp;
        SliderRow(col.transform, "Music Volume", 0f, 1f, 1f,
            "MusicVolumeSlider", "MusicVolumeValueText",
            out musicSlider, out musicValueTmp);

        Spacer(col.transform, 8);

        Slider sfxSlider; TextMeshProUGUI sfxValueTmp;
        SliderRow(col.transform, "SFX Volume", 0f, 1f, 1f,
            "SFXVolumeSlider", "SFXVolumeValueText",
            out sfxSlider, out sfxValueTmp);

        Spacer(col.transform, 28);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 20);

        // ── Back button ───────────────────────────────────────────────────────
        var btnRow = NewGO("ButtonRow", col.transform);
        var bHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        bHlg.spacing               = 0;
        bHlg.childAlignment        = TextAnchor.MiddleRight;
        bHlg.childControlWidth     = false;
        bHlg.childControlHeight    = true;
        bHlg.childForceExpandWidth  = false;
        bHlg.childForceExpandHeight = true;
        LE(btnRow, h: 48);

        var flexSpacer = NewGO("FlexSpacer", btnRow.transform);
        flexSpacer.AddComponent<LayoutElement>().flexibleWidth = 1;

        Button backBtn = ActionBtn(btnRow.transform, "BackFromSettingsButton", "←  BACK", ColBtnBack, ColBtnBackHover, 160);

        // ── Wire MainMenuManager ──────────────────────────────────────────────
        mgr.settingsPanel          = panel;
        mgr.sensitivitySlider      = senSlider;
        mgr.sensitivityValueText   = senValueTmp;
        mgr.masterVolumeSlider     = masterSlider;
        mgr.masterVolumeValueText  = masterValueTmp;
        mgr.musicVolumeSlider      = musicSlider;
        mgr.musicVolumeValueText   = musicValueTmp;
        mgr.sfxVolumeSlider        = sfxSlider;
        mgr.sfxVolumeValueText     = sfxValueTmp;
        mgr.backFromSettingsButton = backBtn;

        panel.SetActive(false);
        EditorUtility.SetDirty(mgr);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Settings panel built and wired.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CREDITS PANEL — main menu
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Colony Under Siege/Rebuild Credits Panel (Main Menu)")]
    public static void BuildCredits()
    {
        MainMenuManager mgr = Object.FindFirstObjectByType<MainMenuManager>();
        if (!ValidateMgr(mgr, out Canvas canvas)) return;

        if (mgr.creditsPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "creditsPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(mgr.creditsPanel);
            mgr.creditsPanel = null;
        }

        Undo.RecordObject(mgr, "Build Credits Panel");

        // ── Full-screen overlay ───────────────────────────────────────────────
        GameObject panel = NewGO("CreditsPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Credits Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColOverlay;

        // ── Scrollable card (740 wide, fills most of screen height) ──────────
        // Card uses a ScrollRect so content can overflow without clipping.
        GameObject card = NewGO("CreditsCard", panel.transform);
        CentreAnchor(card, 740, 820);
        card.AddComponent<Image>().color = ColCard;

        TopEdge(card.transform, ColCyan, 3f);
        LeftEdge(card.transform, ColCyan, 4f);

        // ── Content column ────────────────────────────────────────────────────
        GameObject col = NewGO("ContentColumn", card.transform);
        FillParent(col);
        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(40, 40, 32, 32);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Header ────────────────────────────────────────────────────────────
        var headerRow = NewGO("HeaderRow", col.transform);
        var hHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
        hHlg.spacing               = 14;
        hHlg.childAlignment        = TextAnchor.MiddleLeft;
        hHlg.childControlWidth     = false;
        hHlg.childControlHeight    = true;
        hHlg.childForceExpandWidth  = false;
        hHlg.childForceExpandHeight = true;
        LE(headerRow, h: 44);

        var tagBar = NewGO("TagBar", headerRow.transform);
        tagBar.AddComponent<Image>().color = ColCyan;
        LE(tagBar, w: 4, h: 44);

        var headerTmp = MakeTMP("HeaderTitle", headerRow.transform,
            "C R E D I T S",
            18, FontStyles.Bold, TextAlignmentOptions.Left, ColCyan);
        headerTmp.characterSpacing = 2f;
        LE(headerTmp.gameObject, w: 400, h: 44);

        Spacer(col.transform, 12);
        HRule(col.transform, ColCyanLine, 1f);
        Spacer(col.transform, 22);

        // ── Game title ────────────────────────────────────────────────────────
        var gameTitleTmp = MakeTMP("GameTitle", col.transform,
            "COLONY UNDER SIEGE",
            24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        LE(gameTitleTmp.gameObject, h: 32);

        Spacer(col.transform, 3);

        var gameSubTmp = MakeTMP("GameSubtitle", col.transform,
            "3D Sci-Fi First Person Shooter",
            13, FontStyles.Italic, TextAlignmentOptions.Center, ColSubtext);
        LE(gameSubTmp.gameObject, h: 20);

        Spacer(col.transform, 20);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 18);

        // ── Game Developer ────────────────────────────────────────────────────
        CreditEntryMulti(col.transform, "GAME DEVELOPER",
            "Jhon Arol De Chavez", "Sydnell Bongato", "Juma Galvez", "Lara Jane Calamayo");

        Spacer(col.transform, 14);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 14);

        // ── Level Design ──────────────────────────────────────────────────────
        CreditEntryMulti(col.transform, "LEVEL DESIGN",
            "Jhon Arol De Chavez", "Sydnell Bongato", "Juma Galvez", "Lara Jane Calamayo");

        Spacer(col.transform, 14);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 14);

        // ── Programming ───────────────────────────────────────────────────────
        CreditEntrySingle(col.transform, "PROGRAMMING", "Jhon Arol De Chavez");

        Spacer(col.transform, 14);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 14);

        // ── 3rd Party Assets ──────────────────────────────────────────────────
        SectionLabel(col.transform, "  3RD PARTY ASSETS");
        Spacer(col.transform, 8);

        string[] thirdParty = new[]
        {
            "TextMesh Pro  —  Unity Technologies",
            "QuickOutline  —  Chris Nolet",
            "AllSky Free  —  rpgwhitelock",
            "3D Scifi Kit Starter Kit  —  Creepy Cat",
            "Sci Fi Chip / Doors / Gun  —  MASH Virtual",
            "Sci-Fi Barrel Pack  —  WONILMAX",
            "Alien Ships Pack  —  Autarca",
            "Biomechanical Mutant  —  (character asset)",
            "Drone Guard  —  (character asset)",
            "Sci-Fi Drones  —  (asset pack)",
            "Sci Fi Gun Light  —  (asset pack)",
            "Mars Landscape 3D  —  (environment)",
            "Rocks and Boulders 2  —  (environment)",
            "Sci-Fi Texture Pack 1  —  (texture pack)",
            "HQ Laptop  —  (prop asset)",
            "AmmoBox  —  (prop asset)",
            "Low Poly Grenade Launcher  —  (weapon asset)",
            "Cartoon FX / WarFX  —  JMO Assets",
            "Orbs VFX  —  (VFX pack)",
            "3D Sci-Fi Radio / Locator Station  —  (prop asset)",
            "Mg3D Food Props  —  Add-Ons",
        };

        foreach (string line in thirdParty)
        {
            var entry = MakeTMP("AssetEntry", col.transform,
                "· " + line, 12, FontStyles.Normal, TextAlignmentOptions.Left, ColSubtext);
            entry.enableWordWrapping = true;
            LE(entry.gameObject, h: 18);
            Spacer(col.transform, 2);
        }

        Spacer(col.transform, 18);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 14);

        // ── Engine ────────────────────────────────────────────────────────────
        var toolsTmp = MakeTMP("BuiltWith", col.transform,
            "Built with Unity Engine  ·  Universal Render Pipeline",
            12, FontStyles.Normal, TextAlignmentOptions.Center, ColCreditsDim);
        LE(toolsTmp.gameObject, h: 18);

        Spacer(col.transform, 18);
        HRule(col.transform, ColDivider, 1f);
        Spacer(col.transform, 18);

        // ── Back button ───────────────────────────────────────────────────────
        var btnRow = NewGO("ButtonRow", col.transform);
        var bHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        bHlg.spacing               = 0;
        bHlg.childAlignment        = TextAnchor.MiddleRight;
        bHlg.childControlWidth     = false;
        bHlg.childControlHeight    = true;
        bHlg.childForceExpandWidth  = false;
        bHlg.childForceExpandHeight = true;
        LE(btnRow, h: 48);

        var flexSpacer = NewGO("FlexSpacer", btnRow.transform);
        flexSpacer.AddComponent<LayoutElement>().flexibleWidth = 1;

        Button backBtn = ActionBtn(btnRow.transform, "BackFromCreditsButton", "←  BACK", ColBtnBack, ColBtnBackHover, 160);

        // ── Wire MainMenuManager ──────────────────────────────────────────────
        mgr.creditsPanel          = panel;
        mgr.backFromCreditsButton = backBtn;

        panel.SetActive(false);
        EditorUtility.SetDirty(mgr);
        Selection.activeGameObject = panel;
        Debug.Log("[Builder] Credits panel built and wired.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // UI building helpers
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds one labelled slider row: [Label 220px] [Slider flex] [Value 68px]
    /// Creates full Unity slider visual hierarchy (background, fill, handle).
    /// </summary>
    static void SliderRow(Transform parent, string label, float min, float max, float defaultVal,
        string sliderName, string valueName,
        out Slider slider, out TextMeshProUGUI valueText)
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

        // Label
        var labelTmp = MakeTMP("Label", row.transform,
            label, 15, FontStyles.Bold, TextAlignmentOptions.Left, ColSubtext);
        labelTmp.raycastTarget = false;
        LE(labelTmp.gameObject, w: 220);

        // Slider
        GameObject sliderGo = BuildSlider(sliderName, row.transform, min, max, defaultVal);
        LE(sliderGo, w: 280);
        slider = sliderGo.GetComponent<Slider>();

        // Value label
        var valTmp = MakeTMP(valueName, row.transform,
            "---", 15, FontStyles.Bold, TextAlignmentOptions.Right, ColValueText);
        LE(valTmp.gameObject, w: 68);
        valueText = valTmp;
    }

    /// <summary>
    /// Creates a fully functional Unity slider with the standard visual hierarchy:
    /// Background → Fill Area/Fill → Handle Slide Area/Handle
    /// </summary>
    static GameObject BuildSlider(string name, Transform parent, float min, float max, float value)
    {
        // Root — holds Slider component, transparent bg
        GameObject root = NewGO(name, parent);
        Image rootImg = root.AddComponent<Image>();
        rootImg.color = Color.clear;
        rootImg.raycastTarget = true;

        // Background (track)
        GameObject bg = NewGO("Background", root.transform);
        FillParent(bg);
        bg.GetComponent<RectTransform>().offsetMin = new Vector2(0f,  8f);
        bg.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -8f);
        bg.AddComponent<Image>().color = ColSliderTrack;

        // Fill Area
        GameObject fillArea = NewGO("Fill Area", root.transform);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(5f,  8f);
        faRt.offsetMax = new Vector2(-15f, -8f);

        // Fill
        GameObject fill = NewGO("Fill", fillArea.transform);
        FillParent(fill);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = ColSliderFill;

        // Handle Slide Area
        GameObject handleSlide = NewGO("Handle Slide Area", root.transform);
        FillParent(handleSlide);
        handleSlide.GetComponent<RectTransform>().offsetMin = new Vector2(10f, 0f);
        handleSlide.GetComponent<RectTransform>().offsetMax = new Vector2(-10f, 0f);

        // Handle
        GameObject handle = NewGO("Handle", handleSlide.transform);
        var hRt = handle.GetComponent<RectTransform>();
        hRt.anchorMin = hRt.anchorMax = hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.sizeDelta = new Vector2(22f, 22f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = ColSliderHandle;

        // Slider component
        Slider sldr = root.AddComponent<Slider>();
        sldr.fillRect       = fill.GetComponent<RectTransform>();
        sldr.handleRect     = handle.GetComponent<RectTransform>();
        sldr.targetGraphic  = handleImg;
        sldr.direction      = Slider.Direction.LeftToRight;
        sldr.minValue       = min;
        sldr.maxValue       = max;
        sldr.value          = value;

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = ColSliderHandle;
        cb.highlightedColor = new Color(
            Mathf.Clamp01(ColSliderHandle.r + 0.15f),
            Mathf.Clamp01(ColSliderHandle.g + 0.15f),
            Mathf.Clamp01(ColSliderHandle.b + 0.15f), 1f);
        cb.pressedColor = new Color(ColSliderHandle.r * 0.8f, ColSliderHandle.g * 0.8f,
                                    ColSliderHandle.b * 0.8f, 1f);
        sldr.colors = cb;

        return root;
    }

    /// <summary>
    /// Section label (e.g. "  MOUSE"), small, spaced, cyan-dim.
    /// </summary>
    static void SectionLabel(Transform parent, string text)
    {
        var tmp = MakeTMP("SectionLabel_" + text.Trim(), parent,
            text, 11, FontStyles.Bold, TextAlignmentOptions.Left, ColSectionLabel);
        tmp.characterSpacing = 3f;
        LE(tmp.gameObject, h: 18);
    }

    /// <summary>
    /// Credit block with one name — role label + single name.
    /// </summary>
    static void CreditEntrySingle(Transform parent, string role, string name)
    {
        GameObject block = NewGO(role.Replace(" ", "") + "Block", parent);
        var vlg = block.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 3;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        LE(block, h: 48);

        var roleTmp = MakeTMP("Role", block.transform,
            role, 11, FontStyles.Bold, TextAlignmentOptions.Center, ColCreditsRole);
        roleTmp.characterSpacing = 2.5f;
        LE(roleTmp.gameObject, h: 16);

        var nameTmp = MakeTMP("Name", block.transform,
            name, 17, FontStyles.Bold, TextAlignmentOptions.Center, ColCreditsName);
        LE(nameTmp.gameObject, h: 24);
    }

    /// <summary>
    /// Credit block with multiple names stacked — role label + one row per name.
    /// </summary>
    static void CreditEntryMulti(Transform parent, string role, params string[] names)
    {
        GameObject block = NewGO(role.Replace(" ", "") + "Block", parent);
        var vlg = block.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 2;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        LE(block, h: 18 + names.Length * 22);

        var roleTmp = MakeTMP("Role", block.transform,
            role, 11, FontStyles.Bold, TextAlignmentOptions.Center, ColCreditsRole);
        roleTmp.characterSpacing = 2.5f;
        LE(roleTmp.gameObject, h: 16);

        foreach (string n in names)
        {
            var nameTmp = MakeTMP("Name_" + n.Replace(" ", ""), block.transform,
                n, 15, FontStyles.Normal, TextAlignmentOptions.Center, ColCreditsName);
            LE(nameTmp.gameObject, h: 20);
        }
    }

    /// <summary>
    /// An action button (Back, etc.) right-aligned in a row.
    /// </summary>
    static Button ActionBtn(Transform parent, string goName, string label,
        Color bg, Color hover, float width)
    {
        GameObject go = NewGO(goName, parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 0f);

        Image img = go.AddComponent<Image>();
        img.color = bg;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = bg;
        cb.highlightedColor = hover;
        cb.pressedColor     = new Color(bg.r * 0.75f, bg.g * 0.75f, bg.b * 0.75f, 1f);
        cb.fadeDuration     = 0.08f;
        btn.colors = cb;

        var lbl = MakeTMP("Label", go.transform,
            label, 16, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        FillParent(lbl.gameObject);

        return btn;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Layout micro-helpers
    // ═════════════════════════════════════════════════════════════════════════

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

    static bool ValidateMgr(MainMenuManager mgr, out Canvas canvas)
    {
        canvas = null;
        if (mgr == null)
        {
            EditorUtility.DisplayDialog("Builder",
                "No MainMenuManager found in the active scene.\nOpen the Main Menu scene first.", "OK");
            return false;
        }
        canvas = mgr.GetComponentInParent<Canvas>()
               ?? Object.FindFirstObjectByType<Canvas>();
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
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.fontStyle     = style;
        tmp.alignment     = align;
        tmp.color         = color;
        tmp.raycastTarget = false;
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
