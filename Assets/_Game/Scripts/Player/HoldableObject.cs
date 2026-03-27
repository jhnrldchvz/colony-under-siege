using UnityEngine;

/// <summary>
/// HoldableObject — attach to any physics box the player can grab.
///
/// Setup:
///   1. Add Rigidbody to the GameObject — Use Gravity ON, Is Kinematic OFF.
///   2. Add Box Collider fitted to the mesh.
///   3. Add this script.
///   4. Add Quick Outline component — will be toggled by GrabController.
///   5. Set the GameObject layer to "Holdable" (create this layer).
///   6. In GrabController Inspector → Grab Layer → select "Holdable".
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HoldableObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How heavy the object feels when thrown — affects physics collisions")]
    public float mass = 5f;

    [Tooltip("Object can only be picked up once — becomes regular physics object after first throw")]
    public bool oneTimeUse = false;

    [Header("Outline")]
    public Outline outline;

    // ---------------------------------------------------------------
    private Rigidbody _rb;
    private bool      _used = false;

    private void Awake()
    {
        _rb      = GetComponent<Rigidbody>();
        _rb.mass = mass;

        if (outline == null) outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    // ---------------------------------------------------------------
    // Called by GrabController
    // ---------------------------------------------------------------
    public void OnPickedUp()
    {
        // Can extend — play sound, particles etc.
    }

    public void OnDropped()
    {
        if (oneTimeUse) _used = true;
    }

    public void OnThrown()
    {
        if (oneTimeUse) _used = true;
    }

    public void SetOutline(bool show, Color color)
    {
        if (outline == null) return;
        outline.enabled      = show;
        if (show) outline.OutlineColor = color;
    }

    public bool CanBeGrabbed => !_used;
}