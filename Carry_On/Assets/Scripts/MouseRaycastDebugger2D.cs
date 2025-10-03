using UnityEngine;
using UnityEngine.EventSystems;

public class MouseRaycastDebugger2D : MonoBehaviour {
    [SerializeField] private LayerMask layerMask = ~0; // default: everything
    [SerializeField] private bool ignoreUI = true;

    void Update() {
        if (ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) {
            Debug.Log("UI under mouse");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, layerMask);

        if (hit.collider != null) {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");
        } else {
            Debug.Log("Hit: (none)");
        }
    }
}
