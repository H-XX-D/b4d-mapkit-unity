using UnityEngine;

namespace B4D
{
    /// A mesh the game will actually render, supplied as a .glb file.
    ///
    /// This is the opposite end from B4DReference: use it when you want the art
    /// itself in the game, not just as a guide while building. The game reads a
    /// practical subset of glTF: indexed triangles, normals, texture
    /// coordinates, vertex colours, and PBR base colour with an embedded
    /// texture. Anything it cannot read falls back to a plain box, so a bad
    /// asset costs you one prop rather than the level.
    ///
    /// Check the licence on anything from the Asset Store before shipping it.
    /// Plenty of packs allow use in a built game but forbid redistributing the
    /// source mesh, and a .glb in a web page is about as redistributable as it
    /// gets. B4DReference exists for exactly that case.
    [AddComponentMenu("Blox 4 Dead/Model Prop")]
    [DisallowMultipleComponent]
    public class B4DModelProp : MonoBehaviour
    {
        [Tooltip("Name this asset goes by in the map file. Props sharing a key share one copy.")]
        public string assetKey = "crate";

        [Tooltip("The .glb file to use. Import it into the project as a plain asset.")]
        public Object glb;

        [Tooltip("Carry the mesh inside the map file as base64 rather than shipping a separate file. Keeps the game a single self contained page; only sensible for small assets.")]
        public bool inlineInMap = true;

        [Tooltip("Multiplies the size of the mesh. The game works in metres.")]
        public float scale = 1f;

        [Tooltip("Blocks movement. Turn off for a prop players walk through.")]
        public bool solid = true;

        [Tooltip("Half extents of the blocking box, in metres. Use Fit Collider To Renderers to take these from the art.")]
        public Vector2 colliderHalfExtents = new Vector2(1f, 1f);

        [Tooltip("Collider label, shown in debug views.")]
        public string colliderKind = "model";

        [Tooltip("Height of the stand-in box shown until the mesh loads. Leave at 0 to derive it from the collider.")]
        public float placeholderHeight = 0f;
    }
}
