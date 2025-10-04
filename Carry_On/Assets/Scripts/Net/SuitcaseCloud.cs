using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable] public class SuitcaseItemDTO { public string item_id; public float x, y, w, h; public string note; }
[Serializable] public class SuitcaseDTO { public string player_id; public string version; public string title; public SuitcaseItemDTO[] items; }

public static class SuitcaseCloud {
    static SupabaseConfig _cfg;
    static string BaseUrl {
        get {
            if (_cfg == null) _cfg = Resources.Load<SupabaseConfig>("SupabaseConfig");
            if (_cfg == null || string.IsNullOrEmpty(_cfg.projectUrl)) {
                Debug.LogError("SupabaseConfig.asset not found in Resources or projectUrl empty.");
                return null;
            }
            return _cfg.projectUrl.TrimEnd('/') + "/rest/v1/suitcases";
        }
    }
    static string AnonKey {
        get {
            if (_cfg == null) _cfg = Resources.Load<SupabaseConfig>("SupabaseConfig");
            return _cfg?.anonKey;
        }
    }

    [Serializable] class PayloadWrapper { public SuitcaseDTO payload; }

    public static IEnumerator PostSuitcase(SuitcaseDTO dto, Action<bool, string> done) {
        var url = BaseUrl; if (url == null) { done(false, "No URL"); yield break; }
        var body = JsonUtility.ToJson(new PayloadWrapper { payload = dto });
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", AnonKey);
        req.SetRequestHeader("Authorization", "Bearer " + AnonKey);
        req.SetRequestHeader("Prefer", "return=minimal");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) done(false, req.error + " :: " + req.downloadHandler.text);
        else done(true, null);
    }

    public static IEnumerator GetLatest(int limit, Action<string> onJson) {
        var url = BaseUrl; if (url == null) { onJson?.Invoke(null); yield break; }
        var getUrl = url + $"?select=payload,created_at,id&order=created_at.desc&limit={limit}";
        var req = UnityWebRequest.Get(getUrl);
        req.SetRequestHeader("apikey", AnonKey);
        req.SetRequestHeader("Authorization", "Bearer " + AnonKey);
        req.SetRequestHeader("Accept", "application/json");
        yield return req.SendWebRequest();
        onJson?.Invoke(req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null);
    }
}
