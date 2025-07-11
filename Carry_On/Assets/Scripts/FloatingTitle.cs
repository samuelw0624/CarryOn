using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingTitle : MonoBehaviour {
    public float floatSpeed = 1f; // Speed of the floating effect
    public float floatAmount = 10f; // How far it moves up and down

    private Vector3 startPos;

    private void Start() {
        startPos = transform.position;
        StartCoroutine(FloatEffect());
    }

    private IEnumerator FloatEffect() {
        while (true) {
            yield return MoveTitle(startPos.y, startPos.y + floatAmount, floatSpeed);
            yield return MoveTitle(startPos.y + floatAmount, startPos.y, floatSpeed);
        }
    }

    private IEnumerator MoveTitle(float startY, float endY, float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float newY = Mathf.Lerp(startY, endY, elapsed / duration);
            transform.position = new Vector3(startPos.x, newY, startPos.z);
            yield return null;
        }
    }
}
