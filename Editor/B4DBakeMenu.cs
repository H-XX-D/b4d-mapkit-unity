using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace B4D
{
    /// Turns the selected Unity art into a .glb the game can render, and wires it
    /// straight onto a model prop.
    public static class B4DBakeMenu
    {
        [MenuItem("GameObject/Blox 4 Dead/Bake Selection To glb", false, 35)]
        static void BakeSelection()
        {
            var selection = Selection.gameObjects;
            if (selection.Length == 0)
            {
                EditorUtility.DisplayDialog("Nothing selected", "Select the art you want to bake.", "OK");
                return;
            }

            // One glb per selected object keeps assets addressable by name. Bake a
            // group by parenting it under one object first.
            var root = selection[0];
            var suggested = Sanitise(root.name);

            var path = EditorUtility.SaveFilePanelInProject(
                "Bake to glb", suggested, "glb",
                "The game loads this file. Keeping it inside the project lets a model prop point at it.");
            if (string.IsNullOrEmpty(path)) return;

            var options = new B4DBakeOptions
            {
                maxTextureSize = EditorPrefs.GetInt("B4D.MaxTextureSize", 1024)
            };

            B4DBakeReport report;
            byte[] bytes;
            try
            {
                bytes = B4DGltfBaker.Bake(root, options, out report);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Bake failed", e.Message, "OK");
                Debug.LogException(e, root);
                return;
            }

            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);

            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            var model = root.GetComponent<B4DModelProp>();
            if (model == null && EditorUtility.DisplayDialog("Bake finished",
                    report.Summary() + "\n\nAdd a B4D Model Prop to this object and point it at the file?",
                    "Add it", "No thanks"))
            {
                model = Undo.AddComponent<B4DModelProp>(root);
                model.assetKey = suggested;
            }

            if (model != null)
            {
                Undo.RecordObject(model, "Assign baked glb");
                model.glb = asset;
                if (string.IsNullOrWhiteSpace(model.assetKey) || model.assetKey == "crate")
                {
                    model.assetKey = suggested;
                }
                FitColliderFrom(root, model);
                EditorUtility.SetDirty(model);
            }

            foreach (var warning in report.warnings) Debug.LogWarning($"[B4D bake] {warning}", root);

            if (report.warnings.Count > 0)
            {
                // The black-and-textureless case is quiet and easy to miss, so say it
                // plainly rather than leaving it in the log.
                EditorUtility.DisplayDialog("Baked, with notes",
                    report.Summary() + $"\n\n{report.warnings.Count} thing(s) needed attention:\n\n"
                    + string.Join("\n\n", report.warnings.Take(4))
                    + (report.warnings.Count > 4 ? "\n\nSee the Console for the rest." : ""),
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Baked", report.Summary(), "OK");
            }
        }

        [MenuItem("GameObject/Blox 4 Dead/Bake Selection To glb", true)]
        static bool BakeSelectionEnabled()
            => Selection.gameObjects.Any(go => go.GetComponentInChildren<Renderer>() != null);

        static void FitColliderFrom(GameObject root, B4DModelProp model)
        {
            var renderers = root.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            model.colliderHalfExtents = new Vector2(bounds.extents.x, bounds.extents.z);
            model.placeholderHeight = Mathf.Round(bounds.size.y * 100f) / 100f;
        }

        static string Sanitise(string name)
        {
            var cleaned = new string(name.ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray())
                .Trim('_');
            while (cleaned.Contains("__")) cleaned = cleaned.Replace("__", "_");
            return string.IsNullOrEmpty(cleaned) ? "asset" : cleaned;
        }
    }
}
