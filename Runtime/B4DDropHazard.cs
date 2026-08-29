using UnityEngine;

namespace B4D
{
    /// A heavy load hanging from a cable. Shoot the load or the cable and it
    /// comes down on whatever is underneath.
    [AddComponentMenu("Blox 4 Dead/Hazard - Drop")]
    [DisallowMultipleComponent]
    public class B4DDropHazard : MonoBehaviour
    {
        [Tooltip("Resting height of the load, in metres.")]
        public float y = 5.4f;

        [Tooltip("Height the cable is anchored at. Turn off to sit 9m above the load.")]
        public OptionalFloat anchorY = OptionalFloat.Of(8.6f);

        public float width = 12f;
        public float depth = 5.6f;
        public float height = 2.9f;

        public Color color = new Color(0.541f, 0.231f, 0.184f);

        [Min(0f)] public float damage = 900f;
        [Min(0f)] public float radius = 7f;

        public string label = "DO NOT STAND UNDER";
    }
}
