// Reference stubs for the Unity runtime API.
//
// There is no Unity on a build machine, so there is no UnityEngine.dll to
// compile against. These declare the same signatures Unity does, which is
// enough for the compiler to check types, names, overloads and generics across
// the whole package.
//
// What a clean build here proves: the package is syntactically valid and type
// correct, every member it calls exists with the arity and types it uses, and
// nothing relies on a language feature past what Unity 2021.3 accepts.
//
// What it cannot prove: that a signature below matches the real Unity one. The
// surface is written from the documented API, but a divergence here would show
// up as a false pass. Treat this as a strong static check, not as a substitute
// for opening the project.
//
// This folder ends in ~ so Unity ignores it entirely.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    // ---------------------------------------------------------------- objects

    public class Object
    {
        public string name { get; set; } = "";
        public HideFlags hideFlags { get; set; }
        public int GetInstanceID() => 0;
        public override string ToString() => name;

        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float delay) { }
        public static void DestroyImmediate(Object o) { }
        public static void DestroyImmediate(Object o, bool allowDestroyingAssets) { }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static T FindObjectOfType<T>() where T : Object => default;
        public static T[] FindObjectsOfType<T>() where T : Object => new T[0];
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => 0;
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);
    }

    public enum HideFlags { None = 0, HideInHierarchy = 1, DontSave = 4 }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject => default;
    }

    public class TextAsset : Object
    {
        public string text => "";
        public byte[] bytes => new byte[0];
    }

    // ---------------------------------------------------------------- vectors

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector2 normalized => this;
        public static Vector2 zero => default;
        public static Vector2 one => default;
        public static float Distance(Vector2 a, Vector2 b) => 0f;
        public static float Dot(Vector2 a, Vector2 b) => 0f;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a;
        public static Vector2 operator +(Vector2 a, Vector2 b) => a;
        public static Vector2 operator -(Vector2 a, Vector2 b) => a;
        public static Vector2 operator -(Vector2 a) => a;
        public static Vector2 operator *(Vector2 a, float b) => a;
        public static Vector2 operator /(Vector2 a, float b) => a;
        public static bool operator ==(Vector2 a, Vector2 b) => false;
        public static bool operator !=(Vector2 a, Vector2 b) => true;
        public override bool Equals(object o) => false;
        public override int GetHashCode() => 0;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0f; }
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector3 normalized => this;
        public static Vector3 zero => default;
        public static Vector3 one => default;
        public static Vector3 up => default;
        public static Vector3 down => default;
        public static Vector3 right => default;
        public static Vector3 left => default;
        public static Vector3 forward => default;
        public static Vector3 back => default;
        public static float Distance(Vector3 a, Vector3 b) => 0f;
        public static float Dot(Vector3 a, Vector3 b) => 0f;
        public static Vector3 Cross(Vector3 a, Vector3 b) => a;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 Scale(Vector3 a, Vector3 b) => a;
        public static Vector3 Normalize(Vector3 a) => a;
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a) => a;
        public static Vector3 operator *(Vector3 a, float b) => a;
        public static Vector3 operator *(float a, Vector3 b) => b;
        public static Vector3 operator /(Vector3 a, float b) => a;
        public static bool operator ==(Vector3 a, Vector3 b) => false;
        public static bool operator !=(Vector3 a, Vector3 b) => true;
        public override bool Equals(object o) => false;
        public override int GetHashCode() => 0;
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Vector4 zero => default;
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public Vector3 eulerAngles { get => default; set { } }
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => default;
        public static Quaternion Euler(Vector3 euler) => default;
        public static Quaternion AngleAxis(float angle, Vector3 axis) => default;
        public static Quaternion LookRotation(Vector3 forward) => default;
        public static Quaternion LookRotation(Vector3 forward, Vector3 up) => default;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion Inverse(Quaternion q) => q;
        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
        public static Vector3 operator *(Quaternion a, Vector3 b) => b;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color linear => this;
        public Color gamma => this;
        public float grayscale => 0f;
        public float maxColorComponent => 0f;
        public static Color white => default;
        public static Color black => default;
        public static Color clear => default;
        public static Color red => default;
        public static Color green => default;
        public static Color blue => default;
        public static Color yellow => default;
        public static Color gray => default;
        public static Color Lerp(Color a, Color b, float t) => a;
        public static Color operator *(Color a, float b) => a;
        public static Color operator +(Color a, Color b) => a;
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) => default;
        public static implicit operator Color(Color32 c) => default;
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        public float x, y, width, height;
        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
        public Vector2 center => default;
        public Vector2 size => default;
        public Vector2 position => default;
        public bool Contains(Vector2 point) => false;
        public static Rect zero => default;
    }

    public struct Bounds
    {
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; extents = default; min = default; max = default; }
        public Vector3 center, extents, size, min, max;
        public void Encapsulate(Bounds other) { }
        public void Encapsulate(Vector3 point) { }
        public void Expand(float amount) { }
        public void SetMinMax(Vector3 min, Vector3 max) { }
        public bool Contains(Vector3 point) => false;
        public bool Intersects(Bounds other) => false;
    }

    public struct Matrix4x4
    {
        public Vector3 MultiplyPoint(Vector3 point) => point;
        public Vector3 MultiplyPoint3x4(Vector3 point) => point;
        public Vector3 MultiplyVector(Vector3 vector) => vector;
        public Matrix4x4 inverse => this;
        public Matrix4x4 transpose => this;
        public Quaternion rotation => default;
        public Vector3 lossyScale => default;
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) => default;
        public static Matrix4x4 Scale(Vector3 s) => default;
        public static Matrix4x4 identity => default;
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => a;
    }

    public struct Ray
    {
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction; }
        public Vector3 origin, direction;
        public Vector3 GetPoint(float distance) => origin;
    }

    public struct Plane
    {
        public Plane(Vector3 normal, Vector3 point) { this.normal = normal; distance = 0f; }
        public Vector3 normal;
        public float distance;
        public bool Raycast(Ray ray, out float enter) { enter = 0f; return false; }
    }

    public static class Mathf
    {
        public const float PI = 3.14159274f;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Deg2Rad = 0.0174532924f;
        public const float Rad2Deg = 57.29578f;
        public const float Epsilon = 1.401298E-45f;

        public static float Abs(float f) => f;
        public static int Abs(int f) => f;
        public static float Max(float a, float b) => a;
        public static float Max(params float[] values) => 0f;
        public static int Max(int a, int b) => a;
        public static float Min(float a, float b) => a;
        public static int Min(int a, int b) => a;
        public static float Clamp(float value, float min, float max) => value;
        public static int Clamp(int value, int min, int max) => value;
        public static float Clamp01(float value) => value;
        public static float Lerp(float a, float b, float t) => a;
        public static float InverseLerp(float a, float b, float value) => 0f;
        public static float Round(float f) => f;
        public static int RoundToInt(float f) => 0;
        public static float Floor(float f) => f;
        public static int FloorToInt(float f) => 0;
        public static float Ceil(float f) => f;
        public static int CeilToInt(float f) => 0;
        public static float Sqrt(float f) => f;
        public static float Pow(float f, float p) => f;
        public static float Sin(float f) => f;
        public static float Cos(float f) => f;
        public static float Tan(float f) => f;
        public static float Atan2(float y, float x) => 0f;
        public static float Sign(float f) => 1f;
        public static bool Approximately(float a, float b) => false;
        public static float Repeat(float t, float length) => t;
        public static float MoveTowards(float current, float target, float maxDelta) => target;
    }

    public static class Random
    {
        public static float value => 0f;
        public static float Range(float min, float max) => min;
        public static int Range(int min, int max) => min;
        public static Vector3 insideUnitSphere => default;
    }

    public static class Time
    {
        public static float time => 0f;
        public static float deltaTime => 0f;
        public static float realtimeSinceStartup => 0f;
    }

    public static class Application
    {
        public static string dataPath => "";
        public static string persistentDataPath => "";
        public static bool isPlaying => false;
    }

    // ------------------------------------------------------------- components

    public class Component : Object
    {
        public Transform transform => null;
        public GameObject gameObject => null;
        public string tag { get; set; }
        public bool CompareTag(string tag) => false;
        public T GetComponent<T>() => default;
        public Component GetComponent(Type type) => null;
        public T GetComponentInParent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T GetComponentInChildren<T>(bool includeInactive) => default;
        public T[] GetComponents<T>() => new T[0];
        public T[] GetComponentsInParent<T>() => new T[0];
        public T[] GetComponentsInChildren<T>() => new T[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) => new T[0];
        public bool TryGetComponent<T>(out T component) { component = default; return false; }
    }

    public class Behaviour : Component { public bool enabled { get; set; } public bool isActiveAndEnabled => false; }
    public class MonoBehaviour : Behaviour { }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale => default;
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 forward { get; set; }
        public Vector3 right { get; set; }
        public Vector3 up { get; set; }
        public Transform parent { get; set; }
        public Transform root => null;
        public int childCount => 0;
        public Matrix4x4 worldToLocalMatrix => default;
        public Matrix4x4 localToWorldMatrix => default;
        public Transform GetChild(int index) => null;
        public Transform Find(string name) => null;
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetSiblingIndex(int index) { }
        public Vector3 TransformPoint(Vector3 position) => position;
        public Vector3 InverseTransformPoint(Vector3 position) => position;
        public Vector3 TransformDirection(Vector3 direction) => direction;
        public void LookAt(Transform target) { }
        public void Translate(Vector3 translation) { }
        public void Rotate(Vector3 eulers) { }
        public IEnumerator GetEnumerator() => null;
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        public GameObject(string name, params Type[] components) { this.name = name; }
        public Transform transform => null;
        public bool activeSelf => false;
        public bool activeInHierarchy => false;
        public int layer { get; set; }
        public string tag { get; set; }
        public GameObject scene => null;
        public void SetActive(bool value) { }
        public bool CompareTag(string tag) => false;
        public T AddComponent<T>() where T : Component => default;
        public Component AddComponent(Type type) => null;
        public T GetComponent<T>() => default;
        public T GetComponentInParent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T GetComponentInChildren<T>(bool includeInactive) => default;
        public T[] GetComponents<T>() => new T[0];
        public T[] GetComponentsInChildren<T>() => new T[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) => new T[0];
        public bool TryGetComponent<T>(out T component) { component = default; return false; }
        public static GameObject Find(string name) => null;
    }

    // ---------------------------------------------------------------- meshes

    public class Renderer : Component
    {
        // Renderer carries its own enabled flag; it does not inherit Behaviour.
        public bool enabled { get; set; }
        public bool isVisible => false;
        public Bounds bounds => default;
        public Material material { get; set; }
        public Material[] materials { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] sharedMaterials { get; set; }
        public bool receiveShadows { get; set; }
    }

    public class Mesh : Object
    {
        public Mesh() { }
        public Vector3[] vertices { get; set; }
        public Vector3[] normals { get; set; }
        public Vector4[] tangents { get; set; }
        public Vector2[] uv { get; set; }
        public Vector2[] uv2 { get; set; }
        public Color[] colors { get; set; }
        public Color32[] colors32 { get; set; }
        public int[] triangles { get; set; }
        public int vertexCount => 0;
        public int subMeshCount { get; set; }
        public Bounds bounds { get; set; }
        public bool isReadable => true;
        public int[] GetTriangles(int submesh) => new int[0];
        public void SetTriangles(int[] triangles, int submesh) { }
        public void Clear() { }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void UploadMeshData(bool markNoLongerReadable) { }
    }

    public class MeshFilter : Component
    {
        public Mesh mesh { get; set; }
        public Mesh sharedMesh { get; set; }
    }

    public class MeshRenderer : Renderer { }

    public class SkinnedMeshRenderer : Renderer
    {
        public Mesh sharedMesh { get; set; }
        public Transform rootBone { get; set; }
        public Transform[] bones { get; set; }
        public void BakeMesh(Mesh mesh) { }
        public void BakeMesh(Mesh mesh, bool useScale) { }
    }

    // ------------------------------------------------------------- materials

    public class Shader : Object
    {
        public int passCount => 0;
        public static Shader Find(string name) => null;
        public static int PropertyToID(string name) => 0;
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }
        public Shader shader { get; set; }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public bool HasProperty(string name) => false;
        public bool HasProperty(int nameID) => false;
        public Color GetColor(string name) => default;
        public float GetFloat(string name) => 0f;
        public int GetInt(string name) => 0;
        public Texture GetTexture(string name) => null;
        public Vector4 GetVector(string name) => default;
        public void SetColor(string name, Color value) { }
        public void SetFloat(string name, float value) { }
        public void SetTexture(string name, Texture value) { }
    }

    public class Texture : Object
    {
        public int width { get; set; }
        public int height { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
    }

    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp, Mirror }
    public enum TextureFormat { Alpha8, RGB24, RGBA32, ARGB32, DXT1, DXT5 }
    public enum RenderTextureFormat { ARGB32, Default, RFloat }
    public enum RenderTextureReadWrite { Default, sRGB, Linear }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain, bool linear) { }
        public TextureFormat format => TextureFormat.RGBA32;
        public bool isReadable => true;
        public void ReadPixels(Rect source, int destX, int destY) { }
        public void ReadPixels(Rect source, int destX, int destY, bool recalculateMipMaps) { }
        public void Apply() { }
        public void Apply(bool updateMipmaps) { }
        public Color GetPixel(int x, int y) => default;
        public Color[] GetPixels() => new Color[0];
        public void SetPixels(Color[] colors) { }
        public void SetPixel(int x, int y, Color color) { }
        public byte[] EncodeToPNG() => new byte[0];
        public byte[] EncodeToJPG() => new byte[0];
        public byte[] EncodeToJPG(int quality) => new byte[0];
        public bool LoadImage(byte[] data) => false;
    }

    public class RenderTexture : Texture
    {
        public RenderTexture(int width, int height, int depth) { }
        public static RenderTexture active { get; set; }
        public static RenderTexture GetTemporary(int width, int height) => null;
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer) => null;
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format) => null;
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite) => null;
        public static void ReleaseTemporary(RenderTexture temp) { }
        public void Release() { }
    }

    public static class Graphics
    {
        public static void Blit(Texture source, RenderTexture dest) { }
        public static void Blit(Texture source, RenderTexture dest, Material mat) { }
    }

    // ---------------------------------------------------------------- drawing

    public static class Debug
    {
        public static void Log(object message) { }
        public static void Log(object message, Object context) { }
        public static void LogWarning(object message) { }
        public static void LogWarning(object message, Object context) { }
        public static void LogError(object message) { }
        public static void LogError(object message, Object context) { }
        public static void LogException(Exception exception) { }
        public static void LogException(Exception exception, Object context) { }
        public static void DrawLine(Vector3 start, Vector3 end) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color) { }
        public static void Assert(bool condition) { }
    }

    public static class Gizmos
    {
        public static Color color { get; set; }
        public static Matrix4x4 matrix { get; set; }
        public static void DrawCube(Vector3 center, Vector3 size) { }
        public static void DrawWireCube(Vector3 center, Vector3 size) { }
        public static void DrawSphere(Vector3 center, float radius) { }
        public static void DrawWireSphere(Vector3 center, float radius) { }
        public static void DrawLine(Vector3 from, Vector3 to) { }
        public static void DrawRay(Vector3 from, Vector3 direction) { }
        public static void DrawIcon(Vector3 center, string name) { }
    }

    // -------------------------------------------------------------- colliders

    public class Collider : Component
    {
        public bool enabled { get; set; }
        public bool isTrigger { get; set; }
        public Bounds bounds => default;
        public Material sharedMaterial { get; set; }
    }

    public class BoxCollider : Collider
    {
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
    }

    public class SphereCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; }
    }

    public class CapsuleCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; }
        public float height { get; set; }
        public int direction { get; set; }
    }

    public class MeshCollider : Collider
    {
        public Mesh sharedMesh { get; set; }
        public bool convex { get; set; }
    }

    public class TerrainCollider : Collider { }

    // ------------------------------------------------------------- attributes

    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string tooltip) { } }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    [AttributeUsage(AttributeTargets.Field)] public class MinAttribute : Attribute { public MinAttribute(float min) { } }
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class SpaceAttribute : Attribute { public SpaceAttribute() { } public SpaceAttribute(float height) { } }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : Attribute { public HeaderAttribute(string header) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TextAreaAttribute : Attribute { public TextAreaAttribute() { } public TextAreaAttribute(int minLines, int maxLines) { } }
    [AttributeUsage(AttributeTargets.Class)] public class AddComponentMenu : Attribute { public AddComponentMenu(string menuName) { } public AddComponentMenu(string menuName, int order) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    [AttributeUsage(AttributeTargets.Class)] public class ExecuteInEditMode : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class ExecuteAlways : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class SelectionBaseAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; public int order; }
}
