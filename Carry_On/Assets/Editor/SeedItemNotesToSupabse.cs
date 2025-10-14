#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class SeedItemNotesToSupabase {
    [MenuItem("Tools/Carry-On/Seed Item Notes to Supabase")]
    public static void RunSeeding() {
        var cfg = Resources.Load<SupabaseConfig>("SupabaseConfig");
        if (cfg == null || string.IsNullOrWhiteSpace(cfg.projectUrl) || string.IsNullOrWhiteSpace(cfg.anonKey)) {
            EditorUtility.DisplayDialog("Seed Item Notes",
                "SupabaseConfig.asset not found in Resources/ (or missing projectUrl/anonKey).",
                "OK");
            return;
        }

        // Find all ItemDef assets in the project
        var guids = AssetDatabase.FindAssets("t:ItemDef");
        if (guids == null || guids.Length == 0) {
            EditorUtility.DisplayDialog("Seed Item Notes", "No ItemDef assets found.", "OK");
            return;
        }

        // Load assets
        var items = new List<ItemDef>();
        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(path);
            if (def != null && !string.IsNullOrWhiteSpace(def.ItemID))
                items.Add(def);
        }

        if (items.Count == 0) {
            EditorUtility.DisplayDialog("Seed Item Notes", "Found ItemDef assets, but none had a valid ItemID.", "OK");
            return;
        }

        // Start editor coroutine
        EditorCoroutineUtility.StartCoroutineOwnerless(SeedCoroutine(cfg, items));
    }

    private static IEnumerator SeedCoroutine(SupabaseConfig cfg, List<ItemDef> defs) {
        string baseUrl = cfg.projectUrl.TrimEnd('/') + "/rest/v1/item_notes";
        string key = cfg.anonKey;

        int total = defs.Count;
        int uploaded = 0;
        int skippedDup = 0;
        int skippedNoEmpty = 0;
        int skippedEmptyLocal = 0;

        try {
            for (int i = 0; i < defs.Count; i++) {
                var def = defs[i];
                EditorUtility.DisplayProgressBar("Seeding Item Notes",
                    $"({i + 1}/{defs.Count}) {def.ItemID}", (float)(i + 1) / defs.Count);

                // Pull existing notes for this item_id
                var existing = new string[3] { "", "", "" };
                bool fetchDone = false;
                yield return EditorGetItemNotes(baseUrl, key, def.ItemID, arr => { existing = arr; fetchDone = true; });
                while (!fetchDone) yield return null;

                // Prepare local notes list
                var localNotes = new List<(int slot, string text)>();
                if (!string.IsNullOrWhiteSpace(def.noteA)) localNotes.Add((0, def.noteA.Trim()));
                if (!string.IsNullOrWhiteSpace(def.noteB)) localNotes.Add((1, def.noteB.Trim()));
                if (!string.IsNullOrWhiteSpace(def.noteC)) localNotes.Add((2, def.noteC.Trim()));

                if (localNotes.Count == 0) {
                    skippedEmptyLocal++;
                    continue;
                }

                // For each local note: if exact text already present in any slot ¡ú skip
                // Else: insert into the first empty slot; if none empty ¡ú skip (we never overwrite here)
                foreach (var (_, text) in localNotes) {
                    if (string.IsNullOrWhiteSpace(text)) { skippedEmptyLocal++; continue; }

                    bool dup =
                        string.Equals(existing[0] ?? "", text, StringComparison.Ordinal) ||
                        string.Equals(existing[1] ?? "", text, StringComparison.Ordinal) ||
                        string.Equals(existing[2] ?? "", text, StringComparison.Ordinal);

                    if (dup) { skippedDup++; continue; }

                    int emptySlot = Array.FindIndex(existing, s => string.IsNullOrWhiteSpace(s));
                    if (emptySlot < 0) {
                        // no empty slot ¡ú leave cloud as-is (do not overwrite during seed)
                        skippedNoEmpty++;
                        continue;
                    }

                    bool ok = false; string err = null;
                    yield return EditorUpsertToSlot(baseUrl, key, def.ItemID, emptySlot, text, null,
                        (success, msg) => { ok = success; err = msg; });

                    if (ok) {
                        uploaded++;
                        existing[emptySlot] = text; // update local snapshot to avoid duping next local note
                    } else {
                        Debug.LogError($"[Seed] Upsert failed for {def.ItemID} slot {emptySlot}: {err}");
                    }

                    // tiny yield to keep the editor responsive
                    yield return null;
                }
            }
        }
        finally {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("Seed Item Notes ¡ª Done",
            $"Items: {total}\n" +
            $"Inserted: {uploaded}\n" +
            $"Skipped (duplicate text): {skippedDup}\n" +
            $"Skipped (no empty slot): {skippedNoEmpty}\n" +
            $"Skipped (no local notes): {skippedEmptyLocal}",
            "OK");
    }

    // ===== HTTP helpers (Editor-safe coroutines) =====

    private static IEnumerator EditorGetItemNotes(string baseUrl, string key, string itemId, Action<string[]> done) {
        // GET /item_notes?select=item_id,slot,note&item_id=eq.<id>&t=cache_bust
        string url = baseUrl +
            $"?select=item_id,slot,note&item_id=eq.{UnityWebRequest.EscapeURL(itemId)}&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        using (var req = UnityWebRequest.Get(url)) {
            req.SetRequestHeader("apikey", key);
            req.SetRequestHeader("Authorization", "Bearer " + key);
            req.SetRequestHeader("Accept", "application/json");

            var op = req.SendWebRequest();
            while (!op.isDone) yield return null;

            var result = new string[3] { "", "", "" };
            if (req.result == UnityWebRequest.Result.Success) {
                // Wrap array to parse with JsonUtility
                var wrapped = "{\"items\":" + req.downloadHandler.text + "}";
                var arr = JsonUtility.FromJson<RowWrap>(wrapped);
                if (arr != null && arr.items != null) {
                    foreach (var r in arr.items) {
                        if (r.slot >= 0 && r.slot <= 2)
                            result[r.slot] = r.note ?? "";
                    }
                }
            } else {
                Debug.LogError("[Seed] GET failed: " + req.error + " :: " + req.downloadHandler.text);
            }
            done?.Invoke(result);
        }
    }

    private static IEnumerator EditorUpsertToSlot(string baseUrl, string key, string itemId, int slot, string note, string editorGuid, Action<bool, string> done) {
        // POST with on_conflict=item_id,slot
        string url = baseUrl + "?on_conflict=item_id,slot";

        // Omit editor if invalid
        string editorField = null;
        if (!string.IsNullOrWhiteSpace(editorGuid) && Guid.TryParse(editorGuid, out var g))
            editorField = g.ToString();

        var sb = new StringBuilder();
        sb.Append("{");
        sb.AppendFormat("\"item_id\":\"{0}\",", EscapeJson(itemId));
        sb.AppendFormat("\"slot\":{0},", slot);
        sb.AppendFormat("\"note\":\"{0}\"", EscapeJson(note ?? string.Empty));
        if (editorField != null) sb.AppendFormat(",\"editor\":\"{0}\"", editorField);
        sb.Append("}");
        var json = sb.ToString();

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)) {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", key);
            req.SetRequestHeader("Authorization", "Bearer " + key);
            req.SetRequestHeader("Prefer", "resolution=ignore-duplicates,return=minimal");

            var op = req.SendWebRequest();
            while (!op.isDone) yield return null;

            if (req.result != UnityWebRequest.Result.Success)
                done?.Invoke(false, req.error + " :: " + req.downloadHandler.text);
            else
                done?.Invoke(true, null);
        }
    }

    // ===== JSON helpers =====

    [Serializable] private class Row { public string item_id; public int slot; public string note; }
    [Serializable] private class RowWrap { public Row[] items; }

    private static string EscapeJson(string s) =>
        (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
}
#endif
