using UnityEngine;
using System.Linq;

public class SuitcaseArea : MonoBehaviour {
    // Put your BoxCollider2D or PolygonCollider2D on the same object.
    [HideInInspector] public PolygonCollider2D poly;  // or BoxCollider2D if you kept that
    public Collider2D[] rimBlocks;

    void Awake() {
        poly = GetComponent<PolygonCollider2D>();      // grab the collider once
        rimBlocks = GetComponentsInChildren<Collider2D>()
                    .Where(c => c != poly)
                    .ToArray();
    }
}
