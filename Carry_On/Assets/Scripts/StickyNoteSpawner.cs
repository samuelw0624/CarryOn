using UnityEngine;

public class StickyNoteSpawner : MonoBehaviour {
    [Header("References")]
    public GameObject stickyNotePrefab;
    public RectTransform canvasRoot;
    public RectTransform suitcaseRect;

    [Header("Sticky Note Appearance")]
    public Sprite[] noteSprites;
    public TMPro.TMP_FontAsset[] fontOptions;

    [Header("Spawn Settings")]
    public int noteCount = 5;
    public Vector2 offsetFromSuitcase = new Vector2(100f, 0f); // initial offset right of suitcase
    public float horizontalSpacing = 150f; // distance between notes

    void Start() {
        Vector2 suitcasePos = suitcaseRect.anchoredPosition;
        float suitcaseRightEdge = suitcasePos.x + (suitcaseRect.rect.width * suitcaseRect.localScale.x * 0.5f);
        Vector2 startPos = new Vector2(suitcaseRightEdge, suitcasePos.y) + offsetFromSuitcase;

        for (int i = 0; i < noteCount; i++) {
            GameObject note = Instantiate(stickyNotePrefab, canvasRoot);
            RectTransform rt = note.GetComponent<RectTransform>();

            rt.anchoredPosition = startPos + new Vector2(i * horizontalSpacing, 0f);

            // Apply appearance
            var rand = note.GetComponent<StickyNoteRandomizer>();
            if (rand != null) {
                rand.noteSprites = noteSprites;
                rand.fontOptions = fontOptions;
            }
        }
    }
}
