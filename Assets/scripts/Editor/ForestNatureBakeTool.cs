#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Bakes permanent high-quality nature textures and materials to disk in Assets/Materials/
/// and assigns them directly to the scene GameObjects (Plane, Tree, Roots, TreeLevels, Paths)
/// so that the ground is permanently vibrant emerald grass and never gray!
/// </summary>
public static class ForestNatureBakeTool
{
    [MenuItem("BeanstalkWar/🌲 Permanently Bake Nature Materials to Scene")]
    public static void ManualBake()
    {
        BakeAndApplyAllNatureMaterials(true);
    }

    public static void BakeAndApplyAllNatureMaterials(bool showDialog)
    {
        if (!Directory.Exists("Assets/Materials"))
        {
            Directory.CreateDirectory("Assets/Materials");
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        // 1. Generate & Save PNG Textures
        Texture2D grassTex = CreateNoiseTex(256, 256, new Color(0.18f, 0.62f, 0.28f), new Color(0.28f, 0.78f, 0.38f), 18f, true);
        SaveTexturePNG(grassTex, "Assets/Materials/Grass_Albedo.png");

        Texture2D barkTex = CreateBarkTex(256, 256, new Color(0.32f, 0.20f, 0.10f), new Color(0.48f, 0.32f, 0.18f));
        SaveTexturePNG(barkTex, "Assets/Materials/Bark_Albedo.png");

        Texture2D canopyTex = CreateNoiseTex(256, 256, new Color(0.16f, 0.58f, 0.25f), new Color(0.25f, 0.72f, 0.35f), 12f, false);
        SaveTexturePNG(canopyTex, "Assets/Materials/Canopy_Albedo.png");

        Texture2D stoneTex = CreateNoiseTex(128, 128, new Color(0.40f, 0.42f, 0.38f), new Color(0.26f, 0.36f, 0.24f), 10f, false);
        SaveTexturePNG(stoneTex, "Assets/Materials/Stone_Albedo.png");

        Texture2D waterTex = CreateNoiseTex(128, 128, new Color(0.12f, 0.65f, 0.85f, 0.85f), new Color(0.25f, 0.85f, 0.95f, 0.9f), 8f, false);
        SaveTexturePNG(waterTex, "Assets/Materials/Water_Albedo.png");

        AssetDatabase.Refresh();

        // 2. Create or Update Material Assets
        Material grassMat = GetOrCreateMaterial("Assets/Materials/ForestGrass.mat", litShader);
        grassMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Grass_Albedo.png");
        grassMat.mainTextureScale = new Vector2(25f, 25f);
        if (grassMat.HasProperty("_Smoothness")) grassMat.SetFloat("_Smoothness", 0.12f);
        EditorUtility.SetDirty(grassMat);

        Material barkMat = GetOrCreateMaterial("Assets/Materials/AncientBark.mat", litShader);
        barkMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Bark_Albedo.png");
        barkMat.mainTextureScale = new Vector2(3f, 14f);
        if (barkMat.HasProperty("_Smoothness")) barkMat.SetFloat("_Smoothness", 0.08f);
        EditorUtility.SetDirty(barkMat);

        Material canopyMat = GetOrCreateMaterial("Assets/Materials/CanopyFoliage.mat", litShader);
        canopyMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Canopy_Albedo.png");
        canopyMat.mainTextureScale = new Vector2(4f, 4f);
        if (canopyMat.HasProperty("_Smoothness")) canopyMat.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(canopyMat);

        Material stoneMat = GetOrCreateMaterial("Assets/Materials/ForestStone.mat", litShader);
        stoneMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Stone_Albedo.png");
        stoneMat.mainTextureScale = new Vector2(3f, 3f);
        EditorUtility.SetDirty(stoneMat);

        Material waterMat = GetOrCreateMaterial("Assets/Materials/WaterfallWater.mat", litShader);
        waterMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Water_Albedo.png");
        waterMat.color = new Color(0.15f, 0.75f, 0.95f, 0.85f);
        if (waterMat.HasProperty("_Smoothness")) waterMat.SetFloat("_Smoothness", 0.92f);
        EditorUtility.SetDirty(waterMat);

        AssetDatabase.SaveAssets();

        // 3. Apply Directly to Scene Objects
        int objectsUpdated = 0;

        // Ground
        GameObject ground = GameObject.FindWithTag("Ground");
        if (ground == null) ground = GameObject.Find("Plane");
        if (ground != null)
        {
            MeshRenderer mr = ground.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = grassMat;
                EditorUtility.SetDirty(mr);
                objectsUpdated++;
            }
        }

        // Tree Trunk
        GameObject tree = GameObject.FindWithTag("Tree");
        if (tree == null) tree = GameObject.Find("Tree");
        if (tree != null)
        {
            MeshRenderer mr = tree.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = barkMat;
                EditorUtility.SetDirty(mr);
                objectsUpdated++;
            }
        }

        // Roots
        GameObject roots = GameObject.Find("Roots");
        if (roots != null)
        {
            foreach (MeshRenderer r in roots.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterial = barkMat;
                EditorUtility.SetDirty(r);
                objectsUpdated++;
            }
        }

        // Tree Levels
        GameObject levels = GameObject.Find("TreeLevels");
        if (levels != null)
        {
            foreach (MeshRenderer r in levels.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterial = canopyMat;
                EditorUtility.SetDirty(r);
                objectsUpdated++;
            }
        }

        // Paths
        GameObject paths = GameObject.Find("Paths");
        if (paths != null)
        {
            foreach (MeshRenderer r in paths.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterial = stoneMat;
                EditorUtility.SetDirty(r);
                objectsUpdated++;
            }
        }

        // 4. Save Scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log($"🌲 [ForestNatureBakeTool] Successfully baked & applied permanent nature materials to {objectsUpdated} scene objects!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("BeanstalkWar Nature Overhaul", 
                $"تم بنجاح تثبيت وتطبيق خامات الطبيعة الدائمة على المشهد:\n- الأرضية أصبحت عشب زمردي نضر\n- جذع الشجرة والجذور أصبحا خشب بلوط عتيق\n- منصات الطوابق أصبحت أوراق شجر مظلية\nتم حفظ المشهد بنجاح!", "رائع");
        }
    }

    static Material GetOrCreateMaterial(string path, Shader shader)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void SaveTexturePNG(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    static Texture2D CreateNoiseTex(int width, int height, Color col1, Color col2, float scale, bool specks)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width * scale;
                float ny = (float)y / height * scale;
                float n = Mathf.PerlinNoise(nx, ny);
                float n2 = Mathf.PerlinNoise(nx * 2.5f, ny * 2.5f) * 0.5f;
                Color c = Color.Lerp(col1, col2, Mathf.Clamp01(n * 0.7f + n2 * 0.3f));
                if (specks && (x * 17 + y * 11) % 31 == 0)
                {
                    c = Color.Lerp(c, new Color(1f, 0.9f, 0.3f), 0.4f);
                }
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static Texture2D CreateBarkTex(int width, int height, Color col1, Color col2)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width * 14f;
                float ny = (float)y / height * 3f;
                float grain = Mathf.PerlinNoise(nx, ny);
                float crevices = Mathf.Sin(nx * Mathf.PI * 4f + grain * 2f);
                Color c = Color.Lerp(col1, col2, Mathf.Clamp01(grain * 0.8f + crevices * 0.2f));
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
#endif
