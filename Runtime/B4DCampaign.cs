using UnityEngine;

namespace B4D
{
    /// Root of a campaign. Put one in the scene and parent everything else under
    /// it. Everything the exporter writes is found by walking this object's
    /// children, so a scene can hold several campaigns side by side.
    [AddComponentMenu("Blox 4 Dead/Campaign Root")]
    [DisallowMultipleComponent]
    public class B4DCampaign : MonoBehaviour
    {
        [Tooltip("Stable key for this map, lower case with underscores.")]
        public string id = "new_campaign";

        [Tooltip("Campaign slot this map occupies, 0 to 4.")]
        [Range(0, 4)] public int index = 2;

        [Tooltip("Material palette, ambient light and wall settings. Must match a key in CAMPAIGN_THEMES in the game.")]
        public string theme = "slaughterhouse";

        [Tooltip("Vehicle waiting at the end of the map. Leave empty for none.")]
        public string extraction = "cattleTruck";

        [Tooltip("Scatter the shared incidental set dressing through the map.")]
        public bool quirkyProps = true;
    }
}
