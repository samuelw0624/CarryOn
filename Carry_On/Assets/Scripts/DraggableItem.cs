using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

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

    /* ©¤©¤ glow effect ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    [SerializeField] Color glowRejectColor = new(0.753f, 0.188f, 0.188f, 1f); // red
    [SerializeField] Color glowHoverColor = new Color(1f, 1f, 0f, 1f); // yellow
    [SerializeField] Color glowAcceptColor = new(0f, 0f, 0f, 0f); // off
    Material materialInstance;

    [Header("Drag Settings")]
    [Range(0f, 1f)]
    [SerializeField] float baseDragResistance = 0.2f; // starting resistance
    //public float maxDragSpeed = 2000f; // tweak this

    float dragResistance; // actual runtime value, adjusted per item

    /* ©¤©¤ note attachment ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    [SerializeField] private GameObject stickyNoteIcon;
    private bool hasNote = false;

    static readonly List<PolygonCollider2D> inSuitcase = new();
    bool isDragging = false;
    public bool allowDragging = true;

    public AudioClip pickupSFX;
    AudioSource myAudiosouece;

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

        var img = GetComponent<Image>();
        if (img != null)
            img.alphaHitTestMinimumThreshold = 0.1f; // Ignore transparent clicks
    }
    void Start() {
        myAudiosouece = GetComponent<AudioSource>();
        if (tooltip == null)
            tooltip = FindObjectOfType<TooltipController>();

        var img = GetComponent<Image>();
        if (img != null && img.material != null) {
            materialInstance = Instantiate(img.material); // unique per item
            img.material = materialInstance;
            SetGlow(glowAcceptColor);
        }

        if (materialInstance.HasProperty("_EnableGlow")) {
            materialInstance.SetFloat("_EnableGlow", 1);  // turn on glow
        }

        if (materialInstance.HasProperty("_GlowColor")) {
            materialInstance.SetColor("_GlowColor", glowAcceptColor); // apply initial color
        }

        int picks = ItemPickTracker.GetPickCount(itemData);
        float extraResistance = Mathf.Clamp01(picks * 0.1f); // adjust scale as needed
        dragResistance = Mathf.Clamp(baseDragResistance + extraResistance, 0f, 0.95f);

        if (stickyNoteIcon != null)
            stickyNoteIcon.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (isDragging) return;

        if (tooltip && itemData != null)
            tooltip.ShowTooltip(itemData);

        SetGlow(glowHoverColor);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (tooltip)
            tooltip.HideTooltip();

        SetGlow(glowAcceptColor);
    }


    /* ©¤©¤ begin drag ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    public void OnBeginDrag(PointerEventData e) {
        if (!allowDragging) return;
        isDragging = true;
        myAudiosouece.PlayOneShot(pickupSFX); 

        if (tooltip)
            tooltip.HideTooltip(); // hide immediately if showing

        rt.SetAsLastSibling();              // always render on top
        inSuitcase.Remove(poly);            // will re-add if accepted

        dragStartedInCase = FullyInsideSuitcase();
        SetGlow(glowAcceptColor);
    }

    /* ©¤©¤ during drag ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    public void OnDrag(PointerEventData e) {
        if (!allowDragging) return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rt, e.position, e.pressEventCamera, out var world)) {
            Vector3 currentPos = rt.position;
            Vector3 newPos = Vector3.Lerp(currentPos, world, 1f - dragResistance);
            rt.position = newPos;
        }

        bool overlaps = OverlapsAnything();
        SetGlow(overlaps ? glowRejectColor : glowAcceptColor);
    }

    /* ©¤©¤ end drag ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤ */
    public void OnEndDrag(PointerEventData e) {
        if (!allowDragging) return;
        isDragging = false;

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

        SetGlow(glowAcceptColor);
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
        foreach (var other in inSuitcase) {
            if (other == null) continue;
            if (poly.IsTouching(other))
                return true;
        }

        // c) rim blockers
        foreach (var rim in suitcase.rimBlocks)
            if (poly.IsTouching(rim))
                return true;

        return false;
    }
    void SetGlow(Color c) {
        if (materialInstance != null)
            materialInstance.SetColor("_GlowColor", c); // Make sure "_GlowColor" is the right property
    }

    public bool TryAttachNote(DraggableNote note) {
        if (hasNote) return false;

        string newNoteText = note.inputField.text.Trim();
        if (string.IsNullOrEmpty(newNoteText)) return false;

        // Local snapshot of empties
        bool aEmpty = string.IsNullOrWhiteSpace(itemData.noteA);
        bool bEmpty = string.IsNullOrWhiteSpace(itemData.noteB);
        bool cEmpty = string.IsNullOrWhiteSpace(itemData.noteC);

        // Tentative local choice for immediate UI feedback: first empty if any; otherwise we¡¯ll let server pick oldest and then mirror.
        int localChosen = aEmpty ? 0 : (bEmpty ? 1 : (cEmpty ? 2 : -1));

        // Apply the tentative write locally (only if we actually found an empty slot)
        if (localChosen == 0) itemData.noteA = newNoteText;
        else if (localChosen == 1) itemData.noteB = newNoteText;
        else if (localChosen == 2) itemData.noteC = newNoteText;

        // Visuals: show the sticky icon and retire the dragged note UI
        if (stickyNoteIcon) stickyNoteIcon.SetActive(true);
        note.gameObject.SetActive(false);
        note.inputField.interactable = false;
        note.enabled = false;

        StickyNoteController.Instance?.RegisterItemCovered(this);

        // Permanent upsert to canonical store (prefer empty; if full, server overwrites the oldest slot)
        string editor = PlayerPrefs.GetString("PLAYER_ID", string.Empty);
        if (!Guid.TryParse(editor, out _)) {
            editor = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("PLAYER_ID", editor);
        }
        CoroutineRunner.Run(ItemNotesCloud.UpsertChoosingSlot(
            itemData.ItemID,
            newNoteText,
            editor,
            aEmpty, bEmpty, cEmpty,
            (chosenSlot, ok, err) => {
                if (!ok) {
                    Debug.LogError("[Carry-On] Upsert note failed: " + err);
                    return; // keep local tentative state rather than reverting UX
            }

            // Mirror the authoritative slot locally. If server chose a different slot (full case), move the note there.
            if (chosenSlot != localChosen && localChosen != -1) {
                // Clear the tentative slot we filled locally
                if (localChosen == 0) itemData.noteA = string.Empty;
                    else if (localChosen == 1) itemData.noteB = string.Empty;
                    else if (localChosen == 2) itemData.noteC = string.Empty;
                }

            // Write into the authoritative slot (covers both full and empty cases)
            if (chosenSlot == 0) itemData.noteA = newNoteText;
                else if (chosenSlot == 1) itemData.noteB = newNoteText;
                else if (chosenSlot == 2) itemData.noteC = newNoteText;
            }
        ));

        hasNote = true;
        return true;
    }
    public static void ClearSuitcaseColliders() {
        inSuitcase.Clear();
    }
}
