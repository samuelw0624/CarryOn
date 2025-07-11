using UnityEngine;
using TMPro;

public class TooltipController : MonoBehaviour {
    [Header("Text References")]
    public TMP_Text descriptionText;
    public TMP_Text noteAText;
    public TMP_Text noteBText;
    public TMP_Text noteCText;

    [Header("Control")]
    public CanvasGroup canvasGroup;
    public Vector2 offset = new Vector2(50, -50);
    public float padding = 30f;
    public float zoomScale = 1.5f;

    Camera cam;
    RectTransform rt;
    Vector3 originalScale;
    RectTransform canvasRect;

    void Awake() {
        cam = Camera.main;
        rt = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        originalScale = transform.localScale;
        HideTooltip();
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
        Vector2 offsetPos = screenMouse + offset;

        // Clamp position based on tooltip size
        Vector2 clampedScreenPos = ClampToScreen(offsetPos);

        // Convert to world position using canvas RectTransform
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect, clampedScreenPos, cam, out Vector3 world)) {

            rt.position = world;
        }

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        rt.localScale = shiftHeld ? originalScale * zoomScale : originalScale;
    }

    Vector2 ClampToScreen(Vector2 pos) {
        float canvasWidth = Screen.width;
        float canvasHeight = Screen.height;

        float width = rt.rect.width * rt.lossyScale.x;
        float height = rt.rect.height * rt.lossyScale.y;

        float x = pos.x;
        float y = pos.y;

        // Flip left if overflowing right
        if (pos.x + width + padding > canvasWidth) {
            x = pos.x - width - offset.x * 2f;  // flip to left
        }

        // Flip up if overflowing bottom
        if (pos.y - height - padding < 0f) {
            y = pos.y + height + offset.y * 2f; // flip upward
        }

        // Still clamp within bounds
        x = Mathf.Clamp(x, padding, canvasWidth - width - padding);
        y = Mathf.Clamp(y, padding + height, canvasHeight - padding);

        return new Vector2(x, y);
    }
}
