using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the Main Menu panel and auto-wires every MainMenuManager reference.
/// Run via:  Colony Under Siege → Rebuild Main Menu Panel
/// </summary>
public static class MainMenuPanelBuilder
{
    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color ColBg          = new Color(0.03f, 0.05f, 0.10f, 1.00f); // near-black navy
    static readonly Color ColLeftPane    = new Color(0.05f, 0.08f, 0.15f, 0.95f); // slightly lighter panel
    static readonly Color ColCyan        = new Color(0.00f, 0.84f, 1.00f, 1.00f); // main accent
    static readonly Color ColCyanDim     = new Color(0.00f, 0.84f, 1.00f, 0.18f); // subtle glow
    static readonly Color ColCyanLine    = new Color(0.00f, 0.84f, 1.00f, 0.55f); // divider line
    static readonly Color ColPreTitle    = new Color(0.40f, 0.65f, 0.80f, 1.00f); // muted cyan-grey
    static readonly Color ColSubtitle    = new Color(0.60f, 0.70f, 0.85f, 1.00f); // soft blue-white
    static readonly Color ColVersion     = new Color(0.35f, 0.42f, 0.55f, 1.00f); // dim grey
    static readonly Color ColBtnNormal   = new Color(0.07f, 0.11f, 0.20f, 0.55f); // subtle dark base
    static readonly Color ColBtnHover    = new Color(0.00f, 0.84f, 1.00f, 0.14f); // faint cyan wash
    static readonly Color ColBtnPressed  = new Color(0.00f, 0.84f, 1.00f, 0.25f);
    static readonly Color ColBtnAccent   = new Color(0.00f, 0.84f, 1.00f, 1.00f); // left bar
    static readonly Color ColQuitAccent  = new Color(0.85f, 0.20f, 0.20f, 1.00f); // red for quit
    static readonly Color ColQuitHover   = new Color(0.85f, 0.20f, 0.20f, 0.12f);
    static readonly Color ColSeparator   = new Color(0.15f, 0.20f, 0.30f, 1.00f); // subtle divider

    // ── Entry point ──────────────────────────────────────────────────────────

    [MenuItem("Colony Under Siege/Rebuild Main Menu Panel")]
    public static void Build()
    {
        MainMenuManager mgr = Object.FindFirstObjectByType<MainMenuManager>();
        if (mgr == null)
        {
            EditorUtility.DisplayDialog("Builder", "No MainMenuManager found in the active scene.", "OK");
            return;
        }

        Canvas canvas = mgr.GetComponentInParent<Canvas>()
                     ?? Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Builder", "No Canvas found in the active scene.", "OK");
            return;
        }

        if (mgr.mainPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "mainPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(mgr.mainPanel);
            mgr.mainPanel = null;
        }

        Undo.RecordObject(mgr, "Build Main Menu Panel");

        // ── Root panel (full screen) ─────────────────────────────────────────
        GameObject panel = NewGO("MainPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Main Menu Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColBg;

        // ── Left pane (title + buttons) ──────────────────────────────────────
        // Anchored to left 40% of screen, full height
        GameObject leftPane = NewGO("LeftPane", panel.transform);
        var lpRt = leftPane.GetComponent<RectTransform>();
        lpRt.anchorMin  = new Vector2(0f, 0f);
        lpRt.anchorMax  = new Vector2(0f, 1f);
        lpRt.offsetMin  = new Vector2(0f,   0f);
        lpRt.offsetMax  = new Vector2(500f, 0f);
        leftPane.AddComponent<Image>().color = ColLeftPane;

        // ── Vertical layout inside left pane ─────────────────────────────────
        GameObject contentCol = NewGO("ContentColumn", leftPane.transform);
        FillParent(contentCol);
        var vlg = contentCol.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(52, 40, 80, 60);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;   // VLG must set heights from LayoutElement
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Pre-title (small label above game name) ───────────────────────────
        var preTitle = MakeTMP("PreTitleText", contentCol.transform,
            "S C I - F I   F P S   ·   2 1 8 7",
            13, FontStyles.Bold, TextAlignmentOptions.Left, ColPreTitle);
        preTitle.characterSpacing = 2f;
        LE(preTitle.gameObject, h: 22);

        Spacer(contentCol.transform, 14);

        // ── Game title ───────────────────────────────────────────────────────
        var titleTmp = MakeTMP("TitleText", contentCol.transform,
            "COLONY\nUNDER SIEGE",
            62, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
        titleTmp.lineSpacing = -10f;
        LE(titleTmp.gameObject, h: 138);

        Spacer(contentCol.transform, 18);

        // ── Cyan accent line ─────────────────────────────────────────────────
        var line = NewGO("AccentLine", contentCol.transform);
        var lineImg = line.AddComponent<Image>();
        lineImg.color = ColCyanLine;
        LE(line, h: 2);

        Spacer(contentCol.transform, 14);

        // ── Subtitle ─────────────────────────────────────────────────────────
        var subtitleTmp = MakeTMP("SubtitleText", contentCol.transform,
            "The colony needs you.",
            18, FontStyles.Italic, TextAlignmentOptions.Left, ColSubtitle);
        LE(subtitleTmp.gameObject, h: 28);

        Spacer(contentCol.transform, 52);

        // ── Button separator ─────────────────────────────────────────────────
        var sep = NewGO("ButtonSeparator", contentCol.transform);
        sep.AddComponent<Image>().color = ColSeparator;
        LE(sep, h: 1);

        Spacer(contentCol.transform, 8);

        // ── Buttons ──────────────────────────────────────────────────────────
        Button newGameBtn  = MakeMenuButton("NewGameButton",    contentCol.transform, "NEW GAME",     ColBtnAccent, ColBtnHover,  ColBtnPressed);
        Button continueBtn = MakeMenuButton("ContinueButton",   contentCol.transform, "CONTINUE",     ColBtnAccent, ColBtnHover,  ColBtnPressed);
        Button tutorialBtn = MakeMenuButton("TutorialButton",   contentCol.transform, "TUTORIAL",     ColBtnAccent, ColBtnHover,  ColBtnPressed);
        Button stagesBtn   = MakeMenuButton("StageLevelsButton",contentCol.transform, "STAGE SELECT", ColBtnAccent, ColBtnHover,  ColBtnPressed);
        Button settingsBtn = MakeMenuButton("SettingsButton",   contentCol.transform, "SETTINGS",     ColBtnAccent, ColBtnHover,  ColBtnPressed);
        Button creditsBtn  = MakeMenuButton("CreditsButton",    contentCol.transform, "CREDITS",      ColBtnAccent, ColBtnHover,  ColBtnPressed);

        Spacer(contentCol.transform, 8);
        var sep2 = NewGO("QuitSeparator", contentCol.transform);
        sep2.AddComponent<Image>().color = ColSeparator;
        LE(sep2, h: 1);
        Spacer(contentCol.transform, 8);

        Button quitBtn = MakeMenuButton("QuitButton", contentCol.transform, "QUIT", ColQuitAccent, ColQuitHover, ColQuitHover);

        // ── Version text (bottom-left of left pane) ───────────────────────────
        GameObject versionGo = NewGO("VersionText", leftPane.transform);
        var vRt = versionGo.GetComponent<RectTransform>();
        vRt.anchorMin = new Vector2(0f, 0f);
        vRt.anchorMax = new Vector2(1f, 0f);
        vRt.offsetMin = new Vector2(52f,  16f);
        vRt.offsetMax = new Vector2(-16f, 40f);
        var vTmp = versionGo.AddComponent<TextMeshProUGUI>();
        vTmp.text           = "v0.1.0";
        vTmp.fontSize       = 13;
        vTmp.fontStyle      = FontStyles.Normal;
        vTmp.alignment      = TextAlignmentOptions.Left;
        vTmp.color          = ColVersion;
        vTmp.raycastTarget  = false;

        // ── Right side decorative panel ───────────────────────────────────────
        BuildRightDecor(panel.transform);

        // ── Wire MainMenuManager ─────────────────────────────────────────────
        mgr.mainPanel        = panel;
        mgr.titleText        = titleTmp;
        mgr.versionText      = vTmp;
        mgr.subtitleText     = subtitleTmp;
        mgr.newGameButton    = newGameBtn;
        mgr.continueButton   = continueBtn;
        mgr.tutorialButton   = tutorialBtn;
        mgr.stageLevelsButton = stagesBtn;
        mgr.settingsButton   = settingsBtn;
        mgr.creditsButton    = creditsBtn;
        mgr.quitButton       = quitBtn;

        panel.SetActive(true);

        EditorUtility.SetDirty(mgr);
        EditorUtility.SetDirty(panel);
        Selection.activeGameObject = panel;

        Debug.Log("[MainMenuPanelBuilder] Main menu panel built and wired.");
    }

    // ── Right decorative side ─────────────────────────────────────────────────

    static void BuildRightDecor(Transform parent)
    {
        // Right area — space for background art / scene camera render
        GameObject right = NewGO("RightDecor", parent);
        var rt = right.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(500f, 0f);
        rt.offsetMax = Vector2.zero;

        // ── Background image slot — assign your art sprite here ──────────────
        // Select RightDecor → BackgroundImage in the Inspector and set Source Image
        GameObject bgImg = NewGO("BackgroundImage", right.transform);
        FillParent(bgImg);
        Image bg = bgImg.AddComponent<Image>();
        bg.color          = new Color(0.06f, 0.09f, 0.16f, 1f); // placeholder tint
        bg.preserveAspect = false;  // set true if you want letterboxing

        // Gradient overlay fading from left pane into the right
        GameObject fade = NewGO("EdgeFade", right.transform);
        FillParent(fade);
        Image fadeImg = fade.AddComponent<Image>();
        fadeImg.color = new Color(0.05f, 0.08f, 0.15f, 0.35f);

        // Corner accent lines — top-right
        CornerAccent(right.transform, "CornerTR",
            new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(-12f, -12f), 120f, 2f);

        // Corner accent lines — bottom-right
        CornerAccent(right.transform, "CornerBR",
            new Vector2(1f, 0f), new Vector2(-12f, 12f), new Vector2(-12f, 12f), 120f, 2f);

        // Horizontal scan line (decorative)
        GameObject scan = NewGO("ScanLine", right.transform);
        var sRt = scan.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0.5f);
        sRt.anchorMax = new Vector2(1f, 0.5f);
        sRt.offsetMin = new Vector2(0f,  -1f);
        sRt.offsetMax = new Vector2(0f,   1f);
        scan.AddComponent<Image>().color = ColCyanDim;

        // Right-side label
        var label = MakeTMP("RightLabel", right.transform,
            "COLONY DEFENSE FORCE\nOPERATIONAL STATUS: CRITICAL",
            11, FontStyles.Bold, TextAlignmentOptions.Right, ColPreTitle);
        label.characterSpacing = 1.5f;
        var lblRt = label.gameObject.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0f);
        lblRt.anchorMax = new Vector2(1f, 0f);
        lblRt.offsetMin = new Vector2(0f,   20f);
        lblRt.offsetMax = new Vector2(-20f, 56f);
    }

    static void CornerAccent(Transform parent, string name,
        Vector2 anchor, Vector2 pivotOffset, Vector2 origin, float size, float thickness)
    {
        // Horizontal bar
        GameObject h = NewGO(name + "_H", parent);
        var hRt = h.GetComponent<RectTransform>();
        hRt.anchorMin = anchor;
        hRt.anchorMax = anchor;
        hRt.pivot     = anchor;
        hRt.anchoredPosition = pivotOffset;
        hRt.sizeDelta = new Vector2(size, thickness);
        h.AddComponent<Image>().color = ColCyanLine;

        // Vertical bar
        GameObject v = NewGO(name + "_V", parent);
        var vRt = v.GetComponent<RectTransform>();
        vRt.anchorMin = anchor;
        vRt.anchorMax = anchor;
        vRt.pivot     = anchor;
        vRt.anchoredPosition = pivotOffset;
        vRt.sizeDelta = new Vector2(thickness, size);
        v.AddComponent<Image>().color = ColCyanLine;
    }

    // ── Menu button builder ───────────────────────────────────────────────────
    // Each button:  [4px accent bar] [label text]
    // The bg image is transparent normally, faint cyan wash on hover.

    static Button MakeMenuButton(string name, Transform parent,
        string label, Color accentColor, Color hoverColor, Color pressColor)
    {
        // Root — transparent bg, receives Button component
        GameObject root = NewGO(name, parent);
        Image rootImg   = root.AddComponent<Image>();
        rootImg.color   = ColBtnNormal;
        rootImg.raycastTarget = true;

        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = rootImg;

        ColorBlock cb    = ColorBlock.defaultColorBlock;
        cb.normalColor   = ColBtnNormal;
        cb.highlightedColor = hoverColor;
        cb.pressedColor  = pressColor;
        cb.selectedColor = hoverColor;
        cb.fadeDuration  = 0.08f;
        btn.colors = cb;

        LE(root, h: 52);

        // Left accent bar (4px)
        GameObject bar = NewGO("AccentBar", root.transform);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 0.15f);
        barRt.anchorMax = new Vector2(0f, 0.85f);
        barRt.offsetMin = new Vector2(0f,  0f);
        barRt.offsetMax = new Vector2(4f,  0f);
        bar.AddComponent<Image>().color = accentColor;

        // Label
        GameObject labelGo = NewGO("Label", root.transform);
        var lRt = labelGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = new Vector2(16f, 0f);
        lRt.offsetMax = Vector2.zero;

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 19;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Left;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    static void Spacer(Transform parent, float height)
    {
        var go = NewGO("Spacer", parent);
        LE(go, h: height);
    }

    static void LE(GameObject go, float w = -1, float h = -1)
    {
        var le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth  = w;
        if (h >= 0) le.preferredHeight = h;
    }
}
