using UnityEngine;
using UnityEngine.UI;

public class NoteManager : MonoBehaviour {
    public static NoteManager Instance;

    [Header("UI")]
    public GameObject readyToBoardButton; // Drag the button here

    private int notesAttached = 0;
    private int totalNotes = 5;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (readyToBoardButton != null)
            readyToBoardButton.SetActive(false); // hide at start
    }

    public void RegisterNoteAttachment() {
        notesAttached++;
        if (notesAttached >= totalNotes && readyToBoardButton != null) {
            readyToBoardButton.SetActive(true);
        }
    }
}