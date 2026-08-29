using System.Collections.Generic;
using UnityEngine;

namespace B4D
{
    /// A chapter's device. The squad has to run it, which draws a sustained
    /// attack, and only when the work finishes does that chapter's gate open.
    [AddComponentMenu("Blox 4 Dead/Objective")]
    [DisallowMultipleComponent]
    public class B4DObjective : MonoBehaviour
    {
        [Min(1)] public int chapter = 1;

        public B4DObjectiveType type = B4DObjectiveType.signal;

        [Tooltip("Shown to the squad as their current order, e.g. RESTART THE CHAIN LINE.")]
        public string label = "DO THE THING";

        [Tooltip("Short sign text on the device itself, e.g. CHAIN LINE.")]
        public string verb = "DEVICE";

        [Tooltip("Flavour key used for barks and set dressing, e.g. chain, freezer.")]
        public string kind = "generic";

        [Tooltip("Seconds of work once the device is started.")]
        [Min(1f)] public float duration = 10f;

        [Tooltip("breakers only: seconds every switch must be held live at the same time.")]
        public OptionalFloat window = OptionalFloat.None;

        [Tooltip("escort only: where the cart has to end up.")]
        public Transform cartTo;

        [Tooltip("Secondary stations, placed relative to this device.")]
        public List<B4DObjectiveNode> nodes = new List<B4DObjectiveNode>();
    }
}
