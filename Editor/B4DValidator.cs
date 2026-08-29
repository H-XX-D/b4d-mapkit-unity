using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace B4D
{
    public enum B4DLevel { Error, Warning }

    public class B4DProblem
    {
        public B4DLevel level;
        public string message;
        public Object context;
        public B4DProblem(B4DLevel level, string message, Object context = null)
        {
            this.level = level; this.message = message; this.context = context;
        }
        public override string ToString() => $"{(level == B4DLevel.Error ? "ERROR" : "WARN")}  {message}";
    }

    /// The same rules the game applies when it loads a map, run in the editor so
    /// problems surface while you are still looking at the level.
    ///
    /// An error means the map will not build or cannot be played. A warning means
    /// it builds but something is off, most often a hazard placed where no player
    /// can ever reach it.
    public static class B4DValidator
    {
        static readonly string[] ObjectiveTypeNames = { "signal", "breakers", "fuel", "triangulate", "escort" };

        /// Everything of a kind in the campaign, minus whatever sits under
        /// scenery marked reference only. Those never travel to the game, so
        /// holding them to the game's rules would only produce noise.
        static List<T> Gather<T>(B4DCampaign root) where T : Component
        {
            return root.GetComponentsInChildren<T>(false)
                .Where(c => !B4DExporter.UnderReference(c))
                .ToList();
        }

        public static List<B4DProblem> Validate(B4DCampaign root)
        {
            var problems = new List<B4DProblem>();
            if (root == null)
            {
                problems.Add(new B4DProblem(B4DLevel.Error, "no B4DCampaign root in the scene"));
                return problems;
            }

            void Err(string m, Object c = null) => problems.Add(new B4DProblem(B4DLevel.Error, m, c));
            void Warn(string m, Object c = null) => problems.Add(new B4DProblem(B4DLevel.Warning, m, c));

            if (string.IsNullOrWhiteSpace(root.id)) Err("campaign id is empty", root);
            if (string.IsNullOrWhiteSpace(root.theme)) Err("campaign theme is empty", root);

            var zones = Gather<B4DZone>(root);
            if (zones.Count == 0)
            {
                Err("the campaign has no zones, so there is nowhere to walk", root);
                return problems;
            }

            var seen = new HashSet<string>();
            foreach (var zone in zones)
            {
                if (string.IsNullOrWhiteSpace(zone.zoneName)) Err("a zone has no name", zone);
                else if (!seen.Add(zone.zoneName)) Err($"two zones are both named \"{zone.zoneName}\"", zone);
                if (zone.halfX <= 0f || zone.halfZ <= 0f) Err($"zone \"{zone.zoneName}\" has no area", zone);
                if (string.IsNullOrWhiteSpace(zone.floor)) Err($"zone \"{zone.zoneName}\" has no floor material", zone);
                if (Mathf.Abs(zone.transform.position.y) > 0.01f)
                    Warn($"zone \"{zone.zoneName}\" is off the ground plane; only its X and Z are exported", zone);
            }

            // A zone touching nothing else can never be entered.
            if (zones.Count > 1)
            {
                foreach (var zone in zones)
                {
                    if (!zones.Any(other => zone.ConnectsTo(other)))
                        Err($"zone \"{zone.zoneName}\" overlaps no other zone, so it is unreachable", zone);
                }
            }

            bool InAnyZone(float x, float z) => zones.Any(zone => zone.Contains(x, z));

            var objectives = Gather<B4DObjective>(root);
            foreach (var objective in objectives)
            {
                var pos = objective.transform.position;
                if (string.IsNullOrWhiteSpace(objective.label)) Err($"objective in chapter {objective.chapter} has no label", objective);
                if (string.IsNullOrWhiteSpace(objective.verb)) Err($"objective in chapter {objective.chapter} has no sign text", objective);
                if (string.IsNullOrWhiteSpace(objective.kind)) Err($"objective in chapter {objective.chapter} has no kind", objective);
                if (objective.duration <= 0f) Err($"objective \"{objective.label}\" has no duration", objective);
                if (objective.type == B4DObjectiveType.escort && objective.cartTo == null)
                    Err($"objective \"{objective.label}\" is an escort but has no cart destination", objective);
                if (objective.type == B4DObjectiveType.breakers && !objective.window.use)
                    Warn($"objective \"{objective.label}\" is a breakers puzzle with no window set; the switches will never have to line up", objective);
                if (!InAnyZone(pos.x, pos.z))
                    Warn($"objective \"{objective.label}\" sits outside every zone and will be relocated to the nearest clear spot at load", objective);

                foreach (var node in objective.nodes)
                {
                    if (!InAnyZone(pos.x + node.dx, pos.z + node.dz))
                        Warn($"node \"{node.label}\" of \"{objective.label}\" sits outside every zone and will be relocated at load", objective);
                }
            }

            var chapters = objectives.Select(o => o.chapter).ToList();
            foreach (var chapter in chapters.Distinct())
            {
                if (chapters.Count(c => c == chapter) > 1)
                    Err($"chapter {chapter} has more than one objective", root);
            }

            var gates = Gather<B4DGate>(root);
            if (objectives.Count > 0 && gates.Count != objectives.Count)
                Warn($"the map has {objectives.Count} objectives but {gates.Count} gates; the game expects one gate per chapter", root);

            foreach (var barrel in Gather<B4DBarrel>(root))
            {
                var p = barrel.transform.position;
                if (!InAnyZone(p.x, p.z)) Warn($"a fuel barrel at {p.x:0.#}, {p.z:0.#} sits outside every zone and can never be shot", barrel);
            }

            foreach (var drop in Gather<B4DDropHazard>(root))
            {
                var p = drop.transform.position;
                if (!InAnyZone(p.x, p.z)) Warn($"drop hazard \"{drop.label}\" at {p.x:0.#}, {p.z:0.#} sits outside every zone and can never be triggered", drop);
                if (drop.anchorY.use && drop.anchorY.value <= drop.y)
                    Err($"drop hazard \"{drop.label}\" has its cable anchor at or below the load", drop);
            }

            foreach (var prop in Gather<B4DProp>(root))
            {
                if (string.IsNullOrWhiteSpace(prop.material)) Err($"a {prop.type} prop has no material", prop);
                if (prop.solid && (prop.colliderHalfExtents.x <= 0f || prop.colliderHalfExtents.y <= 0f))
                    Err($"a solid {prop.type} prop has a zero sized collider", prop);
            }

            foreach (var model in Gather<B4DModelProp>(root))
            {
                var p = model.transform.position;
                if (string.IsNullOrWhiteSpace(model.assetKey)) Err("a model prop has no asset key", model);
                if (model.glb == null) Err($"model prop \"{model.assetKey}\" has no glb file assigned", model);
                if (model.scale <= 0f) Err($"model prop \"{model.assetKey}\" has a scale of zero or less", model);
                if (model.solid && (model.colliderHalfExtents.x <= 0f || model.colliderHalfExtents.y <= 0f))
                    Err($"model prop \"{model.assetKey}\" is solid but has a zero sized collider", model);
                if (!InAnyZone(p.x, p.z))
                    Warn($"model prop \"{model.assetKey}\" sits outside every zone, so no player will ever see it", model);
            }

            // Two props sharing a key must share a file, or one silently wins.
            var byKey = Gather<B4DModelProp>(root)
                .Where(m => !string.IsNullOrWhiteSpace(m.assetKey))
                .GroupBy(m => m.assetKey);
            foreach (var group in byKey)
            {
                var distinct = group.Select(m => m.glb).Distinct().Count();
                if (distinct > 1)
                    Err($"{distinct} different files are all called \"{group.Key}\"; give them separate keys", group.First());
            }

            return problems;
        }
    }
}
