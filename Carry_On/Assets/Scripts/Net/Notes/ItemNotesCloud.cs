// Assets/Scripts/Cloud/ItemNotesCloud.cs
using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class ItemNotesCloud {
    [Serializable]
    struct Row {
        public string item_id;
        public int slot;       // 0..2
        public string note;
        public string editor;  // optional
    }
    [Serializable]
    struct RowWithTime {
        public string item_id;
        public int slot;
        public string note;
        public string updated_at; // iso string
    }
    [Serializable] class Wrap<T> { public T[] items; }

    static SupabaseConfig Cfg {
        get {
            var c = Resources.Load<SupabaseConfig>("SupabaseConfig");
            if (c == null) Debug.LogError("[ItemNotesCloud] SupabaseConfig.asset missing in Resources.");
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

    static IEnumerator FetchThreeSlots(string itemId, Action<RowWithTime[]> done) {
        var url = BaseUrl + $"?select=item_id,slot,note,updated_at&item_id=eq.{UnityWebRequest.EscapeURL(itemId)}";
        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("apikey", Key);
        req.SetRequestHeader("Authorization", "Bearer " + Key);
        req.SetRequestHeader("Accept", "application/json");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) {
            Debug.LogError("[ItemNotesCloud] fetch slots failed: " + req.error + " :: " + req.downloadHandler.text);
            done(Array.Empty<RowWithTime>());
        } else {
            done(ParseArray<RowWithTime>(req.downloadHandler.text));
        }
    }

    static IEnumerator UpsertToSlot(string itemId, int slot, string note, string editor, Action<bool, string> done) {
        var url = BaseUrl + "?on_conflict=item_id,slot";

        // Normalize editor GUID; omit if invalid/empty
        string editorField = null;
        if (!string.IsNullOrWhiteSpace(editor) && Guid.TryParse(editor, out var g))
            editorField = g.ToString();

        // Build JSON manually so we can omit editor when null/invalid
        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        sb.AppendFormat("\"item_id\":\"{0}\",", EscapeJson(itemId));
        sb.AppendFormat("\"slot\":{0},", slot);
        sb.AppendFormat("\"note\":\"{0}\"", EscapeJson(note ?? string.Empty));
        if (editorField != null) sb.AppendFormat(",\"editor\":\"{0}\"", editorField);
        sb.Append("}");
        var json = sb.ToString();

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", Key);
        req.SetRequestHeader("Authorization", "Bearer " + Key);
        req.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            done?.Invoke(false, req.error + " :: " + req.downloadHandler.text);
        else
            done?.Invoke(true, null);
    }

    // Tiny JSON string escaper (handles quotes and backslashes)
    static string EscapeJson(string s) =>
        (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");


    /// <summary>
    /// Chooses a slot for newNote: prefer first empty (0..2) from local snapshot.
    /// If full, fetch timestamps and overwrite oldest slot. Calls back with chosen slot.
    /// </summary>
    public static IEnumerator UpsertChoosingSlot(
        string itemId,
        string newNote,
        string editorGuid,
        bool localAEmpty, bool localBEmpty, bool localCEmpty,
        Action<int, bool, string> onDone // (chosenSlot, ok, err)
    ) {
        if (string.IsNullOrWhiteSpace(itemId)) { onDone?.Invoke(-1, false, "Missing itemId"); yield break; }
        if (string.IsNullOrWhiteSpace(newNote)) { onDone?.Invoke(-1, false, "Empty note"); yield break; }

        // 1) Prefer first empty slot from local state.
        if (localAEmpty || localBEmpty || localCEmpty) {
            int chosen = localAEmpty ? 0 : (localBEmpty ? 1 : 2);
            bool ok = false; string err = null;
            yield return UpsertToSlot(itemId, chosen, newNote, editorGuid, (s, e) => { ok = s; err = e; });
            onDone?.Invoke(chosen, ok, err);
            yield break;
        }

        // 2) All full: fetch server timestamps and overwrite oldest
        RowWithTime[] rows = null;
        yield return FetchThreeSlots(itemId, r => rows = r);
        // Initialize default dates far-future
        var oldestSlot = 0;
        DateTime oldestTime = DateTime.MaxValue;

        // If server has rows, pick oldest updated_at; otherwise fall back to slot 0.
        if (rows != null && rows.Length > 0) {
            foreach (var r in rows) {
                DateTime t;
                if (!DateTime.TryParse(r.updated_at, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out t)) {
                    t = DateTime.UtcNow; // if parse fails, treat as "now"
                }
                if (t < oldestTime) { oldestTime = t; oldestSlot = r.slot; }
            }
        } else {
            oldestSlot = 0;
        }

        bool ok2 = false; string err2 = null;
        yield return UpsertToSlot(itemId, oldestSlot, newNote, editorGuid, (s, e) => { ok2 = s; err2 = e; });
        onDone?.Invoke(oldestSlot, ok2, err2);
    }
}
