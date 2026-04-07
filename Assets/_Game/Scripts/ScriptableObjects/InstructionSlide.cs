using UnityEngine;

/// <summary>
/// Data for one instruction slide shown before gameplay starts.
/// Assign in the Inspector on UIManager per stage.
/// </summary>
[System.Serializable]
public class InstructionSlide
{
    [Tooltip("Image shown on this slide")]
    public Sprite image;

    [Tooltip("Text shown below the image")]
    [TextArea(2, 5)]
    public string bodyText = "";
}
