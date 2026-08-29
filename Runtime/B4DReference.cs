using UnityEngine;

namespace B4D
{
    /// Marks a GameObject as scenery for authoring only.
    ///
    /// Drop a real warehouse, a crate pack, anything from the Asset Store into
    /// the scene and mark it with this. It is never exported and never reaches
    /// the game, so nothing is redistributed. It exists so you can lay out zones
    /// and objectives against art you can actually read, instead of guessing
    /// against grey boxes.
    ///
    /// Use the tools under Blox 4 Dead to turn what you have built into map
    /// data: fit a zone to a building, or take a prop's collider from its
    /// renderer bounds.
    [AddComponentMenu("Blox 4 Dead/Reference Scenery (not exported)")]
    [DisallowMultipleComponent]
    public class B4DReference : MonoBehaviour
    {
        [Tooltip("A note to yourself about what this stands in for.")]
        public string note = "";

        [Tooltip("Draw the footprint this scenery covers on the ground plane.")]
        public bool showFootprint = true;
    }
}
