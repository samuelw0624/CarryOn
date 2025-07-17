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
        if (panelRoot != null)
            panelRoot.SetActive(true);

        foreach (Transform child in displayArea)
            Destroy(child.gameObject);

        if (suitcase == null || suitcase.Count == 0) {
            Debug.Log("No suitcase data to show. Displaying empty suitcase.");
            return;
        }

        foreach (var packed in suitcase) {
            GameObject item = Instantiate(itemPrefab, displayArea);
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.localPosition = packed.localposition;
            rt.sizeDelta = packed.sizeDelta;

            Image img = item.GetComponent<Image>();
            if (img != null)
                img.sprite = packed.sprite;

            if (item.TryGetComponent(out CanvasGroup cg))
                cg.blocksRaycasts = false;

            if (item.TryGetComponent(out DraggableItem di))
                Destroy(di);
        }
    }

    public void HideDisplayPanel() {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
