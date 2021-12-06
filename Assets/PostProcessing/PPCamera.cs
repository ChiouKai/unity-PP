using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;


#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
[ImageEffectAllowedInSceneView]
[DisallowMultipleComponent]
[AddComponentMenu("PostProcessing/PPCamera", 1000)]
[RequireComponent(typeof(Camera))]
public class PPCamera : MonoBehaviour
{
    public static PPCamera layer = null;

    public Transform Trigger;
    public LayerMask PostLayer;
    public bool EnabledPostProcessing = false;
    public AntialiasingQuality antialiasingQuality;
    public Antialiasing antialiasingMode = Antialiasing.None;
    public bool isStopNaNEnabled;
    public bool dithering;
 
    CommandBuffer CBufferForPost;
    CommandBuffer CBufferForLut;
    CommandBuffer CBufferForAO;
    CommandBuffer CBufferForFog;
    Camera m_Camera;


    public bool lutPassChange;
    public bool changed;
    bool initialized = false;
    bool m_UseRGBM;
    bool HDR = false;

    public ColorGradingMode gradingMode { get { return resource.setting.m_ColorGradingMode; }
                                          set { resource.setting.m_ColorGradingMode = value; } }
    public PPResourceAndSetting resource;
    PPManager manager;
    public PPStack stack;
    public Dictionary<PPVolume, float> volumeWeight;

    const int k_MaxPyramidSize = 16;

    GraphicsFormat m_SMAAEdgeFormat;
    GraphicsFormat m_GaussianCoCFormat;
    GraphicsFormat m_DefaultHDRFormat;

    RenderData m_RenderData;
    MaterialLibrary m_Library;
    PPColorGradingLut lutPass;

    public AmbientOcclusion ambientOcclusion;
    AmbientOcclusionType aoType;
    Dictionary<AmbientOcclusionType, AOPass> DicOfAOpass;
    AOPass aoPass;

    PPDepthOfField m_DepthOfField;
    PPMotionBlur m_MotionBlur;
    PPPaniniProjection m_PaniniProjection;
    PPBloom m_Bloom;
    PPLensDistortion m_LensDistortion;
    PPVignette m_Vignette;
    PPChromaticAberration m_ChromaticAberration;
    PPFog m_Fog;

    PPColorLookup m_ColorLookup;
    PPColorAdjustments m_ColorAdjustments;
    PPTonemapping m_Tonemapping;
    PPFilmGrain m_FilmGrain;

    bool PPEnabled;
    private void OnEnable()
    {
        initialized = false;
        PPEnabled = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;
        if (!PPEnabled)
        {
            Debug.LogWarning("PP is Not Support with OpenGLES2.");
            this.enabled = false;
            return;
        }

        changed = true;
        lutPassChange = true;
        layer = this;
        manager = PPManager.instance;

        if(m_Camera == null)
            m_Camera = GetComponent<Camera>();

        SetCommandBuffer();

        SetStack();

        manager.CameraRegister(this, PostLayer);
        
        if (m_RenderData == null)
            m_RenderData = new RenderData(m_Camera);

        if (DicOfAOpass == null)
            DicOfAOpass = AOPass.GetAllAOpass();

        if (resource == null)
            StartCoroutine(WaitResource());
        else
        {
            if (m_Library == null)
                m_Library = new MaterialLibrary(resource);
            if (lutPass == null)
                lutPass = new PPColorGradingLut(resource, m_RenderData, stack);
            SetHDR(false);
            if (ambientOcclusion != null)
            {
                aoType = ambientOcclusion.aoType;
                aoPass = DicOfAOpass[ambientOcclusion.aoType];
                aoPass.Initialize(m_Camera, resource, m_RenderData, ambientOcclusion.AO);
            }
            else
                aoPass = DicOfAOpass[AmbientOcclusionType.None];

            initialized = true;
        }
        if (ShaderConstant._BloomMipUp == null)
        {
            ShaderConstant._BloomMipUp = new int[k_MaxPyramidSize];
            ShaderConstant._BloomMipDown = new int[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderConstant._BloomMipUp[i] = Shader.PropertyToID("_BloomMipUp" + i);
                ShaderConstant._BloomMipDown[i] = Shader.PropertyToID("_BloomMipDown" + i);
            }
        }

        //m_Camera.forceIntoRenderTexture = true; //https://forum.unity.com/threads/commandbuffer-rendering-scene-flipped-upside-down-in-forward-rendering.415922/
    }
    IEnumerator WaitResource()
    {
        yield return resource != null;
        m_Library = new MaterialLibrary(resource);
        lutPass = new PPColorGradingLut(resource, m_RenderData, stack);
        SetHDR(false);
        if(ambientOcclusion != null)
        {
            aoType = ambientOcclusion.aoType;
            aoPass = DicOfAOpass[ambientOcclusion.aoType];
            aoPass.Initialize(m_Camera, resource, m_RenderData, ambientOcclusion.AO);
        }
        else
            aoPass = DicOfAOpass[AmbientOcclusionType.None];
        initialized = true;
    }

    private void OnDisable()
    {
        if (PPEnabled)
        {
            manager.CameraUnregister(this);

            if (lutTexture != null)
                lutTexture.Release();
            if (layer == this)
                layer = null;
            CBufferForPost.Clear();
            CBufferForFog.Clear();
            m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, CBufferForPost);
            m_Camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, CBufferForFog);
            aoPass?.CleanUp(CBufferForAO);
        }
    }
    private void OnDestroy()
    {
        if (m_Library != null)
            m_Library.Cleanup();
    }

    private void OnValidate()
    {
        changed = true;
        lutPassChange = true;
        if (CBufferForPost != null)
        {
            CBufferForPost.Clear();
            CBufferForAO.Clear();
            CBufferForFog.Clear();
        }
    }

    public void EnabledPP()
    {
        EnabledPostProcessing = !EnabledPostProcessing;
        OnValidate();
    }

    private void OnPreRender()
    {

        if (!EnabledPostProcessing|| !initialized)
            return;
        UpdateData();

        if (changed) 
        {
            //Debug.Log("test");
            CBufferForPost.Clear();
            CBufferForAO.Clear();
            CBufferForFog.Clear();
            changed = false;
            //if (aoPass != null)
            aoPass.Execute(CBufferForAO);

            if (volumeWeight.Count != 0 || antialiasingMode != Antialiasing.None || isStopNaNEnabled == true)
            {
                manager.UpdateStack(this, volumeWeight, PostLayer);

                lutPassChange |= CheckHDR();
                lutPassChange |= CheckLutSize();
                SetDescriptor();

                if (lutPassChange)
                {
                    lutPassChange = false;
                    lutTexture = lutPass.Execute(CBufferForLut);
                }

                //if (m_Library == null)
                //    m_Library = new MaterialLibrary(resource);
                PostProcessingRender();

                DoFog(CBufferForFog);
            }
        }
        else
        {
            aoPass.UpdateAfterSet(CBufferForAO);
            UpdateMotionBlur();
            UpdateFilmGrain();
            SetupDithering(finalMaterial);
        }
    }

    void UpdateData()
    {
        ChangeAOType(ambientOcclusion != null ? ambientOcclusion.aoType : AmbientOcclusionType.None);

        changed |= m_RenderData.UpdataAndCheck();
        changed |= manager.CalVolumeWeight(PostLayer, Trigger, volumeWeight,out var lutTmpChange);
        lutPassChange |= lutTmpChange;
    }

    void SetCommandBuffer()
    {
        if (CBufferForPost == null)
        {
            CBufferForPost = new CommandBuffer { name = "Post-processing" };
            CBufferForLut = new CommandBuffer();
            CBufferForAO = new CommandBuffer() { name = "Ambient occlusion" };
            CBufferForFog = new CommandBuffer() { name = "Fog" };
        }
        else
        {
            m_Camera.RemoveAllCommandBuffers();
        }
#if !UNITY_2019_1_OR_NEWER // OnRenderImage (below) implies forceIntoRenderTexture
            m_Camera.forceIntoRenderTexture = true; // Needed when running Forward / LDR / No MSAA
#endif
            m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, CBufferForPost);
            m_Camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, CBufferForFog);
    }

    void SetStack()
    {
        if (stack == null)
        {
            stack = manager.CreateStack();

            m_DepthOfField = stack.GetComponent<PPDepthOfField>();
            m_MotionBlur = stack.GetComponent<PPMotionBlur>();
            m_PaniniProjection = stack.GetComponent<PPPaniniProjection>();
            m_Bloom = stack.GetComponent<PPBloom>();
            m_LensDistortion = stack.GetComponent<PPLensDistortion>();
            m_ChromaticAberration = stack.GetComponent<PPChromaticAberration>();
            m_Vignette = stack.GetComponent<PPVignette>();
            m_ColorLookup = stack.GetComponent<PPColorLookup>();
            m_ColorAdjustments = stack.GetComponent<PPColorAdjustments>();
            m_Tonemapping = stack.GetComponent<PPTonemapping>();
            m_FilmGrain = stack.GetComponent<PPFilmGrain>();
            m_Fog = stack.GetComponent<PPFog>();
        }
    }
    int lutsize;
    bool CheckLutSize()
    {
        if (lutsize != resource.setting.colorGradingLutSize)
        {
            lutsize = resource.setting.colorGradingLutSize;
            return true;
        }
        return false;
    }
    bool CheckHDR()
    {
        bool tmpHDR = m_RenderData.allowHDR && m_RenderData.supportHDR;
        if (HDR == tmpHDR)
            return false;
        HDR = tmpHDR;

        SetHDR(true);

        return true;
    }
    void SetHDR(bool check)
    {
        if (!check)
        {
            HDR = m_Camera.allowHDR && m_RenderData.supportHDR;
        }
        gradingMode = HDR ? resource.setting.m_ColorGradingMode : ColorGradingMode.LowDynamicRange;

        if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
        {
            m_DefaultHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            m_UseRGBM = false;
        }
        else
        {
            m_DefaultHDRFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? GraphicsFormat.R8G8B8A8_SRGB
                : GraphicsFormat.R8G8B8A8_UNorm;
            m_UseRGBM = true;
        }
    }

    public void ChangeAOType(AmbientOcclusionType type)
    {
        if (aoType == type)
            return;
        aoType = type;
        changed = true;
        if (aoPass != null)
        {
            aoPass.CleanUp(CBufferForAO);
            aoPass = DicOfAOpass[type];
            if (ambientOcclusion != null)
                aoPass.Initialize(m_Camera, resource, m_RenderData, ambientOcclusion.AO);
        }
    }


    Material finalMaterial;//for update
    public RenderTexture lutTexture;
    void PostProcessingRender()
    {
        if (m_Library == null)
            return;
        var material = m_Library.M_uber;
        if (material == null)
        {
            return;
        }
        bool isSceneViewCamera = m_Camera.cameraType == CameraType.SceneView;

        //when change scene sceneCamera didn't use OnDisable and OnEnable 
        if (isSceneViewCamera)
        {
            m_DepthOfField = stack.GetComponent<PPDepthOfField>();
            m_MotionBlur = stack.GetComponent<PPMotionBlur>();
            m_PaniniProjection = stack.GetComponent<PPPaniniProjection>();
            m_Bloom = stack.GetComponent<PPBloom>();
            m_LensDistortion = stack.GetComponent<PPLensDistortion>();
            m_ChromaticAberration = stack.GetComponent<PPChromaticAberration>();
            m_Vignette = stack.GetComponent<PPVignette>();
            m_ColorLookup = stack.GetComponent<PPColorLookup>();
            m_ColorAdjustments = stack.GetComponent<PPColorAdjustments>();
            m_Tonemapping = stack.GetComponent<PPTonemapping>();
            m_FilmGrain = stack.GetComponent<PPFilmGrain>();
            m_Fog = stack.GetComponent<PPFog>();
        }

        int source = ShaderConstant._TempTarget;
        int destination = ShaderConstant._TempTarget2;

        void swap() { int tmp = source; source = destination; destination = tmp; }

        CBufferForPost.Clear();
        CBufferForPost.GetTemporaryRT(source, GetCompatibleDescriptor(), FilterMode.Bilinear);
        CBufferForPost.GetTemporaryRT(destination, GetCompatibleDescriptor(), FilterMode.Bilinear);
        CBufferForPost.Blit(BuiltinRenderTextureType.CameraTarget, source);

        CBufferForPost.SetGlobalMatrix(ShaderConstant._FullscreenProjMat, GL.GetGPUProjectionMatrix(Matrix4x4.identity, true));

        //KillNaN
        if (isStopNaNEnabled && m_Library.M_stopNaN != null)
        {
            Utils.Blit(CBufferForPost, source, Utils.BlitDstDiscardContent(CBufferForPost, destination), m_Library.M_stopNaN, 0);
            swap();
        }

        //anti-aliasing
        if (antialiasingMode == Antialiasing.SubpixelMorphologicalAntialiasing)
        {
            DoSubpixelMorphologicalAntialiasing(CBufferForPost, source, destination);
            swap();
        }

        if (m_DepthOfField.isActive()&&!isSceneViewCamera)
        {
            DoDepthOfField(CBufferForPost, source, destination);
            swap();
        }
        if (m_MotionBlur.isActive()&&!isSceneViewCamera)
        {
            DoMotionBlur(CBufferForPost, source, destination);
            swap();
        }
        else
            motionBlurSet = false;

        // Panini projection is done as a fullscreen pass after all depth-based effects are done
        // and before bloom kicks in
        if (m_PaniniProjection.isActive()&&!isSceneViewCamera)
        {
            DoPaniniProjection(CBufferForPost, source, destination);
            swap();
        }

        material.shaderKeywords = null;
        bool bloomActive = m_Bloom.isActive();
        if (bloomActive)
        {
            SetupBloom(CBufferForPost, source, material);
        }
        if (m_LensDistortion.isActive()&&!isSceneViewCamera)
        {
            SetupLensDistortion(material);
        }
        SetupVignette(material);
        SetupChromaticAberration(material);
        SetupColorGrading(material);
        material.SetTexture(ShaderConstant._InternalLut, lutTexture);

        var colorLoadAction = RenderBufferLoadAction.DontCare;

        if (antialiasingMode == Antialiasing.FastApproximateAntialiasing)
        {
            CBufferForPost.SetGlobalTexture(ShaderConstant._SourceTex, source);

            CBufferForPost.SetRenderTarget(destination, colorLoadAction, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            CBufferForPost.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            CBufferForPost.DrawMesh(m_Library.fullscreenMesh, Matrix4x4.identity, material);

            CBufferForPost.SetViewProjectionMatrices(m_Camera.worldToCameraMatrix, m_Camera.projectionMatrix);
            swap();

            material = m_Library.M_finalPass;
            material.shaderKeywords = null;

            material.EnableKeyword(ShaderConstant.Fxaa);
            SetSourceSize(CBufferForPost, m_Descriptor);
        }


        SetupGrain(material);
        SetupDithering(material);

        if (Display.main.requiresSrgbBlitToBackbuffer)
        {
            material.EnableKeyword(ShaderConstant.LinearToSRGBConversion);
        }

        CBufferForPost.SetGlobalTexture(ShaderConstant._SourceTex, source);

        RenderTargetIdentifier cameraTarget = BuiltinRenderTextureType.CameraTarget;
        if (!CheckDefaultViewport())
            colorLoadAction = RenderBufferLoadAction.Load;

        CBufferForPost.SetRenderTarget(cameraTarget, colorLoadAction, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
        CBufferForPost.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        CBufferForPost.SetViewport(m_RenderData.pixelRect);
        CBufferForPost.DrawMesh(m_Library.fullscreenMesh, Matrix4x4.identity, material);

        CBufferForPost.SetViewProjectionMatrices(m_Camera.worldToCameraMatrix, m_Camera.projectionMatrix);


        //if (m_RenderData.targetTexture != null)
        //{
        //    CBufferForLut.Blit(cameraTarget, m_RenderData.targetTexture);
        //}
        finalMaterial = material;

        //SetupGrain(uberMaterial);
        //SetupDithering(uberMaterial);

        //if (!hasFinalPass && RequireSRGBconversion())
        //{
        //    uberMaterial.EnableKeyword(ShaderConstant.LinearToSRGBConversion);
        //}
        //m_CmdBuffer.SetGlobalTexture(ShaderConstant._SourceTex, source);
        //var colorLoadAction = RenderBufferLoadAction.DontCare;
        //if (!hasFinalPass && !isDefaultViewport)
        //{
        //    colorLoadAction = RenderBufferLoadAction.Load;
        //}
        //RenderTargetIdentifier target = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
        //RenderTargetIdentifier cameraTarget;
        //if (hasFinalPass)
        //    cameraTarget = Sec;
        //else
        //{
        //    if (m_Camera.targetTexture != null)
        //        cameraTarget = new RenderTargetIdentifier(m_Camera.targetTexture);
        //    else
        //        cameraTarget = target;
        //}
        //m_CmdBuffer.SetRenderTarget(cameraTarget, colorLoadAction, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
        //m_CmdBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        //if (cameraTarget == target)
        //{
        //    m_CmdBuffer.SetViewport(m_Camera.pixelRect);
        //}
        //m_CmdBuffer.DrawMesh(library.fullscreenMesh, Matrix4x4.identity, uberMaterial);

        //m_CmdBuffer.SetViewProjectionMatrices(m_Camera.worldToCameraMatrix, m_Camera.projectionMatrix);

        if (bloomActive)
            CBufferForPost.ReleaseTemporaryRT(ShaderConstant._BloomMipUp[0]);
        CBufferForPost.ReleaseTemporaryRT(source);
        CBufferForPost.ReleaseTemporaryRT(destination);

    }


    #region SMAA
    void DoSubpixelMorphologicalAntialiasing(CommandBuffer CBuffer, int source, int destination)
    {

        var material = m_Library.M_subpixelMorphologicalAntialiasing;
        if (material == null)
            return;
        CBuffer.BeginSample("SubpixelMorphologicalAntialiasing");
        var pixelRect = m_RenderData.rect;


        const int kStencilBit = 64;

        material.SetVector(ShaderConstant._Metrics, new Vector4(1f / m_Descriptor.width, 1f / m_Descriptor.height, m_Descriptor.width, m_Descriptor.height));
        material.SetTexture(ShaderConstant._AreaTexture, resource.textures.smaaAreaTex);
        material.SetTexture(ShaderConstant._SearchTexture, resource.textures.smaaSearchTex);
        material.SetInt(ShaderConstant._StencilRef, kStencilBit);
        material.SetInt(ShaderConstant._StencilMask, kStencilBit);

        material.shaderKeywords = null;

        switch (antialiasingQuality)
        {
            case AntialiasingQuality.Low:
                material.EnableKeyword("_SMAA_PRESET_LOW");
                break;
            case AntialiasingQuality.Medium:
                material.EnableKeyword("_SMAA_PRESET_MEDIUM");
                break;
            case AntialiasingQuality.High:
                material.EnableKeyword("_SMAA_PRESET_HIGH");
                break;
        }

        RenderTargetIdentifier stencil = ShaderConstant._EdgeTexture;
        int tempDepthBits = 24;
        if (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8_UNorm, FormatUsage.Render) && SystemInfo.graphicsDeviceVendor.ToLowerInvariant().Contains("arm"))
            m_SMAAEdgeFormat = GraphicsFormat.R8G8_UNorm;
        else
            m_SMAAEdgeFormat = GraphicsFormat.R8G8B8A8_UNorm;

        CBuffer.GetTemporaryRT(ShaderConstant._EdgeTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_SMAAEdgeFormat, tempDepthBits), FilterMode.Point);
        CBuffer.GetTemporaryRT(ShaderConstant._BlendTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8G8B8A8_UNorm), FilterMode.Point);

        CBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        CBuffer.SetViewport(pixelRect);
        //edge detection
        CBuffer.SetRenderTarget(new RenderTargetIdentifier(ShaderConstant._EdgeTexture, 0, CubemapFace.Unknown, -1),
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, stencil, 
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        CBuffer.ClearRenderTarget(true, true, Color.clear);
        CBuffer.SetGlobalTexture(ShaderConstant._ColorTexture, source);
        CBuffer.DrawMesh(m_Library.fullscreenMesh, Matrix4x4.identity, material, 0, 0);

        //blend weight
        CBuffer.SetRenderTarget(new RenderTargetIdentifier(ShaderConstant._BlendTexture, 0, CubemapFace.Unknown, -1), 
            RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store,stencil,
            RenderBufferLoadAction.Load,RenderBufferStoreAction.DontCare);
        CBuffer.ClearRenderTarget(false, true, Color.clear);
        CBuffer.SetGlobalTexture(ShaderConstant._ColorTexture, ShaderConstant._EdgeTexture);
        CBuffer.DrawMesh(m_Library.fullscreenMesh, Matrix4x4.identity, material, 0, 1);

        //Neighborhood blending
        CBuffer.SetRenderTarget(new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1),
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
        CBuffer.SetGlobalTexture(ShaderConstant._ColorTexture, source);
        CBuffer.SetGlobalTexture(ShaderConstant._BlendTexture, ShaderConstant._BlendTexture);
        CBuffer.DrawMesh(m_Library.fullscreenMesh, Matrix4x4.identity, material, 0, 2);

        CBuffer.ReleaseTemporaryRT(ShaderConstant._EdgeTexture);
        CBuffer.ReleaseTemporaryRT(ShaderConstant._BlendTexture);
        CBuffer.SetViewProjectionMatrices(m_Camera.worldToCameraMatrix, m_Camera.projectionMatrix);

        CBuffer.EndSample("SubpixelMorphologicalAntialiasing");
    }
    #endregion

    #region Depth Of Field
    //Depth of field
    void DoDepthOfField(CommandBuffer CBuffer, int source, int destination)
    {
        if (m_DepthOfField.mode == DepthOfFieldMode.Gaussian)
            DoGaussianDepthOfField(CBuffer, source, destination);
        else
            DoBokehDepthOfField(CBuffer, source, destination);
    }

    RenderTargetIdentifier[] m_MRT2;
    void DoGaussianDepthOfField(CommandBuffer CBuffer, int source, int destination)
    {
        CBuffer.BeginSample("GaussianDepthOfField");
        Material material = m_Library.M_gaussianDepthOfField;
        if (material == null)
            return;
        
        int downSample = 2;

        int width = m_Descriptor.width / downSample;
        int height = m_Descriptor.height / downSample;
        float farStart = m_DepthOfField.gaussianFarStart;
        float farEnd = Mathf.Max(farStart, m_DepthOfField.gaussianFarEnd);

        float nearStart = m_DepthOfField.gaussianNearStart;
        float nearEnd = Mathf.Min(nearStart, m_DepthOfField.gaussianNearEnd);

        float maxRadius = m_DepthOfField.gaussianMaxRadius * (width / 1080f);
        maxRadius = Mathf.Min(maxRadius, 2f);

        if (m_DepthOfField.highQualitySampling)
        {
            material.EnableKeyword(ShaderConstant.HighQualitySampling);
        }
        else
        {
            material.DisableKeyword(ShaderConstant.HighQualitySampling);
        }
        material.SetVector(ShaderConstant._CoCParams, new Vector4(farStart, farEnd, nearStart, nearEnd));
        material.SetFloat("MaxRadius", maxRadius);

        if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_UNorm, FormatUsage.Linear | FormatUsage.Render))
            m_GaussianCoCFormat = GraphicsFormat.R16_UNorm;
        else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, FormatUsage.Linear | FormatUsage.Render))
            m_GaussianCoCFormat = GraphicsFormat.R16_SFloat;
        else // Expect CoC banding
            m_GaussianCoCFormat = GraphicsFormat.R8_UNorm;

        CBuffer.GetTemporaryRT(ShaderConstant._FullCoCTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_GaussianCoCFormat), FilterMode.Bilinear);
        CBuffer.GetTemporaryRT(ShaderConstant._HalfCoCTexture, GetCompatibleDescriptor(width, height, m_GaussianCoCFormat), FilterMode.Bilinear);
        CBuffer.GetTemporaryRT(ShaderConstant._PingTexture, GetCompatibleDescriptor(width, height, m_DefaultHDRFormat), FilterMode.Bilinear);
        CBuffer.GetTemporaryRT(ShaderConstant._PongTexture, GetCompatibleDescriptor(width, height, m_DefaultHDRFormat), FilterMode.Bilinear);
        // Note: fresh temporary RTs don't require explicit RenderBufferLoadAction.DontCare, only when they are reused (such as PingTexture)

        SetSourceSize(CBuffer, m_Descriptor);
        CBuffer.SetGlobalVector(ShaderConstant._DownSampleScaleFactor, new Vector4(1.0f / downSample, 1.0f / downSample, downSample, downSample));

        Utils.Blit(CBuffer, source, ShaderConstant._FullCoCTexture, material, 0);

        // Downscale & prefilter color + coc
        if(m_MRT2 == null)
            m_MRT2 = new RenderTargetIdentifier[2] { ShaderConstant._HalfCoCTexture, ShaderConstant._PingTexture };

        CBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        CBuffer.SetViewport(m_RenderData.pixelRect);
        CBuffer.SetGlobalTexture(ShaderConstant._ColorTexture, source);
        CBuffer.SetGlobalTexture(ShaderConstant._FullCoCTexture, ShaderConstant._FullCoCTexture);
        CBuffer.SetRenderTarget(m_MRT2, ShaderConstant._HalfCoCTexture, 0, CubemapFace.Unknown, -1);
        CBuffer.DrawMesh(m_Library.fullscreenMesh, Matrix4x4.identity, material, 0, 1);

        //blur
        CBuffer.SetGlobalTexture(ShaderConstant._HalfCoCTexture, ShaderConstant._HalfCoCTexture);
        Utils.Blit(CBuffer, ShaderConstant._PingTexture, ShaderConstant._PongTexture, material, 2);
        Utils.Blit(CBuffer, ShaderConstant._PongTexture, Utils.BlitDstDiscardContent(CBuffer, ShaderConstant._PingTexture), material, 3);

        //composite
        CBuffer.SetGlobalTexture(ShaderConstant._ColorTexture, ShaderConstant._PingTexture);
        CBuffer.SetGlobalTexture(ShaderConstant._FullCoCTexture, ShaderConstant._FullCoCTexture);
        Utils.Blit(CBuffer, source, Utils.BlitDstDiscardContent(CBuffer, destination), material, 4);

        CBuffer.ReleaseTemporaryRT(ShaderConstant._FullCoCTexture);
        CBuffer.ReleaseTemporaryRT(ShaderConstant._HalfCoCTexture);
        CBuffer.ReleaseTemporaryRT(ShaderConstant._PingTexture);
        CBuffer.ReleaseTemporaryRT(ShaderConstant._PongTexture);
        CBuffer.EndSample("GaussianDepthOfField");
    }

    int m_BokehHash;
    Vector4[] m_BokehKernel;
    void DoBokehDepthOfField(CommandBuffer CBuffer, int source, int destination)
    {
        CBuffer.BeginSample("BokehDepthOfField");
        var material = m_Library.M_bokehDepthOfField;
        if (material == null)
            return;
        
        int downsample = 2;

        int width = m_Descriptor.width / downsample;
        int height = m_Descriptor.height / downsample;

        float F = m_DepthOfField.focalLength / 1000f;
        float A = m_DepthOfField.focalLength / m_DepthOfField.aperture;
        float P = m_DepthOfField.focusDistance;
        float maxCoC = (A * F) / (P - F);
        float maxRadius = GetMaxBokehRadiusInPixels(m_Descriptor.height);
        float rcpAspect = 1f / (width / (float)height);

        material.SetVector(ShaderConstant._CoCParams, new Vector4(P, maxCoC, maxRadius, rcpAspect));

        int hash = m_DepthOfField.GetHashCode();
        if(hash!= m_BokehHash)
        {
            m_BokehHash = hash;
            PrepareBokehKernel();
        }
        material.SetVectorArray(ShaderConstant._BokehKernel, m_BokehKernel);

        CBuffer.GetTemporaryRT(ShaderConstant._FullCoCTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8_UNorm), FilterMode.Bilinear);
        CBuffer.GetTemporaryRT(ShaderConstant._PingTexture, GetCompatibleDescriptor(width, height, GraphicsFormat.R16G16B16A16_SFloat), FilterMode.Bilinear);
        CBuffer.GetTemporaryRT(ShaderConstant._PongTexture, GetCompatibleDescriptor(width, height, GraphicsFormat.R16G16B16A16_SFloat), FilterMode.Bilinear);

        SetSourceSize(CBuffer, m_Descriptor);
        material.SetVector(ShaderConstant._DownSampleScaleFactor, new Vector4(1.0f / downsample, 1.0f / downsample, downsample, downsample));
        // Compute CoC
        Utils.Blit(CBuffer, source, ShaderConstant._FullCoCTexture, material, 0);
        CBuffer.SetGlobalTexture(ShaderConstant._FullCoCTexture, ShaderConstant._FullCoCTexture);

        // Downscale & prefilter color + coc
        Utils.Blit(CBuffer, source, ShaderConstant._PingTexture, material, 1);

        // Bokeh blur
        Utils.Blit(CBuffer, ShaderConstant._PingTexture, ShaderConstant._PongTexture, material, 2);

        // Post-filtering
        Utils.Blit(CBuffer, ShaderConstant._PongTexture, Utils.BlitDstDiscardContent(CBuffer, ShaderConstant._PingTexture), material, 3);

        CBuffer.SetGlobalTexture(ShaderConstant._DofTexture, ShaderConstant._PingTexture);
        Utils.Blit(CBuffer, source, Utils.BlitDstDiscardContent(CBuffer, destination), material, 4);

        CBuffer.ReleaseTemporaryRT(ShaderConstant._FullCoCTexture);
        CBuffer.ReleaseTemporaryRT(ShaderConstant._PingTexture);
        CBuffer.ReleaseTemporaryRT(ShaderConstant._PongTexture);
        CBuffer.EndSample("BokehDepthOfField");
    }

    float GetMaxBokehRadiusInPixels(float viewportHeight)
    {
        // Estimate the maximum radius of bokeh (empirically derived from the ring count)
        const float kRadiusInPixels = 14f;
        return Mathf.Min(0.05f, kRadiusInPixels / viewportHeight);
    }

    void PrepareBokehKernel()
    {
        const int kRings = 4;
        const int kPointsPerRing = 7;

        // Check the existing array
        if (m_BokehKernel == null)
            m_BokehKernel = new Vector4[42];

        // Fill in sample points (concentric circles transformed to rotated N-Gon)
        int idx = 0;
        float bladeCount = m_DepthOfField.bladeCount;
        float curvature = 1f - m_DepthOfField.bladeCurvature;
        float rotation = m_DepthOfField.bladeRotation * Mathf.Deg2Rad;
        const float PI = Mathf.PI;
        const float TWO_PI = Mathf.PI * 2f;

        for (int ring = 1; ring < kRings; ring++)
        {
            float bias = 1f / kPointsPerRing;
            float radius = (ring + bias) / (kRings - 1f + bias);
            int points = ring * kPointsPerRing;

            for (int point = 0; point < points; point++)
            {
                // Angle on ring
                float phi = 2f * PI * point / points;

                // Transform to rotated N-Gon
                // Adapted from "CryEngine 3 Graphics Gems" [Sousa13]
                float nt = Mathf.Cos(PI / bladeCount);
                float dt = Mathf.Cos(phi - (TWO_PI / bladeCount) * Mathf.Floor((bladeCount * phi + Mathf.PI) / TWO_PI));
                float r = radius * Mathf.Pow(nt / dt, curvature);
                float u = r * Mathf.Cos(phi - rotation);
                float v = r * Mathf.Sin(phi - rotation);

                m_BokehKernel[idx] = new Vector4(u, v);
                idx++;
            }
        }
    }
    #endregion

    #region MotionBlur
    bool motionBlurSet = false;
    Matrix4x4 m_PrevViewProjM;
    Material MotionBlurMaterial;
    void DoMotionBlur(CommandBuffer CBuffer, int source, int destination)
    { 
        MotionBlurMaterial = m_Library.M_cameraMotionBlur;
        if (MotionBlurMaterial == null)
        {
            motionBlurSet = false;
            return;
        }
        CBuffer.BeginSample("MotionBlur");
        var proj = m_Camera.projectionMatrix;
        var view = m_Camera.worldToCameraMatrix;
        var viewProj = proj * view;
        //keep update

        MotionBlurMaterial.SetMatrix("_ViewProjM", viewProj);

        if (motionBlurSet)
            MotionBlurMaterial.SetMatrix("_PrevViewProjM", m_PrevViewProjM);
        else
        {
            MotionBlurMaterial.SetMatrix("_PrevViewProjM", viewProj);
            motionBlurSet = true;
        }
        m_PrevViewProjM = viewProj;

        MotionBlurMaterial.SetFloat("_Intensity", m_MotionBlur.intensity);
        MotionBlurMaterial.SetFloat("_Clamp", m_MotionBlur.clamp);
        SetSourceSize(CBuffer, m_Descriptor);

        Utils.Blit(CBuffer, source, Utils.BlitDstDiscardContent(CBuffer, destination), MotionBlurMaterial, (int)m_MotionBlur.quality.value);
        CBuffer.EndSample("MotionBlur");
    }
    void UpdateMotionBlur()
    {
        if (motionBlurSet)
        {
            var proj = m_Camera.projectionMatrix;
            var view = m_Camera.worldToCameraMatrix;
            var viewProj = proj * view;
            //keep update
            MotionBlurMaterial.SetMatrix("_ViewProjM", viewProj);
            MotionBlurMaterial.SetMatrix("_PrevViewProjM", m_PrevViewProjM);
            m_PrevViewProjM = viewProj;
        }
    }

    #endregion

    #region PaniniProjection
    void DoPaniniProjection(CommandBuffer CBuffer, int source, int destination)
    {
        var material = m_Library.M_paniniProjection;
        if (material == null)
        {
            return;
        }
        CBuffer.BeginSample("PaniniProjection");

        float distance = m_PaniniProjection.distance;
        var viewExtrnts = CalcViewExtents();
        var cropExtents = CalcCropExtents(distance);

        float scaleX = cropExtents.x / viewExtrnts.x;
        float scaleY = cropExtents.y / viewExtrnts.y;
        float scaleF = Mathf.Min(scaleX, scaleY);

        float paniniD = distance;
        float paniniS = Mathf.Lerp(1f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

        material.SetVector(ShaderConstant._Params, new Vector4(viewExtrnts.x, viewExtrnts.y, paniniD, paniniS));
        material.EnableKeyword(1f - Mathf.Abs(paniniD) > float.Epsilon ? 
            ShaderConstant.PaniniGeneric : ShaderConstant.PaniniUnitDistance);
        Utils.Blit(CBuffer, source, Utils.BlitDstDiscardContent(CBuffer, destination), material);

        CBuffer.EndSample("PaniniProjection");
    }
    Vector2 CalcViewExtents()
    {
        float fovY = m_RenderData.fieldOfView * Mathf.Deg2Rad;
        float aspect = m_Descriptor.width / (float)m_Descriptor.height;

        float viewExtY = Mathf.Tan(0.5f * fovY);
        float viewExtX = aspect * viewExtY;

        return new Vector2(viewExtX, viewExtY);
    }
    Vector2 CalcCropExtents(float d)
    {
        // given
        //    S----------- E--X-------
        //    |    `  ~.  /,´
        //    |-- ---    Q
        //    |        ,/    `
        //  1 |      ,´/       `
        //    |    ,´ /         ´
        //    |  ,´  /           ´
        //    |,`   /             ,
        //    O    /
        //    |   /               ,
        //  d |  /
        //    | /                ,
        //    |/                .
        //    P
        //    |              ´
        //    |         , ´
        //    +-    ´
        //
        // have X
        // want to find E

        float viewDist = 1f + d;

        var projPos = CalcViewExtents();
        var projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);

        float cylDistMinusD = 1f / projHyp;
        float cylDist = cylDistMinusD + d;
        var cylPos = projPos * cylDistMinusD;


        return cylPos * (viewDist / cylDist);
    }

    #endregion

    #region Bloom

    void SetupBloom(CommandBuffer CBuffer, int source, Material uberMaterial)
    {
        var material = m_Library.M_bloom;
        if (material == null)
        {
            return;
        }

        int tw = m_Descriptor.width >> 1;
        int th = m_Descriptor.height >> 1;

        // Determine the iteration count
        int maxSize = Mathf.Max(tw, th);
        int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
        iterations -= m_Bloom.skipIterations;
        int mipCount = Mathf.Clamp(iterations, 1, k_MaxPyramidSize);

        // Pre-filtering parameters
        float clamp = m_Bloom.clamp;
        float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold);
        float thresholdKnee = threshold * 0.5f;

        float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter);

        material.SetVector(ShaderConstant._Params, new Vector4(scatter, clamp, threshold, thresholdKnee));
        Utils.SetKeyword(material, ShaderConstant.BloomHQ, m_Bloom.highQualityFiltering);
        Utils.SetKeyword(material, ShaderConstant.UseRGBM, m_UseRGBM);

        CBuffer.BeginSample("Bloom");

        var desc = GetCompatibleDescriptor(tw, th, m_DefaultHDRFormat);
        CBuffer.GetTemporaryRT(ShaderConstant._BloomMipDown[0], desc, FilterMode.Bilinear);
        CBuffer.GetTemporaryRT(ShaderConstant._BloomMipUp[0], desc, FilterMode.Bilinear);
        Utils.Blit(CBuffer, source, ShaderConstant._BloomMipDown[0], material, 0);

        int lastDown = ShaderConstant._BloomMipDown[0];
        for(int i = 1; i < mipCount; ++i)
        {
            tw = Mathf.Max(1, tw >> 1);
            th = Mathf.Max(1, th >> 1);
            int mipDown = ShaderConstant._BloomMipDown[i];
            int mipUp = ShaderConstant._BloomMipUp[i];
            desc.width = tw;
            desc.height = th;
            CBuffer.GetTemporaryRT(mipDown, desc, FilterMode.Bilinear);
            CBuffer.GetTemporaryRT(mipUp, desc, FilterMode.Bilinear);

            Utils.Blit(CBuffer, lastDown, mipUp, material, 1);
            Utils.Blit(CBuffer, mipUp, mipDown, material, 2);

            lastDown = mipDown;
        }

        for(int i =mipCount -2; i >= 0; --i)
        {
            int lowMip = (i == mipCount - 2) ? ShaderConstant._BloomMipDown[i + 1] : ShaderConstant._BloomMipUp[i + 1];
            int highMip = ShaderConstant._BloomMipDown[i];
            int dst = ShaderConstant._BloomMipUp[i];

            CBuffer.SetGlobalTexture(ShaderConstant._SourceTexLowMip, lowMip);
            Utils.Blit(CBuffer, highMip, Utils.BlitDstDiscardContent(CBuffer, dst), material, 3);
        }
        for (int i = 1; i < mipCount; ++i)
        {
            CBuffer.ReleaseTemporaryRT(ShaderConstant._BloomMipDown[i]);
            CBuffer.ReleaseTemporaryRT(ShaderConstant._BloomMipUp[i]);
        }
        CBuffer.ReleaseTemporaryRT(ShaderConstant._BloomMipDown[0]);

        var tint = m_Bloom.tint.value.linear;
        var luma = Luminance(tint);
        tint = luma > 0f ? tint * (1f / luma) : Color.white;

        uberMaterial.SetVector(ShaderConstant._Bloom_Params, new Vector4(m_Bloom.intensity, tint.r, tint.g, tint.b));
        uberMaterial.SetFloat(ShaderConstant._Bloom_RGBM, m_UseRGBM ? 1f : 0f);
        CBuffer.SetGlobalTexture(ShaderConstant._Bloom_Texture, ShaderConstant._BloomMipUp[0]);

        CBuffer.EndSample("Bloom");
        float dirtIntensity = m_Bloom.dirtIntensity;

        if (m_Bloom.highQualityFiltering)
            uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderConstant.BloomHQDirt : ShaderConstant.BloomHQ);
        else
            uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderConstant.BloomLQDirt : ShaderConstant.BloomLQ);

        if (dirtIntensity <= 0f)
            return;

        var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
        float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
        float screenRatio = m_Descriptor.width / (float)m_Descriptor.height;
        var dirtScaleOffset = new Vector4(1f, 1f, 0f, 0f);


        if (dirtRatio > screenRatio)
        {
            dirtScaleOffset.x = screenRatio / dirtRatio;
            dirtScaleOffset.z = (1f - dirtScaleOffset.x) * 0.5f;
        }
        else if (screenRatio > dirtRatio)
        {
            dirtScaleOffset.y = dirtRatio / screenRatio;
            dirtScaleOffset.w = (1f - dirtScaleOffset.y) * 0.5f;
        }
        uberMaterial.SetVector(ShaderConstant._LensDirt_Params, dirtScaleOffset);
        uberMaterial.SetFloat(ShaderConstant._LensDirt_Intensity, dirtIntensity);
        uberMaterial.SetTexture(ShaderConstant._LensDirt_Texture, dirtTexture);


    }

    #endregion

    #region LensDistrotion
    void SetupLensDistortion(Material material)
    {

        float amount = 1.6f * Mathf.Max(Mathf.Abs(m_LensDistortion.intensity.value * 100f), 1f);
        float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
        float sigma = 2f * Mathf.Tan(theta * 0.5f);
        var center = m_LensDistortion.center.value * 2f - Vector2.one;
        var p1 = new Vector4(
            center.x,
            center.y,
            Mathf.Max(m_LensDistortion.xMultiplier.value, 1e-4f),
            Mathf.Max(m_LensDistortion.yMultiplier.value, 1e-4f)
        );
        var p2 = new Vector4(
            m_LensDistortion.intensity.value >= 0f ? theta : 1f / theta,
            sigma,
            1f / m_LensDistortion.scale.value,
            m_LensDistortion.intensity.value * 100f
        );

        material.SetVector(ShaderConstant._Distortion_Params1, p1);
        material.SetVector(ShaderConstant._Distortion_Params2, p2);

        material.EnableKeyword(ShaderConstant.Distortion);
    }
    #endregion

    #region Chromatic Aberration

    void SetupChromaticAberration(Material material)
    {
        material.SetVector(ShaderConstant._Chroma_Params
            , new Vector3(m_ChromaticAberration.intensity.value * 0.05f
            , m_ChromaticAberration.center.value.x - 0.5f, m_ChromaticAberration.center.value.y - 0.5f));

        if (m_ChromaticAberration.isActive())
            material.EnableKeyword(ShaderConstant.ChromaticAberration);
    }

    #endregion

    #region Vignette

    void SetupVignette(Material material)
    {
        var color = m_Vignette.color.value;
        var center = m_Vignette.center.value;
        var aspectRatio = m_Descriptor.width / (float)m_Descriptor.height;

        var v1 = new Vector4(color.r, color.g, color.b, 
            m_Vignette.rounded.value ? aspectRatio : 1f);
        var v2 = new Vector4(center.x, center.y, 
            m_Vignette.intensity * 3f, m_Vignette.smoothness * 5f);

        material.SetVector(ShaderConstant._Vignette_Params1, v1);
        material.SetVector(ShaderConstant._Vignette_Params2, v2);
    }
    #endregion

    #region Color Grading

    void SetupColorGrading(Material material)
    {
        int lutHeight = lutsize;
        int lutWidth = lutHeight * lutHeight;
        bool hdr = resource.setting.m_ColorGradingMode == ColorGradingMode.HighDynamicRange;

        float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure);
        material.SetVector(ShaderConstant._Lut_Params, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear));
        material.SetTexture(ShaderConstant._UserLut, m_ColorLookup.texture);
        material.SetVector(ShaderConstant._UserLut_Params,
            !m_ColorLookup.isActive() ? Vector4.zero
            : new Vector4(1f / m_ColorLookup.texture.value.width,
                          1f / m_ColorLookup.texture.value.height,
                          m_ColorLookup.texture.value.height - 1f,
                          m_ColorLookup.contribution.value));
        if (hdr)
        {
            material.EnableKeyword(ShaderConstant.HDRGrading);
        }
        else
        {
            switch (m_Tonemapping.mode.value)
            {
                case TonemappingMode.Neutral: material.EnableKeyword(ShaderConstant.TonemapNeutral);break;
                case TonemappingMode.ACES: material.EnableKeyword(ShaderConstant.TonemapACES);break;
                default: break;
            }
        }

    }

    #endregion

    #region Film Grain

    bool FilmGrainSet = false;
    void SetupGrain(Material material)
    {
        if(m_FilmGrain.isActive())
        {
            material.EnableKeyword(ShaderConstant.FilmGrain);

            var texture = m_FilmGrain.texture.value;

            if (m_FilmGrain.type.value != FilmGrainLookup.Custom)
                texture = resource.textures.filmGrainTex[(int)m_FilmGrain.type.value];
            
            float offsetX = Random.value;//update
            float offsetY = Random.value;

            var tilingParams = texture == null
                ? Vector4.zero
                : new Vector4(m_RenderData.pixelWidth / (float)texture.width, m_RenderData.pixelHeight / (float)texture.height, offsetX, offsetY);

            material.SetTexture(ShaderConstant._Grain_Texture, texture);
            material.SetVector(ShaderConstant._Grain_Params, new Vector2(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value));
            material.SetVector(ShaderConstant._Grain_TilingParams, tilingParams);
            FilmGrainSet = true;
        }
        else
        {
            FilmGrainSet = false;
        }
    }
    void UpdateFilmGrain()
    {
        if (FilmGrainSet)
        {
            var texture = m_FilmGrain.texture.value;

            if (m_FilmGrain.type.value != FilmGrainLookup.Custom)
                texture = resource.textures.filmGrainTex[(int)m_FilmGrain.type.value];

            float offsetX = Random.value;//update
            float offsetY = Random.value;

            var tilingParams = new Vector4(m_RenderData.pixelWidth / (float)texture.width, m_RenderData.pixelHeight / (float)texture.height, offsetX, offsetY);
            finalMaterial.SetVector(ShaderConstant._Grain_TilingParams, tilingParams);
        }
    }

    #endregion

    #region Dithering

    int m_DitheringTextureIndex;
    void SetupDithering(Material material)
    {
        if (dithering)
        {
            material.EnableKeyword(ShaderConstant.Dithering);

            var blueNoise = resource.textures.blueNoise16LTex;

            if (blueNoise == null || blueNoise.Length == 0)
                m_DitheringTextureIndex = 0;

            if (++m_DitheringTextureIndex >= blueNoise.Length)
                m_DitheringTextureIndex = 0;

            float rndOffsetX = Random.value;
            float rndOffsetY = Random.value;

            // Ideally we would be sending a texture array once and an index to the slice to use
            // on every frame but these aren't supported on all Universal targets
            var noiseTex = blueNoise[m_DitheringTextureIndex];

            material.SetTexture(ShaderConstant._BlueNoise_Texture, noiseTex);
            material.SetVector(ShaderConstant._Dithering_Params, new Vector4(
                m_RenderData.pixelWidth / (float)noiseTex.width,
                m_RenderData.pixelHeight / (float)noiseTex.height,
                rndOffsetX,
                rndOffsetY
            ));
        }
    }


    #endregion

    #region Fog
    void DoFog(CommandBuffer CBuffer)
    {
        if (m_Fog.isActive() && (m_RenderData.renderingPath != RenderingPath.Forward || !RenderSettings.fog))
        {
            if (m_Library == null)
                return;
            CBuffer.BeginSample("Fog");
            var material = m_Library.M_Fog;
            material.shaderKeywords = null;
            var fogColor = QualitySettings.activeColorSpace == ColorSpace.Linear ? m_Fog.FogColor.value.linear : m_Fog.FogColor;

            material.SetColor(ShaderConstant._FogColor, fogColor);

            material.SetVector(ShaderConstant._FogParams, new Vector4(m_Fog.Density * 0.01f, m_Fog.Start, m_Fog.End, m_Fog.IncludeSkybox));

            switch (m_Fog.Mode.value)
            {
                case FogMode.Linear:
                    material.EnableKeyword("FOG_LINEAR");
                    break;
                case FogMode.Exponential:
                    material.EnableKeyword("FOG_EXP");
                    break;
                case FogMode.ExponentialSquared:
                    material.EnableKeyword("FOG_EXP2");
                    break;
            }

            var fbFormat = HDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            CBuffer.GetTemporaryRT(ShaderConstant._TempTarget, m_RenderData.pixelWidth, m_RenderData.pixelHeight, 24, FilterMode.Bilinear, fbFormat);
            CBuffer.Blit(BuiltinRenderTextureType.CameraTarget, ShaderConstant._TempTarget);
            CBuffer.Blit(ShaderConstant._TempTarget, BuiltinRenderTextureType.CameraTarget, material, 0);
            CBuffer.ReleaseTemporaryRT(ShaderConstant._TempTarget);
            CBuffer.EndSample("Fog");
        }
    }

    #endregion

    RenderTextureDescriptor m_Descriptor;
    //[Range(0.1f, 2f)]
    //public float renderScale = 1f;
    void SetDescriptor()
    {
        RenderTextureDescriptor desc;
        
        if (m_RenderData.targetTexture == null)
        {
            desc = new RenderTextureDescriptor((int)(m_RenderData.pixelWidth /** renderScale*/), (int)(m_RenderData.pixelHeight/* * renderScale*/))
            {
                graphicsFormat = HDR ? GetHDRformat() : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR),
                depthBufferBits = 32,//?
                msaaSamples = QualitySettings.antiAliasing,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
            };
        }
        else
        {
            desc = m_RenderData.targetTexture.descriptor;
            desc.width = m_RenderData.pixelWidth;
            desc.height = m_RenderData.pixelHeight;
            if (m_Camera.cameraType == CameraType.SceneView && !HDR)
            {
                desc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            }
        }

        desc.enableRandomWrite = false;
        desc.bindMS = false;
        desc.useDynamicScale = m_RenderData.allowDynamicResolution;

        desc.useMipMap = false;
        desc.autoGenerateMips = false;

        desc.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(desc);

        m_Descriptor = desc;
    }

    GraphicsFormat GetHDRformat()
    {
        if (!Graphics.preserveFramebufferAlpha && SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
            return GraphicsFormat.B10G11R11_UFloatPack32;
        else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16_SFloat, FormatUsage.Linear | FormatUsage.Render))
            return GraphicsFormat.R16G16B16A16_SFloat;
        else
            return SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);

    }

    RenderTextureDescriptor GetCompatibleDescriptor()
    => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat, m_Descriptor.depthBufferBits);

    RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
    {
        var desc = m_Descriptor;
        desc.depthBufferBits = depthBufferBits;
        desc.msaaSamples = 1;
        desc.width = width;
        desc.height = height;
        desc.graphicsFormat = format;
        return desc;
    }

    public float Luminance(in Color color) => color.r * 0.2126729f + color.g * 0.7151522f + color.b * 0.072175f;
    
    internal void SetSourceSize(CommandBuffer cmd, RenderTextureDescriptor desc)
    {
        float width = desc.width;
        float height = desc.height;
        if (desc.useDynamicScale)
        {
            width *= ScalableBufferManager.widthScaleFactor;
            height *= ScalableBufferManager.heightScaleFactor;
        }
        cmd.SetGlobalVector(ShaderConstant._SourceSize, new Vector4(width, height, 1.0f / width, 1.0f / height));
    }

    bool CheckDefaultViewport()
    {
        bool check = !(Mathf.Abs(m_RenderData.rect.x) > 0.0f || Mathf.Abs(m_RenderData.rect.y) > 0.0f ||
                Mathf.Abs(m_RenderData.rect.width) < 1.0f || Mathf.Abs(m_RenderData.rect.height) < 1.0f);
        return check;
    } 



    class MaterialLibrary
    {
        public readonly Material M_stopNaN;
        public readonly Material M_subpixelMorphologicalAntialiasing;
        public readonly Material M_gaussianDepthOfField;
        public readonly Material M_bokehDepthOfField;
        public readonly Material M_cameraMotionBlur;
        public readonly Material M_paniniProjection;
        public readonly Material M_bloom;
        public readonly Material M_uber;
        public readonly Material M_finalPass;

        public readonly Material M_Fog;

        public MaterialLibrary(PPResourceAndSetting data)
        {
            M_stopNaN = Utils.CreateMaterial(data.shaders.stopNan);
            M_subpixelMorphologicalAntialiasing = Utils.CreateMaterial(data.shaders.subpixelMorphologicalAntialiasing);
            M_gaussianDepthOfField = Utils.CreateMaterial(data.shaders.gaussianDepthOfField);
            M_bokehDepthOfField = Utils.CreateMaterial(data.shaders.bokehDepthOfField);
            M_cameraMotionBlur = Utils.CreateMaterial(data.shaders.cameraMotionBlur);
            M_paniniProjection = Utils.CreateMaterial(data.shaders.paniniProjection);
            M_bloom = Utils.CreateMaterial(data.shaders.bloom);
            M_uber = Utils.CreateMaterial(data.shaders.uberPost);
            M_finalPass = Utils.CreateMaterial(data.shaders.finalPostPass);
            M_Fog = Utils.CreateMaterial(data.shaders.Fog);
        }


        internal void Cleanup()
        {
            Utils.Destroy(M_stopNaN);
            Utils.Destroy(M_subpixelMorphologicalAntialiasing);
            Utils.Destroy(M_gaussianDepthOfField);
            Utils.Destroy(M_bokehDepthOfField);
            Utils.Destroy(M_cameraMotionBlur);
            Utils.Destroy(M_paniniProjection);
            Utils.Destroy(M_bloom);
            Utils.Destroy(M_uber);
            Utils.Destroy(M_finalPass);
            Utils.Destroy(s_FullscreenMesh);
            Utils.Destroy(M_Fog);
        }
        Mesh s_FullscreenMesh = null;
        public Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 1.0f;
                float bottomV = 0.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }
    } 
}
static class ShaderConstant
{
    public static readonly int _TempTarget = Shader.PropertyToID("_TempTarget");
    public static readonly int _TempTarget2 = Shader.PropertyToID("_TempTarget2");
    public static readonly int _FullscreenProjMat = Shader.PropertyToID("_FullscreenProjMat");

    public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
    public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");

    public static readonly int _Metrics = Shader.PropertyToID("_Metrics");
    public static readonly int _AreaTexture = Shader.PropertyToID("_AreaTexture");
    public static readonly int _SearchTexture = Shader.PropertyToID("_SearchTexture");
    public static readonly int _EdgeTexture = Shader.PropertyToID("_EdgeTexture");
    public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");
    public static readonly int _ColorTexture = Shader.PropertyToID("_ColorTexture");

    public static readonly int _FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
    public static readonly int _HalfCoCTexture = Shader.PropertyToID("_HalfCoCTexture");
    public static readonly int _DofTexture = Shader.PropertyToID("_DofTexture");
    public static readonly int _CoCParams = Shader.PropertyToID("_CoCParams");
    public static readonly int _BokehKernel = Shader.PropertyToID("_BokehKernel");
    public static readonly int _PongTexture = Shader.PropertyToID("_PongTexture");
    public static readonly int _PingTexture = Shader.PropertyToID("_PingTexture");
    public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

    public static readonly int _Params = Shader.PropertyToID("_Params");
    public static readonly int _SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
    public static readonly int _Bloom_Params = Shader.PropertyToID("_Bloom_Params");
    public static readonly int _Bloom_RGBM = Shader.PropertyToID("_Bloom_RGBM");
    public static readonly int _Bloom_Texture = Shader.PropertyToID("_Bloom_Texture");
    public static readonly int _LensDirt_Texture = Shader.PropertyToID("_LensDirt_Texture");
    public static readonly int _LensDirt_Params = Shader.PropertyToID("_LensDirt_Params");
    public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
    public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
    public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
    public static readonly int _Chroma_Params = Shader.PropertyToID("_Chroma_Params");
    public static readonly int _Vignette_Params1 = Shader.PropertyToID("_Vignette_Params1");
    public static readonly int _Vignette_Params2 = Shader.PropertyToID("_Vignette_Params2");
    public static readonly int _UserLut_Params = Shader.PropertyToID("_UserLut_Params");
    public static readonly int _InternalLut = Shader.PropertyToID("_InternalLut");
    public static readonly int _UserLut = Shader.PropertyToID("_UserLut");

    public static readonly int _Grain_Texture = Shader.PropertyToID("_Grain_Texture");
    public static readonly int _Grain_Params = Shader.PropertyToID("_Grain_Params");
    public static readonly int _Grain_TilingParams = Shader.PropertyToID("_Grain_TilingParams");

    public static readonly int _BlueNoise_Texture = Shader.PropertyToID("_BlueNoise_Texture");
    public static readonly int _Dithering_Params = Shader.PropertyToID("_Dithering_Params");

    public static readonly int _SourceTex = Shader.PropertyToID("_SourceTex");
    public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");

    public static int[] _BloomMipUp;
    public static int[] _BloomMipDown;

    public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
    public static readonly int _ColorBalance = Shader.PropertyToID("_ColorBalance");
    public static readonly int _ColorFilter = Shader.PropertyToID("_ColorFilter");
    public static readonly int _ChannelMixerRed = Shader.PropertyToID("_ChannelMixerRed");
    public static readonly int _ChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");
    public static readonly int _ChannelMixerBlue = Shader.PropertyToID("_ChannelMixerBlue");
    public static readonly int _HueSatCon = Shader.PropertyToID("_HueSatCon");
    public static readonly int _Lift = Shader.PropertyToID("_Lift");
    public static readonly int _Gamma = Shader.PropertyToID("_Gamma");
    public static readonly int _Gain = Shader.PropertyToID("_Gain");
    public static readonly int _Shadows = Shader.PropertyToID("_Shadows");
    public static readonly int _Midtones = Shader.PropertyToID("_Midtones");
    public static readonly int _Highlights = Shader.PropertyToID("_Highlights");
    public static readonly int _ShaHiLimits = Shader.PropertyToID("_ShaHiLimits");
    public static readonly int _SplitShadows = Shader.PropertyToID("_SplitShadows");
    public static readonly int _SplitHighlights = Shader.PropertyToID("_SplitHighlights");
    public static readonly int _CurveMaster = Shader.PropertyToID("_CurveMaster");
    public static readonly int _CurveRed = Shader.PropertyToID("_CurveRed");
    public static readonly int _CurveGreen = Shader.PropertyToID("_CurveGreen");
    public static readonly int _CurveBlue = Shader.PropertyToID("_CurveBlue");
    public static readonly int _CurveHueVsHue = Shader.PropertyToID("_CurveHueVsHue");
    public static readonly int _CurveHueVsSat = Shader.PropertyToID("_CurveHueVsSat");
    public static readonly int _CurveLumVsSat = Shader.PropertyToID("_CurveLumVsSat");
    public static readonly int _CurveSatVsSat = Shader.PropertyToID("_CurveSatVsSat");

    public static readonly int _FogParams = Shader.PropertyToID("_FogParams");
    public static readonly int _FogColor = Shader.PropertyToID("_FogColor");
    

    public static readonly string HighQualitySampling = "_HIGH_QUALITY_SAMPLING";

    public static readonly string PaniniGeneric = "_GENERIC";
    public static readonly string PaniniUnitDistance = "_UNIT_DISTANCE";

    public static readonly string BloomLQ = "_BLOOM_LQ";
    public static readonly string BloomHQ = "_BLOOM_HQ";
    public static readonly string BloomLQDirt = "_BLOOM_LQ_DIRT";
    public static readonly string BloomHQDirt = "_BLOOM_HQ_DIRT";
    public static readonly string UseRGBM = "_USE_RGBM";
    public static readonly string Distortion = "_DISTORTION";
    public static readonly string ChromaticAberration = "_CHROMATIC_ABERRATION";
    public static readonly string HDRGrading = "_HDR_GRADING";
    public static readonly string TonemapACES = "_TONEMAP_ACES";
    public static readonly string TonemapNeutral = "_TONEMAP_NEUTRAL";
    public static readonly string FilmGrain = "_FILM_GRAIN";
    public static readonly string Fxaa = "_FXAA";
    public static readonly string Dithering = "_DITHERING";
    public static readonly string ScreenSpaceOcclusion = "_SCREEN_SPACE_OCCLUSION";

    public static readonly string LinearToSRGBConversion = "_LINEAR_TO_SRGB_CONVERSION";


    //HBAO
    public static int mainTex = Shader.PropertyToID("_MainTex");
    public static int hbaoTex = Shader.PropertyToID("_HBAOTex");
    public static int tempTex = Shader.PropertyToID("_TempTex");
    public static int tempTex2 = Shader.PropertyToID("_TempTex2");
    public static int noiseTex = Shader.PropertyToID("_NoiseTex");
    public static int depthTex = Shader.PropertyToID("_DepthTex");
    public static int normalsTex = Shader.PropertyToID("_NormalsTex");
    public static int[] depthSliceTex;
    public static int[] normalsSliceTex;
    public static int[] aoSliceTex;
    public static int[] deinterleaveOffset;
    public static int atlasOffset = Shader.PropertyToID("_AtlasOffset");
    public static int jitter = Shader.PropertyToID("_Jitter");
    public static int uvTransform = Shader.PropertyToID("_UVTransform");
    public static int inputTexelSize = Shader.PropertyToID("_Input_TexelSize");
    public static int aoTexelSize = Shader.PropertyToID("_AO_TexelSize");
    public static int deinterleavedAOTexelSize = Shader.PropertyToID("_DeinterleavedAO_TexelSize");
    public static int reinterleavedAOTexelSize = Shader.PropertyToID("_ReinterleavedAO_TexelSize");
    public static int uvToView = Shader.PropertyToID("_UVToView");
    public static int worldToCameraMatrix = Shader.PropertyToID("_WorldToCameraMatrix");
    public static int targetScale = Shader.PropertyToID("_TargetScale");
    public static int radius = Shader.PropertyToID("_Radius");
    public static int maxRadiusPixels = Shader.PropertyToID("_MaxRadiusPixels");
    public static int negInvRadius2 = Shader.PropertyToID("_NegInvRadius2");
    public static int angleBias = Shader.PropertyToID("_AngleBias");
    public static int aoMultiplier = Shader.PropertyToID("_AOmultiplier");
    public static int intensity = Shader.PropertyToID("_Intensity");
    public static int multiBounceInfluence = Shader.PropertyToID("_MultiBounceInfluence");
    public static int offscreenSamplesContrib = Shader.PropertyToID("_OffscreenSamplesContrib");
    public static int maxDistance = Shader.PropertyToID("_MaxDistance");
    public static int distanceFalloff = Shader.PropertyToID("_DistanceFalloff");
    public static int baseColor = Shader.PropertyToID("_BaseColor");
    public static int colorBleedSaturation = Shader.PropertyToID("_ColorBleedSaturation");
    public static int albedoMultiplier = Shader.PropertyToID("_AlbedoMultiplier");
    public static int colorBleedBrightnessMask = Shader.PropertyToID("_ColorBleedBrightnessMask");
    public static int colorBleedBrightnessMaskRange = Shader.PropertyToID("_ColorBleedBrightnessMaskRange");
    public static int blurDeltaUV = Shader.PropertyToID("_BlurDeltaUV");
    public static int blurSharpness = Shader.PropertyToID("_BlurSharpness");
    public static int temporalParams = Shader.PropertyToID("_TemporalParams");

    //GTAO



    static ShaderConstant()
    {
        depthSliceTex = new int[4 * 4];
        normalsSliceTex = new int[4 * 4];
        aoSliceTex = new int[4 * 4];
        for (int i = 0; i < 4 * 4; i++)
        {
            depthSliceTex[i] = Shader.PropertyToID("_DepthSliceTex" + i);
            normalsSliceTex[i] = Shader.PropertyToID("_NormalsSliceTex" + i);
            aoSliceTex[i] = Shader.PropertyToID("_AOSliceTex" + i);
        }
        deinterleaveOffset = new int[] {
                Shader.PropertyToID("_Deinterleave_Offset00"),
                Shader.PropertyToID("_Deinterleave_Offset10"),
                Shader.PropertyToID("_Deinterleave_Offset01"),
                Shader.PropertyToID("_Deinterleave_Offset11")
            };
    }
}
public enum Antialiasing
{
    None,
    FastApproximateAntialiasing,
    SubpixelMorphologicalAntialiasing,
}
public enum AntialiasingQuality
{
    Low,
    Medium,
    High
}
public enum ColorGradingMode
{
    LowDynamicRange,
    HighDynamicRange
}
