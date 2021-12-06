using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using UnityEngine;

public class PPResourceAndSetting : ScriptableObject
{
#if UNITY_EDITOR
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
    internal class CreatePPResourceAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var instance = CreateInstance<PPResourceAndSetting>();
            AssetDatabase.CreateAsset(instance, pathName);
            ResourceReloader.ReloadAllNullIn(instance, "Assets/PostProcessing");
            Selection.activeObject = instance;
        }
    }

    [MenuItem("Assets/Create/Postprocess/PPResource")]
    static void CreatePostProcessData()
    {
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreatePPResourceAsset>(), "CustomPostProcessData.asset", null, null);
    }
#endif

    [Serializable, ReloadGroup]
    public class ShaderResorce
    {
        [Reload("shader/Copy/URP/Shaders/PostProcessing/Bloom.shader")]
        public Shader bloom;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/CameraMotionBlur.shader")]
        public Shader cameraMotionBlur;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/GaussianDepthOfField.shader")]
        public Shader gaussianDepthOfField;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/BokehDepthOfField.shader")]
        public Shader bokehDepthOfField;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/PaniniProjection.shader")]
        public Shader paniniProjection;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/SubpixelMorphologicalAntialiasing.shader")]
        public Shader subpixelMorphologicalAntialiasing;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/StopNaN.shader")]
        public Shader stopNan;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/UberPost.shader")]
        public Shader uberPost;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/FinalPost.shader")]
        public Shader finalPostPass;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/LutBuilderLdr.shader")]
        public Shader lutBuilderLdr;
        [Reload("shader/Copy/URP/Shaders/PostProcessing/LutBuilderHdr.shader")]
        public Shader lutBuilderHdr;
        [Reload("shader/HBAO/HBAO.shader")]
        public Shader HBAO;
        [Reload("shader/GTAO/GTAO.shader")]
        public Shader GTAO;
        [Reload("shader/Fog.shader")]
        public Shader Fog;
    }
    [Serializable, ReloadGroup]
    public class TextureResources
    {
        [Reload("Textures/BlueNoise16/L/LDR_LLL1_{0}.png",0,32)]
        public Texture2D[] blueNoise16LTex;
        [Reload(new[]
        {
            "Textures/FilmGrain/Large01.png",
            "Textures/FilmGrain/Large02.png",
            "Textures/FilmGrain/Medium01.png",
            "Textures/FilmGrain/Medium02.png",
            "Textures/FilmGrain/Medium03.png",
            "Textures/FilmGrain/Medium04.png",
            "Textures/FilmGrain/Medium05.png",
            "Textures/FilmGrain/Medium06.png",
            "Textures/FilmGrain/Thin01.png",
            "Textures/FilmGrain/Thin02.png",
        })]
        public Texture2D[] filmGrainTex;
        [Reload("Textures/SMAA/AreaTex.tga")]
        public Texture smaaAreaTex;
        [Reload("Textures/SMAA/SearchTex.tga")]
        public Texture smaaSearchTex;
    }
    [Serializable]
    public class PPSetting
    {

        public ColorGradingMode m_ColorGradingMode = ColorGradingMode.LowDynamicRange;
        [SerializeField]
        private int m_ColorGradingLutSize = 32;
        public int colorGradingLutSize
        {
            get { return m_ColorGradingLutSize; }
            set { m_ColorGradingLutSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(value, k_MinLutSize, k_MaxLutSize)); }
        }
    }
    public ShaderResorce shaders;
    public TextureResources textures;
    public PPSetting setting;
    public const int k_MinLutSize = 16;
    public const int k_MaxLutSize = 64;
}
