using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BossHealthBar — large health bar shown at the top of the screen during boss fight.
/// Attach to a UI Panel in the HUD Canvas.
///
/// Setup:
///   1. Create Panel in HUD Canvas → rename "BossHealthBarPanel"
///   2. Add Background Image (dark) full width
///   3. Add FillImage (red, Image Type: Filled, Fill Method: Horizontal)
///   4. Optionally add TMP for boss name and HP text
///   5. Attach this script to the Panel
///   6. Wire BossController.bossHealthBar slot
///   7. Set Panel inactive by default — BossController.Initialize() activates it
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI")]
    public Image           fillImage;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI hpText;

    [Header("Colors")]
    public Color fullColor = new Color(0.85f, 0.15f, 0.15f);
    public Color lowColor  = new Color(0.5f,  0.05f, 0.05f);

    [Header("Settings")]
    public string bossName  = "Reactor Guardian";
    public float  fillSpeed = 5f;

    // ---------------------------------------------------------------
    private float _targetFill  = 1f;
    private float _currentFill = 1f;
    private int   _maxHealth;

    private void Update()
    {
        _currentFill = Mathf.Lerp(_currentFill, _targetFill,
                                  Time.deltaTime * fillSpeed);
        if (fillImage != null)
        {
            fillImage.fillAmount = _currentFill;
            fillImage.color = Color.Lerp(lowColor, fullColor, _currentFill);
        }
    }

    public void Initialize(int maxHP)
    {
        _maxHealth   = maxHP;
        _targetFill  = 1f;
        _currentFill = 1f;

        gameObject.SetActive(true);

        if (bossNameText != null) bossNameText.text = bossName;
        if (hpText       != null) hpText.text       = $"{maxHP} / {maxHP}";
        if (fillImage    != null) fillImage.fillAmount = 1f;
    }

    public void UpdateHealth(int currentHP, int maxHP)
    {
        _targetFill = Mathf.Clamp01((float)currentHP / maxHP);
        if (hpText != null) hpText.text = $"{currentHP} / {maxHP}";
    }
}