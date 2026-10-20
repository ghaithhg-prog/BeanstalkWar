#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Netcode;

[InitializeOnLoad]
public static class LevelBossSetupTool
{
    [MenuItem("BeanstalkWar/Setup 3 Level Bosses in Scene")]
    public static void SetupBossesInScene()
    {
        GameObject botPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bot.prefab");
        if (botPrefab == null)
        {
            Debug.LogError("LevelBossSetupTool: Could not find Assets/Prefabs/Bot.prefab!");
            return;
        }

        GameObject treeLevelsObj = GameObject.Find("TreeLevels");
        if (treeLevelsObj == null)
        {
            Debug.LogError("LevelBossSetupTool: TreeLevels object not found in current scene!");
            return;
        }

        // إعدادات الزعماء الثلاثة لكل مستوى
        CreateOrUpdateBoss(treeLevelsObj, "Level_1", 1, "FOREST GOLIATH [LV 1]", 1800f, 38f, 2.2f, 
            new Color(0.2f, 0.75f, 0.3f), new Color(0.4f, 1f, 0.2f), 250, 500, botPrefab);

        CreateOrUpdateBoss(treeLevelsObj, "Level_2", 2, "INFERNO TITAN [LV 2]", 2600f, 55f, 2.5f, 
            new Color(0.92f, 0.25f, 0.1f), new Color(1f, 0.45f, 0f), 400, 750, botPrefab);

        CreateOrUpdateBoss(treeLevelsObj, "Level_3", 3, "ANCIENT OVERLORD [LV 3]", 3500f, 75f, 2.8f, 
            new Color(0.85f, 0.7f, 0.15f), new Color(1f, 0.85f, 0.2f), 600, 1200, botPrefab);

        // التأكد من وجود PlayerProgressionAndShopUI في المشهد
        EnsureProgressionUIInScene();

        // حفظ المشهد
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("🎉 ALL 3 LEVEL BOSSES CREATED & CONFIGURED IN SCENE HIERARCHY!");
        EditorUtility.DisplayDialog("BeanstalkWar", "تم بنجاح تثبيت الـ 3 زعماء في مشهد اللعبة داخل Hierarchy تحت كل مستوى!\n- Boss_Level_1 تحت Level_1\n- Boss_Level_2 تحت Level_2\n- Boss_Level_3 تحت Level_3\nوتم استبعاد TopLevel للكريستالة.", "موافق");
    }

    static void CreateOrUpdateBoss(GameObject treeLevels, string levelName, int levelNum, string title, 
        float hp, float dmg, float scale, Color color, Color aura, int coins, int xp, GameObject prefab)
    {
        Transform levelTf = treeLevels.transform.Find(levelName);
        if (levelTf == null)
        {
            Debug.LogWarning($"Level {levelName} not found under TreeLevels!");
            return;
        }

        string bossObjName = $"Boss_{levelName}";
        Transform existingBoss = treeLevels.transform.Find(bossObjName);
        if (existingBoss == null) existingBoss = levelTf.Find(bossObjName);
        GameObject bossObj;

        if (existingBoss == null)
        {
            bossObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, treeLevels.transform);
            bossObj.name = bossObjName;
            Undo.RegisterCreatedObjectUndo(bossObj, "Create Level Boss");
        }
        else
        {
            bossObj = existingBoss.gameObject;
            bossObj.transform.SetParent(treeLevels.transform, true);
        }

        bossObj.transform.position = levelTf.position + Vector3.up * 1.5f;
        bossObj.transform.localScale = Vector3.one * scale;

        // إيقاف ذكاء البوت العادي
        BotController botAI = bossObj.GetComponent<BotController>();
        if (botAI != null)
        {
            botAI.enabled = false;
        }

        // ضبط سكربت الزعيم
        LevelBoss boss = bossObj.GetComponent<LevelBoss>();
        if (boss == null)
        {
            boss = Undo.AddComponent<LevelBoss>(bossObj);
        }

        boss.SetupBossData(levelNum, title, hp, dmg, scale, color, aura, coins, xp);

        // تلوين النموذج
        Renderer[] renderers = bossObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r.gameObject.CompareTag("Weapon") || r.gameObject.CompareTag("Crystal")) continue;
            r.material.color = color;
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", color * 0.5f);
        }

        Debug.Log($"✔ Setup {bossObjName} under {levelName} successfully.");
    }

    static void EnsureProgressionUIInScene()
    {
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager != null)
        {
            if (gameManager.GetComponent<PlayerProgressionAndShopUI>() == null)
            {
                Undo.AddComponent<PlayerProgressionAndShopUI>(gameManager);
            }
            if (gameManager.GetComponent<LevelBossManager>() == null)
            {
                Undo.AddComponent<LevelBossManager>(gameManager);
            }
        }
    }
}
#endif
