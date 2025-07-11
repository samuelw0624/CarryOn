using UnityEngine;

public class NoteEditSpawner : MonoBehaviour {
    public Transform suitcaseRoot; // The UI parent to spawn into
    public GameObject itemPrefab;  // Prefab with DraggableItem and visuals

    void Start() {
        foreach (PackedItem packed in SuitcaseData.Instance.packedItems) {
            GameObject go = Instantiate(itemPrefab, suitcaseRoot);
            RectTransform rt = go.GetComponent<RectTransform>();

            // Apply local position relative to suitcase
            rt.anchoredPosition = packed.localposition;

            // Assign item data
            var di = go.GetComponent<DraggableItem>();
            di.itemData = packed.itemDef;

            // Set sprite and native size
            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img != null && packed.sprite != null) {
                img.sprite = packed.sprite;
                img.SetNativeSize(); //This ensures correct aspect and scale
            }
        }
    }
}