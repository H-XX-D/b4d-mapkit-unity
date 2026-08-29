using System.Collections.Generic;
using UnityEngine;

namespace B4D
{
    /// Set dressing. One component covers every prop type in the game: pick the
    /// type and the inspector shows exactly the fields that type needs.
    ///
    /// Simple props (box, cylinder, vat, lightPole) that share every setting but
    /// their position are collapsed into a single JSON entry with an `at` list on
    /// export, so duplicating one around the level stays cheap in the map file.
    [AddComponentMenu("Blox 4 Dead/Prop")]
    [DisallowMultipleComponent]
    public class B4DProp : MonoBehaviour
    {
        public B4DPropType type = B4DPropType.box;

        [Tooltip("Theme material name, e.g. steel, rust, tile.")]
        public string material = "steel";

        [Tooltip("Blocks movement. Turn off for a prop players walk through.")]
        public bool solid = false;

        [Tooltip("Half extents of the blocking box, in metres.")]
        public Vector2 colliderHalfExtents = new Vector2(1f, 1f);

        [Tooltip("Collider label, shown in debug views, e.g. pen-rail.")]
        public string colliderKind = "prop";

        public bool castShadow = false;

        [Tooltip("Type specific numbers. Managed by the inspector, edit through it rather than by hand.")]
        public List<B4DNamedValue> values = new List<B4DNamedValue>();

        /// Fills in any field the chosen type needs and drops any left over from
        /// a previous type, keeping values the two types share.
        public void SyncFieldsToType()
        {
            if (!B4DPropSchema.Fields.TryGetValue(type, out var wanted)) return;
            var kept = new List<B4DNamedValue>();
            foreach (var field in wanted)
            {
                var existing = values.Find(v => v.key == field.key);
                kept.Add(existing ?? new B4DNamedValue(field.key, float.IsNaN(field.value) ? 0f : field.value));
            }
            values = kept;
        }

        public float Get(string key, float fallback = 0f)
        {
            var found = values.Find(v => v.key == key);
            return found != null ? found.value : fallback;
        }
    }
}
