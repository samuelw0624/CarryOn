using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
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

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // NEW: Cloud + Catalog config
    [Header("Cloud (Supabase)")]
    [Tooltip("How many latest cloud suitcases to pull before sampling 5 for the carousel.")]
    public int fetchLimit = 100;

    [Header("Catalog (ItemID ¡ú ItemDef/Sprite)")]
    [Tooltip("Fill this once in the Inspector so we can resolve item_id from the cloud.")]
    public ItemCatalogEntry[] catalog = Array.Empty<ItemCatalogEntry>();

    // fast lookups built from 'catalog'
    Dictionary<string, ItemCatalogEntry> catalogLookup;

    // cloud picks cached for the current session
    List<List<PackedItem>> cloudPicks = new();

    [Serializable]
    public class ItemCatalogEntry {
        public ItemDef def;          // drag your ItemDef SO here
        public Sprite sprite;        // sprite to show in the carousel for this item
        public string ItemID => def ? def.ItemID : null;
    }

    // matches the Supabase JSON wrapper we used
    [Serializable] class CloudRow { public SuitcaseDTO payload; public string created_at; public string id; }
    [Serializable] class CloudRowsWrapper { public CloudRow[] rows; }

    public GameObject foregroundPlant;

    void Start() {
        // Build lookup once
        catalogLookup = new Dictionary<string, ItemCatalogEntry>();
        foreach (var e in catalog) {
            if (e != null && e.def != null && !string.IsNullOrEmpty(e.ItemID)) {
                catalogLookup[e.ItemID] = e;
            }
        }

        // Start async init (cloud ¡ú fallback local)
        StartCoroutine(InitCarouselAsync());

        if (closeButton != null)
            closeButton.onClick.AddListener(HideDisplayPanel);

        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(() =>
                SceneLoader.Instance.LoadScene("TitleScene"));

        HideDisplayPanel();
    }

    IEnumerator InitCarouselAsync() {
        // 1) Try cloud
        bool gotCloud = false;
        yield return StartCoroutine(TryPopulateButtonsFromCloud(() => gotCloud = true));

        // 2) Fallback to local archive if needed
        if (!gotCloud) {
            PopulateButtonsFromLocalArchive();
        }
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // NEW: Cloud path
    IEnumerator TryPopulateButtonsFromCloud(Action onSuccess) {
        string json = null;
        yield return SuitcaseCloud.GetLatest(fetchLimit, s => json = s);

        if (string.IsNullOrEmpty(json)) {
            Debug.LogWarning("[Carry-On] Cloud fetch returned empty or failed. Falling back to local archive.");
            yield break;
        }

        // wrap array for JsonUtility
        var wrapped = "{\"rows\":" + json + "}";
        var parsed = JsonUtility.FromJson<CloudRowsWrapper>(wrapped);
        if (parsed?.rows == null || parsed.rows.Length == 0) {
            Debug.LogWarning("[Carry-On] No cloud rows. Falling back to local archive.");
            yield break;
        }

        // convert all cloud rows to PackedItem lists
        List<List<PackedItem>> allCloudSuitcases = new();
        foreach (var r in parsed.rows) {
            var suitcase = ConvertToPacked(r.payload);
            if (suitcase != null && suitcase.Count > 0)
                allCloudSuitcases.Add(suitcase);
        }

        if (allCloudSuitcases.Count == 0) {
            Debug.LogWarning("[Carry-On] Cloud rows parsed, but no valid suitcases.");
            yield break;
        }

        // pick up to number of buttons at random from cloud
        cloudPicks.Clear();
        System.Random rng = new System.Random();
        HashSet<int> used = new HashSet<int>();

        int count = Mathf.Min(suitcaseButtons.Length, allCloudSuitcases.Count);
        for (int i = 0; i < count; i++) {
            int idx;
            int guard = 0;
            do {
                idx = rng.Next(0, allCloudSuitcases.Count);
            } while (used.Contains(idx) && guard++ < 1000);

            used.Add(idx);
            cloudPicks.Add(allCloudSuitcases[idx]);
        }

        // wire buttons to show cloud suitcases
        for (int i = 0; i < suitcaseButtons.Length; i++) {
            var btnComp = suitcaseButtons[i];
            if (btnComp == null || btnComp.button == null) continue;

            btnComp.button.onClick.RemoveAllListeners();

            if (i < cloudPicks.Count) {
                var data = cloudPicks[i]; // capture for closure
                btnComp.button.onClick.AddListener(() => ShowSuitcase(data));
            } else {
                // if fewer cloud picks than buttons, gray out or show empty
                btnComp.button.onClick.AddListener(() => ShowSuitcase(null));
            }
        }

        onSuccess?.Invoke();
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // Existing local-archive path (unchanged logic)
    void PopulateButtonsFromLocalArchive() {
        int total = SuitcaseArchive.Instance.GetTotalSuitcases();
        if (total == 0) {
            if (suitcaseButtons.Length > 0 && suitcaseButtons[0].button != null)
                suitcaseButtons[0].button.onClick.AddListener(() => ShowSuitcase(null));
            return;
        }

        System.Random rng = new System.Random();
        HashSet<int> usedIndices = new HashSet<int>();

        foreach (var btn in suitcaseButtons) {
            if (btn == null || btn.button == null) continue;

            int index;
            int guard = 0;
            do {
                index = rng.Next(0, total);
            } while (usedIndices.Contains(index) && usedIndices.Count < total && guard++ < 1000);

            usedIndices.Add(index);

            btn.button.onClick.RemoveAllListeners();
            // fetch from your archive on click (same as before)
            btn.button.onClick.AddListener(() => {
                var localSuitcase = SuitcaseArchive.Instance.GetSuitcase(index);
                ShowSuitcase(localSuitcase);
            });
        }
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // Convert Supabase payload ¡ú your PackedItem list
    List<PackedItem> ConvertToPacked(SuitcaseDTO dto) {
        List<PackedItem> outList = new();
        if (dto?.items == null) return outList;

        foreach (var it in dto.items) {
            if (string.IsNullOrEmpty(it.item_id)) continue;

            // resolve ItemDef & default sprite via our catalog
            ItemCatalogEntry entry;
            if (!catalogLookup.TryGetValue(it.item_id, out entry) || entry.def == null) {
                // unknown id: skip to keep UI clean
                continue;
            }

            // reconstruct using YOUR exact layout rules:
            // - anchoredPosition from x,y
            // - sizeDelta from w,h
            // (rotation not used in your current code; add if you later include it)
            var packed = new PackedItem {
                itemDef = entry.def,
                localposition = new Vector2(it.x, it.y),
                sizeDelta = new Vector2(it.w, it.h),
                sprite = entry.sprite,    // we can't send Sprite from cloud, so use catalog default
                note = it.note            // carry per-item note if present
            };
            outList.Add(packed);
        }
        return outList;
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // Your original display logic ¡ª with two tiny additions:
    //  (a) if sprite missing, try to resolve from catalog (safety)
    //  (b) apply downloaded note text onto itemData for tooltip/labels
    public void ShowSuitcase(List<PackedItem> suitcase) {
        if (panelRoot != null) panelRoot.SetActive(true);
        foregroundPlant.SetActive(false);

        foreach (Transform child in displayArea) Destroy(child.gameObject);
        if (suitcase == null || suitcase.Count == 0) return;

        foreach (var packed in suitcase) {
            GameObject item = Instantiate(itemPrefab, displayArea);

            var rt = item.GetComponent<RectTransform>();
            rt.anchoredPosition = (Vector2)packed.localposition; // keep your UI positioning
            rt.sizeDelta = packed.sizeDelta;
            item.transform.SetAsLastSibling();

            var img = item.GetComponent<Image>();
            if (img) {
                if (packed.sprite != null) img.sprite = packed.sprite;
                else {
                    // safety: try catalog if sprite missing
                    if (packed.itemDef != null && catalogLookup.TryGetValue(packed.itemDef.ItemID, out var entry) && entry.sprite != null)
                        img.sprite = entry.sprite;
                }
            }

            var cg = item.GetComponent<CanvasGroup>();
            if (cg) { cg.blocksRaycasts = true; cg.interactable = false; }

            var di = item.GetComponent<DraggableItem>();
            if (di) {
                di.itemData = packed.itemDef;
                di.allowDragging = false;

                // NEW: overlay note text from cloud (non-destructive)
                if (!string.IsNullOrWhiteSpace(packed.note) && di.itemData != null) {
                    di.itemData.noteA = packed.note; // used by your tooltip/show code
                }
            }
        }
    }

    public void HideDisplayPanel() {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        foregroundPlant.SetActive(true);
    }
}
