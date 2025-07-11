using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class DraggableNote : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private RectTransform rt;
    private CanvasGroup cg;

    [Header("Note Components")]
    public TMP_InputField inputField;

    void Awake() {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (inputField == null)
            inputField = GetComponentInChildren<TMP_InputField>();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (inputField != null && inputField.isFocused) return;
        cg.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData) {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector3 worldPos)) {
            rt.position = worldPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        cg.blocksRaycasts = true;

        if (eventData.pointerEnter != null) {
            var item = eventData.pointerEnter.GetComponent<DraggableItem>()
                       ?? eventData.pointerEnter.GetComponentInParent<DraggableItem>();

            if (item != null) {
                var receiver = item.GetComponent<NoteReceiver>();
                if (receiver != null && receiver.TryAttachNote(this)) {
                    this.enabled = false;
                }
            }
        }
    }
}
