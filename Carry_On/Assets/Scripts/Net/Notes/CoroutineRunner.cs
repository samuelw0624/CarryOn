// Assets/Scripts/Cloud/CoroutineRunner.cs
using System.Collections;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour {
    static CoroutineRunner _instance;
    public static CoroutineRunner Instance {
        get {
            if (_instance == null) {
                var go = new GameObject("[CoroutineRunner]");
                go.hideFlags = HideFlags.HideAndDontSave;
                _instance = go.AddComponent<CoroutineRunner>();
                Object.DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    public static void Run(IEnumerator routine) => Instance.StartCoroutine(routine);
}
