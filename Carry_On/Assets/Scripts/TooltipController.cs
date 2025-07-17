using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipController : MonoBehaviour {
    [Header("Text References")]
    public TMP_Text descriptionText;
    public TMP_Text noteAText;
    public TMP_Text noteBText;
    public TMP_Text noteCText;

    [Header("Layout Containers")]
    public RectTransform contentContainer;
    public RectTransform descriptionContainer;
    public RectTransform stickyNotesContainer;
    public HorizontalLayoutGroup layoutGroup;

    [Header("Control")]
    public CanvasGroup canvasGroup;
    public Vector2 offset = new Vector2(50, -50);
    public float zoomScale = 1.5f;

    private Camera cam;
    private RectTransform rt;
    private Vector3 originalScale;
    private RectTransform canvasRect;

    void Awake() {
        cam = Camera.main;
        rt = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        originalScale = transform.localScale;
        HideTooltip();
        DisableTooltipRaycasts(); 
    }

    public void ShowTooltip(ItemDef item) {
        descriptionText.text = item.description;
        noteAText.text = item.noteA;
        noteBText.text = item.noteB;
        noteCText.text = item.noteC;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rt.SetAsLastSibling();
    }

    public void HideTooltip() {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    void Update() {
        if (canvasGroup.alpha <= 0f) return;

        Vector2 screenMouse = Input.mousePosition;

        // Determine new pivot based on screen position
        Vector2 newPivot;
        newPivot.x = (screenMouse.x > Screen.width * 0.6f) ? 1f : 0f;
        newPivot.y = (screenMouse.y < Screen.height * 0.4f) ? 0f : 1f;

        // Always run layout flip logic based on newPivot.x
        if (newPivot.x == 1f) {
            // Right side ¡ª notes on left
            descriptionContainer.SetSiblingIndex(0);
            stickyNotesContainer.SetSiblingIndex(1);
            layoutGroup.childAlignment = TextAnchor.UpperRight;
            //Debug.Log("Mouse on RIGHT ¡ú Description on LEFT, Sticky notes on RIGHT");
        } else {
            // Left side ¡ª notes on right
            stickyNotesContainer.SetSiblingIndex(0);
            descriptionContainer.SetSiblingIndex(1);
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            //Debug.Log("Mouse on LEFT ¡ú Sticky notes on LEFT, Description on RIGHT");
        }

        // Only update pivot if it's different
        if (rt.pivot != newPivot) {
            rt.pivot = newPivot;
        }

        // Pivot-aware offset
        Vector2 adjustedOffset = new Vector2(
            (rt.pivot.x == 1f ? -offset.x : offset.x),
            (rt.pivot.y == 0f ? -offset.y : offset.y)
        );

        Vector2 offsetPos = screenMouse + adjustedOffset;

        // Convert to world space and clamp if needed
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, offsetPos, cam, out Vector3 worldPos)) {
            rt.position = worldPos;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector3 bottomLeft = cam.WorldToViewportPoint(corners[0]);
            Vector3 topRight = cam.WorldToViewportPoint(corners[2]);

            Vector3 adjustment = Vector3.zero;

            if (topRight.y > 1f) adjustment.y = 1f - topRight.y;
            if (bottomLeft.y < 0f) adjustment.y = 0f - bottomLeft.y;
            if (topRight.x > 1f) adjustment.x = 1f - topRight.x;
            if (bottomLeft.x < 0f) adjustment.x = 0f - bottomLeft.x;

            if (adjustment != Vector3.zero) {
                Vector3 screenAdjustment = new Vector3(adjustment.x * Screen.width, adjustment.y * Screen.height, 0f);
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    canvasRect,
                    cam.WorldToScreenPoint(rt.position) + screenAdjustment,
                    cam,
                    out Vector3 clampedWorldPos
                );
                rt.position = clampedWorldPos;
            }
        }

        // Shift zoom effect
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        rt.localScale = shiftHeld ? originalScale * zoomScale : originalScale;
    }

    void DisableTooltipRaycasts() {
        foreach (var graphic in GetComponentsInChildren<Graphic>()) {
            graphic.raycastTarget = false;
        }
    }
}
