using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace B4D
{
    /// The map kit's front door: pick the campaign, see what is wrong with it,
    /// and write it out. Open with Window > Blox 4 Dead > Map Kit.
    public class B4DMapKitWindow : EditorWindow
    {
        B4DCampaign campaign;
        List<B4DProblem> problems = new List<B4DProblem>();
        Vector2 scroll;
        string lastWrittenPath;

        [MenuItem("Window/Blox 4 Dead/Map Kit")]
        public static void Open() => GetWindow<B4DMapKitWindow>("B4D Map Kit").Show();

        void OnEnable()
        {
            if (campaign == null) campaign = FindObjectOfType<B4DCampaign>();
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            campaign = (B4DCampaign)EditorGUILayout.ObjectField("Campaign", campaign, typeof(B4DCampaign), true);

            if (campaign == null)
            {
                EditorGUILayout.HelpBox("No campaign in the scene. Create one to start a map.", MessageType.Info);
                if (GUILayout.Button("Create campaign root")) CreateCampaign();
                DrawImportRow();
                return;
            }

            EditorGUILayout.Space();
            DrawCounts();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Check map", GUILayout.Height(26))) Check();
                using (new EditorGUI.DisabledScope(problems.Any(p => p.level == B4DLevel.Error)))
                {
                    if (GUILayout.Button("Export JSON", GUILayout.Height(26))) ExportToFile();
                }
            }

            DrawImportRow();

            if (!string.IsNullOrEmpty(lastWrittenPath))
            {
                EditorGUILayout.HelpBox($"Written to {lastWrittenPath}", MessageType.None);
                if (GUILayout.Button("Copy JSON to clipboard for live preview"))
                {
                    EditorGUIUtility.systemCopyBuffer =
                        $"window.B4D_CAMPAIGN_OVERRIDES = {{ \"{campaign.id}\": {File.ReadAllText(lastWrittenPath)} }};";
                    ShowNotification(new GUIContent("Paste into the game console"));
                }
            }

            DrawProblems();
        }

        void DrawCounts()
        {
            var zones = campaign.GetComponentsInChildren<B4DZone>(false).Length;
            var objectives = campaign.GetComponentsInChildren<B4DObjective>(false).Length;
            var gates = campaign.GetComponentsInChildren<B4DGate>(false).Length;
            var props = campaign.GetComponentsInChildren<B4DProp>(false).Length;
            var barrels = campaign.GetComponentsInChildren<B4DBarrel>(false).Length;
            var drops = campaign.GetComponentsInChildren<B4DDropHazard>(false).Length;
            var models = campaign.GetComponentsInChildren<B4DModelProp>(false).Length;
            var reference = campaign.GetComponentsInChildren<B4DReference>(false).Length;
            EditorGUILayout.LabelField(
                $"{zones} zones · {objectives} objectives · {gates} gates · {props + models} props · {barrels + drops} hazards",
                EditorStyles.miniLabel);
            if (reference > 0)
            {
                EditorGUILayout.LabelField(
                    $"{reference} reference object(s) in the scene, none of which are exported",
                    EditorStyles.miniLabel);
            }
        }

        void DrawImportRow()
        {
            if (!GUILayout.Button("Import JSON into the scene")) return;
            var path = EditorUtility.OpenFilePanel("Import campaign JSON", B4DProjectPaths.EnsureMaps(), "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                campaign = B4DImporter.Import(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
                Check();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import failed", e.Message, "OK");
            }
        }

        void DrawProblems()
        {
            if (problems.Count == 0) return;
            EditorGUILayout.Space();
            var errors = problems.Count(p => p.level == B4DLevel.Error);
            var warnings = problems.Count - errors;
            EditorGUILayout.LabelField(
                errors > 0 ? $"{errors} error(s), {warnings} warning(s)" : $"No errors, {warnings} warning(s)",
                EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var problem in problems.OrderBy(p => p.level))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        new GUIContent(problem.level == B4DLevel.Error ? "✕" : "!"),
                        GUILayout.Width(14));
                    EditorGUILayout.LabelField(problem.message, EditorStyles.wordWrappedMiniLabel);
                    if (problem.context != null && GUILayout.Button("Select", GUILayout.Width(52)))
                    {
                        Selection.activeObject = problem.context;
                        EditorGUIUtility.PingObject(problem.context);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void Check() => problems = B4DValidator.Validate(campaign);

        void ExportToFile()
        {
            Check();
            if (problems.Any(p => p.level == B4DLevel.Error))
            {
                EditorUtility.DisplayDialog("Cannot export", "Fix the errors first.", "OK");
                return;
            }
            var path = EditorUtility.SaveFilePanel("Export campaign JSON",
                B4DProjectPaths.EnsureMaps(), $"{campaign.id}.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            // Any mesh not carried inline is copied to an assets folder beside the map.
            B4DExporter.OutputDirectory = Path.GetDirectoryName(path);
            File.WriteAllText(path, B4DExporter.Export(campaign));
            lastWrittenPath = path;
            AssetDatabase.Refresh();
        }

        void CreateCampaign()
        {
            var go = new GameObject("Campaign");
            Undo.RegisterCreatedObjectUndo(go, "Create Campaign");
            campaign = go.AddComponent<B4DCampaign>();
            Selection.activeGameObject = go;
        }
    }

    /// Palette. Everything lands under the campaign root and next to the view,
    /// so pieces can be dropped straight into the level.
    public static class B4DPalette
    {
        [MenuItem("GameObject/Blox 4 Dead/Zone", false, 10)] static void Zone() => Spawn<B4DZone>("Zone");
        [MenuItem("GameObject/Blox 4 Dead/Objective", false, 11)] static void Objective() => Spawn<B4DObjective>("Objective");
        [MenuItem("GameObject/Blox 4 Dead/Gate", false, 12)] static void Gate() => Spawn<B4DGate>("Gate");
        [MenuItem("GameObject/Blox 4 Dead/Fuel barrel", false, 13)] static void Barrel() => Spawn<B4DBarrel>("Fuel barrel");
        [MenuItem("GameObject/Blox 4 Dead/Drop hazard", false, 14)] static void Drop() => Spawn<B4DDropHazard>("Drop hazard");
        [MenuItem("GameObject/Blox 4 Dead/Prop", false, 15)] static void Prop()
        {
            var prop = Spawn<B4DProp>("Prop");
            prop.SyncFieldsToType();
        }

        static T Spawn<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            var root = Object.FindObjectOfType<B4DCampaign>();
            if (root != null) go.transform.SetParent(root.transform, true);

            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                var pivot = view.pivot;
                go.transform.position = new Vector3(Mathf.Round(pivot.x), 0f, Mathf.Round(pivot.z));
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            Selection.activeGameObject = go;
            return go.AddComponent<T>();
        }
    }
}
