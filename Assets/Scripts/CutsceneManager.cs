using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Manages cutscene dialogue playback.
/// Plays audio for each line, displays text, and advances on click.
/// Loads the next scene when all dialogue is finished.
/// 
/// Setup:
/// 1. Create a Canvas with a speaker name Text, dialogue Text, and a prompt Text ("Click to continue...")
/// 2. Attach this script to an empty GameObject
/// 3. Add DialogueLine entries in the Inspector (speaker, text, audio clip)
/// 4. Set the next scene name
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    [Header("Dialogue Lines")]
    [Tooltip("Add all dialogue lines for this cutscene in order.")]
    public DialogueLine[] dialogueLines;

    [Header("UI References")]
    [Tooltip("Text element for the speaker's name.")]
    public TextMeshProUGUI speakerNameText;

    [Tooltip("Text element for the dialogue content.")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("Text element showing 'Click to continue...' prompt. Hidden while audio is playing.")]
    public TextMeshProUGUI continuePromptText;

    [Header("Audio")]
    [Tooltip("AudioSource to play dialogue clips. Will be auto-created if not assigned.")]
    public AudioSource audioSource;

    [Header("Navigation")]
    [Tooltip("Scene to load after all dialogue is finished.")]
    public string nextSceneName;

    [Header("Settings")]
    [Tooltip("If true, player must wait for audio to finish before advancing.")]
    public bool waitForAudioToFinish = true;

    private int currentLineIndex = -1;
    private bool audioFinished = false;
    private bool cutsceneComplete = false;
    private Mouse mouse;

    void Start()
    {
        mouse = Mouse.current;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (continuePromptText != null)
            continuePromptText.gameObject.SetActive(false);

        // Show first line
        ShowNextLine();
    }

    void Update()
    {
        if (mouse == null) mouse = Mouse.current;
        if (mouse == null) return;

        if (cutsceneComplete) return;

        // Check if audio finished playing
        if (!audioFinished && !audioSource.isPlaying && currentLineIndex >= 0)
        {
            audioFinished = true;
            if (continuePromptText != null)
                continuePromptText.gameObject.SetActive(true);
        }

        // Handle click to advance
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (waitForAudioToFinish && !audioFinished)
                return;

            ShowNextLine();
        }
    }

    void ShowNextLine()
    {
        currentLineIndex++;

        // All lines finished — load next scene
        if (currentLineIndex >= dialogueLines.Length)
        {
            cutsceneComplete = true;
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];

        // Update UI
        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        if (dialogueText != null)
            dialogueText.text = line.dialogueText;

        if (continuePromptText != null)
            continuePromptText.gameObject.SetActive(false);

        // Play audio
        audioFinished = false;
        audioSource.Stop();

        if (line.audioClip != null)
        {
            audioSource.clip = line.audioClip;
            audioSource.Play();
        }
        else
        {
            // No audio clip — allow immediate advance
            audioFinished = true;
            if (continuePromptText != null)
                continuePromptText.gameObject.SetActive(true);
        }
    }
}