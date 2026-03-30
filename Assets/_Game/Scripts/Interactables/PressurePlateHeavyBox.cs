using UnityEngine;

/// <summary>
/// PressurePlateHeavyBox — marks a Rigidbody as heavy enough to
/// activate pressure plates. Also shows a proximity label.
///
/// Setup:
///   1. Add to any box you want to work as a plate weight
///   2. Set mass in Rigidbody to at least PressurePlate.minMass
///   3. Optionally wire a label canvas for pickup hint
///
/// This extends HoldableObject functionality — attach both.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PressurePlateHeavyBox : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Must match or exceed PressurePlate.minMass to activate it")]
    public float boxMass = 8f;

    [Tooltip("Hint shown to player near this box")]
    public string hintText = "Heavy container — place on pressure plate";

    private void Awake()
    {
        GetComponent<Rigidbody>().mass = boxMass;
    }
}