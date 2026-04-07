using UnityEngine;

/// <summary>
/// StoryboardSlide — data for one narrative slide shown in the Storyboard panel.
///
/// Assign arrays of these in UIManager per stage.
/// The storyboard supports a title line, a body paragraph, and an optional image.
/// </summary>
[System.Serializable]
public class StoryboardSlide
{
    [Tooltip("Optional full-bleed or panel image for this slide")]
    public Sprite image;

    [Tooltip("Short title line — e.g. character name or location")]
    public string titleText = "";

    [Tooltip("Narrative body text — 2-4 sentences max for readability")]
    [TextArea(3, 6)]
    public string bodyText = "";
}
