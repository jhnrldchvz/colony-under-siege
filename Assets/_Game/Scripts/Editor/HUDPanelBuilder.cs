using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// Builds the in-game HUD panel and wires UIManager + DifficultyHUD references.
/// Run via: Colony Under Siege -> Rebuild HUD Panel
/// </summary>
public static class HUDPanelBuilder
{
    // Palette
    static readonly Color ColPanelBg     = new Color(0.02f, 0.04f, 0.09f, 0.78f);
    static readonly Color ColPanelDark   = new Color(0.02f, 0.03f, 0.07f, 0.88f);
    static readonly Color ColAccent      = new Color(0.00f, 0.84f, 1.00f, 1.00f);
    static readonly Color ColAccentDim   = new Color(0.00f, 0.84f, 1.00f, 0.35f);
    static readonly Color ColText        = new Color(0.93f, 0.96f, 1.00f, 1.00f);
    static readonly Color ColSubtext     = new Color(0.70f, 0.78f, 0.90f, 1.00f);
    static readonly Color ColBarBg       = new Color(0.08f, 0.12f, 0.20f, 1.00f);
    static readonly Color ColHealthFill  = new Color(0.12f, 0.80f, 0.45f, 1.00f);
    static readonly Color ColWarnBg      = new Color(0.55f, 0.12f, 0.12f, 0.95f);

    [MenuItem("Colony Under Siege/Rebuild HUD Panel")]
    public static void Build()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (!Validate(ui, out Canvas canvas)) return;

        if (ui.hudPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild?",
                "hudPanel already assigned. Destroy and rebuild?", "Rebuild", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(ui.hudPanel);
            ui.hudPanel = null;
        }

        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go != null && go.name == "HUDPanel")
                Undo.DestroyObjectImmediate(go);
        }

        Undo.RecordObject(ui, "Build HUD Panel");

        GameObject panel = NewGO("HUDPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create HUD Panel");
        FillParent(panel);
        var hudCg = panel.AddComponent<CanvasGroup>();
        hudCg.interactable = false;
        hudCg.blocksRaycasts = false;

        // Objective panel
        RectTransform objPanel = CreatePanel(panel.transform, "ObjectivePanel",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, 200f), new Vector2(20f, -20f), ColPanelBg);
        VerticalLayoutGroup objVlg = objPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        objVlg.padding = new RectOffset(16, 16, 12, 12);
        objVlg.spacing = 6;
        objVlg.childAlignment = TextAnchor.UpperLeft;
        objVlg.childControlWidth = true;
        objVlg.childControlHeight = false;
        objVlg.childForceExpandWidth = true;
        objVlg.childForceExpandHeight = false;

        var objTitle = MakeTMP("ObjectiveTitle", objPanel.transform,
            "OBJECTIVES", 14, FontStyles.Bold, TextAlignmentOptions.Left, ColAccent);
        AddLE(objTitle.gameObject, h: 20);

        var objText = MakeTMP("ObjectiveText", objPanel.transform,
            "-", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, ColText);
        objText.textWrappingMode = TextWrappingModes.Normal;
        AddLE(objText.gameObject, h: 140, flexH: 1);

        // Ammo panel
        RectTransform ammoPanel = CreatePanel(panel.transform, "AmmoPanel",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(280f, 110f), new Vector2(-20f, -20f), ColPanelBg);
        HorizontalLayoutGroup ammoHlg = ammoPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        ammoHlg.padding = new RectOffset(12, 12, 10, 10);
        ammoHlg.spacing = 10;
        ammoHlg.childAlignment = TextAnchor.MiddleLeft;
        ammoHlg.childControlWidth = false;
        ammoHlg.childControlHeight = true;
        ammoHlg.childForceExpandWidth = false;
        ammoHlg.childForceExpandHeight = true;

        var weaponIcon = NewGO("WeaponIcon", ammoPanel.transform);
        var weaponIconRt = weaponIcon.GetComponent<RectTransform>();
        weaponIconRt.sizeDelta = new Vector2(56f, 56f);
        var weaponIconImg = weaponIcon.AddComponent<Image>();
        weaponIconImg.color = Color.white;
        weaponIconImg.preserveAspect = true;
        AddLE(weaponIcon, w: 56, h: 56);

        var ammoCol = NewGO("AmmoTextColumn", ammoPanel.transform);
        var ammoVlg = ammoCol.AddComponent<VerticalLayoutGroup>();
        ammoVlg.padding = new RectOffset(0, 0, 0, 0);
        ammoVlg.spacing = 2;
        ammoVlg.childAlignment = TextAnchor.MiddleLeft;
        ammoVlg.childControlWidth = true;
        ammoVlg.childControlHeight = false;
        ammoVlg.childForceExpandWidth = true;
        ammoVlg.childForceExpandHeight = false;

        var ammoLabel = MakeTMP("AmmoLabel", ammoCol.transform,
            "AMMO", 12, FontStyles.Bold, TextAlignmentOptions.Left, ColSubtext);
        AddLE(ammoLabel.gameObject, h: 18);

        var ammoText = MakeTMP("AmmoText", ammoCol.transform,
            "0 / 0", 24, FontStyles.Bold, TextAlignmentOptions.Left, ColText);
        AddLE(ammoText.gameObject, h: 32);

        // Health panel
        RectTransform healthPanel = CreatePanel(panel.transform, "HealthPanel",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(320f, 90f), new Vector2(20f, 20f), ColPanelBg);
        VerticalLayoutGroup hpVlg = healthPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        hpVlg.padding = new RectOffset(14, 14, 10, 10);
        hpVlg.spacing = 6;
        hpVlg.childAlignment = TextAnchor.UpperLeft;
        hpVlg.childControlWidth = true;
        hpVlg.childControlHeight = false;
        hpVlg.childForceExpandWidth = true;
        hpVlg.childForceExpandHeight = false;

        var hpHeader = NewGO("HealthHeader", healthPanel.transform);
        var hpHlg = hpHeader.AddComponent<HorizontalLayoutGroup>();
        hpHlg.spacing = 8;
        hpHlg.childAlignment = TextAnchor.MiddleLeft;
        hpHlg.childControlWidth = false;
        hpHlg.childControlHeight = true;
        hpHlg.childForceExpandWidth = false;
        hpHlg.childForceExpandHeight = true;
        AddLE(hpHeader, h: 20);

        var hpLabel = MakeTMP("HealthLabel", hpHeader.transform,
            "HEALTH", 12, FontStyles.Bold, TextAlignmentOptions.Left, ColSubtext);
        AddLE(hpLabel.gameObject, w: 70, h: 20);

        var healthText = MakeTMP("HealthText", hpHeader.transform,
            "100 / 100", 14, FontStyles.Bold, TextAlignmentOptions.Left, ColText);
        AddLE(healthText.gameObject, h: 20, flexW: 1);

        var barBg = NewGO("HealthBarBg", healthPanel.transform);
        barBg.AddComponent<Image>().color = ColBarBg;
        AddLE(barBg, h: 14);

        var barFill = NewGO("HealthBarFill", barBg.transform);
        var barFillRt = barFill.GetComponent<RectTransform>();
        barFillRt.anchorMin = Vector2.zero;
        barFillRt.anchorMax = Vector2.one;
        barFillRt.offsetMin = Vector2.zero;
        barFillRt.offsetMax = Vector2.zero;
        var barFillImg = barFill.AddComponent<Image>();
        barFillImg.color = ColHealthFill;
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillOrigin = 0;
        barFillImg.fillAmount = 1f;

        // Difficulty panel
        RectTransform diffPanel = CreatePanel(panel.transform, "DifficultyPanel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(320f, 170f), new Vector2(0f, -70f), ColPanelDark);
        VerticalLayoutGroup diffVlg = diffPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        diffVlg.padding = new RectOffset(12, 12, 10, 10);
        diffVlg.spacing = 6;
        diffVlg.childAlignment = TextAnchor.UpperLeft;
        diffVlg.childControlWidth = true;
        diffVlg.childControlHeight = false;
        diffVlg.childForceExpandWidth = true;
        diffVlg.childForceExpandHeight = false;

        var diffTitle = MakeTMP("DifficultyTitle", diffPanel.transform,
            "DIFFICULTY", 12, FontStyles.Bold, TextAlignmentOptions.Left, ColAccent);
        AddLE(diffTitle.gameObject, h: 18);

        var tierRow = NewGO("TierRow", diffPanel.transform);
        var tierHlg = tierRow.AddComponent<HorizontalLayoutGroup>();
        tierHlg.spacing = 8;
        tierHlg.childAlignment = TextAnchor.MiddleLeft;
        tierHlg.childControlWidth = false;
        tierHlg.childControlHeight = true;
        tierHlg.childForceExpandWidth = false;
        tierHlg.childForceExpandHeight = true;
        AddLE(tierRow, h: 24);

        var tierLabelHdr = MakeTMP("TierLabel", tierRow.transform,
            "TIER", 11, FontStyles.Bold, TextAlignmentOptions.Left, ColSubtext);
        AddLE(tierLabelHdr.gameObject, w: 40, h: 18);

        var tierBadge = NewGO("TierBadge", tierRow.transform);
        var tierBadgeRt = tierBadge.GetComponent<RectTransform>();
        tierBadgeRt.sizeDelta = new Vector2(90f, 22f);
        var tierBadgeImg = tierBadge.AddComponent<Image>();
        tierBadgeImg.color = ColAccent;
        AddLE(tierBadge, w: 90, h: 22);

        var tierLabel = MakeTMP("TierValue", tierBadge.transform,
            "NORMAL", 11, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
        FillParent(tierLabel.gameObject);

        // Window accuracy row
        var windowRow = BuildAccuracyRow(diffPanel.transform, "WINDOW", out Image windowFill, out TextMeshProUGUI windowText);
        AddLE(windowRow, h: 20);

        // Lifetime accuracy row
        var lifeRow = BuildAccuracyRow(diffPanel.transform, "LIFETIME", out Image lifeFill, out TextMeshProUGUI lifeText);
        AddLE(lifeRow, h: 20);

        // Counters row
        var countersRow = NewGO("CountersRow", diffPanel.transform);
        var ctrHlg = countersRow.AddComponent<HorizontalLayoutGroup>();
        ctrHlg.spacing = 18;
        ctrHlg.childAlignment = TextAnchor.MiddleLeft;
        ctrHlg.childControlWidth = false;
        ctrHlg.childControlHeight = true;
        ctrHlg.childForceExpandWidth = false;
        ctrHlg.childForceExpandHeight = true;
        AddLE(countersRow, h: 38);

        BuildCounterGroup(countersRow.transform, "FIRED", out TextMeshProUGUI shotsFired);
        BuildCounterGroup(countersRow.transform, "HIT", out TextMeshProUGUI shotsHit);

        // Attach DifficultyHUD and wire
        DifficultyHUD diffHud = diffPanel.gameObject.AddComponent<DifficultyHUD>();
        diffHud.tierLabel = tierLabel;
        diffHud.tierBadgeBackground = tierBadgeImg;
        diffHud.windowAccuracyBar = windowFill;
        diffHud.windowAccuracyText = windowText;
        diffHud.lifetimeAccuracyBar = lifeFill;
        diffHud.lifetimeAccuracyText = lifeText;
        diffHud.shotsFiredText = shotsFired;
        diffHud.shotsHitText = shotsHit;

        // Healing warning banner (hidden by default)
        RectTransform warning = CreatePanel(panel.transform, "HealingWarningBanner",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 36f), new Vector2(0f, -20f), ColWarnBg);
        var warningText = MakeTMP("WarningText", warning.transform,
            "AI CORE HEALING - DEACTIVATE TERMINALS", 14, FontStyles.Bold,
            TextAlignmentOptions.Center, Color.white);
        FillParent(warningText.gameObject);
        warning.gameObject.SetActive(false);

        // Key item panel
        RectTransform keyPanel = CreatePanel(panel.transform, "KeyItemsPanel",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(280f, 160f), new Vector2(-20f, 20f), ColPanelBg);
        VerticalLayoutGroup keyVlg = keyPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        keyVlg.padding = new RectOffset(10, 10, 10, 10);
        keyVlg.spacing = 6;
        keyVlg.childAlignment = TextAnchor.UpperLeft;
        keyVlg.childControlWidth = true;
        keyVlg.childControlHeight = false;
        keyVlg.childForceExpandWidth = true;
        keyVlg.childForceExpandHeight = false;

        var keyItem = BuildKeyItemRow(keyPanel.transform, "KeyItemIndicator", "Access Key",
            out Image keyIcon, out TextMeshProUGUI keyText);
        var cell1 = BuildKeyItemRow(keyPanel.transform, "PowerCell1Indicator", "Power Cell 1",
            out Image cell1Icon, out TextMeshProUGUI cell1Text);
        var cell2 = BuildKeyItemRow(keyPanel.transform, "PowerCell2Indicator", "Power Cell 2",
            out Image cell2Icon, out TextMeshProUGUI cell2Text);
        var deact = BuildKeyItemRow(keyPanel.transform, "DeactivationDeviceIndicator", "Deactivation Device",
            out Image deactIcon, out TextMeshProUGUI deactText);

        keyItem.SetActive(false);
        cell1.SetActive(false);
        cell2.SetActive(false);
        deact.SetActive(false);

        // Crosshair
        var crosshair = NewGO("Crosshair", panel.transform);
        var crossRt = crosshair.GetComponent<RectTransform>();
        crossRt.anchorMin = new Vector2(0.5f, 0.5f);
        crossRt.anchorMax = new Vector2(0.5f, 0.5f);
        crossRt.pivot = new Vector2(0.5f, 0.5f);
        crossRt.sizeDelta = new Vector2(24f, 24f);
        crossRt.anchoredPosition = Vector2.zero;
        BuildCrosshair(crosshair.transform, Color.white);

        // Reload indicator
        RectTransform reload = CreatePanel(panel.transform, "ReloadIndicator",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 36f), new Vector2(0f, -32f), ColPanelDark);
        var reloadText = MakeTMP("ReloadText", reload.transform,
            "RELOADING", 14, FontStyles.Bold, TextAlignmentOptions.Center, ColText);
        FillParent(reloadText.gameObject);
        reload.gameObject.SetActive(false);

        // Wire UIManager references
        ui.hudPanel = panel;
        ui.objectiveText = objText;
        ui.weaponIcon = weaponIconImg;
        ui.ammoText = ammoText;
        ui.healthBarFill = barFillImg;
        ui.healthText = healthText;
        ui.crosshair = crosshair;
        ui.reloadIndicator = reload.gameObject;
        ui.keyItemIndicator = keyItem;
        ui.keyItemIcon = keyIcon;
        ui.keyItemText = keyText;
        ui.powerCell1Indicator = cell1;
        ui.powerCell1Icon = cell1Icon;
        ui.powerCell1Text = cell1Text;
        ui.powerCell2Indicator = cell2;
        ui.powerCell2Icon = cell2Icon;
        ui.powerCell2Text = cell2Text;
        ui.deactivationDeviceIndicator = deact;
        ui.deactivationDeviceIcon = deactIcon;
        ui.deactivationDeviceText = deactText;
        ui.healingWarningBanner = warning.gameObject;
        ui.healingWarningText = warningText;

        // Optional: auto-wire AICoreManager banner if present
        AICoreManager core = Object.FindFirstObjectByType<AICoreManager>();
        if (core != null)
        {
            Undo.RecordObject(core, "Wire AI Core Banner");
            core.healingWarningBanner = warning.gameObject;
            EditorUtility.SetDirty(core);
        }

        panel.SetActive(true);

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = panel;

        Debug.Log("[HUDPanelBuilder] HUD panel built and wired.");
    }

    // Accuracy row builder
    static GameObject BuildAccuracyRow(Transform parent, string label,
        out Image fillImage, out TextMeshProUGUI pctText)
    {
        var row = NewGO(label + "Row", parent);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var lbl = MakeTMP(label + "Label", row.transform,
            label, 10, FontStyles.Bold, TextAlignmentOptions.Left, ColSubtext);
        AddLE(lbl.gameObject, w: 60, h: 16);

        var bar = NewGO(label + "BarBg", row.transform);
        bar.AddComponent<Image>().color = ColBarBg;
        AddLE(bar, w: 130, h: 12);

        var fill = NewGO("Fill", bar.transform);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = ColAccentDim;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 0f;

        pctText = MakeTMP(label + "Pct", row.transform,
            "0%", 10, FontStyles.Bold, TextAlignmentOptions.Right, ColText);
        AddLE(pctText.gameObject, w: 40, h: 16);

        fillImage = fillImg;
        return row;
    }

    static GameObject BuildCounterGroup(Transform parent, string label,
        out TextMeshProUGUI valueText)
    {
        var group = NewGO(label + "Group", parent);
        var vlg = group.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        AddLE(group, w: 90, h: 32);

        var lbl = MakeTMP(label + "Lbl", group.transform,
            label, 9, FontStyles.Bold, TextAlignmentOptions.Left, ColSubtext);
        AddLE(lbl.gameObject, h: 12);

        valueText = MakeTMP(label + "Value", group.transform,
            "0", 16, FontStyles.Bold, TextAlignmentOptions.Left, ColText);
        AddLE(valueText.gameObject, h: 18);

        return group;
    }

    static GameObject BuildKeyItemRow(Transform parent, string name, string label,
        out Image icon, out TextMeshProUGUI text)
    {
        var row = NewGO(name, parent);
        row.AddComponent<Image>().color = ColPanelDark;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;
        AddLE(row, h: 32);

        var iconGo = NewGO("Icon", row.transform);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(20f, 20f);
        icon = iconGo.AddComponent<Image>();
        icon.color = Color.white;
        icon.preserveAspect = true;
        AddLE(iconGo, w: 20, h: 20);

        text = MakeTMP("Label", row.transform,
            label, 13, FontStyles.Normal, TextAlignmentOptions.Left, ColText);
        AddLE(text.gameObject, h: 20, flexW: 1);

        return row;
    }

    static void BuildCrosshair(Transform parent, Color color)
    {
        var h = NewGO("CrosshairH", parent);
        var hRt = h.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0.5f, 0.5f);
        hRt.anchorMax = new Vector2(0.5f, 0.5f);
        hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.sizeDelta = new Vector2(18f, 2f);
        hRt.anchoredPosition = Vector2.zero;
        h.AddComponent<Image>().color = color;

        var v = NewGO("CrosshairV", parent);
        var vRt = v.GetComponent<RectTransform>();
        vRt.anchorMin = new Vector2(0.5f, 0.5f);
        vRt.anchorMax = new Vector2(0.5f, 0.5f);
        vRt.pivot = new Vector2(0.5f, 0.5f);
        vRt.sizeDelta = new Vector2(2f, 18f);
        vRt.anchoredPosition = Vector2.zero;
        v.AddComponent<Image>().color = color;
    }

    static RectTransform CreatePanel(Transform parent, string name,
        Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos, Color bg)
    {
        RectTransform rt = NewGO(name, parent).GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.gameObject.AddComponent<Image>().color = bg;
        return rt;
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
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float size, FontStyles style, TextAlignmentOptions align, Color color)
    {
        var go = NewGO(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void AddLE(GameObject go, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
    {
        var le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth = w;
        if (h >= 0) le.preferredHeight = h;
        if (flexW >= 0) le.flexibleWidth = flexW;
        if (flexH >= 0) le.flexibleHeight = flexH;
    }

    static bool Validate(UIManager ui, out Canvas canvas)
    {
        canvas = null;
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Builder",
                "No UIManager found in the active scene.\nOpen a stage scene first.", "OK");
            return false;
        }

        Canvas nearest = ui.GetComponentInParent<Canvas>()
                      ?? Object.FindFirstObjectByType<Canvas>();
        canvas = nearest != null ? nearest.rootCanvas : null;

        if (canvas != null) return true;
        EditorUtility.DisplayDialog("Builder", "No Canvas found in scene.", "OK");
        return false;
    }
}
