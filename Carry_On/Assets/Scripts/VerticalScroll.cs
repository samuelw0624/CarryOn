using UnityEngine;

public class VerticalScroll : MonoBehaviour {
    public Transform bg1;
    public Transform bg2;
    public float scrollSpeed = 1f;

    private float bgHeight;

    void Start() {
        // Assumes both backgrounds are the same size
        bgHeight = Mathf.Abs(bg1.position.y - bg2.position.y);
    }

    void Update() {
        // Move both backgrounds downward
        Vector3 move = Vector3.down * scrollSpeed * Time.deltaTime;
        bg1.position += move;
        bg2.position += move;

        // Check if bg1 is below the screen
        if (bg1.position.y <= -bgHeight) {
            bg1.position = new Vector3(bg1.position.x, bg2.position.y + bgHeight, bg1.position.z);
            SwapBackgrounds();
        }

        // Check if bg2 is below the screen
        if (bg2.position.y <= -bgHeight) {
            bg2.position = new Vector3(bg2.position.x, bg1.position.y + bgHeight, bg2.position.z);
            SwapBackgrounds();
        }
    }

    // Keep bg1 always above bg2 logically
    void SwapBackgrounds() {
        var temp = bg1;
        bg1 = bg2;
        bg2 = temp;
    }
}
