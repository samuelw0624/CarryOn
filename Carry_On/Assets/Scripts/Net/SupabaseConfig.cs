using UnityEngine;

[CreateAssetMenu(menuName = "Carry-On/Supabase Config")]
public class SupabaseConfig : ScriptableObject {
    [Tooltip("Example: https://yourproj.supabase.co")]
    public string projectUrl;
    [TextArea]
    public string anonKey;
}
