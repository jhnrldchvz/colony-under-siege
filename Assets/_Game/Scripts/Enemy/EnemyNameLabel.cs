using UnityEngine;
using TMPro;

/// <summary>
/// EnemyNameLabel — shows the enemy type name above the health bar.
/// Sits on a separate World Space Canvas so it never affects
/// the health bar layout.
///
/// Setup:
///   1. Create a second World Space Canvas on the enemy prefab
///      named "NameCanvas". Scale (0.01,0.01,0.01), Pos (0,2.8,0).
///   2. Add a TextMeshPro child inside it.
///   3. Attach this script to NameCanvas.
///   4. Wire the TMP text into the labelText slot.
///   5. The name is read automatically from EnemyStats.enemyName.
/// </summary>
public class EnemyNameLabel : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI labelText;

    [Header("Visibility")]
    public float showRange = 15f;
    public float fadeSpeed = 8f;

    private Camera      _cam;
    private Transform   _player;
    private CanvasGroup _cg;

    private void Start()
    {
        _cam = Camera.main;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 0f;

        RefreshName();
    }

    /// <summary>
    /// Reads the enemy name from the stat preset.
    /// Called from Start() and can be called manually if the preset
    /// is assigned after Start() runs.
    /// </summary>
    public void RefreshName()
    {
        if (labelText == null) return;

        EnemyAI ai = GetComponentInParent<EnemyAI>();

        if (ai != null && ai.statPreset != null &&
            !string.IsNullOrEmpty(ai.statPreset.enemyName))
        {
            labelText.text = ai.statPreset.enemyName;
            Debug.Log($"[EnemyNameLabel] Name set to: {ai.statPreset.enemyName}");
        }
        else
        {
            // Fallback — show nothing if no preset assigned
            labelText.text = "";
            if (ai != null && ai.statPreset == null)
                Debug.LogWarning($"[EnemyNameLabel] No statPreset on {ai.gameObject.name} — assign one in Inspector.");
        }
    }

    private void Update()
    {
        if (_cam == null || _player == null) return;

        // Billboard
        transform.LookAt(transform.position + _cam.transform.forward);

        // Fade in/out based on distance
        float dist   = Vector3.Distance(transform.position, _player.position);
        float target = dist <= showRange ? 1f : 0f;
        _cg.alpha    = Mathf.Lerp(_cg.alpha, target, Time.deltaTime * fadeSpeed);
    }

    /// <summary>Set the name text directly — call from EnemyAI if needed.</summary>
    public void SetName(string enemyName)
    {
        if (labelText != null)
            labelText.text = enemyName;
    }
}