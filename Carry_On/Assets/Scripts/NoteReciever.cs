using UnityEngine;

public class NoteReceiver : MonoBehaviour {
    public GameObject stickyNoteIcon; // Drag small icon here
    private bool hasNote = false;

    public bool TryAttachNote(DraggableNote note) {
        if (hasNote) return false;
        NoteManager.Instance?.RegisterNoteAttachment();

        string newNoteText = note.inputField.text.Trim();
        if (string.IsNullOrEmpty(newNoteText)) return false;

        ItemDef def = GetComponent<DraggableItem>().itemData;
        if (def != null) {
            if (string.IsNullOrEmpty(def.noteA)) def.noteA = newNoteText;
            else if (string.IsNullOrEmpty(def.noteB)) def.noteB = newNoteText;
            else def.noteC = newNoteText;
        }

        // Visually mark as annotated
        hasNote = true;
        if (stickyNoteIcon != null)
            stickyNoteIcon.SetActive(true);

        // Snap and shrink the note
        note.gameObject.SetActive(false);
        note.enabled = false; // optional safety

        // Lock editing
        note.inputField.interactable = false;

        return true;
    }
}
