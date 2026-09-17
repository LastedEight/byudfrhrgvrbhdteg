// ---------------------------------------------------------------------------
// Compile-check stubs. NOT part of the mod and never shipped.
//
// These declare just enough of the UnityEngine surface for `mcs` to type-check
// the mod's sources on a machine that has neither Unity nor VTOL VR installed.
// Every member here mirrors the real signature; the bodies are never executed.
// See tools/compile-check/README.md.
// ---------------------------------------------------------------------------
#pragma warning disable 169, 649, 67, 414

using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public float magnitude { get { return 0f; } }
        public float sqrMagnitude { get { return 0f; } }
        public Vector2 normalized { get { return this; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return a; }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return a; }
        public static Vector2 operator *(Vector2 a, float b) { return a; }
        public static Vector2 operator /(Vector2 a, float b) { return a; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 down { get { return new Vector3(0, -1, 0); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public float magnitude { get { return 0f; } }
        public float sqrMagnitude { get { return 0f; } }
        public Vector3 normalized { get { return this; } }
        public static float Distance(Vector3 a, Vector3 b) { return 0f; }
        public static float Dot(Vector3 a, Vector3 b) { return 0f; }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return a; }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a; }
        public static Vector3 Scale(Vector3 a, Vector3 b) { return a; }
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) { return v; }
        public static Vector3 ClampMagnitude(Vector3 v, float m) { return v; }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator -(Vector3 a) { return a; }
        public static Vector3 operator *(Vector3 a, float b) { return a; }
        public static Vector3 operator *(float b, Vector3 a) { return a; }
        public static Vector3 operator /(Vector3 a, float b) { return a; }
        public string ToString(string format) { return ""; }
        public override string ToString() { return ""; }
    }

    public struct Quaternion
    {
        public static Quaternion identity { get { return new Quaternion(); } }
        public static Quaternion Euler(float x, float y, float z) { return identity; }
        public static Quaternion LookRotation(Vector3 forward, Vector3 up) { return identity; }
        public static Quaternion LookRotation(Vector3 forward) { return identity; }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) { return a; }
        public static Quaternion operator *(Quaternion a, Quaternion b) { return a; }
        public static Vector3 operator *(Quaternion a, Vector3 b) { return b; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
        public static Color black { get { return new Color(0, 0, 0); } }
        public static Color clear { get { return new Color(0, 0, 0, 0); } }
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Rad2Deg = 57.29578f;
        public const float Deg2Rad = 0.0174532924f;
        public static float Abs(float v) { return 0f; }
        public static float Sign(float v) { return 0f; }
        public static float Clamp(float v, float a, float b) { return 0f; }
        public static int Clamp(int v, int a, int b) { return 0; }
        public static float Clamp01(float v) { return 0f; }
        public static float Max(float a, float b) { return 0f; }
        public static int Max(int a, int b) { return 0; }
        public static float Min(float a, float b) { return 0f; }
        public static int Min(int a, int b) { return 0; }
        public static float Lerp(float a, float b, float t) { return 0f; }
        public static float Exp(float v) { return 0f; }
        public static float Pow(float a, float b) { return 0f; }
        public static float Sqrt(float v) { return 0f; }
        public static float Sin(float v) { return 0f; }
        public static float Cos(float v) { return 0f; }
        public static float Asin(float v) { return 0f; }
        public static float Atan2(float y, float x) { return 0f; }
        public static float Repeat(float t, float length) { return 0f; }
        public static float MoveTowards(float a, float b, float d) { return 0f; }
        public static float PerlinNoise(float x, float y) { return 0f; }
        public static int RoundToInt(float v) { return 0; }
        public static int FloorToInt(float v) { return 0; }
        public static int CeilToInt(float v) { return 0; }
    }

    public static class Random
    {
        public static float value { get { return 0f; } }
    }

    public static class Time
    {
        public static float deltaTime { get { return 0f; } }
        public static float fixedDeltaTime { get { return 0f; } }
        public static float time { get { return 0f; } }
        public static float unscaledTime { get { return 0f; } }
        public static float unscaledDeltaTime { get { return 0f; } }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public static class PlayerPrefs
    {
        public static float GetFloat(string key, float def) { return def; }
        public static void SetFloat(string key, float value) { }
        public static int GetInt(string key, int def) { return def; }
        public static void SetInt(string key, int value) { }
        public static void Save() { }
    }

    public enum KeyCode { None, W, A, S, D, Q, E, Z, Home, PageUp, PageDown, UpArrow, DownArrow, LeftArrow, RightArrow, F8, F9, F10, F11, F12 }

    public static class Input
    {
        public static bool GetKey(KeyCode key) { return false; }
        public static bool GetKeyDown(KeyCode key) { return false; }
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum Space { World, Self }
    public enum ForceMode { Force, Acceleration, Impulse, VelocityChange }
    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }
    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic, ContinuousSpeculative }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp, Mirror }
    public enum RenderTextureFormat { Default, ARGB32 }
    public enum CameraClearFlags { Skybox, Color, SolidColor, Depth, Nothing }
    public enum StereoTargetEyeMask { None, Left, Right, Both }
    public enum LightType { Spot, Directional, Point, Area }
    public enum AudioRolloffMode { Logarithmic, Linear, Custom }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum TextAlignment { Left, Center, Right }

    public static class LayerMask
    {
        public static string LayerToName(int layer) { return ""; }
        public static int NameToLayer(string name) { return 0; }
    }

    public class Object
    {
        public string name;
        public HideFlags hideFlags;
        public static void Destroy(Object obj) { }
        public static void DestroyImmediate(Object obj) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static Object FindObjectOfType(Type type) { return null; }
        public static Object[] FindObjectsOfType(Type type) { return new Object[0]; }
        public static T FindObjectOfType<T>() where T : Object { return default(T); }
        public static bool operator ==(Object a, Object b) { return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !ReferenceEquals(a, b); }
        public override bool Equals(object other) { return base.Equals(other); }
        public override int GetHashCode() { return base.GetHashCode(); }
    }

    public enum HideFlags { None, HideInHierarchy, DontSave }

    public class Component : Object
    {
        public Transform transform { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public T GetComponent<T>() { return default(T); }
        public Component GetComponent(Type type) { return null; }
        public T GetComponentInParent<T>() { return default(T); }
        public T GetComponentInChildren<T>() { return default(T); }
        public T[] GetComponents<T>() { return new T[0]; }
        public T[] GetComponentsInChildren<T>() { return new T[0]; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return new T[0]; }
        public T AddComponent<T>() where T : Component { return default(T); }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get { return false; } }
    }

    public class MonoBehaviour : Behaviour
    {
        public void Invoke(string method, float time) { }
        public void CancelInvoke() { }
    }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public Transform transform { get { return null; } }
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf { get { return false; } }
        public bool activeInHierarchy { get { return false; } }
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component { return default(T); }
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInChildren<T>() { return default(T); }
        public T[] GetComponents<T>() { return new T[0]; }
        public T[] GetComponentsInChildren<T>() { return new T[0]; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return new T[0]; }
        public static GameObject CreatePrimitive(PrimitiveType type) { return null; }
        public static GameObject Find(string name) { return null; }
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 forward { get { return Vector3.forward; } }
        public Vector3 up { get { return Vector3.up; } }
        public Vector3 right { get { return Vector3.right; } }
        public Transform parent { get; set; }
        public Transform root { get { return null; } }
        public int childCount { get { return 0; } }
        public Transform GetChild(int index) { return null; }
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public bool IsChildOf(Transform parent) { return false; }
        public Vector3 InverseTransformDirection(Vector3 direction) { return direction; }
        public Vector3 TransformDirection(Vector3 direction) { return direction; }
        public Vector3 InverseTransformPoint(Vector3 point) { return point; }
        public Vector3 TransformPoint(Vector3 point) { return point; }
        public void Rotate(float x, float y, float z, Space space) { }
        public void LookAt(Transform target) { }
    }

    public class Rigidbody : Component
    {
        public float mass { get; set; }
        public float drag { get; set; }
        public float angularDrag { get; set; }
        public bool useGravity { get; set; }
        public bool isKinematic { get; set; }
        public float maxAngularVelocity { get; set; }
        public Vector3 velocity { get; set; }
        public Vector3 angularVelocity { get; set; }
        public Vector3 centerOfMass { get; set; }
        public Vector3 worldCenterOfMass { get { return Vector3.zero; } }
        public RigidbodyInterpolation interpolation { get; set; }
        public CollisionDetectionMode collisionDetectionMode { get; set; }
        public void AddForce(Vector3 force, ForceMode mode) { }
        public void AddTorque(Vector3 torque, ForceMode mode) { }
        public void AddRelativeForce(Vector3 force, ForceMode mode) { }
        public void AddRelativeTorque(Vector3 torque, ForceMode mode) { }
        public void WakeUp() { }
        public void Sleep() { }
    }

    public class Collider : Component
    {
        public bool enabled { get; set; }
        public bool isTrigger { get; set; }
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

    public class CapsuleCollider : Collider { }
    public class MeshCollider : Collider { }

    public class Collision
    {
        public Vector3 relativeVelocity { get { return Vector3.zero; } }
        public Collider collider { get { return null; } }
    }

    public struct RaycastHit
    {
        public Vector3 point { get { return Vector3.zero; } }
        public Vector3 normal { get { return Vector3.zero; } }
        public float distance { get { return 0f; } }
        public Transform transform { get { return null; } }
        public Collider collider { get { return null; } }
    }

    public static class Physics
    {
        public static Vector3 gravity { get { return new Vector3(0, -9.81f, 0); } }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance) { hit = new RaycastHit(); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance, int layerMask, QueryTriggerInteraction q) { hit = new RaycastHit(); return false; }
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float distance) { return new RaycastHit[0]; }
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float distance, int layerMask, QueryTriggerInteraction q) { return new RaycastHit[0]; }
        public static void IgnoreCollision(Collider a, Collider b, bool ignore) { }
    }

    public class Texture : Object
    {
        public int width { get; set; }
        public int height { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
    }

    public sealed class Texture2D : Texture
    {
        public Texture2D(int width, int height) { }
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixels(Color[] colors) { }
        public void Apply(bool updateMipmaps) { }
        public void Apply() { }
    }

    public sealed class RenderTexture : Texture
    {
        public RenderTexture(int width, int height, int depth) { }
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format) { }
        public int antiAliasing { get; set; }
        public bool Create() { return true; }
        public void Release() { }
        public static RenderTexture active { get; set; }
    }

    public sealed class Shader : Object
    {
        public static Shader Find(string name) { return null; }
        public static int PropertyToID(string name) { return 0; }
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public Vector2 mainTextureScale { get; set; }
        public Vector2 mainTextureOffset { get; set; }
        public int renderQueue { get; set; }
        public Shader shader { get; set; }
        public void SetFloat(string name, float value) { }
        public void SetInt(string name, int value) { }
        public void SetColor(string name, Color value) { }
        public void SetTexture(string name, Texture value) { }
        public void EnableKeyword(string keyword) { }
    }

    public class Renderer : Component
    {
        public bool enabled { get; set; }
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] materials { get; set; }
    }

    public class MeshRenderer : Renderer { }

    public sealed class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public Vector2[] uv { get; set; }
        public int[] triangles { get; set; }
        public Vector3[] normals { get; set; }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
    }

    public sealed class MeshFilter : Component
    {
        public Mesh mesh { get; set; }
        public Mesh sharedMesh { get; set; }
    }

    public sealed class LineRenderer : Renderer
    {
        public bool useWorldSpace { get; set; }
        public int positionCount { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public void SetPosition(int index, Vector3 position) { }
    }

    public sealed class Font : Object
    {
        public Material material { get; set; }
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) { return null; }
    }

    public sealed class TextMesh : Component
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
        public Color color { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
    }

    public sealed class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float range { get; set; }
        public float intensity { get; set; }
        public float spotAngle { get; set; }
    }

    public sealed class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) { return null; }
        public bool SetData(float[] data, int offsetSamples) { return true; }
    }

    public sealed class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public float spatialBlend { get; set; }
        public float minDistance { get; set; }
        public float maxDistance { get; set; }
        public AudioRolloffMode rolloffMode { get; set; }
        public bool isPlaying { get { return false; } }
        public void Play() { }
        public void Stop() { }
    }

    public sealed class Camera : Behaviour
    {
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public int cullingMask { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public StereoTargetEyeMask stereoTargetEye { get; set; }
        public float depth { get; set; }
        public float aspect { get; set; }
        public RenderTexture targetTexture { get; set; }
        public static Camera main { get { return null; } }
        public static Camera[] allCameras { get { return new Camera[0]; } }
        public void Render() { }
    }

    public static class Screen
    {
        public static int width { get { return 0; } }
        public static int height { get { return 0; } }
    }
}
