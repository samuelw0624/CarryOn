using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    [Header("Note Tracking")]
    [SerializeField] private int totalNotes = 5;
    private int notesAttached = 0;

    void Awake() {
        // Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (readyToBoardButton != null)
            readyToBoardButton.SetActive(false);
    }

    void Start() {
        SpawnAndStyleNotes();
    }

    private void SpawnAndStyleNotes() {
        foreach (GameObject point in spawnPoints) {
            if (point == null) continue;

            RectTransform pointRect = point.GetComponent<RectTransform>();
            if (pointRect == null) continue;

            GameObject note = Instantiate(stickyNotePrefab, canvasRoot);
            RectTransform noteRect = note.GetComponent<RectTransform>();
            noteRect.anchoredPosition = pointRect.anchoredPosition;

            ApplyRandomStyle(note);
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
    public void RegisterNoteAttachment() {
        notesAttached++;
        if (notesAttached >= totalNotes && readyToBoardButton != null) {
            readyToBoardButton.SetActive(true);
        }
    }
    public void LoadCarouselScene() {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene("CarouselScene");
        else
            Debug.LogWarning("SceneLoader.Instance is missing.");
    }
}
