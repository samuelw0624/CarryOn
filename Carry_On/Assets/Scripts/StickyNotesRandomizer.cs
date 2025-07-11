using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StickyNoteRandomizer : MonoBehaviour {
    [Header("Note UI References")]
    public Image backgroundImage;
    public Image inputFieldImage;
    public TMP_InputField inputField;
    public TextMeshProUGUI inputText;

    [HideInInspector] public Sprite[] noteSprites;
    [HideInInspector] public TMP_FontAsset[] fontOptions;

    void Start() {
        // Random sprite
        if (noteSprites.Length > 0) {
            var sprite = noteSprites[Random.Range(0, noteSprites.Length)];
            if (backgroundImage != null) backgroundImage.sprite = sprite;
            if (inputFieldImage != null) inputFieldImage.sprite = sprite;
        }

        // Random font
        if (fontOptions.Length > 0) {
            var font = fontOptions[Random.Range(0, fontOptions.Length)];
            if (inputText != null) inputText.font = font;
            if (inputField != null) inputField.textComponent.font = font;
        }
    }
}
