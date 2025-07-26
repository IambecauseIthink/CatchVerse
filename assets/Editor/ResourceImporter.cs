using UnityEngine;
using UnityEditor;
using System.IO;

public class ResourceImporter : EditorWindow
{
    private string imagesPath = "";
    private string videosPath = "";
    private string modelsPath = "";

    [MenuItem("Tools/Resource Importer")]
    static void ShowWindow()
    {
        GetWindow<ResourceImporter>("Resource Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Creature Resource Importer", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        // Images
        GUILayout.Label("Images Folder:");
        imagesPath = EditorGUILayout.TextField(imagesPath);
        if (GUILayout.Button("Browse Images"))
        {
            imagesPath = EditorUtility.OpenFolderPanel("Select Images Folder", "", "");
        }
        
        GUILayout.Space(5);
        
        // Videos
        GUILayout.Label("Videos Folder:");
        videosPath = EditorGUILayout.TextField(videosPath);
        if (GUILayout.Button("Browse Videos"))
        {
            videosPath = EditorUtility.OpenFolderPanel("Select Videos Folder", "", "");
        }
        
        GUILayout.Space(5);
        
        // Models
        GUILayout.Label("Models Folder:");
        modelsPath = EditorGUILayout.TextField(modelsPath);
        if (GUILayout.Button("Browse Models"))
        {
            modelsPath = EditorUtility.OpenFolderPanel("Select Models Folder", "", "");
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Import All Resources"))
        {
            ImportResources();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Check Resources"))
        {
            CheckAllResources();
        }
    }

    void ImportResources()
    {
        if (!string.IsNullOrEmpty(imagesPath))
            ImportFiles(imagesPath, "Assets/Resources/Images", new string[] { ".png", ".jpg", ".jpeg" });
        
        if (!string.IsNullOrEmpty(videosPath))
            ImportFiles(videosPath, "Assets/Resources/Videos", new string[] { ".mp4", ".avi", ".mov" });
        
        if (!string.IsNullOrEmpty(modelsPath))
            ImportFiles(modelsPath, "Assets/Resources/Models", new string[] { ".glb", ".fbx", ".obj" });
        
        AssetDatabase.Refresh();
        Debug.Log("Resource import completed!");
    }

    void ImportFiles(string sourcePath, string targetPath, string[] extensions)
    {
        if (!Directory.Exists(sourcePath)) return;
        
        string[] files = Directory.GetFiles(sourcePath);
        foreach (string file in files)
        {
            string extension = Path.GetExtension(file).ToLower();
            if (System.Array.Exists(extensions, ext => ext == extension))
            {
                string fileName = Path.GetFileName(file);
                string targetFile = Path.Combine(targetPath, fileName);
                
                if (!File.Exists(targetFile))
                {
                    File.Copy(file, targetFile);
                    Debug.Log($"Imported: {fileName} to {targetPath}");
                }
            }
        }
    }

    [MenuItem("Tools/Check Resources")]
    static void CheckAllResources()
    {
        string[] creatures = { "dragon", "pikachu", "cat", "wolf", "fallback" };
        
        foreach (string creature in creatures)
        {
            bool image = Resources.Load<Sprite>($"Images/{creature}") != null;
            bool video = Resources.Load<UnityEngine.Video.VideoClip>($"Videos/{creature}") != null;
            bool model = Resources.Load<GameObject>($"Models/{creature}") != null;
            
            Debug.Log($"✅ {creature}: Image={image}, Video={video}, Model={model}");
        }
    }

    [MenuItem("Tools/Create Creature Configs")]
    static void CreateCreatureConfigs()
    {
        string[] creatures = { "dragon", "pikachu", "cat", "wolf", "fallback" };
        string configPath = "Assets/Resources/Configurations";
        
        foreach (string creature in creatures)
        {
            string assetPath = $"{configPath}/{creature}_config.asset";
            
            if (!AssetDatabase.LoadAssetAtPath<CreatureConfig>(assetPath))
            {
                CreatureConfig config = ScriptableObject.CreateInstance<CreatureConfig>();
                config.creatureId = creature;
                config.displayName = creature.ToUpper();
                config.modelPath = $"Models/{creature}";
                config.defaultScale = Vector3.one * 0.5f;
                config.defaultAnimation = "idle";
                config.isRare = creature == "dragon" || creature == "wolf";
                
                AssetDatabase.CreateAsset(config, assetPath);
                Debug.Log($"Created config: {creature}_config.asset");
            }
        }
        
        AssetDatabase.SaveAssets();
    }
}