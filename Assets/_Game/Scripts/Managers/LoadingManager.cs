using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Persistent loading screen manager.
/// GameManager.LoadScene() already routes through LoadingManager.Instance.LoadScene(buildIndex).
///
/// Setup:
///   Use the menu item  Colony Under Siege › Build Loading Screen  to generate the Canvas
///   hierarchy inside a DontDestroyOnLoad object, then wire the Inspector fields.
/// </summary>
public class LoadingManager : MonoBehaviour
{
    // ───────────────────────── Singleton ─────────────────────────────────────

    public static LoadingManager Instance { get; private set; }

    // ───────────────────────── Inspector ─────────────────────────────────────

    [Header("Canvas")]
    [Tooltip("Full-screen loading canvas. Sort Order should be >= 100.")]
    public Canvas      loadingCanvas;
    public CanvasGroup canvasGroup;

    [Header("UI Elements")]
    public Image    progressBarFill;   // Image Type: Filled, Horizontal
    public TMP_Text stageNameText;
    public TMP_Text loadingLabel;      // animated LOADING...
    public TMP_Text tipText;
    public TMP_Text percentText;       // optional  e.g. "74%"

    [Header("Timing")]
    [Tooltip("Minimum time the loading screen stays visible (seconds)")]
    public float minimumDisplayTime = 1.5f;
    [Tooltip("Fade in / out duration (seconds)")]
    public float fadeDuration       = 0.35f;

    [Header("Stage Names")]
    [Tooltip("Display name for each Build Settings index. Index 0 = Main Menu.")]
    public string[] stageNames = { "Main Menu", "Stage 1", "Stage 2", "Stage 3", "Stage 4", "Stage 5", "Tutorial" };

    [Header("Tips")]
    [TextArea(1, 3)]
    public string[] tips =
    {
        "Eliminate all enemies to clear a wave.",
        "Use cover — enemies aim for your head.",
        "Throwable objects can stun enemies.",
        "Explore each stage for hidden pickups.",
        "Your shield regenerates when out of danger.",
        "Headshots deal bonus damage.",
        "Watch your ammo — reload between waves.",
        "Boss enemies have multiple weak points.",
        "Environmental hazards affect enemies too.",
        "Some doors unlock only after clearing a wave.",
    };

    // ───────────────────────── Private state ─────────────────────────────────

    private Coroutine _dotCoroutine;

    // ───────────────────────── Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingCanvas != null) loadingCanvas.gameObject.SetActive(false);
        if (canvasGroup   != null) canvasGroup.alpha = 0f;
    }

    // ───────────────────────── Public API ────────────────────────────────────

    public void LoadScene(int buildIndex)
    {
        StartCoroutine(LoadSequence(buildIndex));
    }

    // ───────────────────────── Core coroutine ────────────────────────────────

    private IEnumerator LoadSequence(int buildIndex)
    {
        // 1 — populate UI
        if (stageNameText != null)
            stageNameText.text = ResolveStage(buildIndex);

        if (tipText != null && tips != null && tips.Length > 0)
            tipText.text = tips[Random.Range(0, tips.Length)];

        SetProgress(0f);

        // 2 — show & fade in
        if (loadingCanvas != null) loadingCanvas.gameObject.SetActive(true);
        if (canvasGroup   != null) yield return StartCoroutine(Fade(0f, 1f));

        _dotCoroutine = StartCoroutine(AnimateDots());

        // 3 — async load (hold at 90%)
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        while (!op.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float loadP = Mathf.Clamp01(op.progress / 0.9f);
            float timeP = Mathf.Clamp01(elapsed / minimumDisplayTime);
            SetProgress(Mathf.Min(loadP, timeP));

            if (op.progress >= 0.9f && elapsed >= minimumDisplayTime)
            {
                SetProgress(1f);
                yield return new WaitForSecondsRealtime(0.2f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }

        // 4 — fade out & hide
        if (_dotCoroutine != null) { StopCoroutine(_dotCoroutine); _dotCoroutine = null; }
        if (canvasGroup   != null) yield return StartCoroutine(Fade(1f, 0f));
        if (loadingCanvas != null) loadingCanvas.gameObject.SetActive(false);
    }

    // ───────────────────────── Helpers ───────────────────────────────────────

    private void SetProgress(float t)
    {
        if (progressBarFill != null) progressBarFill.fillAmount = t;
        if (percentText     != null) percentText.text = Mathf.RoundToInt(t * 100f) + "%";
    }

    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;
        float e = 0f;
        canvasGroup.alpha = from;
        while (e < fadeDuration)
        {
            e += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, e / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator AnimateDots()
    {
        if (loadingLabel == null) yield break;
        string[] frames = { "LOADING", "LOADING.", "LOADING..", "LOADING..." };
        int i = 0;
        while (true)
        {
            loadingLabel.text = frames[i++ % frames.Length];
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    private string ResolveStage(int buildIndex)
    {
        if (stageNames != null && buildIndex >= 0 && buildIndex < stageNames.Length)
            return stageNames[buildIndex];
        return $"Stage {buildIndex}";
    }
}
