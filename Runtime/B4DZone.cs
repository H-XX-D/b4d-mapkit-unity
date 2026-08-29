using UnityEngine;

namespace B4D
{
    /// A room or corridor. Zones are the load-bearing concept: the walkable area
    /// is the union of all zones, and where two zones overlap by more than 1.5m
    /// that overlap becomes the doorway between them. Navigation, spawning and
    /// the interior architecture pass are all derived from this list.
    [AddComponentMenu("Blox 4 Dead/Zone")]
    [DisallowMultipleComponent]
    public class B4DZone : MonoBehaviour
    {
        [Tooltip("Unique within the map. Used in nav debugging and by the architecture pass.")]
        public string zoneName = "room";

        [Min(0.1f)] public float halfX = 20f;
        [Min(0.1f)] public float halfZ = 20f;

        [Tooltip("Theme material name for the floor, e.g. tile, blood, mud.")]
        public string floor = "tile";

        [Tooltip("Ceiling height in metres. Turn off for an open-air zone.")]
        public OptionalFloat roof = OptionalFloat.Of(9f);

        [Tooltip("Height of the flickering ceiling lamps. Turn off for no lamps.")]
        public OptionalFloat lampY = OptionalFloat.Of(8.4f);

        [Tooltip("Leave at 0 to let the game pick: 2 lamp columns when the zone is wider than 40m, otherwise 1.")]
        [Min(0)] public int lampCols = 0;

        [Tooltip("Theme material for the ceiling. Empty uses the theme's wall material.")]
        public string roofMaterial = "";

        [Tooltip("Theme material for the lamp cages. Empty uses steel.")]
        public string lampMaterial = "";

        [Tooltip("Turn off for an outdoor yard that should not be walled in.")]
        public bool walls = true;

        public Vector2 Centre => new Vector2(transform.position.x, transform.position.z);

        /// True when this zone overlaps the other enough to walk between them.
        public bool ConnectsTo(B4DZone other)
        {
            if (other == null || other == this) return false;
            var a = Centre; var b = other.Centre;
            var overlapX = Mathf.Min(a.x + halfX, b.x + other.halfX) - Mathf.Max(a.x - halfX, b.x - other.halfX);
            var overlapZ = Mathf.Min(a.y + halfZ, b.y + other.halfZ) - Mathf.Max(a.y - halfZ, b.y - other.halfZ);
            return overlapX > 1.5f && overlapZ > 1.5f;
        }

        public bool Contains(float x, float z)
        {
            var c = Centre;
            return Mathf.Abs(x - c.x) <= halfX && Mathf.Abs(z - c.y) <= halfZ;
        }
    }
}
