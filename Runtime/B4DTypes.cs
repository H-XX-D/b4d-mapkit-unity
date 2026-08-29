using System;
using System.Collections.Generic;
using UnityEngine;

namespace B4D
{
    /// A number that may be absent. Campaign JSON distinguishes "no roof" from
    /// "a roof at height zero", so the kit has to carry that difference through
    /// the inspector rather than defaulting everything to 0.
    [Serializable]
    public struct OptionalFloat
    {
        public bool use;
        public float value;
        public OptionalFloat(bool use, float value) { this.use = use; this.value = value; }
        public static OptionalFloat None => new OptionalFloat(false, 0f);
        public static OptionalFloat Of(float v) => new OptionalFloat(true, v);
    }

    public enum B4DObjectiveType { signal, breakers, fuel, triangulate, escort }

    public enum B4DPropType { box, cylinder, grid, chainLine, carcassRows, lightPole, vat, pipeRun }

    [Serializable]
    public class B4DObjectiveNode
    {
        [Tooltip("Offset from the device, in metres.")]
        public float dx;
        public float dz;
        public string label = "NODE";
    }

    [Serializable]
    public class B4DNamedValue
    {
        public string key;
        public float value;
        public B4DNamedValue() { }
        public B4DNamedValue(string key, float value) { this.key = key; this.value = value; }
    }

    /// Which numeric fields each prop type needs, and what they default to.
    /// The inspector renders exactly these, so adding a prop type to the game
    /// means adding one row here rather than a new component.
    public static class B4DPropSchema
    {
        public static readonly Dictionary<B4DPropType, B4DNamedValue[]> Fields = new Dictionary<B4DPropType, B4DNamedValue[]>
        {
            [B4DPropType.box] = new[] { N("w", 4), N("h", 2), N("d", 2), N("y", float.NaN) },
            [B4DPropType.cylinder] = new[] { N("rTop", 1), N("rBottom", 1), N("h", 3), N("seg", 12), N("y", float.NaN) },
            [B4DPropType.grid] = new[] { N("x0", -20), N("x1", 20), N("stepX", 10), N("z0", -20), N("z1", 20), N("stepZ", 10) },
            [B4DPropType.chainLine] = new[] { N("y", 8.6f), N("length", 84), N("count", 14), N("startX", -40), N("spacing", 6), N("hookY", 8.4f), N("carcassEvery", 2) },
            [B4DPropType.carcassRows] = new[] { N("rows", 5), N("z0", -34), N("rowStep", 17), N("perRow", 9), N("spacing", 6.4f), N("railLength", 58), N("railXA", -6), N("railXB", 10), N("railY", 6.4f), N("startXA", -34), N("startXB", -18), N("carcassY", 3.6f), N("chainY", 5.6f) },
            [B4DPropType.lightPole] = new[] { N("h", 13), N("lightY", 12), N("rTop", 0.35f), N("rBottom", 0.5f), N("intensity", 1.4f), N("distance", 74) },
            [B4DPropType.vat] = new[] { N("rTop", 5.2f), N("rBottom", 5.6f), N("h", 4.4f) },
            [B4DPropType.pipeRun] = new[] { N("y0", 6.5f), N("yStep", 0.9f), N("z0", -160), N("zStep", 12), N("count", 6), N("length", 88), N("radius", 0.4f) }
        };

        /// Prop types that describe their own extents, so several of them are
        /// never collapsed into a shared `at` list on export.
        public static bool IsAreaProp(B4DPropType t)
            => t == B4DPropType.grid || t == B4DPropType.chainLine || t == B4DPropType.carcassRows || t == B4DPropType.pipeRun;

        static B4DNamedValue N(string k, float v) => new B4DNamedValue(k, v);
    }
}
