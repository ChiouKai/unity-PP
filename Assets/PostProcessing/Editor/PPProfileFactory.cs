using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPProfileFactory
{
    [MenuItem("Asset/Create/Volume Profile")]
    static void CreatVolumeProfile()
    {
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            ScriptableObject.CreateInstance<DoCreatePostProcessProfile>(),
            "New VolumeProfile.asset",
            null,
            null);
    }
    public static PPVolumeProfile CreateVolumeProfileAtPath(string path)
    {
        var profile = ScriptableObject.CreateInstance<PPVolumeProfile>();
        profile.name = Path.GetFileName(path);
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return profile;
    }
    public static PPVolumeProfile CreateVolumeProfile(Scene scene, string targetName)
    {
        string path;

        if (string.IsNullOrEmpty(scene.path))
        {
            path = "Assets/";
        }
        else
        {
            var scenePath = Path.GetDirectoryName(scene.path);
            var extPath = scene.name;
            var profilePath = scenePath + Path.DirectorySeparatorChar + extPath;

            if (!AssetDatabase.IsValidFolder(profilePath))
            {
                var directories = profilePath.Split(Path.DirectorySeparatorChar);
                string rootPath = "";
                foreach (var directory in directories)
                {
                    var newPath = rootPath + directory;
                    if (!AssetDatabase.IsValidFolder(newPath))
                        AssetDatabase.CreateFolder(rootPath.TrimEnd(Path.DirectorySeparatorChar), directory);
                    rootPath = newPath + Path.DirectorySeparatorChar;
                }
            }

            path = profilePath + Path.DirectorySeparatorChar;
        }

        path += targetName + " Profile.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        var profile = ScriptableObject.CreateInstance<PPVolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return profile;
    }


}
class DoCreatePostProcessProfile : EndNameEditAction
{
    public override void Action(int instanceId, string pathName, string resourceFile)
    {
        var profile = PPProfileFactory.CreateVolumeProfileAtPath(pathName);
        ProjectWindowUtil.ShowCreatedAsset(profile);
    }
}
