using System.Collections.Generic;
using UnityEngine;

public class SuitcaseArchive : MonoBehaviour {
    public static SuitcaseArchive Instance;

    private List<List<PackedItem>> savedSuitcases = new List<List<PackedItem>>();

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SaveCurrentSuitcase(List<PackedItem> packedItems) {
        if (packedItems == null || packedItems.Count == 0) return;

        // Deep copy the packedItems list
        List<PackedItem> newSuitcase = new List<PackedItem>();
        foreach (var item in packedItems) {
            newSuitcase.Add(new PackedItem {
                itemDef = item.itemDef,
                localposition = item.localposition,
                sizeDelta = item.sizeDelta,
                sprite = item.sprite,
                //rotationZ = item.rotationZ
            });
        }

        savedSuitcases.Add(newSuitcase);
    }

    public List<PackedItem> GetSuitcase(int index) {
        if (index >= 0 && index < savedSuitcases.Count)
            return savedSuitcases[index];
        return null;
    }

    public int GetTotalSuitcases() => savedSuitcases.Count;
}
