// Minimal UnityEngine/UnityEditor/uGUI/TMP stand-ins, only to type-check client scripts outside the Editor.
// They mirror the signatures the client uses; behaviour is empty. Extend them when the client uses new Unity APIs.
#nullable enable
#pragma warning disable CS0067, CS8618, CS0414, CS0649, CS0108, CS0114
using System;
using System.Collections;
using System.Collections.Generic;
internal static class StubPaths
{
    /// <summary>client/DotnetCheck, found by walking up from the test binaries.</summary>
    public static string ProjectDirectory
    {
        get
        {
            var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Bloomlings.Client.DotnetCheck.csproj")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new System.IO.DirectoryNotFoundException("client/DotnetCheck not found.");
        }
    }
}

namespace UnityEngine
{
    public class Object { public string name { get; set; } = ""; public HideFlags hideFlags { get; set; } public static void DontDestroyOnLoad(Object target) { } public static void Destroy(Object o) { } public static T? FindAnyObjectByType<T>() where T : Object => null; public static implicit operator bool(Object? o) => o != null; }
    public enum HideFlags { None, DontSave }
    public class Component : Object { public GameObject gameObject => null!; public Transform transform => null!; public T GetComponentInChildren<T>() => default!; public Component GetComponent(Type t) => null!; public T GetComponent<T>() => default!; public string tag { get; set; } = ""; }
    public class Behaviour : Component { public bool enabled { get; set; } public bool isActiveAndEnabled => true; }
    public class MonoBehaviour : Behaviour { public Coroutine StartCoroutine(IEnumerator r) => null!; public void StopCoroutine(Coroutine c) { } }
    public sealed class Coroutine { }
    public class Transform : Component, IEnumerable { public Vector3 position { get; set; } public Vector3 localScale { get; set; } public void SetParent(Transform p, bool w) { } public int childCount => 0; public Transform GetChild(int i) => null!; public void SetAsFirstSibling() { } public void SetAsLastSibling() { } public Transform parent => null!; public IEnumerator GetEnumerator() => null!; }
    public sealed class RectTransform : Transform { public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, pivot, sizeDelta, anchoredPosition; public Rect rect => default; }
    public class Camera : Behaviour { public bool orthographic; public CameraClearFlags clearFlags; public Color backgroundColor; }
    public enum CameraClearFlags { SolidColor }
    public struct Rect { public Rect(float x, float y, float w, float h) { width = w; height = h; } public float width, height; }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b) : this(r, g, b, 1f) { } public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color white => default; public static Color black => default; public static Color gray => default; public static Color clear => default; public static Color Lerp(Color a, Color b, float t) => a; public static implicit operator Color32(Color c) => default; }
    public struct Color32 { public Color32(byte r, byte g, byte b, byte a) { } }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public static Vector2 zero => default; public static Vector2 one => default; public static Vector2 operator +(Vector2 a, Vector2 b) => a; public static Vector2 operator *(Vector2 a, float b) => a; public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a; }
    public struct Vector3 { public Vector3(float x, float y, float z) { } public static Vector3 zero => default; public static Vector3 one => default; public static Vector3 operator *(Vector3 a, float b) => a; }
    public struct Vector4 { public Vector4(float x, float y, float z, float w) { } public static Vector4 zero => default; public static bool operator ==(Vector4 a, Vector4 b) => true; public static bool operator !=(Vector4 a, Vector4 b) => false; public override bool Equals(object? o) => true; public override int GetHashCode() => 0; }
    public static class Mathf { public const float PI = 3.14f; public static float Atan2(float y, float x) => 0; public static float Min(float a, float b) => a; public static int Min(int a, int b) => a; public static float Max(float a, float b) => a; public static int Max(int a, int b) => a; public static float Clamp01(float v) => v; public static float Clamp(float v, float a, float b) => v; public static float Abs(float v) => v; public static float Sqrt(float v) => v; public static float Sin(float v) => v; public static float Cos(float v) => v; public static float Floor(float v) => v; public static int FloorToInt(float v) => 0; public static int CeilToInt(float v) => 0; }
    public class Texture { public FilterMode filterMode { get; set; } public TextureWrapMode wrapMode { get; set; } public string name { get; set; } = ""; public HideFlags hideFlags { get; set; } }
    public sealed class Texture2D : Texture { public Texture2D(int w, int h, TextureFormat f, bool mip) { } public void SetPixels32(Color32[] p) { } public void Apply(bool a, bool b) { } public static implicit operator Object(Texture2D t) => null!; }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Bilinear }
    public enum TextureWrapMode { Clamp }
    public enum SpriteMeshType { FullRect }
    public sealed class Sprite : Object { public Vector4 border => default; public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint ex, SpriteMeshType m, Vector4 b) => null!; }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject => default!; }
    public sealed class CreateAssetMenuAttribute : Attribute { public string menuName = ""; public string fileName = ""; }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    public sealed class SerializeField : Attribute { }
    public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    public static class Time { public static float unscaledDeltaTime => 0; public static float unscaledTime => 0; }
    public static class ColorUtility { public static bool TryParseHtmlString(string s, out Color c) { c = default; return true; } }
    public class GameObject : Object { public GameObject(string name, params Type[] components) { } public string tag { get; set; } = ""; public T AddComponent<T>() where T : Component => null!; public Transform transform => null!; public void SetActive(bool v) { } public bool activeSelf => true; }
    public enum RuntimePlatform { Android, IPhonePlayer }
    public static class Application { public static string streamingAssetsPath => ""; public static string dataPath => System.IO.Path.GetFullPath(System.IO.Path.Combine(StubPaths.ProjectDirectory, "..", "Assets")); public static string persistentDataPath => System.IO.Path.GetTempPath(); public static bool isEditor => true; public static RuntimePlatform platform => RuntimePlatform.Android; public static int targetFrameRate { get; set; } public static bool CanStreamedLevelBeLoaded(string name) => false; }
    public static class Debug { public static bool isDebugBuild => true; public static void Log(object m) { } public static void LogWarning(object m) { } public static void LogError(object m) { } public static void LogException(Exception e) { } }
    public class CustomYieldInstruction : IEnumerator { public object? Current => null; public bool MoveNext() => false; public void Reset() { } }
    public sealed class WaitUntil : CustomYieldInstruction { public WaitUntil(Func<bool> predicate) { } }
    public sealed class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float s) { } }
    public class AsyncOperation { }
    public enum RenderMode { ScreenSpaceOverlay }
    public sealed class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; public static void ForceUpdateCanvases() { } }
}
namespace UnityEngine { public static class GUILayout { public static bool Button(string text) => false; } }
namespace UnityEngine.Events { public delegate void UnityAction(); public class UnityEvent { public void AddListener(UnityAction a) { } } }
namespace UnityEngine.UI
{
    public class Graphic : Behaviour { public Color color { get; set; } public bool raycastTarget { get; set; } public RectTransform rectTransform => null!; }
    public class Image : Graphic { public enum Type { Simple, Sliced } public Sprite? sprite { get; set; } public Type type { get; set; } public bool preserveAspect { get; set; } }
    public sealed class RawImage : Graphic { public Texture? texture { get; set; } }
    public class Selectable : Behaviour { public bool interactable { get; set; } public Graphic? targetGraphic { get; set; } }
    public sealed class Button : Selectable { public sealed class ButtonClickedEvent : UnityEngine.Events.UnityEvent { } public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent(); }
    public sealed class CanvasScaler : Behaviour { public enum ScaleMode { ScaleWithScreenSize } public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight; }
    public sealed class GraphicRaycaster : Behaviour { }
}
namespace UnityEngine.EventSystems { public sealed class EventSystem : Behaviour { public static EventSystem? current => null; } public sealed class StandaloneInputModule : Behaviour { } }
namespace TMPro
{
    public enum TextAlignmentOptions { Center, Left }
    public enum TextWrappingModes { NoWrap }
    [Flags] public enum FontStyles { Normal = 0, Bold = 1 }
    public class TextMeshProUGUI : UnityEngine.UI.Graphic { public string text { get; set; } = ""; public float fontSize { get; set; } public TextAlignmentOptions alignment { get; set; } public TextWrappingModes textWrappingMode { get; set; } public FontStyles fontStyle { get; set; } }
}
namespace UnityEngine.SceneManagement { public struct Scene { } public static class SceneManager { public static void LoadScene(string name) { } public static void LoadScene(int index) { } } }
namespace UnityEngine.Networking
{
    public sealed class DownloadHandler { public byte[] data => Array.Empty<byte>(); }
    public sealed class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }
        public static UnityWebRequest Get(string uri) => new UnityWebRequest();
        public UnityEngine.AsyncOperation SendWebRequest() => new UnityEngine.AsyncOperation();
        public Result result => Result.Success;
        public string? error => null;
        public DownloadHandler downloadHandler => new DownloadHandler();
        public void Dispose() { }
    }
}
namespace UnityEditor
{
    public sealed class MenuItem : Attribute { public MenuItem(string path) { } }
    public sealed class EditorBuildSettingsScene { public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; } public string path; }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } = Array.Empty<EditorBuildSettingsScene>(); }
    public static class EditorPrefs { public static string GetString(string k, string d) => d; public static void SetString(string k, string v) { } }
    public static class EditorApplication { public static bool isPlaying { get; set; } }
    public class EditorWindow : UnityEngine.ScriptableObject { public static T GetWindow<T>(bool utility, string title) where T : EditorWindow => default!; }
    public static class EditorGUILayout { public static void LabelField(string s) { } public static int IntField(string l, int v) => v; }
    public static class EditorUtility { public static string OpenFilePanel(string t, string d, string e) => ""; public static void SetDirty(UnityEngine.Object o) { } }
    public static class AssetDatabase { public static T? LoadAssetAtPath<T>(string p) where T : UnityEngine.Object => null; public static void CreateAsset(UnityEngine.Object o, string p) { } public static void SaveAssets() { } }
    public static class Selection { public static UnityEngine.Object? activeObject { get; set; } }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene }
    public enum NewSceneMode { Single }
    public static class EditorSceneManager
    {
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m) => default;
        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene, string path) => true;
        public static UnityEngine.SceneManagement.Scene OpenScene(string path) => default;
    }
}
