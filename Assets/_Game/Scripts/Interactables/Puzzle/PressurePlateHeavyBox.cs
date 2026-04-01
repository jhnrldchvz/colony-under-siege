using UnityEngine;

/// <summary>
/// PressurePlateHeavyBox — marks a Rigidbody as heavy enough to
/// activate pressure plates. Any box with this component can activate
/// any pressure plate — no linking required.
///
/// When picked up, ALL pressure plates in the scene highlight.
/// This lets the player know where to bring the box.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PressurePlateHeavyBox : MonoBehaviour
{
    [Header("Settings")]
    public float boxMass = 8f;

    private HoldableObject _holdable;

    private void Awake()
    {
        GetComponent<Rigidbody>().mass = boxMass;
        _holdable = GetComponent<HoldableObject>();
    }

    private void OnEnable()
    {
        if (_holdable != null)
            _holdable.OnHoldStateChanged += OnHoldStateChanged;
    }

    private void OnDisable()
    {
        if (_holdable != null)
            _holdable.OnHoldStateChanged -= OnHoldStateChanged;
    }

    private void OnHoldStateChanged(bool isHeld)
    {
        // Notify ALL pressure plates in the scene — no linking needed
        PressurePlate[] plates = FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);
        foreach (PressurePlate plate in plates)
        {
            if (isHeld) plate.ShowHighlight();
            else        plate.HideHighlight();
        }
    }
}
