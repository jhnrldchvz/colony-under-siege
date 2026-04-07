using System.Collections;
using UnityEngine;

/// <summary>
/// CameraShake — applies positional shake to the cameraHolder transform.
///
/// Setup:
///   1. Attach this script to the same GameObject as the cameraHolder in PlayerController
///      (the child Transform the player looks through, NOT the player root).
///   2. Call CameraShake.Instance.Shake(duration, magnitude) from any script.
///
/// Why localPosition and not localRotation?
///   PlayerController.HandleLook() only touches localRotation, so offsetting
///   localPosition never fights the look system — no jitter, no drift.
///
/// Safe to call while already shaking — restarts with new values.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3   _restLocalPos;
    private Coroutine _shakeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance      = this;
        _restLocalPos = transform.localPosition;
    }

    /// <summary>
    /// Shake the camera for <paramref name="duration"/> seconds
    /// with a peak displacement of <paramref name="magnitude"/> units.
    /// Magnitude eases out to zero over the duration.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);

        _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t          = elapsed / duration;
            float currentMag = magnitude * (1f - t); // linear ease-out

            transform.localPosition = _restLocalPos + new Vector3(
                Random.Range(-1f, 1f) * currentMag,
                Random.Range(-1f, 1f) * currentMag,
                0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = _restLocalPos;
        _shakeRoutine           = null;
    }
}
