using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Manages puzzle level: points, timer, and completion.
/// Attach to a manager GameObject in the scene.
/// 
/// Points = number of items currently in the bag.
/// Max points = total number of items.
/// Level complete when all items are placed (grid full).
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    [Header("References")]
    public SackGrid sackGrid;
    public List<DraggableItem> items = new List<DraggableItem>();

    [Header("Timer")]
    [Tooltip("Time limit in seconds for this level.")]
    public float timeLimit = 60f;
    private float timeRemaining;
    private bool timerRunning = true;

    [Header("UI (Optional)")]
    [Tooltip("Displays current points / max points.")]
    public TextMeshProUGUI pointsText;
    [Tooltip("Displays time remaining.")]
    public TextMeshProUGUI timerText;
    [Tooltip("Shown when level is complete.")]
    public GameObject levelCompleteUI;
    [Tooltip("Shown when time runs out.")]
    public GameObject timeUpUI;

    [Header("Next Level")]
    [Tooltip("Scene name to load when level is complete. Leave empty to not auto-load.")]
    public string nextLevelScene = "";

    [Header("Auto-Find Items")]
    public bool autoFindItems = true;
    public string itemNamePrefix = "LVLOne";

    // Points
    private int currentPoints = 0;
    private int maxPoints = 0;
    private bool levelComplete = false;
    private bool timeUp = false;

    void Start()
    {
        if (sackGrid == null)
            sackGrid = FindFirstObjectByType<SackGrid>();

        if (autoFindItems)
            FindItems();

        maxPoints = items.Count;
        currentPoints = 0;
        timeRemaining = timeLimit;
        levelComplete = false;
        timeUp = false;

        if (levelCompleteUI != null)
            levelCompleteUI.SetActive(false);
        if (timeUpUI != null)
            timeUpUI.SetActive(false);

        LogGridUsage();
    }

    void FindItems()
    {
        items.Clear();
        DraggableItem[] allItems = FindObjectsByType<DraggableItem>(FindObjectsSortMode.None);

        foreach (var item in allItems)
        {
            if (string.IsNullOrEmpty(itemNamePrefix) ||
                item.gameObject.name.StartsWith(itemNamePrefix))
            {
                items.Add(item);
            }
        }
    }

    void Update()
    {
        if (levelComplete || timeUp) return;

        // Update timer
        if (timerRunning)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                timerRunning = false;
                timeUp = true;
                OnTimeUp();
            }
        }

        // Update points based on how many items are currently in the grid
        currentPoints = sackGrid.OccupiedItemCount();

        // Check for level completion
        if (sackGrid.IsGridFull())
        {
            levelComplete = true;
            timerRunning = false;
            OnLevelComplete();
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (pointsText != null)
            pointsText.text = $"Points: {currentPoints} / {maxPoints}";

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timerText.text = $"{minutes}:{seconds:00}";
        }
    }

    void OnLevelComplete()
    {
        Debug.Log($"[PuzzleManager] Level Complete! Points: {currentPoints}/{maxPoints}");

        if (levelCompleteUI != null)
            levelCompleteUI.SetActive(true);

        // Disable dragging on all items
        foreach (var item in items)
            item.enabled = false;
    }

    void OnTimeUp()
    {
        Debug.Log($"[PuzzleManager] Time's Up! Points: {currentPoints}/{maxPoints}");

        if (timeUpUI != null)
            timeUpUI.SetActive(true);

        // Disable dragging on all items
        foreach (var item in items)
            item.enabled = false;
    }

    /// <summary>
    /// Call this from a "Next Level" button.
    /// </summary>
    public void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelScene))
            SceneManager.LoadScene(nextLevelScene);
    }

    /// <summary>
    /// Call this from a "Retry" button.
    /// </summary>
    public void RetryLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Call this from a "Reset" button without reloading the scene.
    /// </summary>
    public void ResetPuzzle()
    {
        foreach (var item in items)
        {
            item.enabled = true;
            item.ResetItem();
        }

        sackGrid.BuildGrid();
        currentPoints = 0;
        timeRemaining = timeLimit;
        timerRunning = true;
        levelComplete = false;
        timeUp = false;

        if (levelCompleteUI != null)
            levelCompleteUI.SetActive(false);
        if (timeUpUI != null)
            timeUpUI.SetActive(false);
    }

    public int GetCurrentPoints() => currentPoints;
    public int GetMaxPoints() => maxPoints;
    public float GetTimeRemaining() => timeRemaining;
    public bool IsLevelComplete() => levelComplete;
    public bool IsTimeUp() => timeUp;

    public void LogGridUsage()
    {
        int totalCellsNeeded = 0;
        foreach (var item in items)
            totalCellsNeeded += item.GetCellCount();

        Debug.Log($"[PuzzleManager] Items: {items.Count}, " +
                  $"Cells needed: {totalCellsNeeded}, " +
                  $"Cells available: {sackGrid.TotalCells()} " +
                  $"({sackGrid.columns}x{sackGrid.rows})");
    }
}