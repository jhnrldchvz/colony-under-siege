using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Smooth scale-pulse hover effect for Stage Select cards.
/// Attach to each card root alongside the Button component.
/// Selection alpha is managed separately by MainMenuManager — they don't conflict.
/// </summary>
public class StageCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Scale applied while the cursor is over the card")]
    public float hoverScale   = 1.07f;
    [Tooltip("Seconds to reach the target scale (SmoothStep eased)")]
    public float duration     = 0.12f;

    private Coroutine _anim;

    public void OnPointerEnter(PointerEventData _) => Animate(hoverScale);
    public void OnPointerExit (PointerEventData _) => Animate(1f);

    private void Animate(float target)
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(ScaleTo(target));
    }

    private IEnumerator ScaleTo(float target)
    {
        float start   = transform.localScale.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float s = Mathf.Lerp(start, target, t);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        transform.localScale = new Vector3(target, target, 1f);
    }
}
