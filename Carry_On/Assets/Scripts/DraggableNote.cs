using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class DraggableNote : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private RectTransform rt;
    private CanvasGroup cg;

    [Header("Note Components")]
    public TMP_InputField inputField;

    private Vector3 originalScale;
    private Vector3 originalPosition;

    void Awake() {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        if (inputField == null)
            inputField = GetComponentInChildren<TMP_InputField>();

        originalScale = rt.localScale;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (inputField != null && inputField.isFocused) return;

        originalPosition = rt.anchoredPosition;     // Store local position on canvas
        cg.blocksRaycasts = false;                 // Allow drop targets to receive events
        transform.SetAsLastSibling();              // Bring to front
        rt.localScale = originalScale * 0.25f;     // Shrink to 25%
    }

    public void OnDrag(PointerEventData eventData) {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector3 worldPos)) {
            rt.position = worldPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        cg.blocksRaycasts = true;

        bool attachedSuccessfully = false;

        if (eventData.pointerEnter != null) {
            var item = eventData.pointerEnter.GetComponent<DraggableItem>()
                       ?? eventData.pointerEnter.GetComponentInParent<DraggableItem>();

            if (item != null && item.TryAttachNote(this)) {
                // Note successfully attached ¡ú hide and disable
                this.enabled = false;
                attachedSuccessfully = true;
            }
        }

        if (!attachedSuccessfully) {
            // Restore scale and return to spawn position
            rt.localScale = originalScale;
            rt.anchoredPosition = originalPosition;
        }
    }
}
