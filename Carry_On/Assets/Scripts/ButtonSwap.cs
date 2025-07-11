using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSwap : MonoBehaviour {
    public Sprite pressedSprite;
    public Sprite normalSprite;
    public Image buttonImage;
    private RectTransform buttonTransform;

    public float pulseSpeed = 1f; // Adjust speed of pulsation
    public float scaleFactor = 1.1f; // How much it grows
    private void Start() {
        buttonTransform = GetComponent<RectTransform>();
        StartCoroutine(Pulsate());
    }
    public void OnPressDown() {
        // Change to the appropriate pressed sprite based on current state
        buttonImage.sprite = pressedSprite;
    }
    public void OnPressUp() {
        buttonImage.sprite = normalSprite;
    }
    private IEnumerator Pulsate() {
        while (true) {
            yield return ScaleButton(1f, scaleFactor, pulseSpeed);
            yield return ScaleButton(scaleFactor, 1f, pulseSpeed);
        }
    }
    private IEnumerator ScaleButton(float startScale, float endScale, float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(startScale, endScale, elapsed / duration);
            buttonTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }
}
