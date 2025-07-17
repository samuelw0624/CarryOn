using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SuitcaseLoopManager : MonoBehaviour {
    public List<RectTransform> suitcaseRects;  // Right to left: 1 to 5
    public float speed = 100f; // UI moves in pixels/second
    public RectTransform viewport; // Typically your parent RectTransform
    private float suitcaseWidth;
    public float spacing = 40f; // adjust as needed

    void Start() {
        if (suitcaseRects == null || suitcaseRects.Count == 0 || viewport == null) {
            Debug.LogError("Assign all suitcase RectTransforms and the viewport RectTransform.");
            enabled = false;
            return;
        }

        // Assuming all suitcases have same width
        suitcaseWidth = suitcaseRects[0].rect.width;
    }

    void Update() {
        float moveX = speed * Time.deltaTime;

        foreach (RectTransform suitcase in suitcaseRects) {
            suitcase.anchoredPosition += new Vector2(moveX, 0f);
        }

        float viewportRightEdge = viewport.rect.xMax;

        foreach (RectTransform suitcase in suitcaseRects) {
            float suitcaseLeftEdge = suitcase.anchoredPosition.x - suitcaseWidth / 2f;

            if (suitcaseLeftEdge > viewportRightEdge) {
                // Find the current leftmost suitcase
                RectTransform leftmost = suitcaseRects.OrderBy(s => s.anchoredPosition.x).First();

                // Move this suitcase to the left of the leftmost one
                float newX = leftmost.anchoredPosition.x - suitcaseWidth - spacing;
                suitcase.anchoredPosition = new Vector2(newX, suitcase.anchoredPosition.y);
            }
        }
    }
}
