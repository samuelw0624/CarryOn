using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [Header("Groups (drag from hierarchy)")]
    [SerializeField] CanvasGroup titleGroup;   // fades out
    [SerializeField] CanvasGroup letterGroup;  // slides up (alpha already 1)

    [Header("UI Controls")]
    [SerializeField] Button startButton;
    [SerializeField] Button continueButton;

    [Header("Timings")]
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] float slideDuration = 0.4f;

    RectTransform letterRect;
    Vector2 startPos;
    Vector2 targetPos;
    float offsetValue = 150f;

    void Awake() {
        // Cache rect refs & positions
        letterRect = letterGroup.GetComponent<RectTransform>();
        targetPos = letterRect.anchoredPosition + new Vector2(0, offsetValue); // expected (0,0)
        startPos = letterRect.anchoredPosition + new Vector2(0, -offsetValue) + Vector2.down * Screen.height * 0.5f;
        letterRect.anchoredPosition = startPos;

        // UI wiring
        startButton.onClick.AddListener(() => StartCoroutine(PlayIntro()));
        continueButton.onClick.AddListener(
            () => SceneLoader.Instance.LoadScene("PackingScene"));

        //continueButton.interactable = false;   // unlock after slide
    }
    IEnumerator PlayIntro() {
        // Disable the button so it can't be spam-clicked
        startButton.interactable = false;

        /* 1 Fade out title group */
        float t = 0, invFade = 1f / fadeDuration;
        while (t < 1) {
            t += Time.unscaledDeltaTime * invFade;
            titleGroup.alpha = 1 - t;
            yield return null;
        }
        titleGroup.alpha = 0;
        titleGroup.interactable = titleGroup.blocksRaycasts = false;

        /* 2 Slide letter up */
        t = 0;
        float invSlide = 1f / slideDuration;
        while (t < 1) {
            t += Time.unscaledDeltaTime * invSlide;
            letterRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        letterRect.anchoredPosition = targetPos;
    }
}
