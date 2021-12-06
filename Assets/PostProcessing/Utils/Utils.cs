using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityObject = UnityEngine.Object;
public static class Utils 
{
    public static bool CheckSupportHDR()
    {
        var check = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat)
                    || SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
        return check;
    }
    public static Material CreateMaterial(Shader shader)
    {
        if (shader == null)
        {
            Debug.LogError("Cannot create required material because shader is null");
            return null;
        }
        else if (!shader.isSupported)
        {
            Debug.LogError(shader.name+" is not support.");
            return null;
        }
        var mat = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return mat;
    }

    public static void SetKeyword(Material material, string keyword, bool state)
    {
        if (state)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }
    public static BuiltinRenderTextureType BlitDstDiscardContent(CommandBuffer cmd, RenderTargetIdentifier rt)
    {
        // We set depth to DontCare because rt might be the source of PostProcessing used as a temporary target
        // Source typically comes with a depth buffer and right now we don't have a way to only bind the color attachment of a RenderTargetIdentifier
        cmd.SetRenderTarget(new RenderTargetIdentifier(rt, 0, CubemapFace.Unknown, -1),
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
        return BuiltinRenderTextureType.CurrentActive;
    }
    public static void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex = 0)
    {
        cmd.SetGlobalTexture(ShaderConstant._SourceTex, source);
        cmd.Blit(source, destination, material, passIndex);
    }

    public static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int pass = 0)
    {
        cmd.SetGlobalTexture(ShaderConstant.mainTex, source);
        cmd.SetRenderTarget(destination);
        cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, pass);
    }
    public static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier[] destinations, Material material, int pass = 0)
    {
        cmd.SetGlobalTexture(ShaderConstant.mainTex, source);
        cmd.SetRenderTarget(destinations, destinations[0]);
        cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, pass);
    }
    public static void BlitFullscreenTriangleWithClear(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, Color clearColor, int pass = 0)
    {
        cmd.SetGlobalTexture(ShaderConstant.mainTex, source);
        cmd.SetRenderTarget(destination);
        cmd.ClearRenderTarget(false, true, clearColor);
        cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, pass);
    }
    private static Mesh m_FullscreenTriangle;
    public static Mesh fullscreenTriangle
    {
        get
        {
            if (m_FullscreenTriangle != null)
                return m_FullscreenTriangle;

            m_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

            // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
            // this directly in the vertex shader using vertex ids :(
            m_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                });
            m_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
            m_FullscreenTriangle.UploadMeshData(false);

            return m_FullscreenTriangle;
        }
    }
    public static void Destroy(UnityObject obj)
    {
        if (obj != null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityObject.Destroy(obj);
            else
                UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
        }
    }

    static IEnumerable<Type> m_AssemblyTypes;
    public static IEnumerable<Type> GetAllAssemblyTypes()
    {
        if (m_AssemblyTypes == null)
        {
            m_AssemblyTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t =>
                {
                        // Ugly hack to handle mis-versioned dlls
                        var innerTypes = new Type[0];
                    try
                    {
                        innerTypes = t.GetTypes();
                    }
                    catch { }
                    return innerTypes;
                });
        }

        return m_AssemblyTypes;
    }

    /// <summary>
    /// Returns a list of types that inherit from the provided type.
    /// </summary>
    /// <typeparam name="T">Parent Type</typeparam>
    /// <returns>A list of types that inherit from the provided type.</returns>
    public static IEnumerable<Type> GetAllTypesDerivedFrom<T>()
    {
#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
        return UnityEditor.TypeCache.GetTypesDerivedFrom<T>();
#else
            return GetAllAssemblyTypes().Where(t => t.IsSubclassOf(typeof(T)));
#endif
    }

    public static bool isLinearColorSpace { get { return QualitySettings.activeColorSpace == ColorSpace.Linear; } }
    public static Vector2 AdjustBrightnessMaskToGammaSpace(Vector2 v)
    {
        return isLinearColorSpace ? v : new Vector2(Mathf.LinearToGammaSpace(v.x), Mathf.LinearToGammaSpace(v.y));
    }

    public static TextureDimension dimension
    {
        get
        {
            // TEXTURE2D_X macros will now expand to TEXTURE2D or TEXTURE2D_ARRAY
            return useTexArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D;
        }
    }
    public static bool useTexArray
    {
        get
        {
            switch (SystemInfo.graphicsDeviceType)
            {
                case GraphicsDeviceType.Direct3D11:
                case GraphicsDeviceType.Direct3D12:
                case GraphicsDeviceType.PlayStation4:
                case GraphicsDeviceType.PlayStation5:
                case GraphicsDeviceType.Vulkan:
                    return true;

                default:
                    return false;
            }
        }
    }
}
