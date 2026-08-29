using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace B4D
{
    /// Zones are drawn as boxes you can drag by the face, the way a brush works
    /// in a level editor. A zone that connects to nothing is drawn in red, since
    /// that is the one mistake that makes a map unplayable.
    [CustomEditor(typeof(B4DZone))]
    [CanEditMultipleObjects]
    public class B4DZoneEditor : Editor
    {
        BoxBoundsHandle handle = new BoxBoundsHandle();

        void OnSceneGUI()
        {
            var zone = (B4DZone)target;
            var height = zone.roof.use ? zone.roof.value : 3f;

            handle.center = new Vector3(0f, height * 0.5f, 0f);
            handle.size = new Vector3(zone.halfX * 2f, height, zone.halfZ * 2f);
            handle.SetColor(Connected(zone) ? new Color(0.4f, 0.85f, 1f) : Color.red);

            using (new Handles.DrawingScope(Matrix4x4.TRS(zone.transform.position, Quaternion.identity, Vector3.one)))
            {
                EditorGUI.BeginChangeCheck();
                handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Resize Zone");
                    Undo.RecordObject(zone.transform, "Resize Zone");
                    zone.halfX = Mathf.Max(0.5f, handle.size.x * 0.5f);
                    zone.halfZ = Mathf.Max(0.5f, handle.size.z * 0.5f);
                    // Dragging one face moves the centre, so push that back onto the transform.
                    var shift = new Vector3(handle.center.x, 0f, handle.center.z);
                    zone.transform.position += shift;
                }
                Handles.Label(new Vector3(0f, height + 1f, 0f), zone.zoneName);
            }
        }

        static bool Connected(B4DZone zone)
        {
            var all = Object.FindObjectsOfType<B4DZone>();
            if (all.Length < 2) return true;
            foreach (var other in all) if (zone.ConnectsTo(other)) return true;
            return false;
        }
    }

    /// Everything else gets a marker in the scene view so a map reads at a glance
    /// without having to click each object.
    public static class B4DMarkerGizmos
    {
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawZone(B4DZone zone, GizmoType type)
        {
            var height = zone.roof.use ? zone.roof.value : 3f;
            Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.08f);
            Gizmos.matrix = Matrix4x4.TRS(zone.transform.position, Quaternion.identity, Vector3.one);
            Gizmos.DrawCube(new Vector3(0f, height * 0.5f, 0f), new Vector3(zone.halfX * 2f, height, zone.halfZ * 2f));
            Gizmos.matrix = Matrix4x4.identity;
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawObjective(B4DObjective objective, GizmoType type)
        {
            var pos = objective.transform.position;
            Gizmos.color = new Color(1f, 0.7f, 0.24f);
            Gizmos.DrawWireCube(pos + Vector3.up, new Vector3(1.7f, 2.1f, 1.1f));
            Handles.color = Gizmos.color;
            Handles.DrawWireDisc(pos, Vector3.up, 5.2f);
            Handles.Label(pos + Vector3.up * 3.2f, $"Ch{objective.chapter} {objective.label}");

            foreach (var node in objective.nodes)
            {
                var nodePos = pos + new Vector3(node.dx, 0f, node.dz);
                Gizmos.DrawWireCube(nodePos + Vector3.up * 0.8f, new Vector3(0.9f, 1.6f, 0.9f));
                Handles.DrawDottedLine(pos, nodePos, 3f);
                Handles.Label(nodePos + Vector3.up * 2f, node.label);
            }

            if (objective.cartTo != null)
            {
                Handles.color = new Color(0.4f, 1f, 0.5f);
                Handles.DrawDottedLine(pos, objective.cartTo.position, 5f);
                Handles.DrawWireDisc(objective.cartTo.position, Vector3.up, 6.4f);
            }
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawGate(B4DGate gate, GizmoType type)
        {
            var pos = gate.transform.position;
            Gizmos.color = new Color(0.9f, 0.3f, 0.3f);
            Gizmos.DrawWireCube(pos + Vector3.up * 2.5f, new Vector3(gate.width, 5f, 0.6f));
            Handles.Label(pos + Vector3.up * 5.5f, $"Gate {gate.chapter}");
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawBarrel(B4DBarrel barrel, GizmoType type)
        {
            Gizmos.color = barrel.color;
            Gizmos.DrawWireCube(barrel.transform.position + Vector3.up * 0.8f, new Vector3(1.1f, 1.6f, 1.1f));
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawDrop(B4DDropHazard drop, GizmoType type)
        {
            var pos = drop.transform.position;
            Gizmos.color = drop.color;
            Gizmos.DrawWireCube(pos + Vector3.up * drop.y, new Vector3(drop.width, drop.height, drop.depth));
            var anchor = drop.anchorY.use ? drop.anchorY.value : drop.y + 9f;
            Handles.color = Gizmos.color;
            Handles.DrawLine(pos + Vector3.up * drop.y, pos + Vector3.up * anchor);
            Handles.DrawWireDisc(pos, Vector3.up, drop.radius);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawProp(B4DProp prop, GizmoType type)
        {
            if (!prop.solid) return;
            Gizmos.color = new Color(0.6f, 0.9f, 0.6f, 0.9f);
            Gizmos.matrix = Matrix4x4.TRS(prop.transform.position, prop.transform.rotation, Vector3.one);
            var height = Mathf.Max(0.5f, prop.Get("h", 2f));
            Gizmos.DrawWireCube(new Vector3(0f, height * 0.5f, 0f),
                new Vector3(prop.colliderHalfExtents.x * 2f, height, prop.colliderHalfExtents.y * 2f));
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    /// Shows only the numbers the chosen prop type actually uses.
    [CustomEditor(typeof(B4DProp))]
    [CanEditMultipleObjects]
    public class B4DPropEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var prop = (B4DProp)target;

            EditorGUI.BeginChangeCheck();
            var type = (B4DPropType)EditorGUILayout.EnumPopup("Type", prop.type);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(prop, "Change Prop Type");
                prop.type = type;
                prop.SyncFieldsToType();
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("material"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("castShadow"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("solid"));
            if (prop.solid)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("colliderHalfExtents"), new GUIContent("Half extents"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("colliderKind"), new GUIContent("Collider kind"));
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(prop.type.ToString()), EditorStyles.boldLabel);
            if (prop.values.Count == 0) prop.SyncFieldsToType();

            foreach (var field in prop.values)
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(field.key), field.value);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(prop, "Edit Prop");
                    field.value = value;
                }
            }
        }
    }

    /// Optional numbers show as a checkbox plus a field, so "no roof" stays
    /// distinct from "a roof at height zero".
    [CustomPropertyDrawer(typeof(OptionalFloat))]
    public class OptionalFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var use = property.FindPropertyRelative("use");
            var value = property.FindPropertyRelative("value");

            position = EditorGUI.PrefixLabel(position, label);
            var toggleRect = new Rect(position.x, position.y, 16f, position.height);
            var fieldRect = new Rect(position.x + 20f, position.y, position.width - 20f, position.height);

            use.boolValue = EditorGUI.Toggle(toggleRect, use.boolValue);
            using (new EditorGUI.DisabledScope(!use.boolValue))
                value.floatValue = EditorGUI.FloatField(fieldRect, value.floatValue);
        }
    }
}
