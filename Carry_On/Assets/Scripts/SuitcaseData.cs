using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PackedItem {
    public ItemDef itemDef;
    public Vector3 localposition; // world or local depending on setup
    public Sprite sprite;

    public Vector2 sizeDelta;
}

public class SuitcaseData : MonoBehaviour {
    public static SuitcaseData Instance;
    public List<PackedItem> packedItems = new();

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
}