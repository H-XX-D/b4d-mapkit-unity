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

        static void SetValue(B4DProp prop, string key, float value)
        {
            var field = prop.values.Find(v => v.key == key);
            if (field != null) field.value = Mathf.Round(value * 100f) / 100f;
        }

        [MenuItem(Menu + "Mark As Reference Scenery", false, 33)]
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
