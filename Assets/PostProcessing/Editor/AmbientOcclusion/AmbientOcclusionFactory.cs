using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

public static class AmbientOcclusionFactory
{
    [MenuItem("Asset/Create/AmbientOcclusion")]
    static void CreatAmbientOcclusion()
    {
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            ScriptableObject.CreateInstance<DoCreateAmbientOcclusion>(),
            "New AmbientOcclusion.asset",
            null,
            null);
    }
    public static AmbientOcclusion CreateAmbientOcclusionAtPath(string path)
    {
        var ambientOcclusion = ScriptableObject.CreateInstance<AmbientOcclusion>();
        ambientOcclusion.name = Path.GetFileName(path);
        AssetDatabase.CreateAsset(ambientOcclusion, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var baseAOComponentType = Utils.GetAllTypesDerivedFrom<AOComponent>().Where(t => !t.IsAbstract).ToArray();
        foreach (var type in baseAOComponentType)
        {
            var attribute = (AmbientOcclusionAttribute)type.GetCustomAttributes(typeof(AmbientOcclusionAttribute), false)[0];
            if (!ambientOcclusion.AODicKey.Contains((int)attribute.type))
            {
                var component = (AOComponent)ScriptableObject.CreateInstance(type);
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                if (EditorUtility.IsPersistent(ambientOcclusion))
                    AssetDatabase.AddObjectToAsset(component, ambientOcclusion);
                ambientOcclusion.AODicKey.Add((int)attribute.type);
                ambientOcclusion.AODicValue.Add(component);
            }
        }
        return ambientOcclusion;
    }

    class DoCreateAmbientOcclusion : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profile = CreateAmbientOcclusionAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
        }
    }
}
