using System.IO;
using UnityEditor;
using UnityEngine;

namespace FPSGame.Editor
{
    public static class MeshyArenaSetup
    {
        private const string Root = "Assets/Art/Environment/MeshyArenas";
        private const string ResourcesRoot = "Assets/Resources/ArenaKit";

        [MenuItem("Tools/Graphics/Integrate Meshy Arenas")]
        public static void Integrate()
        {
            Directory.CreateDirectory(ResourcesRoot);

            Build("Arena1", "Map1/Meshy_AI_Sci_Fi_Energy_Arena_1012150108_texture_fbx", "Meshy_AI_Sci_Fi_Energy_Arena_1012150108_texture", false);
            Build("Arena2", "Map2/Meshy_AI_Infernal_Bastion_1012150212_texture_fbx", "Meshy_AI_Infernal_Bastion_1012150212_texture", false);
            Build("Arena3", "Map3/Meshy_AI_Crimson_Citadel_Arena_1012150233_texture_fbx", "Meshy_AI_Crimson_Citadel_Arena_1012150233_texture", false);
            Build("Barricade", "Obstacle1/Meshy_AI_Ironclad_Barricade_1012150348_texture_fbx", "Meshy_AI_Ironclad_Barricade_1012150348_texture", true);
            Build("Reactor", "Obstacle2/Meshy_AI_Embercore_Reactor_1012150615_texture_fbx", "Meshy_AI_Embercore_Reactor_1012150615_texture", true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Meshy Arenas] Created three arena prefabs and two reusable obstacle prefabs.");
        }

        private static void Build(string outputName, string relativeFolder, string baseName, bool addSimpleCollider)
        {
            string sourceFolder = Root + "/Source/" + relativeFolder;
            string modelPath = sourceFolder + "/" + baseName + ".fbx";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null) throw new FileNotFoundException("Missing Meshy model", modelPath);

            Material material = BuildMaterial(outputName, sourceFolder, baseName);
            GameObject root = new GameObject(outputName);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;

            if (addSimpleCollider)
            {
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    BoxCollider collider = root.AddComponent<BoxCollider>();
                    collider.center = root.transform.InverseTransformPoint(bounds.center);
                    collider.size = bounds.size;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, ResourcesRoot + "/" + outputName + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static Material BuildMaterial(string outputName, string folder, string baseName)
        {
            string path = Root + "/" + outputName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            string normalPath = folder + "/" + baseName + "_normal.png";
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            material.shader = Shader.Find("Standard");
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + baseName + ".png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + baseName + "_metallic.png"));
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_Metallic", 0.45f);
            material.SetFloat("_Glossiness", 0.32f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
