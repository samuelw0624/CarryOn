using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]         // kinematic, gravity 0
public class DraggableItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler {
    /* ©¤©¤ cached refs ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    RectTransform rt;
    PolygonCollider2D poly;
    CanvasGroup cg;
    static SuitcaseArea suitcase;
    public ItemDef itemData;
    static TooltipController tooltip;

    /* ©¤©¤ zone tracking ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    Vector3 spawnDeskPos;                       // original desk spawn
    Vector3 lastDeskPos; bool hasDeskPos;     // last valid desk spot
    Vector3 lastCasePos; bool hasCasePos;     // last valid suitcase spot
    bool dragStartedInCase;

    static readonly List<PolygonCollider2D> inSuitcase = new();

    /* ©¤©¤ setup ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    void Awake() {
        rt = GetComponent<RectTransform>();
        poly = GetComponent<PolygonCollider2D>();
        cg = GetComponent<CanvasGroup>();

        if (suitcase == null)
            suitcase = FindObjectOfType<SuitcaseArea>();

        spawnDeskPos = rt.position;         // remember factory spot
        lastDeskPos = spawnDeskPos;        // initial desk pos
        hasDeskPos = true;
    }
    void Start() {
        if (tooltip == null)
            tooltip = FindObjectOfType<TooltipController>();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (tooltip && itemData != null)
            tooltip.ShowTooltip(itemData);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (tooltip)
            tooltip.HideTooltip();
    }


    /* ©¤©¤ begin drag ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    public void OnBeginDrag(PointerEventData e) {
        rt.SetAsLastSibling();              // always render on top
        inSuitcase.Remove(poly);            // will re-add if accepted
        if (cg) cg.blocksRaycasts = false;

        dragStartedInCase = FullyInsideSuitcase();
    }

    /* ©¤©¤ during drag ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    public void OnDrag(PointerEventData e) {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rt, e.position, e.pressEventCamera, out var world)) {
            rt.position = world;
        }
    }

    /* ©¤©¤ end drag ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    public void OnEndDrag(PointerEventData e) {
        if (cg) cg.blocksRaycasts = true;

        bool insideCase = FullyInsideSuitcase();
        bool overlaps = OverlapsAnything();

        /* --------- ACCEPT ------------- */
        if (!overlaps) {
            if (insideCase) {
                lastCasePos = rt.position; hasCasePos = true;
                inSuitcase.Add(poly);
            } else {
                lastDeskPos = rt.position; hasDeskPos = true;
                inSuitcase.Remove(poly);   // ensure not double-tracked
            }
            return;
        }

        /* --------- REJECT: snap back --- */
        if (dragStartedInCase && hasCasePos) {
            rt.position = lastCasePos;
            inSuitcase.Add(poly);
        } else if (!dragStartedInCase && hasDeskPos) {
            rt.position = lastDeskPos;
            inSuitcase.Remove(poly);
        } else {   // fallback safety
            rt.position = spawnDeskPos;
        }
    }

    /* ©¤©¤ helper: inside suitcase? ©¤©¤©¤©¤©¤©¤©¤ */
    public bool FullyInsideSuitcase() {
        foreach (var p in poly.points) {
            Vector2 w = poly.transform.TransformPoint(p);
            if (!suitcase.poly.OverlapPoint(w)) return false;
        }
        return true;
    }

    /* ©¤©¤ helper: overlaps any collider? ©¤ */
    bool OverlapsAnything() {
        // a) other draggable items
        foreach (var item in FindObjectsOfType<DraggableItem>())
            if (item != this && poly.IsTouching(item.poly))
                return true;

        // b) items already accepted in suitcase
        foreach (var other in inSuitcase)
            if (poly.IsTouching(other))
                return true;

        // c) rim blockers
        foreach (var rim in suitcase.rimBlocks)
            if (poly.IsTouching(rim))
                return true;

        return false;
    }
}
