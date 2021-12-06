using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[AddComponentMenu("PostProcessing/PPVolume")]
public class PPVolume : MonoBehaviour
{
    public PPVolumeProfile profile = null;

    public bool isGlobal = true;

    [Tooltip("Sets the Volume priority in the stack. A higher value means higher priority. You can use negative values.")]
    public float priority = 0f;

    [Range(0f, 1f), Tooltip("Sets the total weight of this Volume in the Scene. 0 means no effect and 1 means full effect.")]
    public float weight = 1f;

    [Tooltip("Sets the outer distance to start blending from. A value of 0 means no blending and Unity applies the Volume overrides immediately upon entry.")]
    public float blendDistance = 0f;


    public int m_PreviousLayer;
    float m_PreviousPriority;

    private void OnEnable()
    {
        m_PreviousLayer = 1 << gameObject.layer;
        if (profile != null)
        {
            if (profile.lutCount > 0)
            {
                preLut = true;
            }
        }
        PPManager.instance.Register(this, m_PreviousLayer);
    }
    void OnDisable()
    {
        PPManager.instance.Unregister(this, m_PreviousLayer);
    }

    void Update()
    {
        // Unfortunately we need to track the current layer to update the volume manager in
        // real-time as the user could change it at any time in the editor or at runtime.
        // Because no event is raised when the layer changes, we have to track it on every
        // frame :/
        UpdateLayer();

        // Same for priority. We could use a property instead, but it doesn't play nice with the
        // serialization system. Using a custom Attribute/PropertyDrawer for a property is
        // possible but it doesn't work with Undo/Redo in the editor, which makes it useless for
        // our case.
        if (priority != m_PreviousPriority)
        {
            PPManager.instance.ResortByPriority(this, m_PreviousLayer);
            m_PreviousPriority = priority;
        }
    }

    internal void UpdateLayer()
    {
        int layer = 1 << gameObject.layer;
        if (layer != m_PreviousLayer)
        {
            PPManager.instance.UpdateVolumeLayer(this, m_PreviousLayer, layer);
            m_PreviousLayer = layer;
        }
    }
    bool preLut = false;
    public void Change()
    {
        bool tmpLut;
        if (profile != null && profile.lutCount > 0)
            tmpLut = true;
        else
            tmpLut = false;
        if (tmpLut || preLut)
        {
            PPManager.instance.VolumeChange(this, m_PreviousLayer, true);
        }
        else
        {
            PPManager.instance.VolumeChange(this, m_PreviousLayer, false);
        }
        preLut = tmpLut;
    }
}
