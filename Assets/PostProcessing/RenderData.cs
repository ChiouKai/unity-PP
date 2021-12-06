using System.Reflection;
using UnityEngine;
using System;

public class RenderData
{
    //public FieldInfo[] parameters { get; private set; }
    //uint id;
    Camera m_camera;
    public RenderData(Camera cam)
    {
        m_camera = cam;
        supportHDR = Utils.CheckSupportHDR();
        //parameters = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        //id = 0;
    }
    public bool UpdataAndCheck()
    {
        change = false;

        pixelHeight = m_camera.pixelHeight;
        pixelWidth = m_camera.pixelWidth;
        scaledPixelWidth = m_camera.scaledPixelWidth;
        scaledPixelHeight = m_camera.scaledPixelHeight;
        allowHDR = m_camera.allowHDR;
        targetTexture = m_camera.targetTexture;
        rect = m_camera.rect;
        pixelRect = m_camera.pixelRect;
        fieldOfView = m_camera.fieldOfView;
        allowDynamicResolution = m_camera.allowDynamicResolution;
        orthographic = m_camera.orthographic;
        renderingPath = m_camera.actualRenderingPath;
        //uint tmp = CheckValue();
        //if (id != tmp)
        //{
        //    change = true;
        //    id = tmp;
        //}

        return change;
    }

    //public uint CheckValue()
    //{
    //    unchecked
    //    {
    //        uint hash = 17;

    //        for (int i = 0; i < parameters.Length; i++)
    //            hash = hash * 23 + Convert.ToByte(parameters[i]);

    //        return hash;
    //    }
    //}

    public bool supportHDR;
    bool change;

    float m_fieldOfView;
    public float fieldOfView { get => m_fieldOfView; set { if (value != m_fieldOfView) { m_fieldOfView = value; change = true; } } }
    Rect m_rect;
    public Rect rect { get => m_rect; set { if (value != m_rect) { m_rect = value; change = true; } } }

    Rect m_pixelRect;
    public Rect pixelRect { get => m_pixelRect; set { if (value != m_pixelRect) { m_pixelRect = value; change = true; } } }

    bool m_allowHDR;
    public bool allowHDR { get => m_allowHDR; set { if (value != m_allowHDR) { m_allowHDR = value; change = true; } } }

    bool m_allowDynamicResolution;
    public bool allowDynamicResolution { get => m_allowDynamicResolution; set { if (value != m_allowDynamicResolution) { m_allowDynamicResolution = value; change = true; } } }

    RenderTexture m_targetTexture;
    public RenderTexture targetTexture { get => m_targetTexture; set { if (value != m_targetTexture) { m_targetTexture = value; change = true; } } }

    int m_pixelWidth;
    public int pixelWidth { get => m_pixelWidth; set { if (value != m_pixelWidth) { m_pixelWidth = value; change = true; } } }

    int m_pixelHeight;
    public int pixelHeight { get => m_pixelHeight; set { if (value != m_pixelHeight) { m_pixelHeight = value; change = true; } } }

    int m_scaledPixelWidth;
    public int scaledPixelWidth { get => m_scaledPixelWidth; set { if (value != m_scaledPixelWidth) { m_scaledPixelWidth = value; change = true; } } }

    int m_scaledPixelHeight;
    public int scaledPixelHeight { get => m_scaledPixelHeight; set { if (value != m_scaledPixelHeight) { m_scaledPixelHeight = value; change = true; } } }

    bool m_orthographic;
    public bool orthographic { get => m_orthographic; set { if (value != m_orthographic) { m_orthographic = value; change = true; } } }
    RenderingPath m_renderingPath;
    public RenderingPath renderingPath { get => m_renderingPath; set { if (value != m_renderingPath) { m_renderingPath = value; change = true; } } }
}


