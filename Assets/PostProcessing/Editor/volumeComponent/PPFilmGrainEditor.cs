using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[VolumeComponentEditor(typeof(PPFilmGrain))]
sealed class PPFilmGrainEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_Type;
    SerializedDataParameter m_Intensity;
    SerializedDataParameter m_Response;
    SerializedDataParameter m_Texture;

    public override void FindSerializedDataParameter()
    {
        m_Type = Unpack(serializedObject.FindProperty("type"));
        m_Intensity = Unpack(serializedObject.FindProperty("intensity"));
        m_Response = Unpack(serializedObject.FindProperty("response"));
        m_Texture = Unpack(serializedObject.FindProperty("texture"));
    }

    public override void OnInspectorGUI()
    {
        PropertyField(m_Type);

        if (m_Type.value.intValue == (int)FilmGrainLookup.Custom)
        {
            PropertyField(m_Texture);

            var texture = (target as PPFilmGrain).texture.value;

            if (texture != null)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;

                // Fails when using an internal texture as you can't change import settings on
                // builtin resources, thus the check for null
                if (importer != null)
                {
                    bool valid = importer.mipmapEnabled == false
                        && importer.alphaSource == TextureImporterAlphaSource.FromGrayScale
                        && importer.filterMode == FilterMode.Point
                        && importer.textureCompression == TextureImporterCompression.Uncompressed
                        && importer.textureType == TextureImporterType.SingleChannel;

                    if (!valid)
                        EditorUtils.DrawFixMeBox("Invalid texture import settings.", () => SetTextureImportSettings(importer));
                }
            }
        }

        PropertyField(m_Intensity);
        PropertyField(m_Response);
    }

    static void SetTextureImportSettings(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.SingleChannel;
        importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();
    }
}
