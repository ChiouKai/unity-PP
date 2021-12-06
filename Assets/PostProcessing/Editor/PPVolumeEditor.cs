using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

[CustomEditor(typeof(PPVolume))]
public class PPVolumeEditor : Editor
{
    SerializedProperty m_IsGlobal;
    SerializedProperty m_BlendRadius;
    SerializedProperty m_Weight;
    SerializedProperty m_Priority;
    SerializedProperty m_Profile;

    PPVolume volume;
    PPVolumeComponentListEditor m_ComponentList;

    readonly GUIContent[] m_Modes = { new GUIContent("Global"), new GUIContent("Local") };
    private void OnEnable()
    {
        m_IsGlobal = serializedObject.FindProperty("isGlobal");
        m_BlendRadius = serializedObject.FindProperty("blendDistance");
        m_Weight = serializedObject.FindProperty("weight");
        m_Priority = serializedObject.FindProperty("priority");
        m_Profile = serializedObject.FindProperty("profile");

        volume = (PPVolume)target;
        m_ComponentList = new PPVolumeComponentListEditor(this);
        RefreshEffectListEditor(volume.profile);//?
    }
    void OnDisable()
    {
        m_ComponentList?.Clear();
    }
    void RefreshEffectListEditor(PPVolumeProfile asset)
    {
        m_ComponentList.Clear();

        if (asset != null)
            m_ComponentList.Init(asset, new SerializedObject(asset));
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUIContent label = EditorGUIUtility.TrTextContent("Mode", "A global volume is applied to the whole scene.");
        Rect lineRect = EditorGUILayout.GetControlRect();
        int isGlobal = m_IsGlobal.boolValue ? 0 : 1;
        EditorGUI.BeginProperty(lineRect, label, m_IsGlobal);
        {
            EditorGUI.BeginChangeCheck();
            isGlobal = EditorGUI.Popup(lineRect, label, isGlobal, m_Modes);
            if (EditorGUI.EndChangeCheck())
                m_IsGlobal.boolValue = isGlobal == 0;
        }
        EditorGUI.EndProperty();

        if (isGlobal != 0) // Blend radius is not needed for global volumes
        {
            if (!volume.TryGetComponent<Collider>(out _))
            {
                EditorGUILayout.HelpBox("Add a Collider to this GameObject to set boundaries for the local Volume.", MessageType.Info);

                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Add Collider"), EditorStyles.miniButton))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(EditorGUIUtility.TrTextContent("Box"), false, () => Undo.AddComponent<BoxCollider>(volume.gameObject));
                    menu.AddItem(EditorGUIUtility.TrTextContent("Sphere"), false, () => Undo.AddComponent<SphereCollider>(volume.gameObject));
                    menu.AddItem(EditorGUIUtility.TrTextContent("Capsule"), false, () => Undo.AddComponent<CapsuleCollider>(volume.gameObject));
                    menu.AddItem(EditorGUIUtility.TrTextContent("Mesh"), false, () => Undo.AddComponent<MeshCollider>(volume.gameObject));
                    menu.ShowAsContext();
                }
            }
            EditorGUILayout.PropertyField(m_BlendRadius);
            m_BlendRadius.floatValue = Mathf.Max(m_BlendRadius.floatValue, 0f);
        }

        EditorGUILayout.PropertyField(m_Weight);
        EditorGUILayout.PropertyField(m_Priority);

        bool assetHasChanged = false;
        bool showCopy = m_Profile.objectReferenceValue != null;
        bool multiEdit = m_Profile.hasMultipleDifferentValues;

        // The layout system breaks alignment when mixing inspector fields with custom layout'd
        // fields, do the layout manually instead
        int buttonWidth = showCopy ? 45 : 60;
        float indentOffset = EditorGUI.indentLevel * 15f;
        lineRect = EditorGUILayout.GetControlRect();
        var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
        var fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * (showCopy ? 2 : 1), lineRect.height);
        var buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);
        var buttonCopyRect = new Rect(buttonNewRect.xMax, lineRect.y, buttonWidth, lineRect.height);

        GUIContent guiContent = EditorGUIUtility.TrTextContent("Profile", "A reference to a profile asset.");
        EditorGUI.PrefixLabel(labelRect, guiContent);

        using (var scope = new EditorGUI.ChangeCheckScope())
        {
            EditorGUI.BeginProperty(fieldRect, GUIContent.none, m_Profile);

            PPVolumeProfile profile;
            
            profile = (PPVolumeProfile)EditorGUI.ObjectField(fieldRect, m_Profile.objectReferenceValue, typeof(PPVolumeProfile), false);

            if (scope.changed)
            {
                assetHasChanged = true;
                m_Profile.objectReferenceValue = profile;
            }

            EditorGUI.EndProperty();
        }

        using (new EditorGUI.DisabledScope(multiEdit))
        {
            if (GUI.Button(buttonNewRect, EditorGUIUtility.TrTextContent("New", "Create a new profile."), showCopy ? EditorStyles.miniButtonLeft : EditorStyles.miniButton))
            {
                // By default, try to put assets in a folder next to the currently active
                // scene file. If the user isn't a scene, put them in root instead.
                var targetName = volume.name;
                var scene = volume.gameObject.scene;
                var asset = PPProfileFactory.CreateVolumeProfile(scene, targetName);
                m_Profile.objectReferenceValue = asset;
                assetHasChanged = true;
            }
                guiContent = EditorGUIUtility.TrTextContent("Clone", "Create a new profile and copy the content of the currently assigned profile.");
            if (showCopy && GUI.Button(buttonCopyRect, guiContent, EditorStyles.miniButtonRight))
            {
                // Duplicate the currently assigned profile and save it as a new profile
                var origin = volume.profile;
                var path = AssetDatabase.GetAssetPath(m_Profile.objectReferenceValue);

                path = IsAssetInReadOnlyPackage(path)
                    // We may be in a read only package, in that case we need to clone the volume profile in an
                    // editable area, such as the root of the project.
                    ? AssetDatabase.GenerateUniqueAssetPath(Path.Combine("Assets", Path.GetFileName(path)))
                    // Otherwise, duplicate next to original asset.
                    : AssetDatabase.GenerateUniqueAssetPath(path);

                var asset = Instantiate(origin);
                asset.components.Clear();
                AssetDatabase.CreateAsset(asset, path);

                foreach (var item in origin.components)
                {
                    var itemCopy = Instantiate(item);
                    itemCopy.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    itemCopy.name = item.name;
                    asset.components.Add(itemCopy);
                    AssetDatabase.AddObjectToAsset(itemCopy, asset);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                m_Profile.objectReferenceValue = asset;
                assetHasChanged = true;
            }
        }

        EditorGUILayout.Space();

        if (m_Profile.objectReferenceValue == null)
        {
            if (assetHasChanged)
            {
                m_ComponentList.Clear(); // Asset wasn't null before, do some cleanup
                volume.Change();
            }
        }
        else
        {
            if (assetHasChanged || volume.profile != m_ComponentList.asset)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                volume.Change();
                RefreshEffectListEditor(volume.profile);
            }

            if (!multiEdit)
            {
                m_ComponentList.OnGUI();
                EditorGUILayout.Space();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    static bool IsAssetInReadOnlyPackage(string path)
    {
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
        return info != null && (info.source != PackageSource.Local && info.source != PackageSource.Embedded);
    }
}
