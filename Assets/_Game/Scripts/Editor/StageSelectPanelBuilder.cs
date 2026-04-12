using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Programmatically rebuilds the Stage Select panel and auto-wires every
/// MainMenuManager reference.
///
/// Run via:  Colony Under Siege → Rebuild Stage Select Panel
/// </summary>
public static class StageSelectPanelBuilder
{
    // ── Design constants ─────────────────────────────────────────────────────
    static readonly Color ColPanelBg   = new Color(0.04f, 0.07f, 0.13f, 0.97f);
    static readonly Color ColCardBg    = new Color(0.10f, 0.14f, 0.22f, 1.00f);
    static readonly Color ColBottomBar = new Color(0.04f, 0.06f, 0.12f, 0.88f);
    static readonly Color ColCyan      = new Color(0.00f, 0.85f, 1.00f, 1.00f);
    static readonly Color ColCyanDim   = new Color(0.00f, 0.85f, 1.00f, 0.30f);
    static readonly Color ColPlayBtn   = new Color(0.00f, 0.72f, 0.46f, 1.00f);
    static readonly Color ColBackBtn   = new Color(0.18f, 0.20f, 0.26f, 1.00f);
    static readonly Color ColLockBg    = new Color(0.00f, 0.00f, 0.00f, 0.65f);
    static readonly Color ColSubtext   = new Color(0.72f, 0.78f, 0.90f, 1.00f);
    static readonly Color ColDescBg    = new Color(0.07f, 0.10f, 0.17f, 0.85f);

    // Stage defaults — rename or update descriptions in the Inspector after building
    static readonly string[] DefaultNames = {
        "Stage 01", "Stage 02", "Stage 03", "Stage 04", "Stage 05"
    };
    static readonly string[] DefaultDescs = {
        "Infiltrate the abandoned facility and neutralise all hostiles.",
        "Push through the industrial district. Eliminate the patrol commander.",
        "Navigate the collapsed tunnels and disable the defence grid.",
        "Storm the command outpost and secure the data core.",
        "Final assault on the Reactor Chamber. Face the Reactor Guardian."
    };
    // Scene build indices — adjust to match your Build Settings
    static readonly int[] SceneIndices = { 1, 2, 3, 4, 5 };

    // ── Entry point ──────────────────────────────────────────────────────────

    [MenuItem("Colony Under Siege/Rebuild Stage Select Panel")]
    public static void Build()
    {
        MainMenuManager mgr = UnityEngine.Object.FindFirstObjectByType<MainMenuManager>();
        if (mgr == null)
        {
            EditorUtility.DisplayDialog("Builder", "No MainMenuManager found in the active scene.", "OK");
            return;
        }

        Canvas canvas = mgr.GetComponentInParent<Canvas>()
                     ?? UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Builder", "No Canvas found in the active scene.", "OK");
            return;
        }

        // Optionally destroy existing panel
        if (mgr.stageSelectPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "A stageSelectPanel is already assigned. Destroy it and rebuild?",
                "Rebuild", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(mgr.stageSelectPanel);
            mgr.stageSelectPanel = null;
        }

        Undo.RecordObject(mgr, "Build Stage Select Panel");

        // ── Root panel ───────────────────────────────────────────────────────
        GameObject panel = NewGO("StageSelectPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create Stage Select Panel");
        FillParent(panel);
        panel.AddComponent<Image>().color = ColPanelBg;

        // ── Outer vertical layout ────────────────────────────────────────────
        GameObject root = NewGO("RootLayout", panel.transform);
        FillParent(root);
        VerticalLayoutGroup vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.padding            = new RectOffset(80, 80, 60, 60);
        vlg.spacing            = 18;
        vlg.childAlignment     = TextAnchor.UpperCenter;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Title ────────────────────────────────────────────────────────────
        var title = MakeTMP("TitleText", root.transform,
            "SELECT STAGE", 44, FontStyles.Bold, TextAlignmentOptions.Center, ColCyan);
        LE(title.gameObject, h: 58);

        // ── Thin divider ─────────────────────────────────────────────────────
        var divGo = NewGO("Divider", root.transform);
        divGo.AddComponent<Image>().color = ColCyanDim;
        LE(divGo, h: 2);

        // ── Cards row ────────────────────────────────────────────────────────
        GameObject cardsRow = NewGO("CardsContainer", root.transform);
        HorizontalLayoutGroup hlg = cardsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 18;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        LE(cardsRow, h: 250);

        // Build 5 stage cards
        const int COUNT = 5;
        Button[]      btns    = new Button[COUNT];
        CanvasGroup[] groups  = new CanvasGroup[COUNT];
        GameObject[]  locked  = new GameObject[COUNT];

        for (int i = 0; i < COUNT; i++)
            BuildCard(cardsRow.transform, $"StageCard_{i + 1:D2}",
                      DefaultNames[i], out btns[i], out groups[i], out locked[i]);

        // ── Description area (text only) ─────────────────────────────────────
        GameObject descRow = NewGO("DescriptionArea", root.transform);
        Image descBg = descRow.AddComponent<Image>();
        descBg.color = ColDescBg;
        VerticalLayoutGroup dvlg = descRow.AddComponent<VerticalLayoutGroup>();
        dvlg.padding               = new RectOffset(32, 32, 20, 20);
        dvlg.spacing               = 10;
        dvlg.childAlignment        = TextAnchor.MiddleLeft;
        dvlg.childControlWidth     = true;
        dvlg.childControlHeight    = false;
        dvlg.childForceExpandWidth  = true;
        dvlg.childForceExpandHeight = false;
        LE(descRow, h: 130);

        var nameText = MakeTMP("StageNameText", descRow.transform,
            DefaultNames[0], 26, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
        LE(nameText.gameObject, h: 34);

        var descText = MakeTMP("StageDescriptionText", descRow.transform,
            DefaultDescs[0], 17, FontStyles.Normal, TextAlignmentOptions.Left, ColSubtext);
        descText.enableWordWrapping = true;
        LE(descText.gameObject, h: 72);

        // ── Action row ───────────────────────────────────────────────────────
        GameObject actionRow = NewGO("ActionRow", root.transform);
        HorizontalLayoutGroup ahlg = actionRow.AddComponent<HorizontalLayoutGroup>();
        ahlg.spacing               = 24;
        ahlg.childAlignment        = TextAnchor.MiddleCenter;
        ahlg.childControlWidth     = false;
        ahlg.childControlHeight    = false;
        ahlg.childForceExpandWidth  = false;
        ahlg.childForceExpandHeight = false;
        LE(actionRow, h: 64);

        Button backBtn = MakeButton("BackButton", actionRow.transform, "← BACK", ColBackBtn, 200, 56);
        Button playBtn = MakeButton("PlayButton", actionRow.transform, "▶  PLAY", ColPlayBtn, 260, 56);

        // ── Wire MainMenuManager ─────────────────────────────────────────────
        mgr.stageSelectPanel     = panel;
        mgr.stageNameText        = nameText;
        mgr.stageDescriptionText = descText;
        mgr.stagePreviewImage    = null;   // cards ARE the previews; no separate image needed
        mgr.playButton           = playBtn;
        mgr.backFromStagesButton = backBtn;

        StageEntry[] entries = new StageEntry[COUNT];
        for (int i = 0; i < COUNT; i++)
        {
            entries[i] = new StageEntry
            {
                stageName     = DefaultNames[i],
                description   = DefaultDescs[i],
                button        = btns[i],
                cardGroup     = groups[i],
                lockedOverlay = locked[i],
                sceneIndex    = SceneIndices[i],
            };
        }
        mgr.stages = entries;

        panel.SetActive(false);

        EditorUtility.SetDirty(mgr);
        EditorUtility.SetDirty(panel);
        Selection.activeGameObject = panel;

        Debug.Log("[StageSelectPanelBuilder] Panel built and wired. " +
                  "Assign stage preview sprites in the MainMenuManager Stages array.");
    }

    // ── Card builder ─────────────────────────────────────────────────────────

    static void BuildCard(Transform parent, string goName, string label,
        out Button btn, out CanvasGroup cg, out GameObject lockedOverlay)
    {
        // Root — the card itself is the full stage image + button
        GameObject card = NewGO(goName, parent);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(224, 260);

        cg = card.AddComponent<CanvasGroup>();

        // The card Image IS the stage preview — assign your screenshot sprite here
        Image cardImg = card.AddComponent<Image>();
        cardImg.color          = ColCardBg;   // placeholder tint until sprite assigned
        cardImg.preserveAspect = false;       // fill the card frame fully

        btn = card.AddComponent<Button>();
        btn.targetGraphic = cardImg;

        // Hover scale pulse
        card.AddComponent<StageCardHover>();

        // Bottom label strip — semi-transparent bar over the image bottom
        GameObject bar = NewGO("BottomBar", card.transform);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = Vector2.zero;
        barRt.anchorMax = new Vector2(1f, 0.22f);
        barRt.offsetMin = barRt.offsetMax = Vector2.zero;
        bar.AddComponent<Image>().color = ColBottomBar;

        var lbl = MakeTMP("CardLabel", bar.transform,
            label, 15, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        FillParent(lbl.gameObject);

        // Locked overlay (full card, hidden by default)
        lockedOverlay = NewGO("LockedOverlay", card.transform);
        var lockRt = lockedOverlay.GetComponent<RectTransform>();
        lockRt.anchorMin = Vector2.zero;
        lockRt.anchorMax = Vector2.one;
        lockRt.offsetMin = lockRt.offsetMax = Vector2.zero;
        lockedOverlay.AddComponent<Image>().color = ColLockBg;

        var lockLbl = MakeTMP("LockLabel", lockedOverlay.transform,
            "LOCKED", 18, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(1f, 1f, 1f, 0.85f));
        FillParent(lockLbl.gameObject);

        lockedOverlay.SetActive(false);
    }

    // ── Button builder ───────────────────────────────────────────────────────

    static Button MakeButton(string name, Transform parent,
        string label, Color bg, float w, float h)
    {
        GameObject go = NewGO(name, parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);

        Image img = go.AddComponent<Image>();
        img.color = bg;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Clamp01(bg.r + 0.15f),
            Mathf.Clamp01(bg.g + 0.15f),
            Mathf.Clamp01(bg.b + 0.15f), 1f);
        btn.colors = cb;

        var lbl = MakeTMP("Label", go.transform,
            label, 20, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        FillParent(lbl.gameObject);

        return btn;
    }

    // ── Micro helpers ────────────────────────────────────────────────────────

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
        tmp.text            = text;
        tmp.fontSize        = size;
        tmp.fontStyle       = style;
        tmp.alignment       = align;
        tmp.color           = color;
        tmp.raycastTarget   = false;
        return tmp;
    }

    // LayoutElement shorthand
    static void LE(GameObject go, float w = -1, float h = -1)
    {
        var le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth  = w;
        if (h >= 0) le.preferredHeight = h;
    }
}
