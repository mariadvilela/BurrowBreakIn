using UnityEngine;

/// <summary>
/// Represents a single line of dialogue in a cutscene.
/// </summary>
[System.Serializable]
public class DialogueLine
{
    [Tooltip("Character name displayed above the dialogue text.")]
    public string speakerName;

    [Tooltip("The dialogue text to display.")]
    [TextArea(2, 5)]
    public string dialogueText;

    [Tooltip("The audio clip (MP3) for this line.")]
    public AudioClip audioClip;
}