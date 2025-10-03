using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class CarouselSceneManager : MonoBehaviour {
    [Header("Carousel Setup")]
    public CarouselSuitcaseButton[] suitcaseButtons;

    [Header("Display Panel")]
    public GameObject panelRoot;
    public RectTransform displayArea;
    public GameObject itemPrefab;
    public Button closeButton;

    [Header("Back to Title")]
    public Button backToTitleButton;

    void Start() {
        SetupSuitcaseButtons();

        if (closeButton != null)
            closeButton.onClick.AddListener(HideDisplayPanel);

        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(() =>
                SceneLoader.Instance.LoadScene("TitleScene"));

        HideDisplayPanel();
    }

    void SetupSuitcaseButtons() {
        int total = SuitcaseArchive.Instance.GetTotalSuitcases();
        if (total == 0) return;

        if (total == 0) {
            // Optional: make first button open an empty suitcase panel
            if (suitcaseButtons.Length > 0) {
                suitcaseButtons[0].button.onClick.AddListener(() => ShowSuitcase(null));
            }
            return;
        }

        System.Random rng = new System.Random();
        HashSet<int> usedIndices = new HashSet<int>();

        foreach (var btn in suitcaseButtons) {
            int index;
            do {
                index = rng.Next(0, total);
            } while (usedIndices.Contains(index) && usedIndices.Count < total);

            usedIndices.Add(index);
            btn.Initialize(index, this); // pass this manager reference
        }
    }

    public void ShowSuitcase(List<PackedItem> suitcase) {
        if (panelRoot != null) panelRoot.SetActive(true);

        foreach (Transform child in displayArea) Destroy(child.gameObject);
        if (suitcase == null || suitcase.Count == 0) return;

        foreach (var packed in suitcase) {
            GameObject item = Instantiate(itemPrefab, displayArea);

            var rt = item.GetComponent<RectTransform>();
            rt.anchoredPosition = (Vector2)packed.localposition; // better than localPosition for UI
            rt.sizeDelta = packed.sizeDelta;
            item.transform.SetAsLastSibling();                   // ensure on top for raycast

            var img = item.GetComponent<Image>();
            if (img) img.sprite = packed.sprite;

            // Keep raycasts ON so hover works
            var cg = item.GetComponent<CanvasGroup>();
            if (cg) { cg.blocksRaycasts = true; cg.interactable = false; } // optional

            var di = item.GetComponent<DraggableItem>();
            if (di) {
                di.itemData = packed.itemDef;   // same as your working scene
                di.allowDragging = false;         // disable dragging here
            }
        }
    }

    public void HideDisplayPanel() {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
