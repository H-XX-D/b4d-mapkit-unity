using System.Linq;
using UnityEngine;
using UnityEditor;

namespace B4D
{
    /// Tools for building a map against real art.
    ///
    /// The workflow these support: drop whatever scenery you like into the
    /// scene, mark it as reference, block the level out against it, then use
    /// these to derive the map data from what you built. The art stays in
    /// Unity; only the boxes and the gameplay objects travel to the game.
    public static class B4DBuildTools
    {
        const string Menu = "GameObject/Blox 4 Dead/";

        /// World space bounds of everything renderable in the selection.
        static bool TrySelectionBounds(out Bounds bounds)
        {
            bounds = new Bounds();
            var renderers = Selection.gameObjects
                .SelectMany(go => go.GetComponentsInChildren<Renderer>())
                .Where(r => r.enabled)
                .ToArray();
            if (renderers.Length == 0) return false;

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        static bool RequireSelection(out Bounds bounds)
        {
            if (TrySelectionBounds(out bounds)) return true;
            EditorUtility.DisplayDialog("Nothing to measure",
                "Select one or more objects with a renderer first.", "OK");
            return false;
        }

        static Transform CampaignRoot()
        {
            var campaign = Object.FindObjectOfType<B4DCampaign>();
            return campaign ? campaign.transform : null;
        }

        [MenuItem(Menu + "Zone From Selection", false, 30)]
        static void ZoneFromSelection()
        {
            if (!RequireSelection(out var bounds)) return;

            var go = new GameObject("Zone");
            Undo.RegisterCreatedObjectUndo(go, "Zone From Selection");
            var root = CampaignRoot();
            if (root) go.transform.SetParent(root, true);

            // Zones sit on the ground plane; only the footprint and the height matter.
            go.transform.position = new Vector3(bounds.center.x, 0f, bounds.center.z);
            var zone = go.AddComponent<B4DZone>();
            zone.zoneName = Selection.gameObjects[0].name.ToLowerInvariant().Replace(' ', '-');
            zone.halfX = Mathf.Max(0.5f, bounds.extents.x);
            zone.halfZ = Mathf.Max(0.5f, bounds.extents.z);

            var height = bounds.max.y;
            zone.roof = height > 1f ? OptionalFloat.Of(Mathf.Round(height * 10f) / 10f) : OptionalFloat.None;
            zone.lampY = zone.roof.use ? OptionalFloat.Of(zone.roof.value - 0.6f) : OptionalFloat.None;

            Selection.activeGameObject = go;
        }

        [MenuItem(Menu + "Fit Zone To Selection", false, 31)]
        static void FitZoneToSelection()
        {
            var zone = Selection.gameObjects.Select(g => g.GetComponent<B4DZone>()).FirstOrDefault(z => z != null);
            if (zone == null)
            {
                EditorUtility.DisplayDialog("No zone selected",
                    "Select the zone you want to resize together with the scenery it should cover.", "OK");
                return;
            }
            // Measure everything except the zone itself.
            var renderers = Selection.gameObjects
                .Where(g => g != zone.gameObject)
                .SelectMany(g => g.GetComponentsInChildren<Renderer>())
                .Where(r => r.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                EditorUtility.DisplayDialog("Nothing to fit to",
                    "Select the zone and at least one object with a renderer.", "OK");
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Undo.RecordObject(zone, "Fit Zone");
            Undo.RecordObject(zone.transform, "Fit Zone");
            zone.transform.position = new Vector3(bounds.center.x, 0f, bounds.center.z);
            zone.halfX = Mathf.Max(0.5f, bounds.extents.x);
            zone.halfZ = Mathf.Max(0.5f, bounds.extents.z);
        }

        [MenuItem(Menu + "Blocking Prop From Selection", false, 32)]
        static void PropFromSelection()
        {
            if (!RequireSelection(out var bounds)) return;

            var go = new GameObject("Blocking prop");
            Undo.RegisterCreatedObjectUndo(go, "Prop From Selection");
            var root = CampaignRoot();
            if (root) go.transform.SetParent(root, true);
            go.transform.position = new Vector3(bounds.center.x, 0f, bounds.center.z);

            var prop = go.AddComponent<B4DProp>();
            prop.type = B4DPropType.box;
            prop.material = "steel";
            prop.solid = true;
            prop.colliderHalfExtents = new Vector2(bounds.extents.x, bounds.extents.z);
            prop.colliderKind = Selection.gameObjects[0].name.ToLowerInvariant().Replace(' ', '-');
            prop.SyncFieldsToType();
            SetValue(prop, "w", bounds.size.x);
            SetValue(prop, "h", bounds.size.y);
            SetValue(prop, "d", bounds.size.z);
            SetValue(prop, "y", bounds.center.y);

            Selection.activeGameObject = go;
        }


        // ------------------------------------------------------------------
        // working from a scene built the ordinary Unity way
        // ------------------------------------------------------------------

        /// Creates the campaign root and marks everything already in the scene as
        /// reference, so a level built the normal way is ready to annotate without
        /// rearranging anything.
        [MenuItem(Menu + "Set Up Campaign In This Scene", false, 1)]
        static void SetUpCampaign()
        {
            var campaign = Object.FindObjectOfType<B4DCampaign>();
            if (campaign == null)
            {
                var go = new GameObject("Campaign");
                Undo.RegisterCreatedObjectUndo(go, "Set Up Campaign");
                campaign = go.AddComponent<B4DCampaign>();
            }

            // Anything already standing in the scene is art, not map data. Marking
            // it reference keeps it out of the export and out of the checks.
            var marked = 0;
            foreach (var renderer in Object.FindObjectsOfType<MeshRenderer>())
            {
                var root = renderer.transform;
                while (root.parent != null && root.parent != campaign.transform) root = root.parent;
                if (root == campaign.transform) continue;
                if (root.GetComponent<B4DReference>() != null) continue;
                if (root.GetComponentInParent<B4DReference>() != null) continue;
                Undo.AddComponent<B4DReference>(root.gameObject);
                marked++;
            }

            Selection.activeGameObject = campaign.gameObject;
            EditorUtility.DisplayDialog("Campaign ready",
                $"Campaign root added and {marked} existing object(s) marked as reference scenery, "
                + "so none of your art is exported.\n\n"
                + "Next: draw zones over the walkable space, then add an objective and a gate per chapter. "
                + "Window > Blox 4 Dead > Map Kit tells you what is still missing.",
                "OK");
        }

        /// Turns the colliders a scene already has into solids the game blocks on.
        /// A level built normally is already collidered, so this is usually the
        /// fastest way to get its blocking geometry across.
        [MenuItem(Menu + "Blocking Props From Colliders", false, 36)]
        static void PropsFromColliders()
        {
            var colliders = Selection.gameObjects
                .SelectMany(go => go.GetComponentsInChildren<Collider>())
                .Where(c => c.enabled && !c.isTrigger)
                .ToArray();

            if (colliders.Length == 0)
            {
                EditorUtility.DisplayDialog("No colliders found",
                    "Select objects that have colliders on them, or somewhere beneath them. "
                    + "Triggers are skipped, since they do not block movement.", "OK");
                return;
            }

            var root = CampaignRoot();
            var group = new GameObject("Blocking from colliders");
            Undo.RegisterCreatedObjectUndo(group, "Blocking Props From Colliders");
            if (root) group.transform.SetParent(root, true);

            var made = 0;
            foreach (var collider in colliders)
            {
                var go = new GameObject(collider.name);
                go.transform.SetParent(group.transform, true);

                var prop = go.AddComponent<B4DProp>();
                prop.type = B4DPropType.box;
                prop.material = "steel";
                prop.solid = true;
                prop.colliderKind = Sanitise(collider.name);
                prop.SyncFieldsToType();

                // A box collider carries its own orientation, so keep it rather than
                // falling back to an axis aligned box that would be too fat on the
                // diagonal. Everything else uses its world bounds.
                if (collider is BoxCollider box)
                {
                    var scale = collider.transform.lossyScale;
                    var size = new Vector3(box.size.x * scale.x, box.size.y * scale.y, box.size.z * scale.z);
                    var centre = collider.transform.TransformPoint(box.center);
                    go.transform.position = new Vector3(centre.x, 0f, centre.z);
                    go.transform.rotation = Quaternion.Euler(0f, collider.transform.eulerAngles.y, 0f);
                    prop.colliderHalfExtents = new Vector2(Mathf.Abs(size.x) * 0.5f, Mathf.Abs(size.z) * 0.5f);
                    SetValue(prop, "w", Mathf.Abs(size.x));
                    SetValue(prop, "h", Mathf.Abs(size.y));
                    SetValue(prop, "d", Mathf.Abs(size.z));
                    SetValue(prop, "y", centre.y);
                }
                else
                {
                    var bounds = collider.bounds;
                    go.transform.position = new Vector3(bounds.center.x, 0f, bounds.center.z);
                    prop.colliderHalfExtents = new Vector2(bounds.extents.x, bounds.extents.z);
                    SetValue(prop, "w", bounds.size.x);
                    SetValue(prop, "h", bounds.size.y);
                    SetValue(prop, "d", bounds.size.z);
                    SetValue(prop, "y", bounds.center.y);
                }
                made++;
            }

            Selection.activeGameObject = group;
            EditorUtility.DisplayDialog("Blocking props created",
                $"{made} solid(s) created from existing colliders, under \"{group.name}\".\n\n"
                + "They are invisible in the game: they only block movement. The art itself "
                + "stays in Unity unless you bake it.", "OK");
        }

        [MenuItem(Menu + "Blocking Props From Colliders", true)]
        static bool PropsFromCollidersEnabled()
            => Selection.gameObjects.Any(go => go.GetComponentInChildren<Collider>() != null);

        static void SetValue(B4DProp prop, string key, float value)
        {
            var field = prop.values.Find(v => v.key == key);
            if (field != null) field.value = Mathf.Round(value * 100f) / 100f;
        }

        [MenuItem(Menu + "Mark As Reference Scenery", false, 33)]
        static string Sanitise(string name)
        {
            var cleaned = new string(name.ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
            while (cleaned.Contains("--")) cleaned = cleaned.Replace("--", "-");
            return string.IsNullOrEmpty(cleaned) ? "prop" : cleaned;
        }

        static void MarkAsReference()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Nothing selected", "Select the scenery to mark.", "OK");
                return;
            }
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<B4DReference>() != null) continue;
                Undo.AddComponent<B4DReference>(go);
            }
        }

        [MenuItem(Menu + "Fit Collider To Renderers", false, 34)]
        static void FitColliderToRenderers()
        {
            var handled = 0;
            foreach (var go in Selection.gameObjects)
            {
                var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                var model = go.GetComponent<B4DModelProp>();
                if (model != null)
                {
                    Undo.RecordObject(model, "Fit Collider");
                    model.colliderHalfExtents = new Vector2(bounds.extents.x, bounds.extents.z);
                    model.placeholderHeight = Mathf.Round(bounds.size.y * 100f) / 100f;
                    handled++;
                    continue;
                }
                var prop = go.GetComponent<B4DProp>();
                if (prop != null)
                {
                    Undo.RecordObject(prop, "Fit Collider");
                    prop.solid = true;
                    prop.colliderHalfExtents = new Vector2(bounds.extents.x, bounds.extents.z);
                    handled++;
                }
            }
            if (handled == 0)
            {
                EditorUtility.DisplayDialog("Nothing to fit",
                    "Select objects carrying a B4D Prop or B4D Model Prop, with a renderer somewhere beneath them.", "OK");
            }
        }
    }

    /// Reference scenery draws its footprint so you can see what a zone has to cover.
    public static class B4DReferenceGizmos
    {
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void Draw(B4DReference reference, GizmoType type)
        {
            if (!reference.showFootprint) return;
            var renderers = reference.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.55f);
            Gizmos.DrawWireCube(
                new Vector3(bounds.center.x, 0.02f, bounds.center.z),
                new Vector3(bounds.size.x, 0.02f, bounds.size.z));
        }
    }
}
