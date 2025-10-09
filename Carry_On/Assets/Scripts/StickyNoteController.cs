using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class StickyNoteController : MonoBehaviour {
    public static StickyNoteController Instance;

    [Header("Spawn Settings")]
    public GameObject stickyNotePrefab;
    public RectTransform canvasRoot;
    public GameObject[] spawnPoints;

    [Header("Random Appearance")]
    public Sprite[] noteSprites;
    public TMP_FontAsset[] fontOptions;

    [Header("UI")]
    public GameObject readyToBoardButton;

    private readonly HashSet<DraggableItem> coveredItems = new HashSet<DraggableItem>();
    private int notesSpawned = 0; // actual count of notes created at runtime

    void Awake() {
        // Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (readyToBoardButton != null)
            readyToBoardButton.SetActive(false);
    }

    void Start() {
        SpawnAndStyleNotes();
        EvaluateReadyState(); // handles edge cases
    }

    private void SpawnAndStyleNotes() {
        notesSpawned = 0;

        if (spawnPoints == null || canvasRoot == null || stickyNotePrefab == null) return;

        foreach (var point in spawnPoints) {
            if (!point) continue;

            var pointRect = point.GetComponent<RectTransform>();
            if (!pointRect) continue;

            var note = Instantiate(stickyNotePrefab, canvasRoot);
            var noteRect = note.GetComponent<RectTransform>();
            noteRect.anchoredPosition = pointRect.anchoredPosition;

            ApplyRandomStyle(note);
            notesSpawned++;
        }
    }

    private void ApplyRandomStyle(GameObject note) {
        // Random sprite
        Sprite selectedSprite = null;
        if (noteSprites != null && noteSprites.Length > 0)
            selectedSprite = noteSprites[Random.Range(0, noteSprites.Length)];

        // Random font
        TMP_FontAsset selectedFont = null;
        if (fontOptions != null && fontOptions.Length > 0)
            selectedFont = fontOptions[Random.Range(0, fontOptions.Length)];

        // Apply visuals
        Image[] images = note.GetComponentsInChildren<Image>();
        foreach (var img in images) {
            if (img != null && selectedSprite != null)
                img.sprite = selectedSprite;
        }

        TMP_InputField inputField = note.GetComponentInChildren<TMP_InputField>();
        if (inputField != null && selectedFont != null)
            inputField.textComponent.font = selectedFont;

        TextMeshProUGUI inputText = note.GetComponentInChildren<TextMeshProUGUI>();
        if (inputText != null && selectedFont != null)
            inputText.font = selectedFont;
    }

    //Called when a note is successfully attached
    public void RegisterItemCovered(DraggableItem item) {
        if (!item) return;

        // HashSet guarantees uniqueness; no double counting if user tries to re-fire.
        if (coveredItems.Add(item)) {
            EvaluateReadyState();
        }
    }
    private void EvaluateReadyState() {
        int itemCount =
            (SuitcaseData.Instance != null && SuitcaseData.Instance.packedItems != null)
            ? SuitcaseData.Instance.packedItems.Count
            : 0;

        // If spawnPoints is null, treat as 0 notes; otherwise use notes actually spawned.
        int noteCount = Mathf.Max(0, notesSpawned);

        // The goal is: cover every item if there are fewer items than notes,
        // or use all notes if there are more items than notes.
        int threshold = Mathf.Min(itemCount, noteCount);

        bool ready = (threshold > 0) && (coveredItems.Count >= threshold);
        if (readyToBoardButton) readyToBoardButton.SetActive(ready);
    }

    public void LoadCarouselScene() {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene("CarouselScene");
        else
            Debug.LogWarning("SceneLoader.Instance is missing.");
    }
}
