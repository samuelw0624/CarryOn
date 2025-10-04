using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CloudUploadAfterNotes : MonoBehaviour {
    [Header("Scene Refs")]
    public Transform suitcaseRoot;        // where NoteEditItemSpawner instantiates items
    public Button shareOrContinueButton;  // the button player clicks to finish
    public string nextSceneName = "CarouselScene";
    public string galleryTitle = "Carry-On";

    const string PlayerIdKey = "PLAYER_ID";

    void Start() {
        if (shareOrContinueButton != null)
            shareOrContinueButton.onClick.AddListener(() => StartCoroutine(UploadThenContinue()));
    }

    string EnsurePlayerId() {
        if (!PlayerPrefs.HasKey(PlayerIdKey))
            PlayerPrefs.SetString(PlayerIdKey, Guid.NewGuid().ToString());
        return PlayerPrefs.GetString(PlayerIdKey);
    }

    IEnumerator UploadThenContinue() {
        if (suitcaseRoot == null) yield break;

        // Build payload from the actually displayed, note-edited items
        List<SuitcaseItemDTO> items = new();
        foreach (Transform child in suitcaseRoot) {
            var di = child.GetComponent<DraggableItem>();
            var rt = child.GetComponent<RectTransform>();
            if (di == null || rt == null || di.itemData == null) continue;

            items.Add(new SuitcaseItemDTO {
                item_id = di.itemData.ItemID,
                x = rt.anchoredPosition.x,
                y = rt.anchoredPosition.y,
                w = rt.sizeDelta.x,
                h = rt.sizeDelta.y,
                note = di.GetCurrentNoteText()  // captures sticky-note text
            });
        }

        var dto = new SuitcaseDTO {
            player_id = EnsurePlayerId(),
            version = Application.version,
            title = galleryTitle,
            items = items.ToArray()
        };

        bool ok = false; string err = null;
        shareOrContinueButton.interactable = false;

        yield return SuitcaseCloud.PostSuitcase(dto, (success, msg) => { ok = success; err = msg; });

        if (!ok) {
            Debug.LogError("[Carry-On] Cloud upload failed: " + err);
            shareOrContinueButton.interactable = true;
            yield break;
        }

        // proceed only after a successful upload
        SceneLoader.Instance.LoadScene(nextSceneName);
    }
}
