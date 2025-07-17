using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;
    [SerializeField] CanvasGroup fader;
    [SerializeField] float fadeDuration = 0.5f;

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fader != null)
                DontDestroyOnLoad(fader.gameObject); // <- Important!
        } else {
            Destroy(gameObject);
            return;
        }

        if (fader != null) fader.alpha = 1;
        StartCoroutine(Fade(0));
    }

    public void LoadScene(string sceneName) => StartCoroutine(LoadRoutine(sceneName));

    IEnumerator LoadRoutine(string sceneName)
    {
        yield return Fade(1);
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return Fade(0);
    }

    IEnumerator Fade(float target)
    {
        if (fader == null) yield break;
        float start = fader.alpha, t = 0;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fader.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        fader.alpha = target;
    }
}
