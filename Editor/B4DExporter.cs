using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace B4D
{
    /// Turns a scene into campaign JSON the game loads directly.
    ///
    /// Coordinates map straight across: Unity X and Z are the game's X and Z in
    /// metres, and Y is only read where a prop needs a height. Rotation about Y
    /// carries over as rotY in radians.
    public static class B4DExporter
    {
        /// True when this object sits under scenery marked reference only, which
        /// never travels to the game. Blocking out a level inside an imported
        /// prefab is normal, so the check walks all the way up.
        public static bool UnderReference(Component component)
        {
            return component != null && component.GetComponentInParent<B4DReference>() != null;
        }

        /// Everything of a kind in the campaign, minus whatever is reference only.
        static T[] Gather<T>(B4DCampaign root) where T : Component
        {
            return root.GetComponentsInChildren<T>(false).Where(c => !UnderReference(c)).ToArray();
        }

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

            map["zones"] = Gather<B4DZone>(root).Select(ZoneToJson).ToList();

            var assets = new Dictionary<string, object>();
            var props = ExportProps(root);
            props.AddRange(ExportModelProps(root, assets, OutputDirectory));
            if (props.Count > 0) map["props"] = props;
            if (assets.Count > 0) map["assets"] = assets;

            var hazards = ExportHazards(root);
            if (hazards.Count > 0) map["hazards"] = hazards;

            var gates = Gather<B4DGate>(root)
                .OrderBy(g => g.chapter)
                .Select(g => (object)new Dictionary<string, object> { ["width"] = g.width, ["x"] = g.transform.position.x })
                .ToList();
            if (gates.Count > 0) map["gates"] = gates;

            var objectives = Gather<B4DObjective>(root)
                .OrderBy(o => o.chapter)
                .Select(ObjectiveToJson).ToList();
            if (objectives.Count > 0) map["objectives"] = objectives;

            return B4DJson.Write(map) + "\n";
        }

        /// Where the map file is being written, so any .glb kept out of line can
        /// be copied alongside it. Set by the exporter window before it calls in.
        public static string OutputDirectory;

        /// Model props become a shared asset table plus one placement each.
        /// Props naming the same key share a single copy of the mesh.
        static List<object> ExportModelProps(B4DCampaign root, Dictionary<string, object> assets, string outputDirectory)
        {
            var result = new List<object>();
            var grouped = new Dictionary<string, List<B4DModelProp>>();
            var order = new List<string>();

            foreach (var model in Gather<B4DModelProp>(root))
            {
                if (string.IsNullOrWhiteSpace(model.assetKey)) continue;
                if (!grouped.TryGetValue(model.assetKey, out var bucket))
                {
                    bucket = new List<B4DModelProp>();
                    grouped[model.assetKey] = bucket;
                    order.Add(model.assetKey);
                }
                bucket.Add(model);
            }

            foreach (var key in order)
            {
                var bucket = grouped[key];
                var first = bucket[0];
                var uri = BuildAssetUri(first, outputDirectory);
                if (uri == null) continue;

                var asset = new Dictionary<string, object> { ["uri"] = uri };
                if (!Mathf.Approximately(first.scale, 1f)) asset["scale"] = first.scale;
                assets[key] = asset;

                foreach (var model in bucket)
                {
                    var position = model.transform.position;
                    var yaw = model.transform.eulerAngles.y * Mathf.Deg2Rad;
                    var json = new Dictionary<string, object>
                    {
                        ["type"] = "model",
                        ["asset"] = key,
                        ["x"] = position.x,
                        ["z"] = position.z
                    };
                    if (!Mathf.Approximately(position.y, 0f)) json["y"] = position.y;
                    if (!Mathf.Approximately(yaw, 0f)) json["rotY"] = yaw;
                    if (!Mathf.Approximately(model.scale, 1f) && bucket.Count > 1) json["scale"] = model.scale;
                    if (model.placeholderHeight > 0f) json["placeholderHeight"] = model.placeholderHeight;
                    if (model.solid)
                    {
                        json["collide"] = new List<object> { model.colliderHalfExtents.x, model.colliderHalfExtents.y };
                        json["kind"] = model.colliderKind;
                    }
                    result.Add(json);
                }
            }
            return result;
        }

        /// Either the mesh carried inside the map file as base64, or a relative
        /// path with the file copied next to it.
        static string BuildAssetUri(B4DModelProp model, string outputDirectory)
        {
            if (model.glb == null)
            {
                Debug.LogWarning($"[B4D] model prop \"{model.assetKey}\" has no glb assigned and was skipped", model);
                return null;
            }
            var sourcePath = AssetDatabase.GetAssetPath(model.glb);
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                Debug.LogWarning($"[B4D] could not find the file behind \"{model.assetKey}\" and skipped it", model);
                return null;
            }

            if (model.inlineInMap)
            {
                var bytes = File.ReadAllBytes(sourcePath);
                return "data:model/gltf-binary;base64," + Convert.ToBase64String(bytes);
            }

            var fileName = model.assetKey + ".glb";
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                var assetsDir = Path.Combine(outputDirectory, "assets");
                Directory.CreateDirectory(assetsDir);
                File.Copy(sourcePath, Path.Combine(assetsDir, fileName), true);
            }
            return "assets/" + fileName;
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

            var barrels = Gather<B4DBarrel>(root).Select(barrel =>
            {
                var pos = barrel.transform.position;
                var json = new Dictionary<string, object> { ["x"] = pos.x, ["z"] = pos.z };
                var tint = ToHex(barrel.color);
                if (tint != 0xc9552c) json["color"] = tint;
                return (object)json;
            }).ToList();
            if (barrels.Count > 0) hazards["barrels"] = barrels;

            var drops = Gather<B4DDropHazard>(root).Select(drop =>
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

            foreach (var prop in Gather<B4DProp>(root))
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
