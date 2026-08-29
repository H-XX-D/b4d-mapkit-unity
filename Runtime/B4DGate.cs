using UnityEngine;

namespace B4D
{
    /// A checkpoint door. One per chapter, opened by that chapter's objective.
    /// Gates export in chapter order, so order them along the route.
    [AddComponentMenu("Blox 4 Dead/Gate")]
    [DisallowMultipleComponent]
    public class B4DGate : MonoBehaviour
    {
        [Min(1f)] public float width = 14f;

        [Tooltip("Which chapter this gate closes off. Used only to order the gates on export.")]
        [Min(1)] public int chapter = 1;
    }
}
