using UnityEngine;
[CreateAssetMenu(menuName = "Carry-On/Item Definition")]
public class ItemDef : ScriptableObject {
    public string itemName;
    [TextArea] public string description;
    [TextArea] public string noteA;
    [TextArea] public string noteB;
    [TextArea] public string noteC;
}