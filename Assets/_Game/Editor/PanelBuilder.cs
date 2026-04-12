using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// PanelBuilder — Unity Editor utility for Colony Under Siege.
///
/// Adds menu items under "Colony Under Siege/" that rebuild the
/// Storyboard and Instruction panels on the active scene's Canvas.
///
/// Usage:
///   1. Open any in-game scene (Stage [1]–[5]).
///   2. Select the Canvas GameObject that owns UIManager.
///   3. Menu bar → Colony Under Siege → Rebuild Storyboard Panel
///                                    → Rebuild Instruction Panel
///   4. Delete the old panel GameObjects and rewire UIManager references.
/// </summary>
public static class PanelBuilder
{
    // ─────────────────────────────────────────────────────────────────────────
    // Colour palette (Sci-Fi dark / cyan)
    // ─────────────────────────────────────────────────────────────────────────

    static readonly Color C_Teal        = Hex("#1DE9B6");
    static readonly Color C_TealDim     = Hex("#1DE9B6", 0.47f);
    static readonly Color C_Card        = Hex("#111827");
    static readonly Color C_CardDark    = Hex("#070A10");
    static readonly Color C_Overlay     = Hex("#0A0C10", 0.82f);
    static readonly Color C_BtnDark     = Hex("#1A2535");
    static readonly Color C_BtnBorder   = Hex("#2A4060");
    static readonly Color C_BtnHover    = Hex("#243550");
    static readonly Color C_BtnPressed  = Hex("#0F1A28");
    static readonly Color C_BtnDisabled = Hex("#0D1220", 0.50f);
    static readonly Color C_TextBody    = Hex("#C8D6E5");
    static readonly Color C_TextMuted   = Hex("#5A7A8A");
    static readonly Color C_Black       = Color.black;

    // Sprite paths (relative to Assets/)
    const string SPRITE_PANEL = "Assets/_Game/UI/Sprites/Panel_Background.png";
    const string SPRITE_LINE  = "Assets/_Game/UI/Sprites/Line-Separator.png";

    // ─────────────────────────────────────────────────────────────────────────
    // Menu items
    // ─────────────────────────────────────────────────────────────────────────

    static void BuildStoryboard()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null) return;

        // Remove old panel if present
        Transform old = canvas.transform.Find("StoryboardPanel");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
            Debug.Log("[PanelBuilder] Removed old StoryboardPanel.");
        }

        GameObject root = BuildStoryboardPanel(canvas.transform);
        Undo.RegisterCreatedObjectUndo(root, "Rebuild StoryboardPanel");
        Selection.activeGameObject = root;
        Debug.Log("[PanelBuilder] StoryboardPanel rebuilt. Rewire UIManager references.");
    }

    static void BuildInstruction()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null) return;

        Transform old = canvas.transform.Find("InstructionPanel");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
            Debug.Log("[PanelBuilder] Removed old InstructionPanel.");
        }

        GameObject root = BuildInstructionPanel(canvas.transform);
        Undo.RegisterCreatedObjectUndo(root, "Rebuild InstructionPanel");
        Selection.activeGameObject = root;
        Debug.Log("[PanelBuilder] InstructionPanel rebuilt. Rewire UIManager references.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Storyboard Panel builder
    // ─────────────────────────────────────────────────────────────────────────

    static GameObject BuildStoryboardPanel(Transform canvasParent)
    {
        // ── Root ────────────────────────────────────────────────────────────
        var root = MakeFullscreen("StoryboardPanel", canvasParent);
        AddImage(root, C_Overlay);

        // ── Card ────────────────────────────────────────────────────────────
        var card = MakeRect("Card", root);
        SizeDelta(card, 1100, 600);
        Anchor(card, 0.5f, 0.5f, 0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        var cardImg = AddImage(card, C_Card);
        cardImg.sprite = LoadSprite(SPRITE_PANEL);
        cardImg.type   = Image.Type.Sliced;
        AddOutline(card, C_Teal, new Vector2(1.5f, -1.5f));

        // ── Accent bar ──────────────────────────────────────────────────────
        var accent = MakeRect("AccentBar", card);
        Anchor(accent, 0f, 1f, 1f, 1f);
        accent.offsetMin = new Vector2(0, -3);
        accent.offsetMax = Vector2.zero;
        AddImage(accent, C_Teal);

        // ── Image area (left column) ─────────────────────────────────────────
        var imgArea = MakeRect("ImageArea", card);
        Anchor(imgArea, 0f, 0f, 0f, 1f);
        imgArea.offsetMin = new Vector2(0,  56);   // above footer
        imgArea.offsetMax = new Vector2(540, -3);  // below accent bar
        AddImage(imgArea, C_CardDark);

        var sbImg = MakeRect("SB_Image", imgArea);
        Anchor(sbImg, 0f, 0f, 1f, 1f);
        Offset(sbImg, 8, 8, -8, -8);
        var imgComp = sbImg.gameObject.AddComponent<Image>();
        imgComp.preserveAspect  = true;
        imgComp.raycastTarget   = false;
        imgComp.color           = Color.white;

        // ── Content area (right column) ──────────────────────────────────────
        var content = MakeRect("ContentArea", card);
        Anchor(content, 0f, 0f, 1f, 1f);
        content.offsetMin = new Vector2(548,  56);
        content.offsetMax = new Vector2(-0,  -3);

        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding            = new RectOffset(24, 24, 20, 8);
        vlg.spacing            = 12;
        vlg.childAlignment     = TextAnchor.UpperLeft;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Title
        var titleGO = MakeRect("SB_Title", content);
        SizeDelta(titleGO, 0, 36);
        var titleTMP = titleGO.gameObject.AddComponent<TextMeshProUGUI>();
        titleTMP.text           = "MISSION BRIEFING";
        titleTMP.fontSize       = 22;
        titleTMP.fontStyle      = FontStyles.Bold | FontStyles.UpperCase;
        titleTMP.color          = C_Teal;
        titleTMP.alignment      = TextAlignmentOptions.Left;
        titleTMP.characterSpacing = 4;
        titleTMP.overflowMode   = TextOverflowModes.Ellipsis;
        AddLayoutElement(titleGO, minHeight: 36, flexWidth: -1, flexHeight: -1);

        // Divider
        var divGO = MakeRect("Divider", content);
        SizeDelta(divGO, 0, 2);
        var divImg = divGO.gameObject.AddComponent<Image>();
        divImg.sprite = LoadSprite(SPRITE_LINE);
        divImg.color  = C_TealDim;
        divImg.type   = Image.Type.Sliced;
        AddLayoutElement(divGO, minHeight: 2, flexWidth: -1, flexHeight: -1);

        // Body
        var bodyGO = MakeRect("SB_Body", content);
        var bodyTMP = bodyGO.gameObject.AddComponent<TextMeshProUGUI>();
        bodyTMP.text        = "Slide body text goes here...";
        bodyTMP.fontSize    = 15;
        bodyTMP.color       = C_TextBody;
        bodyTMP.lineSpacing = 6;
        bodyTMP.alignment   = TextAlignmentOptions.TopLeft;
        bodyTMP.overflowMode = TextOverflowModes.Overflow;
        AddLayoutElement(bodyGO, minHeight: 80, flexWidth: -1, flexHeight: 1);

        // ── Footer ──────────────────────────────────────────────────────────
        var footer = MakeRect("Footer", card);
        Anchor(footer, 0f, 0f, 1f, 0f);
        footer.offsetMin = Vector2.zero;
        footer.offsetMax = new Vector2(0, 56);

        var hlg = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding            = new RectOffset(16, 16, 10, 10);
        hlg.spacing            = 10;
        hlg.childAlignment     = TextAnchor.MiddleLeft;
        hlg.childControlWidth  = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Counter
        var counterGO = MakeRect("SB_Counter", footer);
        SizeDelta(counterGO, 80, 36);
        var counterTMP = counterGO.gameObject.AddComponent<TextMeshProUGUI>();
        counterTMP.text      = "1 / 5";
        counterTMP.fontSize  = 13;
        counterTMP.fontStyle = FontStyles.Italic;
        counterTMP.color     = C_TextMuted;
        counterTMP.alignment = TextAlignmentOptions.Left;
        AddLayoutElement(counterGO, minHeight: -1, flexWidth: 1, flexHeight: -1);

        // Prev button
        BuildNavButton("SB_PrevButton", footer, "◀  PREV", false);

        // Next button
        BuildNavButton("SB_NextButton", footer, "NEXT  ▶", false);

        // Continue button (primary CTA)
        BuildNavButton("SB_ContinueButton", footer, "CONTINUE", primary: true, width: 150);

        return root.gameObject;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Instruction Panel builder
    // ─────────────────────────────────────────────────────────────────────────

    static GameObject BuildInstructionPanel(Transform canvasParent)
    {
        // ── Root ────────────────────────────────────────────────────────────
        var root = MakeFullscreen("InstructionPanel", canvasParent);
        AddImage(root, C_Overlay);

        // ── Card ────────────────────────────────────────────────────────────
        var card = MakeRect("Card", root);
        SizeDelta(card, 860, 580);
        Anchor(card, 0.5f, 0.5f, 0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        var cardImg = AddImage(card, C_Card);
        cardImg.sprite = LoadSprite(SPRITE_PANEL);
        cardImg.type   = Image.Type.Sliced;
        AddOutline(card, C_Teal, new Vector2(1.5f, -1.5f));

        // ── Accent bar ──────────────────────────────────────────────────────
        var accent = MakeRect("AccentBar", card);
        Anchor(accent, 0f, 1f, 1f, 1f);
        accent.offsetMin = new Vector2(0, -3);
        accent.offsetMax = Vector2.zero;
        AddImage(accent, C_Teal);

        // ── Top image section ────────────────────────────────────────────────
        var topSection = MakeRect("TopSection", card);
        Anchor(topSection, 0f, 1f, 1f, 1f);
        topSection.offsetMin = new Vector2(0, -323);  // 3 accent + 320 image
        topSection.offsetMax = new Vector2(0, -3);
        AddImage(topSection, C_CardDark);

        var slideImgGO = MakeRect("Slide_Image", topSection);
        Anchor(slideImgGO, 0f, 0f, 1f, 1f);
        Offset(slideImgGO, 6, 6, -6, -6);
        var slideImgComp = slideImgGO.gameObject.AddComponent<Image>();
        slideImgComp.preserveAspect = true;
        slideImgComp.raycastTarget  = false;
        slideImgComp.color          = Color.white;

        // ── Bottom section (text) ────────────────────────────────────────────
        var bottom = MakeRect("BottomSection", card);
        Anchor(bottom, 0f, 0f, 1f, 1f);
        bottom.offsetMin = new Vector2(24, 56);
        bottom.offsetMax = new Vector2(-24, -323);

        var vlg = bottom.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding            = new RectOffset(0, 0, 12, 8);
        vlg.spacing            = 8;
        vlg.childAlignment     = TextAnchor.UpperCenter;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Body text
        var bodyGO = MakeRect("Slide_Body", bottom);
        var bodyTMP = bodyGO.gameObject.AddComponent<TextMeshProUGUI>();
        bodyTMP.text        = "Slide body text...";
        bodyTMP.fontSize    = 16;
        bodyTMP.color       = C_TextBody;
        bodyTMP.lineSpacing = 5;
        bodyTMP.alignment   = TextAlignmentOptions.Center;
        bodyTMP.overflowMode = TextOverflowModes.Overflow;
        AddLayoutElement(bodyGO, minHeight: 60, flexWidth: -1, flexHeight: 1);

        // Counter
        var counterGO = MakeRect("Slide_Counter", bottom);
        SizeDelta(counterGO, 0, 22);
        var counterTMP = counterGO.gameObject.AddComponent<TextMeshProUGUI>();
        counterTMP.text      = "1 / 3";
        counterTMP.fontSize  = 12;
        counterTMP.fontStyle = FontStyles.Italic;
        counterTMP.color     = C_TextMuted;
        counterTMP.alignment = TextAlignmentOptions.Center;
        AddLayoutElement(counterGO, minHeight: 22, flexWidth: -1, flexHeight: -1);

        // ── Footer ──────────────────────────────────────────────────────────
        var footer = MakeRect("Footer", card);
        Anchor(footer, 0f, 0f, 1f, 0f);
        footer.offsetMin = Vector2.zero;
        footer.offsetMax = new Vector2(0, 52);

        var hlg = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding            = new RectOffset(20, 20, 8, 8);
        hlg.spacing            = 10;
        hlg.childAlignment     = TextAnchor.MiddleCenter;
        hlg.childControlWidth  = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        BuildNavButton("Slide_PrevButton",  footer, "◀  PREV", false);
        BuildNavButton("Slide_NextButton",  footer, "NEXT  ▶", false);
        BuildNavButton("Slide_StartButton", footer, "START MISSION", primary: true, width: 170);

        return root.gameObject;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────────────

    static void BuildNavButton(string name, RectTransform parent,
                               string label, bool primary, int width = 120)
    {
        var go  = MakeRect(name, parent);
        SizeDelta(go, width, 36);

        // Background image
        var img = go.gameObject.AddComponent<Image>();
        img.color  = primary ? C_Teal : C_BtnDark;
        img.sprite = LoadSprite(SPRITE_PANEL);
        img.type   = Image.Type.Sliced;

        if (!primary)
            AddOutline(go, C_BtnBorder, new Vector2(1f, -1f));

        // Button component
        var btn   = go.gameObject.AddComponent<Button>();
        var block = btn.colors;
        if (primary)
        {
            block.normalColor      = C_Teal;
            block.highlightedColor = Hex("#5EFFD7");
            block.pressedColor     = Hex("#0DB890");
            block.disabledColor    = C_BtnDisabled;
        }
        else
        {
            block.normalColor      = C_BtnDark;
            block.highlightedColor = C_BtnHover;
            block.pressedColor     = C_BtnPressed;
            block.disabledColor    = C_BtnDisabled;
        }
        btn.colors = block;
        btn.targetGraphic = img;

        // Label
        var labelGO  = MakeRect("Label", go);
        Anchor(labelGO, 0f, 0f, 1f, 1f);
        Offset(labelGO, 0, 0, 0, 0);
        var tmp      = labelGO.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = primary ? 14 : 13;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = primary ? C_Black : Hex("#7A9BB5");
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // ── Rect helpers ──────────────────────────────────────────────────────────

    static RectTransform MakeFullscreen(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin   = Vector2.zero;
        rt.anchorMax   = Vector2.one;
        rt.offsetMin   = Vector2.zero;
        rt.offsetMax   = Vector2.zero;
        return rt;
    }

    static RectTransform MakeRect(string name, RectTransform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static Image AddImage(RectTransform rt, Color color)
    {
        var img   = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static void AddOutline(RectTransform rt, Color color, Vector2 distance)
    {
        var outline            = rt.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor    = color;
        outline.effectDistance = distance;
    }

    static void AddLayoutElement(RectTransform rt,
                                 float minHeight  = -1,
                                 float flexWidth  = -1,
                                 float flexHeight = -1)
    {
        var le = rt.gameObject.AddComponent<LayoutElement>();
        if (minHeight  >= 0) le.minHeight       = minHeight;
        if (flexWidth  >= 0) le.flexibleWidth   = flexWidth;
        if (flexHeight >= 0) le.flexibleHeight  = flexHeight;
    }

    static void Anchor(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
    }

    static void SizeDelta(RectTransform rt, float w, float h)
        => rt.sizeDelta = new Vector2(w, h);

    static void Offset(RectTransform rt, float l, float b, float r, float t)
    {
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(r, t);
    }

    // ── Sprite / colour helpers ───────────────────────────────────────────────

    static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[PanelBuilder] Sprite not found at: {path}");
        return sprite;
    }

    static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        c.a = alpha;
        return c;
    }

    // ── Canvas finder ─────────────────────────────────────────────────────────

    static Canvas FindCanvas()
    {
        var uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            var canvas = uiManager.GetComponentInParent<Canvas>();
            if (canvas != null) return canvas;
        }

        var fallback = Object.FindFirstObjectByType<Canvas>();
        if (fallback == null)
            Debug.LogError("[PanelBuilder] No Canvas found in scene. Open a game scene first.");
        return fallback;
    }
}
