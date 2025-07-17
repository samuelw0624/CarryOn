using UnityEngine;

public class ConveyorLoop : MonoBehaviour {
    public Transform belt1;
    public Transform belt2;
    public float speed = 2f;

    private float beltWidth;

    void Start() {
        if (belt1 == null || belt2 == null) {
            Debug.LogError("Both belt1 and belt2 must be assigned.");
            enabled = false;
            return;
        }

        // Calculate width from the SpriteRenderer of one belt
        SpriteRenderer sr = belt1.GetComponent<SpriteRenderer>();
        if (sr != null) {
            beltWidth = sr.bounds.size.x;
        } else {
            Debug.LogError("Missing SpriteRenderer on belt1.");
            enabled = false;
        }
    }

    void Update() {
        // Move both belts to the right
        belt1.Translate(Vector3.right * speed * Time.deltaTime);
        belt2.Translate(Vector3.right * speed * Time.deltaTime);

        // Reposition belt when it moves fully off-screen
        if (belt1.position.x - beltWidth / 2 > Camera.main.transform.position.x + Camera.main.orthographicSize * Camera.main.aspect) {
            belt1.position = new Vector3(belt2.position.x - beltWidth, belt1.position.y, belt1.position.z);
        }
        if (belt2.position.x - beltWidth / 2 > Camera.main.transform.position.x + Camera.main.orthographicSize * Camera.main.aspect) {
            belt2.position = new Vector3(belt1.position.x - beltWidth, belt2.position.y, belt2.position.z);
        }
    }
}
