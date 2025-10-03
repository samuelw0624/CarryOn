using UnityEngine;
using UnityEngine.EventSystems;

public class HoverProbe : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public void OnPointerEnter(PointerEventData e) => Debug.Log($"ENTER {name}");
    public void OnPointerExit(PointerEventData e) => Debug.Log($"EXIT  {name}");
}