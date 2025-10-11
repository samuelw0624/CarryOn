// Assets/Scripts/Cloud/CanonicalNotesApplier.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CanonicalNotesApplier : MonoBehaviour {
    [Tooltip("Parent under which DraggableItem instances live (e.g., suitcaseRoot)")]
    public Transform itemsRoot;

    [Tooltip("Show sticky icon on items that have any canonical note")]
    public bool showStickyIconIfAnyNote = true;

    void Start() {
        if (!itemsRoot) { Debug.LogWarning("[CanonicalNotesApplier] itemsRoot not set."); return; }
        CoroutineRunner.Run(ApplyCanonicalNotes());
    }

    IEnumerator ApplyCanonicalNotes() {
        var items = itemsRoot.GetComponentsInChildren<DraggableItem>(includeInactive: true);
        var ids = items
            .Where(i => i && i.itemData && !string.IsNullOrWhiteSpace(i.itemData.ItemID))
            .Select(i => i.itemData.ItemID)
            .Distinct()
            .ToArray();

        Dictionary<string, string[]> map = null;
        yield return ItemNotesCloudFetch.GetNotesFor(ids, m => map = m);

        if (map == null || map.Count == 0) yield break;

        foreach (var it in items) {
            if (!it || it.itemData == null) continue;
            if (!map.TryGetValue(it.itemData.ItemID, out var notes)) continue;

            it.itemData.noteA = notes.Length > 0 ? notes[0] ?? "" : "";
            it.itemData.noteB = notes.Length > 1 ? notes[1] ?? "" : "";
            it.itemData.noteC = notes.Length > 2 ? notes[2] ?? "" : "";

            if (showStickyIconIfAnyNote && (HasText(it.itemData.noteA) || HasText(it.itemData.noteB) || HasText(it.itemData.noteC))) {
                var iconField = it.GetType().GetField("stickyNoteIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var icon = iconField?.GetValue(it) as GameObject;
                if (icon) icon.SetActive(true);
            }
        }
    }

    static bool HasText(string s) => !string.IsNullOrWhiteSpace(s);
}
