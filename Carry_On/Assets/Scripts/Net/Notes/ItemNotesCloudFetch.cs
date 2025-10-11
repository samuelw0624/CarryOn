// Assets/Scripts/Cloud/ItemNotesCloudFetch.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class ItemNotesCloudFetch {
    [Serializable]
    public struct Row {
        public string item_id;
        public int slot;           // 0..2
        public string note;
        public string updated_at;  // optional for debugging/sorting
    }
    [Serializable] class Wrap<T> { public T[] items; }

    static SupabaseConfig Cfg {
        get {
            var c = Resources.Load<SupabaseConfig>("SupabaseConfig");
            if (c == null) Debug.LogError("[ItemNotesCloudFetch] SupabaseConfig.asset missing in Resources.");
            return c;
        }
    }
    static string BaseUrl => string.IsNullOrEmpty(Cfg?.projectUrl) ? null : Cfg.projectUrl.TrimEnd('/') + "/rest/v1/item_notes";
    static string Key => Cfg?.anonKey;

    static T[] ParseArray<T>(string json) {
        var wrapped = "{\"items\":" + (string.IsNullOrEmpty(json) ? "[]" : json) + "}";
        var w = JsonUtility.FromJson<Wrap<T>>(wrapped);
        return w != null && w.items != null ? w.items : Array.Empty<T>();
    }

    /// <summary>
    /// Returns: map[item_id] => string[3] with notes for slots A/B/C (missing slots = empty).
    /// </summary>
    public static IEnumerator GetNotesFor(string[] itemIds, Action<Dictionary<string, string[]>> done) {
        if (itemIds == null || itemIds.Length == 0 || string.IsNullOrEmpty(BaseUrl)) {
            done?.Invoke(new Dictionary<string, string[]>());
            yield break;
        }

        // Build in.( "id1","id2",... )
        var sb = new StringBuilder("?select=item_id,slot,note,updated_at&item_id=in.(");
        for (int i = 0; i < itemIds.Length; i++) {
            if (i > 0) sb.Append(',');
            var enc = Uri.EscapeDataString(itemIds[i]);
            sb.Append("%22").Append(enc).Append("%22");
        }
        sb.Append(')');

        var url = BaseUrl + sb.ToString();

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("apikey", Key);
        req.SetRequestHeader("Authorization", "Bearer " + Key);
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        var map = new Dictionary<string, string[]>();
        if (req.result == UnityWebRequest.Result.Success) {
            var rows = ParseArray<Row>(req.downloadHandler.text);
            foreach (var r in rows) {
                if (string.IsNullOrWhiteSpace(r.item_id)) continue;
                if (!map.TryGetValue(r.item_id, out var arr)) {
                    arr = new string[3] { "", "", "" };
                    map[r.item_id] = arr;
                }
                if (r.slot >= 0 && r.slot <= 2) arr[r.slot] = r.note ?? "";
            }
        } else {
            Debug.LogError("[ItemNotesCloudFetch] GET failed: " + req.error + " :: " + req.downloadHandler.text);
        }

        done?.Invoke(map);
    }
}
