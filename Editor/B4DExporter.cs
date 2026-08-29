using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace B4D
{
    /// Turns a scene into campaign JSON the game loads directly.
    ///
    /// Coordinates map straight across: Unity X and Z are the game's X and Z in
    /// metres, and Y is only read where a prop needs a height. Rotation about Y
    /// carries over as rotY in radians.
    public static class B4DExporter
    {
        public static string Export(B4DCampaign root)
        {
            var map = new Dictionary<string, object>
            {
                ["schema"] = 1,
                ["id"] = root.id,
                ["index"] = root.index,
                ["theme"] = root.theme
            };
            if (!string.IsNullOrWhiteSpace(root.extraction)) map["extraction"] = root.extraction;
            if (!root.quirkyProps) map["quirkyProps"] = false;

            map["zones"] = root.GetComponentsInChildren<B4DZone>(false).Select(ZoneToJson).ToList();

            var props = ExportProps(root);
            if (props.Count > 0) map["props"] = props;

            var hazards = ExportHazards(root);
            if (hazards.Count > 0) map["hazards"] = hazards;

            var gates = root.GetComponentsInChildren<B4DGate>(false)
                .OrderBy(g => g.chapter)
                .Select(g => (object)new Dictionary<string, object> { ["width"] = g.width, ["x"] = g.transform.position.x })
                .ToList();
            if (gates.Count > 0) map["gates"] = gates;

            var objectives = root.GetComponentsInChildren<B4DObjective>(false)
                .OrderBy(o => o.chapter)
                .Select(ObjectiveToJson).ToList();
            if (objectives.Count > 0) map["objectives"] = objectives;

            return B4DJson.Write(map) + "\n";
        }

        static object ZoneToJson(B4DZone zone)
        {
            var pos = zone.transform.position;
            var json = new Dictionary<string, object>
            {
                ["name"] = zone.zoneName,
                ["x"] = pos.x,
                ["z"] = pos.z,
                ["halfX"] = zone.halfX,
                ["halfZ"] = zone.halfZ,
                ["floor"] = zone.floor
            };
            if (zone.roof.use) json["roof"] = zone.roof.value;
            if (!string.IsNullOrWhiteSpace(zone.roofMaterial)) json["roofMaterial"] = zone.roofMaterial;
            if (zone.lampY.use) json["lampY"] = zone.lampY.value;
            if (zone.lampCols > 0) json["lampCols"] = zone.lampCols;
            if (!string.IsNullOrWhiteSpace(zone.lampMaterial)) json["lampMaterial"] = zone.lampMaterial;
            if (!zone.walls) json["walls"] = false;
            return json;
        }

        static object ObjectiveToJson(B4DObjective objective)
        {
            var pos = objective.transform.position;
            var json = new Dictionary<string, object>
            {
                ["chapter"] = objective.chapter,
                ["x"] = pos.x,
                ["z"] = pos.z,
                ["label"] = objective.label,
                ["verb"] = objective.verb,
                ["duration"] = objective.duration,
                ["kind"] = objective.kind,
                ["type"] = objective.type.ToString()
            };
            if (objective.window.use) json["window"] = objective.window.value;
            if (objective.cartTo != null)
            {
                json["cartTo"] = new Dictionary<string, object>
                {
                    ["x"] = objective.cartTo.position.x,
                    ["z"] = objective.cartTo.position.z
                };
            }
            if (objective.nodes.Count > 0)
            {
                json["nodes"] = objective.nodes.Select(node => (object)new Dictionary<string, object>
                {
                    ["dx"] = node.dx,
                    ["dz"] = node.dz,
                    ["label"] = node.label
                }).ToList();
            }
            return json;
        }

        static Dictionary<string, object> ExportHazards(B4DCampaign root)
        {
            var hazards = new Dictionary<string, object>();

            var barrels = root.GetComponentsInChildren<B4DBarrel>(false).Select(barrel =>
            {
                var pos = barrel.transform.position;
                var json = new Dictionary<string, object> { ["x"] = pos.x, ["z"] = pos.z };
                var tint = ToHex(barrel.color);
                if (tint != 0xc9552c) json["color"] = tint;
                return (object)json;
            }).ToList();
            if (barrels.Count > 0) hazards["barrels"] = barrels;

            var drops = root.GetComponentsInChildren<B4DDropHazard>(false).Select(drop =>
            {
                var pos = drop.transform.position;
                var json = new Dictionary<string, object>
                {
                    ["x"] = pos.x, ["z"] = pos.z, ["y"] = drop.y
                };
                if (drop.anchorY.use) json["anchorY"] = drop.anchorY.value;
                json["width"] = drop.width;
                json["depth"] = drop.depth;
                json["height"] = drop.height;
                json["color"] = ToHex(drop.color);
                json["damage"] = drop.damage;
                if (!Mathf.Approximately(drop.radius, 7f)) json["radius"] = drop.radius;
                json["label"] = drop.label;
                return (object)json;
            }).ToList();
            if (drops.Count > 0) hazards["drops"] = drops;

            return hazards;
        }

        /// Simple props that differ only in where they stand collapse into one
        /// entry with an `at` list, so duplicating a crate around the level does
        /// not bloat the map file.
        static List<object> ExportProps(B4DCampaign root)
        {
            var result = new List<object>();
            var groups = new Dictionary<string, List<B4DProp>>();
            var order = new List<string>();

            foreach (var prop in root.GetComponentsInChildren<B4DProp>(false))
            {
                var key = B4DPropSchema.IsAreaProp(prop.type)
                    ? $"unique:{prop.GetInstanceID()}"
                    : GroupKey(prop);
                if (!groups.TryGetValue(key, out var bucket))
                {
                    bucket = new List<B4DProp>();
                    groups[key] = bucket;
                    order.Add(key);
                }
                bucket.Add(prop);
            }

            foreach (var key in order)
            {
                var bucket = groups[key];
                var first = bucket[0];
                var json = PropBody(first);

                if (B4DPropSchema.IsAreaProp(first.type))
                {
                    // Area props carry their own extents, so they keep an explicit origin.
                    json["x"] = first.transform.position.x;
                    json["z"] = first.transform.position.z;
                }
                else if (bucket.Count == 1 && Mathf.Approximately(RotY(first), 0f))
                {
                    json["x"] = first.transform.position.x;
                    json["z"] = first.transform.position.z;
                }
                else
                {
                    json["at"] = bucket.Select(p =>
                    {
                        var pos = p.transform.position;
                        var rot = RotY(p);
                        var spot = new List<object> { pos.x, pos.z };
                        if (!Mathf.Approximately(rot, 0f)) spot.Add(rot);
                        return (object)spot;
                    }).ToList();
                }

                result.Add(json);
            }

            return result;
        }

        static Dictionary<string, object> PropBody(B4DProp prop)
        {
            var json = new Dictionary<string, object>
            {
                ["type"] = prop.type.ToString(),
                ["material"] = prop.material
            };
            foreach (var field in prop.values)
            {
                // A y of NaN means "sit the prop on its own half height", which the
                // game does when the key is absent.
                if (field.key == "y" && float.IsNaN(field.value)) continue;
                json[field.key] = field.value;
            }
            if (prop.castShadow) json["shadow"] = true;
            if (prop.solid)
            {
                json["collide"] = new List<object> { prop.colliderHalfExtents.x, prop.colliderHalfExtents.y };
                json["kind"] = prop.colliderKind;
            }
            return json;
        }

        /// Two props share an entry when everything except their transform matches.
        static string GroupKey(B4DProp prop)
        {
            var fields = string.Join(",", prop.values.Select(v => $"{v.key}={v.value:R}"));
            return $"{prop.type}|{prop.material}|{prop.solid}|{prop.colliderHalfExtents.x:R}|{prop.colliderHalfExtents.y:R}|{prop.colliderKind}|{prop.castShadow}|{fields}";
        }

        static float RotY(B4DProp prop) => prop.transform.eulerAngles.y * Mathf.Deg2Rad;

        static int ToHex(Color c)
            => (Mathf.RoundToInt(c.r * 255f) << 16) | (Mathf.RoundToInt(c.g * 255f) << 8) | Mathf.RoundToInt(c.b * 255f);
    }
}
