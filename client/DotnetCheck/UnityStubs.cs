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
    public class MonoBehaviour : Behaviour { public void StopAllCoroutines() { } public Coroutine StartCoroutine(IEnumerator r) => null!; public void StopCoroutine(Coroutine c) { } }
    public sealed class Coroutine { }
    public sealed class AudioSource : Behaviour { public AudioClip? clip { get; set; } public bool loop { get; set; } public bool playOnAwake { get; set; } public float volume { get; set; } public float pitch { get; set; } public bool isPlaying => false; public void Play() { } public void Stop() { } public void PlayOneShot(AudioClip clip, float volumeScale) { } }
    public sealed class AudioClip : Object { public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) => new AudioClip(); public bool SetData(float[] data, int offsetSamples) => true; }
    public class Transform : Component, IEnumerable { public Vector3 position { get; set; } public Vector3 localScale { get; set; } public Vector3 localEulerAngles { get; set; } public void SetParent(Transform p, bool w) { } public int childCount => 0; public Transform GetChild(int i) => null!; public void SetAsFirstSibling() { } public void SetAsLastSibling() { } public int GetSiblingIndex() => 0; public void SetSiblingIndex(int index) { } public Transform parent => null!; public Vector3 TransformPoint(Vector3 p) => p; public Vector3 InverseTransformPoint(Vector3 p) => p; public Vector3 TransformVector(Vector3 v) => v; public IEnumerator GetEnumerator() => null!; }
    public sealed class RectTransform : Transform { public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, pivot, sizeDelta, anchoredPosition; public Rect rect => default; public void GetWorldCorners(Vector3[] corners) { } }
    public class Camera : Behaviour { public bool orthographic; public CameraClearFlags clearFlags; public Color backgroundColor; }
    public enum CameraClearFlags { SolidColor }
    public struct Rect { public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; } public float x, y, width, height; public float xMin => x; public float yMin => y; public float xMax => x + width; public float yMax => y + height; }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b) : this(r, g, b, 1f) { } public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color white => default; public static Color black => default; public static Color gray => default; public static Color clear => default; public static Color Lerp(Color a, Color b, float t) => a; public static implicit operator Color32(Color c) => default; public static bool operator ==(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a; public static bool operator !=(Color a, Color b) => !(a == b); public override bool Equals(object? o) => o is Color c && c == this; public override int GetHashCode() => 0; }
    public struct Color32 { public byte r, g, b, a; public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; } }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public static Vector2 zero => default; public static Vector2 one => default; public static Vector2 operator +(Vector2 a, Vector2 b) => a; public static Vector2 operator -(Vector2 a, Vector2 b) => a; public static Vector2 operator *(Vector2 a, float b) => a; public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a; }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } public static Vector3 zero => default; public static Vector3 one => default; public static Vector3 up => default; public static Vector3 operator *(Vector3 a, float b) => a; public static Vector3 operator +(Vector3 a, Vector3 b) => a; public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a; }
    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } public static Vector4 zero => default; public static bool operator ==(Vector4 a, Vector4 b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w; public static bool operator !=(Vector4 a, Vector4 b) => !(a == b); public override bool Equals(object? o) => o is Vector4 v && v == this; public override int GetHashCode() => 0; }
    public static class Mathf { public const float PI = 3.14f; public const float Rad2Deg = 57.29578f; public static float Atan2(float y, float x) => 0; public static float Min(float a, float b) => a; public static int Min(int a, int b) => a; public static float Max(float a, float b) => a; public static int Max(int a, int b) => a; public static float Clamp01(float v) => v; public static float Clamp(float v, float a, float b) => v; public static int Clamp(int v, int a, int b) => v; public static float Abs(float v) => v; public static float Sqrt(float v) => v; public static float Sin(float v) => v; public static float Cos(float v) => v; public static float Floor(float v) => v; public static int FloorToInt(float v) => 0; public static int CeilToInt(float v) => 0; public static float SmoothStep(float a, float b, float t) => t; public static float PingPong(float t, float length) => t; public static float Lerp(float a, float b, float t) => a; public static int RoundToInt(float v) => 0; }
    public class Texture : Object { public int width => 0; public int height => 0; public FilterMode filterMode { get; set; } public TextureWrapMode wrapMode { get; set; } }
    public sealed class Texture2D : Texture { public Texture2D(int w, int h, TextureFormat f, bool mip) { } public void SetPixels32(Color32[] p) { } public void SetPixelData<T>(T[] data, int mipLevel, int sourceDataStartIndex = 0) { } public void Apply(bool a, bool b) { } public void ReadPixels(Rect source, int destX, int destY, bool recalculateMipMaps) { } public byte[] GetRawTextureData() => Array.Empty<byte>(); }
    public sealed class RenderTexture : Texture { public static RenderTexture? active { get; set; } public static RenderTexture GetTemporary(int w, int h, int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite) => null!; public static void ReleaseTemporary(RenderTexture t) { } }
    public enum RenderTextureFormat { ARGB32 }
    public enum RenderTextureReadWrite { Default, Linear, sRGB }
    public static class Graphics { public static void Blit(Texture source, RenderTexture dest) { } }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Bilinear, Trilinear }
    public enum TextureWrapMode { Clamp }
    public enum SpriteMeshType { FullRect, Tight }
    public enum SpriteAlignment { Center, TopLeft, TopCenter, TopRight, LeftCenter, RightCenter, BottomLeft, BottomCenter, BottomRight, Custom }
    public sealed class Sprite : Object { public Vector4 border => default; public Texture2D texture => null!; public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint ex, SpriteMeshType m, Vector4 b) => null!; }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject => default!; }
    public sealed class CreateAssetMenuAttribute : Attribute { public string menuName = ""; public string fileName = ""; }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    public sealed class SerializeField : Attribute { }
    public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    public static class Screen { public static int width => 1080; public static int height => 2340; public static Rect safeArea => new Rect(0, 0, 1080, 2340); }
    public static class Time { public static float unscaledDeltaTime => 0; public static float unscaledTime => 0; }
    public static class ColorUtility { public static bool TryParseHtmlString(string s, out Color c) { c = default; return true; } }
    public class GameObject : Object { public GameObject(string name, params Type[] components) { } public string tag { get; set; } = ""; public T AddComponent<T>() where T : Component => null!; public T GetComponent<T>() => default!; public T GetComponentInChildren<T>() => default!; public Transform transform => null!; public void SetActive(bool v) { } public bool activeSelf => true; public bool activeInHierarchy => true; }
    public enum RuntimePlatform { Android, IPhonePlayer }
    public class TextAsset : Object { public string text => string.Empty; }
    public sealed class Font : Object { }
    public sealed class CanvasGroup : Behaviour { public float alpha { get; set; } public bool interactable { get; set; } public bool blocksRaycasts { get; set; } }
    public class Material : Object { public Material(Material source) { } public Material(Shader shader) { } public void SetColor(string name, Color value) { } public void SetFloat(string name, float value) { } public void EnableKeyword(string keyword) { } public void DisableKeyword(string keyword) { } }
    public sealed class Shader : Object { public static Shader? Find(string name) => null; }
    public static class Resources { public static T? Load<T>(string path) where T : Object => null; public static void UnloadAsset(Object assetToUnload) { } }
    public static class Application { public static string streamingAssetsPath => ""; public static string dataPath => System.IO.Path.GetFullPath(System.IO.Path.Combine(StubPaths.ProjectDirectory, "..", "Assets")); public static string persistentDataPath => System.IO.Path.GetTempPath(); public static bool isEditor => true; public static RuntimePlatform platform => RuntimePlatform.Android; public static string version => "0.1.0"; public static bool isBatchMode => true; public static int targetFrameRate { get; set; } public static bool CanStreamedLevelBeLoaded(string name) => false; }
    public static class Debug { public static bool isDebugBuild => true; public static void Log(object m) { } public static void LogWarning(object m) { } public static void LogError(object m) { } public static void LogException(Exception e) { } }
    public class CustomYieldInstruction : IEnumerator { public object? Current => null; public bool MoveNext() => false; public void Reset() { } }
    public sealed class WaitUntil : CustomYieldInstruction { public WaitUntil(Func<bool> predicate) { } }
    public sealed class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float s) { } }
    public class AsyncOperation { }
    public enum RenderMode { ScreenSpaceOverlay }
    public sealed class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; public static void ForceUpdateCanvases() { } }
}
namespace UnityEngine { public static class GUILayout { public static bool Button(string text) => false; } }
namespace UnityEngine { public interface ICanvasRaycastFilter { bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera); } public static class RectTransformUtility { public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint, Camera cam) => false; } }
namespace UnityEngine.Events { public delegate void UnityAction(); public delegate void UnityAction<T0>(T0 arg0); public class UnityEvent { public void AddListener(UnityAction a) { } public void RemoveAllListeners() { } } public class UnityEvent<T0> { public void AddListener(UnityAction<T0> a) { } public void RemoveAllListeners() { } } }
namespace UnityEngine.UI
{
    public class Graphic : Behaviour { public Color color { get; set; } public bool raycastTarget { get; set; } public RectTransform rectTransform => null!; public void SetVerticesDirty() { } }
    public struct UIVertex { public Vector3 position; public Color32 color; }
    public class VertexHelper { public int currentVertCount => 0; public void PopulateUIVertex(ref UIVertex vertex, int i) { } public void SetUIVertex(UIVertex vertex, int i) { } }
    public abstract class BaseMeshEffect : UnityEngine.MonoBehaviour { protected Graphic graphic => null!; public abstract void ModifyMesh(VertexHelper vh); }
    public class AspectRatioFitter : UnityEngine.MonoBehaviour { public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent } public AspectMode aspectMode { get; set; } public float aspectRatio { get; set; } }
    public class Image : Graphic { public enum Type { Simple, Sliced, Filled } public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 } public Sprite? sprite { get; set; } public Type type { get; set; } public bool preserveAspect { get; set; } public bool fillCenter { get; set; } public float fillAmount { get; set; } public FillMethod fillMethod { get; set; } public float pixelsPerUnitMultiplier { get; set; } }
    public sealed class RawImage : Graphic { public Texture? texture { get; set; } public Rect uvRect { get; set; } }
    public sealed class Mask : UnityEngine.MonoBehaviour { public bool showMaskGraphic { get; set; } }
    public sealed class RectMask2D : UnityEngine.MonoBehaviour { }
    public class Shadow : UnityEngine.MonoBehaviour { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } public bool useGraphicAlpha { get; set; } }
    public class Outline : Shadow { }
    public class Selectable : Behaviour { public enum Transition { None, ColorTint, SpriteSwap, Animation } public Transition transition { get; set; } public bool interactable { get; set; } public Graphic? targetGraphic { get; set; } }
    public sealed class Button : Selectable { public sealed class ButtonClickedEvent : UnityEngine.Events.UnityEvent { } public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent(); }
    public sealed class CanvasScaler : Behaviour { public enum ScaleMode { ScaleWithScreenSize } public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight; }
    public sealed class GraphicRaycaster : Behaviour { }
}
namespace UnityEngine.EventSystems { public sealed class EventSystem : Behaviour { public static EventSystem? current => null; } public sealed class StandaloneInputModule : Behaviour { } public class PointerEventData { } public interface IPointerDownHandler { void OnPointerDown(PointerEventData e); } public interface IPointerUpHandler { void OnPointerUp(PointerEventData e); } public interface IPointerExitHandler { void OnPointerExit(PointerEventData e); } }
namespace TMPro
{
    public enum TextAlignmentOptions { Center, Left, Right }
    public enum TextWrappingModes { NoWrap, Normal }
    [Flags] public enum FontStyles { Normal = 0, Bold = 1, UpperCase = 16 }
    public struct VertexGradient { public VertexGradient(UnityEngine.Color topLeft, UnityEngine.Color topRight, UnityEngine.Color bottomLeft, UnityEngine.Color bottomRight) { } }
    public sealed class TMP_FontAsset : UnityEngine.Object { public UnityEngine.Material material => null!; public static TMP_FontAsset? CreateFontAsset(UnityEngine.Font font, int samplingPointSize, int atlasPadding, UnityEngine.TextCore.LowLevel.GlyphRenderMode renderMode, int atlasWidth, int atlasHeight) => null; }
    public sealed class TMP_InputField : UnityEngine.UI.Selectable
    {
        public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
        public enum ContentType { Standard, Autocorrected, IntegerNumber, DecimalNumber, Alphanumeric, Name, EmailAddress, Password, Pin, Custom }
        public sealed class SubmitEvent : UnityEngine.Events.UnityEvent<string> { }
        public sealed class OnChangeEvent : UnityEngine.Events.UnityEvent<string> { }
        public UnityEngine.RectTransform? textViewport { get; set; }
        public TextMeshProUGUI? textComponent { get; set; }
        public UnityEngine.UI.Graphic? placeholder { get; set; }
        public int characterLimit { get; set; }
        public LineType lineType { get; set; }
        public ContentType contentType { get; set; }
        public string text { get; set; } = "";
        public bool isFocused => false;
        public SubmitEvent onEndEdit { get; } = new SubmitEvent();
        public OnChangeEvent onValueChanged { get; } = new OnChangeEvent();
        public void SetTextWithoutNotify(string input) { }
        public void ActivateInputField() { }
        public void DeactivateInputField() { }
    }
    public class TextMeshProUGUI : UnityEngine.UI.Graphic { public TMP_FontAsset? font { get; set; } public UnityEngine.Material? fontSharedMaterial { get; set; } public bool enableVertexGradient { get; set; } public VertexGradient colorGradient { get; set; } public string text { get; set; } = ""; public float fontSize { get; set; } public TextAlignmentOptions alignment { get; set; } public TextWrappingModes textWrappingMode { get; set; } public FontStyles fontStyle { get; set; } public bool enableAutoSizing { get; set; } public float alpha { get; set; } public float fontSizeMin { get; set; } public float fontSizeMax { get; set; } public float outlineWidth { get; set; } public UnityEngine.Color32 outlineColor { get; set; } public float characterSpacing { get; set; } public UnityEngine.Vector2 GetPreferredValues(string text) => default; }
}
namespace UnityEngine.TextCore.LowLevel { public enum GlyphRenderMode { SDFAA } }
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
        public int timeout { get; set; }
        public void Dispose() { }
    }
}
namespace UnityEditor
{
    public sealed class MenuItem : Attribute { public MenuItem(string path) { } }
    public sealed class EditorBuildSettingsScene { public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; } public string path; }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } = Array.Empty<EditorBuildSettingsScene>(); }
    public static class EditorPrefs { public static string GetString(string k, string d) => d; public static void SetString(string k, string v) { } }
    public static class EditorApplication { public static bool isPlaying { get; set; } public static void Exit(int code) { } }
    public class EditorWindow : UnityEngine.ScriptableObject { public static T GetWindow<T>(bool utility, string title) where T : EditorWindow => default!; }
    public static class EditorGUILayout { public static void LabelField(string s) { } public static int IntField(string l, int v) => v; }
    public static class EditorUtility { public static string OpenFilePanel(string t, string d, string e) => ""; public static void SetDirty(UnityEngine.Object o) { } public static bool DisplayDialog(string title, string message, string ok) => true; }
    public static class AssetDatabase { public static T? LoadAssetAtPath<T>(string p) where T : UnityEngine.Object => null; public static void CreateAsset(UnityEngine.Object o, string p) { } public static void SaveAssets() { } public static void Refresh() { } public static void ImportPackage(string path, bool interactive) { } }
    public static class Selection { public static UnityEngine.Object? activeObject { get; set; } }
    public class AssetImporter : UnityEngine.Object { }
    public enum TextureImporterType { Default, Sprite }
    public enum TextureImporterAlphaSource { None, FromInput, FromGrayScale }
    public enum TextureImporterNPOTScale { None, ToNearest, ToLarger, ToSmaller }
    public enum TextureImporterCompression { Uncompressed, Compressed, CompressedHQ, CompressedLQ }
    public enum SpriteImportMode { None, Single, Multiple, Polygon }
    public sealed class TextureImporterSettings { public UnityEngine.SpriteMeshType spriteMeshType { get; set; } public int spriteAlignment { get; set; } }
    public sealed class TextureImporter : AssetImporter { public TextureImporterType textureType { get; set; } public bool sRGBTexture { get; set; } public TextureImporterAlphaSource alphaSource { get; set; } public bool alphaIsTransparency { get; set; } public bool mipmapEnabled { get; set; } public UnityEngine.TextureWrapMode wrapMode { get; set; } public UnityEngine.FilterMode filterMode { get; set; } public TextureImporterNPOTScale npotScale { get; set; } public bool isReadable { get; set; } public TextureImporterCompression textureCompression { get; set; } public int maxTextureSize { get; set; } public SpriteImportMode spriteImportMode { get; set; } public float spritePixelsPerUnit { get; set; } public void ReadTextureSettings(TextureImporterSettings dest) { } public void SetTextureSettings(TextureImporterSettings src) { } }
    public class AssetPostprocessor { public string assetPath { get; set; } = ""; public AssetImporter assetImporter => null!; }
    [Flags] public enum BuildOptions { None = 0, Development = 1 }
    public enum BuildTarget { Android, iOS }
    public enum BuildTargetGroup { Android, iOS }
    public enum ScriptingImplementation { Mono2x, IL2CPP }
    public enum Il2CppCompilerConfiguration { Debug, Release, Master }
    public struct BuildPlayerOptions { public string[] scenes; public BuildTarget target; public BuildTargetGroup targetGroup; public string locationPathName; public BuildOptions options; }
    public static class BuildPipeline { public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions options) => new UnityEditor.Build.Reporting.BuildReport(); public static BuildTargetGroup GetBuildTargetGroup(BuildTarget target) => default; }
    public static class EditorUserBuildSettings { public static BuildTarget activeBuildTarget => default; public static bool buildAppBundle { get; set; } }
    public enum UIOrientation { Portrait }
    [Flags] public enum AndroidArchitecture { None = 0, ARMv7 = 1, ARM64 = 2 }
    public enum AndroidSdkVersions { AndroidApiLevel26 = 26 }
    public static class PlayerSettings
    {
        public static string companyName { get; set; } = "";
        public static string productName { get; set; } = "";
        public static string bundleVersion { get; set; } = "";
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static void SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget target, string id) { }
        public static class Android
        {
            public static AndroidArchitecture targetArchitectures { get; set; }
            public static AndroidSdkVersions minSdkVersion { get; set; }
            public static int bundleVersionCode { get; set; }
        }
        public static ScriptingImplementation GetScriptingBackend(UnityEditor.Build.NamedBuildTarget target) => default;
        public static void SetScriptingBackend(UnityEditor.Build.NamedBuildTarget target, ScriptingImplementation backend) { }
        public static void SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget target, Il2CppCompilerConfiguration configuration) { }
    }
}
namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
    public sealed class BuildSummary { public UnityEditor.BuildOptions options; public BuildResult result; }
    public sealed class BuildReport { public BuildSummary summary = new BuildSummary(); }
}
namespace UnityEditor.Build
{
    public interface IOrderedCallback { int callbackOrder { get; } }
    public interface IPreprocessBuildWithReport : IOrderedCallback { void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report); }
    public interface IPostprocessBuildWithReport : IOrderedCallback { void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report); }
    public sealed class BuildFailedException : Exception { public BuildFailedException(string message) : base(message) { } }
    public readonly struct NamedBuildTarget { public static NamedBuildTarget Android => default; public static NamedBuildTarget FromBuildTargetGroup(UnityEditor.BuildTargetGroup group) => default; }
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
