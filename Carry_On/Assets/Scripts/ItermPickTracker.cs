using System.Collections.Generic;
using UnityEngine;

public static class ItemPickTracker {
    private const string SaveKey = "ItemPickCounts";
    private static Dictionary<string, int> pickCounts = new();

    static ItemPickTracker() {
        Load();
    }

    public static void RegisterPick(ItemDef item) {
        if (item == null) return;

        string id = item.ItemID;
        if (!pickCounts.ContainsKey(id))
            pickCounts[id] = 0;

        pickCounts[id]++;
        Save();
    }

    public static int GetPickCount(ItemDef item) {
        return item != null && pickCounts.TryGetValue(item.ItemID, out int count) ? count : 0;
    }

    private static void Save() {
        string json = JsonUtility.ToJson(new Wrapper { entries = pickCounts });
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private static void Load() {
        pickCounts.Clear();
        if (!PlayerPrefs.HasKey(SaveKey)) return;

        string json = PlayerPrefs.GetString(SaveKey);
        Wrapper data = JsonUtility.FromJson<Wrapper>(json);
        if (data?.entries != null)
            pickCounts = data.entries;
    }

    [System.Serializable]
    private class Wrapper {
        public Dictionary<string, int> entries = new();
    }
}
