using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PackingPhaseManager : MonoBehaviour {
    [Header("UI")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] Button proceedButton;

    [Header("Settings")]
    [SerializeField, Tooltip("Seconds")]
    int startSeconds = 300;

    float timeLeft;
    bool packingActive = true;
    private RectTransform suitcase;

    void Awake() {
        timeLeft = startSeconds;
        proceedButton.onClick.AddListener(EndPackingEarly);
        UpdateTimerUI();

        // NEW: grab the suitcase RectTransform
        var suitcaseArea = FindObjectOfType<SuitcaseArea>();
        if (suitcaseArea != null)
            suitcase = suitcaseArea.GetComponent<RectTransform>();
    }

    void Update() {
        if (!packingActive) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) {
            timeLeft = 0f;
            EndPacking();
        }
        UpdateTimerUI();
    }

    void UpdateTimerUI() {
        int minutes = Mathf.FloorToInt(timeLeft / 60);
        int seconds = Mathf.FloorToInt(timeLeft % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void EndPackingEarly() => EndPacking();

    void EndPacking() {
        if (!packingActive) return;
        packingActive = false;

        // Store packed items
        SuitcaseData.Instance.packedItems.Clear();

        foreach (var item in FindObjectsOfType<DraggableItem>()) {
            if (item.FullyInsideSuitcase()) {
                var packed = new PackedItem {
                    itemDef = item.itemData,
                    localposition = suitcase.transform.InverseTransformPoint(item.transform.position), // or localPosition if inside a layout
                    sprite = item.GetComponent<UnityEngine.UI.Image>().sprite
                };
                SuitcaseData.Instance.packedItems.Add(packed);
            }
        }
            // Disable drag, transition
            foreach (var drag in FindObjectsOfType<DraggableItem>())
            drag.enabled = false;

        proceedButton.interactable = false;
        SceneLoader.Instance.LoadScene("NoteEditScene");
    }
}
