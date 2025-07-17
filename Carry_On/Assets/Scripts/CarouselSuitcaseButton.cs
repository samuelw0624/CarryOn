using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CarouselSuitcaseButton : MonoBehaviour {
    public Button button;
    private int suitcaseIndex;
    private CarouselSceneManager manager;

    public void Initialize(int index, CarouselSceneManager mgr) {
        suitcaseIndex = index;
        manager = mgr;
    }

    void Start() {
        button.onClick.AddListener(() => {
            if (suitcaseIndex >= 0 && SuitcaseArchive.Instance != null) {
                var suitcaseData = SuitcaseArchive.Instance.GetSuitcase(suitcaseIndex);
                manager.ShowSuitcase(suitcaseData);
            }
        });
    }
}
