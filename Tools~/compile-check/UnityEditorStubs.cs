// Reference stubs for the Unity editor API. Same caveat as UnityStubs.cs: these
// match the signatures the package calls, written from the documented API. A
// divergence here would show up as a false pass, so a clean build is a strong
// static check rather than proof the package runs.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    // ------------------------------------------------------------ inspectors

    public class Editor : ScriptableObject
    {
        public UnityEngine.Object target => null;
        public UnityEngine.Object[] targets => new UnityEngine.Object[0];
        public SerializedObject serializedObject => null;
        public virtual void OnInspectorGUI() { }
        public void DrawDefaultInspector() { }
        public void Repaint() { }
    }

    public class EditorWindow : ScriptableObject
    {
        public string title { get; set; }
        public GUIContent titleContent { get; set; }
        public Rect position { get; set; }
        public static T GetWindow<T>() where T : EditorWindow => default;
        public static T GetWindow<T>(string title) where T : EditorWindow => default;
        public static T GetWindow<T>(bool utility, string title) where T : EditorWindow => default;
        public void Show() { }
        public void Close() { }
        public void Repaint() { }
        public void ShowNotification(GUIContent notification) { }
    }

    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object obj) { }
        public SerializedProperty FindProperty(string propertyPath) => null;
        public void Update() { }
        public bool ApplyModifiedProperties() => false;
    }

    public class SerializedProperty
    {
        public bool boolValue { get; set; }
        public float floatValue { get; set; }
        public int intValue { get; set; }
        public string stringValue { get; set; }
        public Color colorValue { get; set; }
        public Vector2 vector2Value { get; set; }
        public Vector3 vector3Value { get; set; }
        public UnityEngine.Object objectReferenceValue { get; set; }
        public int arraySize { get; set; }
        public bool isExpanded { get; set; }
        public string displayName => "";
        public SerializedProperty FindPropertyRelative(string relativePropertyPath) => null;
        public SerializedProperty GetArrayElementAtIndex(int index) => null;
        public bool NextVisible(bool enterChildren) => false;
    }

    public abstract class PropertyDrawer
    {
        public virtual void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }
        public virtual float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0f;
    }

    // ------------------------------------------------------------ attributes

    public enum GizmoType { Pickable = 1, NotInSelectionHierarchy = 2, NonSelected = 8, Selected = 4, Active = 16, InSelectionHierarchy = 32 }

    [AttributeUsage(AttributeTargets.Class)]
    public class CustomEditor : Attribute
    {
        public CustomEditor(Type inspectedType) { }
        public CustomEditor(Type inspectedType, bool editorForChildClasses) { }
    }

    [AttributeUsage(AttributeTargets.Class)] public class CanEditMultipleObjects : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class CustomPropertyDrawer : Attribute
    {
        public CustomPropertyDrawer(Type type) { }
        public CustomPropertyDrawer(Type type, bool useForChildren) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }
    }

    [AttributeUsage(AttributeTargets.Method)] public class DrawGizmo : Attribute
    {
        public DrawGizmo(GizmoType gizmo) { }
        public DrawGizmo(GizmoType gizmo, Type drawnGizmoType) { }
    }

    [AttributeUsage(AttributeTargets.Method)] public class InitializeOnLoadMethod : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class InitializeOnLoad : Attribute { }

    // ---------------------------------------------------------------- editing

    public static class Selection
    {
        public static GameObject[] gameObjects => new GameObject[0];
        public static UnityEngine.Object[] objects { get; set; }
        public static Transform[] transforms => new Transform[0];
        public static GameObject activeGameObject { get; set; }
        public static Transform activeTransform { get; set; }
        public static UnityEngine.Object activeObject { get; set; }
        public static int count => 0;
    }

    public static class Undo
    {
        public static void RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, string name) { }
        public static void RecordObject(UnityEngine.Object objectToUndo, string name) { }
        public static void RecordObjects(UnityEngine.Object[] objectsToUndo, string name) { }
        public static void RegisterCompleteObjectUndo(UnityEngine.Object objectToUndo, string name) { }
        public static void DestroyObjectImmediate(UnityEngine.Object objectToUndo) { }
        public static void SetTransformParent(Transform transform, Transform newParent, string name) { }
        public static T AddComponent<T>(GameObject gameObject) where T : Component => default;
        public static int GetCurrentGroup() => 0;
        public static void CollapseUndoOperations(int groupIndex) { }
    }

    public static class AssetDatabase
    {
        public static string GetAssetPath(UnityEngine.Object assetObject) => "";
        public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object => default;
        public static UnityEngine.Object LoadAssetAtPath(string assetPath, Type type) => null;
        public static void ImportAsset(string path) { }
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static string[] FindAssets(string filter) => new string[0];
        public static string[] FindAssets(string filter, string[] searchInFolders) => new string[0];
        public static string GUIDToAssetPath(string guid) => "";
        public static string AssetPathToGUID(string path) => "";
        public static bool DeleteAsset(string path) => false;
        public static string GenerateUniqueAssetPath(string path) => path;
    }

    public static class EditorPrefs
    {
        public static int GetInt(string key) => 0;
        public static int GetInt(string key, int defaultValue) => defaultValue;
        public static void SetInt(string key, int value) { }
        public static bool GetBool(string key, bool defaultValue) => defaultValue;
        public static void SetBool(string key, bool value) { }
        public static string GetString(string key, string defaultValue) => defaultValue;
        public static void SetString(string key, string value) { }
        public static float GetFloat(string key, float defaultValue) => defaultValue;
        public static void SetFloat(string key, float value) { }
    }

    public static class EditorUtility
    {
        public static bool DisplayDialog(string title, string message, string ok) => false;
        public static bool DisplayDialog(string title, string message, string ok, string cancel) => false;
        public static int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt) => 0;
        public static string SaveFilePanel(string title, string directory, string defaultName, string extension) => "";
        public static string SaveFilePanelInProject(string title, string defaultName, string extension, string message) => "";
        public static string OpenFilePanel(string title, string directory, string extension) => "";
        public static string OpenFolderPanel(string title, string folder, string defaultName) => "";
        public static void SetDirty(UnityEngine.Object target) { }
        public static void DisplayProgressBar(string title, string info, float progress) { }
        public static bool DisplayCancelableProgressBar(string title, string info, float progress) => false;
        public static void ClearProgressBar() { }
        public static void FocusProjectWindow() { }
    }

    public static class ObjectNames
    {
        public static string NicifyVariableName(string name) => name;
        public static string GetInspectorTitle(UnityEngine.Object obj) => "";
    }

    public static class EditorApplication
    {
        public static bool isPlaying => false;
        public static bool isCompiling => false;
        public static Action update { get; set; }
        public static void delayCall(Action call) { }
    }

    public static class EditorSceneManagerBridge { }

    // ---------------------------------------------------------------- drawing

    public static class Handles
    {
        public static Color color { get; set; }
        public static Matrix4x4 matrix { get; set; }
        public static void Label(Vector3 position, string text) { }
        public static void Label(Vector3 position, GUIContent content) { }
        public static void DrawLine(Vector3 p1, Vector3 p2) { }
        public static void DrawDottedLine(Vector3 p1, Vector3 p2, float screenSpaceSize) { }
        public static void DrawWireDisc(Vector3 center, Vector3 normal, float radius) { }
        public static void DrawSolidDisc(Vector3 center, Vector3 normal, float radius) { }
        public static void DrawAAPolyLine(float width, params Vector3[] points) { }
        public static void DrawWireCube(Vector3 center, Vector3 size) { }
        public static Vector3 PositionHandle(Vector3 position, Quaternion rotation) => position;
        public static bool Button(Vector3 position, Quaternion direction, float size, float pickSize, Action cap) => false;

        public class DrawingScope : IDisposable
        {
            public DrawingScope(Matrix4x4 matrix) { }
            public DrawingScope(Color color) { }
            public void Dispose() { }
        }
    }

    public static class EditorGUI
    {
        public static int indentLevel { get; set; }
        public static bool showMixedValue { get; set; }
        public static void BeginChangeCheck() { }
        public static bool EndChangeCheck() => false;
        public static Rect PrefixLabel(Rect totalPosition, GUIContent label) => totalPosition;
        public static bool Toggle(Rect position, bool value) => value;
        public static float FloatField(Rect position, float value) => value;
        public static int IntField(Rect position, int value) => value;
        public static string TextField(Rect position, string text) => text;
        public static Color ColorField(Rect position, Color value) => value;
        public static void LabelField(Rect position, string label) { }
        public static void PropertyField(Rect position, SerializedProperty property) { }
        public static void PropertyField(Rect position, SerializedProperty property, GUIContent label) { }
        public static float GetPropertyHeight(SerializedProperty property) => 0f;

        public class DisabledScope : IDisposable
        {
            public DisabledScope(bool disabled) { }
            public void Dispose() { }
        }

        public class IndentLevelScope : IDisposable
        {
            public IndentLevelScope() { }
            public IndentLevelScope(int increment) { }
            public void Dispose() { }
        }
    }

    public static class EditorGUIUtility
    {
        public static string systemCopyBuffer { get; set; }
        public static float singleLineHeight => 18f;
        public static float standardVerticalSpacing => 2f;
        public static float labelWidth { get; set; }
        public static void PingObject(UnityEngine.Object obj) { }
        public static GUIContent IconContent(string name) => null;
        public static GUIContent TrTextContent(string text) => null;
    }

    public static class EditorStyles
    {
        public static GUIStyle label => null;
        public static GUIStyle miniLabel => null;
        public static GUIStyle boldLabel => null;
        public static GUIStyle miniBoldLabel => null;
        public static GUIStyle largeLabel => null;
        public static GUIStyle helpBox => null;
        public static GUIStyle wordWrappedLabel => null;
        public static GUIStyle wordWrappedMiniLabel => null;
        public static GUIStyle toolbarButton => null;
        public static GUIStyle foldout => null;
        public static GUIStyle textField => null;
    }

    public enum MessageType { None, Info, Warning, Error }

    public static class EditorGUILayout
    {
        public static void Space() { }
        public static void Space(float width) { }
        public static void LabelField(string label) { }
        public static void LabelField(string label, params GUILayoutOption[] options) { }
        public static void LabelField(string label, GUIStyle style) { }
        public static void LabelField(string label, GUIStyle style, params GUILayoutOption[] options) { }
        public static void LabelField(string label, string label2) { }
        public static void LabelField(GUIContent label, params GUILayoutOption[] options) { }
        public static void HelpBox(string message, MessageType type) { }
        public static void HelpBox(string message, MessageType type, bool wide) { }
        public static UnityEngine.Object ObjectField(string label, UnityEngine.Object obj, Type objType, bool allowSceneObjects) => obj;
        public static UnityEngine.Object ObjectField(string label, UnityEngine.Object obj, Type objType, bool allowSceneObjects, params GUILayoutOption[] options) => obj;
        public static void PropertyField(SerializedProperty property) { }
        public static void PropertyField(SerializedProperty property, GUIContent label) { }
        public static void PropertyField(SerializedProperty property, bool includeChildren) { }
        public static float FloatField(string label, float value) => value;
        public static int IntField(string label, int value) => value;
        public static string TextField(string label, string text) => text;
        public static bool Toggle(string label, bool value) => value;
        public static Color ColorField(string label, Color value) => value;
        public static Vector2 Vector2Field(string label, Vector2 value) => value;
        public static Vector3 Vector3Field(string label, Vector3 value) => value;
        public static float Slider(string label, float value, float left, float right) => value;
        public static Enum EnumPopup(string label, Enum selected) => selected;
        public static int Popup(string label, int selectedIndex, string[] displayedOptions) => selectedIndex;
        public static bool Foldout(bool foldout, string content) => foldout;
        public static Vector2 BeginScrollView(Vector2 scrollPosition) => scrollPosition;
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options) => scrollPosition;
        public static void EndScrollView() { }
        public static void BeginHorizontal() { }
        public static void EndHorizontal() { }
        public static void BeginVertical() { }
        public static void EndVertical() { }

        public class HorizontalScope : IDisposable
        {
            public HorizontalScope() { }
            public HorizontalScope(GUIStyle style) { }
            public HorizontalScope(params GUILayoutOption[] options) { }
            public void Dispose() { }
        }

        public class VerticalScope : IDisposable
        {
            public VerticalScope() { }
            public VerticalScope(GUIStyle style) { }
            public void Dispose() { }
        }
    }

    public class SceneView : EditorWindow
    {
        public static SceneView lastActiveSceneView => null;
        public static SceneView currentDrawingSceneView => null;
        public Vector3 pivot { get; set; }
        public Quaternion rotation { get; set; }
        public float size { get; set; }
        public Camera camera => null;
        public static void RepaintAll() { }
        public void AlignViewToObject(Transform t) { }
    }

    public class Camera : Component { }
}

namespace UnityEditor.IMGUI.Controls
{
    public class BoxBoundsHandle
    {
        public UnityEngine.Vector3 center { get; set; }
        public UnityEngine.Vector3 size { get; set; }
        public UnityEngine.Color wireframeColor { get; set; }
        public UnityEngine.Color handleColor { get; set; }
        public void SetColor(UnityEngine.Color color) { }
        public void DrawHandle() { }
    }

    public class SphereBoundsHandle
    {
        public UnityEngine.Vector3 center { get; set; }
        public float radius { get; set; }
        public void SetColor(UnityEngine.Color color) { }
        public void DrawHandle() { }
    }
}

namespace UnityEngine
{
    // GUI types live in UnityEngine, not UnityEditor.
    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public int fontSize { get; set; }
        public bool wordWrap { get; set; }
        public bool richText { get; set; }
    }

    public class GUIContent
    {
        public GUIContent() { }
        public GUIContent(string text) { }
        public GUIContent(string text, string tooltip) { }
        public string text { get; set; }
        public string tooltip { get; set; }
    }

    public class GUILayoutOption { }

    public static class GUILayout
    {
        public static bool Button(string text) => false;
        public static bool Button(string text, params GUILayoutOption[] options) => false;
        public static bool Button(GUIContent content, params GUILayoutOption[] options) => false;
        public static void Label(string text) { }
        public static void Label(string text, params GUILayoutOption[] options) { }
        public static void Space(float pixels) { }
        public static void FlexibleSpace() { }
        public static bool Toggle(bool value, string text) => value;
        public static string TextField(string text) => text;
        public static void BeginHorizontal() { }
        public static void EndHorizontal() { }
        public static void BeginVertical() { }
        public static void EndVertical() { }
        public static GUILayoutOption Width(float width) => null;
        public static GUILayoutOption Height(float height) => null;
        public static GUILayoutOption ExpandWidth(bool expand) => null;
        public static GUILayoutOption MinWidth(float minWidth) => null;
    }

    public static class GUI
    {
        public static Color color { get; set; }
        public static Color backgroundColor { get; set; }
        public static bool enabled { get; set; }
        public static bool changed { get; set; }
    }
}
