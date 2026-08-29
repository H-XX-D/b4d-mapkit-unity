using UnityEngine;

namespace B4D
{
    /// A fuel barrel. Shoot it and it goes up, taking the crowd around it with it.
    [AddComponentMenu("Blox 4 Dead/Hazard - Fuel Barrel")]
    [DisallowMultipleComponent]
    public class B4DBarrel : MonoBehaviour
    {
        [Tooltip("Barrel tint. The game's default is a rusty orange.")]
        public Color color = new Color(0.788f, 0.333f, 0.173f);
    }
}
