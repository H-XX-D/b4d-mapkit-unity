using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace B4D
{
    /// Rebuilds a scene from campaign JSON, so an existing map can be opened,
    /// edited and written back out.
    public static class B4DImporter
    {
        public static B4DCampaign Import(string json, string objectName = null)
        {
            var map = B4DJson.Obj(B4DJson.Parse(json));
            if (map == null) throw new System.FormatException("campaign JSON is not an object");

            var schema = B4DJson.I(map, "schema", 0);
            if (schema != 1) throw new System.FormatException($"campaign schema {schema} is not supported, expected 1");

            var id = B4DJson.S(map, "id", "imported_campaign");
            var rootObject = new GameObject(objectName ?? $"Campaign {id}");
            Undo.RegisterCreatedObjectUndo(rootObject, "Import Campaign");

            var root = rootObject.AddComponent<B4DCampaign>();
            root.id = id;
            root.index = B4DJson.I(map, "index", 2);
            root.theme = B4DJson.S(map, "theme", "slaughterhouse");
            root.extraction = B4DJson.S(map, "extraction", "");
            root.quirkyProps = B4DJson.B(map, "quirkyProps", true);

            var zonesParent = Child(rootObject, "Zones");
            foreach (var entry in B4DJson.Arr(map["zones"]))
            {
                var z = B4DJson.Obj(entry);
                var go = Child(zonesParent, B4DJson.S(z, "name", "zone"));
                go.transform.position = new Vector3(B4DJson.F(z, "x"), 0f, B4DJson.F(z, "z"));
                var zone = go.AddComponent<B4DZone>();
                zone.zoneName = B4DJson.S(z, "name", "zone");
                zone.halfX = B4DJson.F(z, "halfX", 10f);
                zone.halfZ = B4DJson.F(z, "halfZ", 10f);
                zone.floor = B4DJson.S(z, "floor", "tile");
                zone.roof = B4DJson.Has(z, "roof") ? OptionalFloat.Of(B4DJson.F(z, "roof")) : OptionalFloat.None;
                zone.lampY = B4DJson.Has(z, "lampY") ? OptionalFloat.Of(B4DJson.F(z, "lampY")) : OptionalFloat.None;
                zone.lampCols = B4DJson.I(z, "lampCols", 0);
                zone.roofMaterial = B4DJson.S(z, "roofMaterial", "");
                zone.lampMaterial = B4DJson.S(z, "lampMaterial", "");
                zone.walls = B4DJson.B(z, "walls", true);
            }

            if (map.ContainsKey("props")) ImportProps(rootObject, B4DJson.Arr(map["props"]));
            if (map.ContainsKey("hazards")) ImportHazards(rootObject, B4DJson.Obj(map["hazards"]));

            if (map.ContainsKey("gates"))
            {
                var parent = Child(rootObject, "Gates");
                var gates = B4DJson.Arr(map["gates"]);
                for (var i = 0; i < gates.Count; i++)
                {
                    var g = B4DJson.Obj(gates[i]);
                    var go = Child(parent, $"Gate {i + 1}");
                    go.transform.position = new Vector3(B4DJson.F(g, "x"), 0f, 0f);
                    var gate = go.AddComponent<B4DGate>();
                    gate.width = B4DJson.F(g, "width", 14f);
                    gate.chapter = i + 1;
                }
            }

            if (map.ContainsKey("objectives")) ImportObjectives(rootObject, B4DJson.Arr(map["objectives"]));

            Selection.activeGameObject = rootObject;
            return root;
        }

        static void ImportObjectives(GameObject rootObject, List<object> entries)
        {
            var parent = Child(rootObject, "Objectives");
            foreach (var entry in entries)
            {
                var o = B4DJson.Obj(entry);
                var label = B4DJson.S(o, "label", "OBJECTIVE");
                var go = Child(parent, $"Ch{B4DJson.I(o, "chapter", 1)} {label}");
                go.transform.position = new Vector3(B4DJson.F(o, "x"), 0f, B4DJson.F(o, "z"));

                var objective = go.AddComponent<B4DObjective>();
                objective.chapter = B4DJson.I(o, "chapter", 1);
                objective.label = label;
                objective.verb = B4DJson.S(o, "verb", "DEVICE");
                objective.kind = B4DJson.S(o, "kind", "generic");
                objective.duration = B4DJson.F(o, "duration", 10f);
                objective.window = B4DJson.Has(o, "window") ? OptionalFloat.Of(B4DJson.F(o, "window")) : OptionalFloat.None;
                if (System.Enum.TryParse<B4DObjectiveType>(B4DJson.S(o, "type", "signal"), out var parsed)) objective.type = parsed;

                if (B4DJson.Has(o, "cartTo"))
                {
                    var cart = B4DJson.Obj(o["cartTo"]);
                    var cartObject = Child(go, "Cart destination");
                    cartObject.transform.position = new Vector3(B4DJson.F(cart, "x"), 0f, B4DJson.F(cart, "z"));
                    objective.cartTo = cartObject.transform;
                }

                if (B4DJson.Has(o, "nodes"))
                {
                    foreach (var nodeEntry in B4DJson.Arr(o["nodes"]))
                    {
                        var n = B4DJson.Obj(nodeEntry);
                        objective.nodes.Add(new B4DObjectiveNode
                        {
                            dx = B4DJson.F(n, "dx"),
                            dz = B4DJson.F(n, "dz"),
                            label = B4DJson.S(n, "label", "NODE")
                        });
                    }
                }
            }
        }

        static void ImportHazards(GameObject rootObject, Dictionary<string, object> hazards)
        {
            if (hazards == null) return;
            if (hazards.ContainsKey("barrels"))
            {
                var parent = Child(rootObject, "Barrels");
                foreach (var entry in B4DJson.Arr(hazards["barrels"]))
                {
                    var b = B4DJson.Obj(entry);
                    var go = Child(parent, "Fuel barrel");
                    go.transform.position = new Vector3(B4DJson.F(b, "x"), 0f, B4DJson.F(b, "z"));
                    var barrel = go.AddComponent<B4DBarrel>();
                    barrel.color = FromHex(B4DJson.I(b, "color", 0xc9552c));
                }
            }
            if (hazards.ContainsKey("drops"))
            {
                var parent = Child(rootObject, "Drop hazards");
                foreach (var entry in B4DJson.Arr(hazards["drops"]))
                {
                    var d = B4DJson.Obj(entry);
                    var go = Child(parent, B4DJson.S(d, "label", "Drop hazard"));
                    go.transform.position = new Vector3(B4DJson.F(d, "x"), 0f, B4DJson.F(d, "z"));
                    var drop = go.AddComponent<B4DDropHazard>();
                    drop.y = B4DJson.F(d, "y", 5.4f);
                    drop.anchorY = B4DJson.Has(d, "anchorY") ? OptionalFloat.Of(B4DJson.F(d, "anchorY")) : OptionalFloat.None;
                    drop.width = B4DJson.F(d, "width", 12f);
                    drop.depth = B4DJson.F(d, "depth", 5.6f);
                    drop.height = B4DJson.F(d, "height", 2.9f);
                    drop.color = FromHex(B4DJson.I(d, "color", 0x8a3b2f));
                    drop.damage = B4DJson.F(d, "damage", 900f);
                    drop.radius = B4DJson.F(d, "radius", 7f);
                    drop.label = B4DJson.S(d, "label", "DO NOT STAND UNDER");
                }
            }
        }

        static void ImportProps(GameObject rootObject, List<object> entries)
        {
            var parent = Child(rootObject, "Props");
            foreach (var entry in entries)
            {
                var p = B4DJson.Obj(entry);
                if (!System.Enum.TryParse<B4DPropType>(B4DJson.S(p, "type", "box"), out var type)) continue;

                // One JSON entry with an `at` list becomes one object per position,
                // which is what you actually want to click on in the scene.
                var spots = new List<Vector3>();
                if (B4DJson.Has(p, "at"))
                {
                    foreach (var spot in B4DJson.Arr(p["at"]))
                    {
                        var coords = B4DJson.Arr(spot);
                        spots.Add(new Vector3(
                            System.Convert.ToSingle(coords[0]), 0f, System.Convert.ToSingle(coords[1])));
                    }
                }
                else spots.Add(new Vector3(B4DJson.F(p, "x"), 0f, B4DJson.F(p, "z")));

                var rotations = new List<float>();
                if (B4DJson.Has(p, "at"))
                {
                    foreach (var spot in B4DJson.Arr(p["at"]))
                    {
                        var coords = B4DJson.Arr(spot);
                        rotations.Add(coords.Count > 2 ? System.Convert.ToSingle(coords[2]) : 0f);
                    }
                }
                else rotations.Add(B4DJson.F(p, "rotY", 0f));

                for (var i = 0; i < spots.Count; i++)
                {
                    var go = Child(parent, type.ToString());
                    go.transform.position = spots[i];
                    go.transform.rotation = Quaternion.Euler(0f, rotations[i] * Mathf.Rad2Deg, 0f);

                    var prop = go.AddComponent<B4DProp>();
                    prop.type = type;
                    prop.material = B4DJson.S(p, "material", "steel");
                    prop.castShadow = B4DJson.B(p, "shadow", false);
                    if (B4DJson.Has(p, "collide"))
                    {
                        var extents = B4DJson.Arr(p["collide"]);
                        prop.solid = true;
                        prop.colliderHalfExtents = new Vector2(
                            System.Convert.ToSingle(extents[0]), System.Convert.ToSingle(extents[1]));
                        prop.colliderKind = B4DJson.S(p, "kind", "prop");
                    }
                    prop.SyncFieldsToType();
                    foreach (var field in prop.values)
                    {
                        if (B4DJson.Has(p, field.key)) field.value = B4DJson.F(p, field.key);
                    }
                }
            }
        }

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, true);
            return go;
        }

        static Color FromHex(int hex)
            => new Color(((hex >> 16) & 0xff) / 255f, ((hex >> 8) & 0xff) / 255f, (hex & 0xff) / 255f);
    }
}
