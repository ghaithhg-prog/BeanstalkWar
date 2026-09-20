#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Netcode;

public static class CrystalSetupTool
{
    [MenuItem("BeanstalkWar/Setup Crystal & 3 Altars in Scene")]
    public static void SetupCrystalAndAltars()
    {
        // ============================================
        // 1. إعداد الكريستالة (Crystal Item)
        // ============================================
        CrystalItem existingCrystal = Object.FindAnyObjectByType<CrystalItem>();
        GameObject crystalObj;

        if (existingCrystal == null)
        {
            crystalObj = new GameObject("CrystalItem");
            Undo.RegisterCreatedObjectUndo(crystalObj, "Create Crystal Item");

            // NetworkObject
            NetworkObject netObj = crystalObj.AddComponent<NetworkObject>();

            // CrystalItem Component
            CrystalItem crystalScript = crystalObj.AddComponent<CrystalItem>();

            // Collider للالتقاط
            SphereCollider col = crystalObj.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.5f;

            // الشكل البصري (Visual Model)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "CrystalVisual";
            visual.transform.SetParent(crystalObj.transform, false);
            visual.transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);

            // إزالة Collider من الشكل البصري حتى لا يتداخل
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // إضافة توهج ضوئي
            GameObject lightObj = new GameObject("CrystalGlow");
            lightObj.transform.SetParent(crystalObj.transform, false);
            Light glow = lightObj.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0f, 0.9f, 1f); // كحلي / سماوي متوهج
            glow.range = 8f;
            glow.intensity = 3f;

            // ربط المتغيرات في السكربت
            crystalScript.visualModel = visual;
            crystalScript.glowLight = glow;

            // محاولة وضعها في قمة الشجرة إن وجدت
            GameObject topLevel = GameObject.Find("TopLevel");
            if (topLevel != null)
            {
                crystalObj.transform.position = topLevel.transform.position + Vector3.up * 1.5f;
                crystalScript.topSpawnPoint = topLevel.transform;
            }

            Debug.Log("✅ [BeanstalkWar] تم إنشاء الكريستالة ومكوناتها بنجاح في المشهد!");
        }
        else
        {
            Debug.Log("ℹ️ [BeanstalkWar] الكريستالة موجودة بالفعل في المشهد.");
        }

        // ============================================
        // 2. إعداد المذابح الثلاثة (3 Sacred Altars)
        // ============================================
        CrystalAltar[] existingAltars = Object.FindObjectsByType<CrystalAltar>(FindObjectsSortMode.None);
        if (existingAltars.Length == 0)
        {
            GameObject altarsGroup = GameObject.Find("SacredAltars");
            if (altarsGroup == null)
            {
                altarsGroup = new GameObject("SacredAltars");
                Undo.RegisterCreatedObjectUndo(altarsGroup, "Create Sacred Altars Group");
            }

            // إيجاد مركز الشجرة لتوزيع المذابح حولها
            Vector3 center = Vector3.zero;
            GameObject tree = GameObject.FindGameObjectWithTag("Tree");
            if (tree != null) center = tree.transform.position;
            center.y = 0f;

            // 3 زوايا حول القاعدة (مسافة 15 متراً من المركز)
            float radius = 15f;
            string[] names = { "Altar_East (شرق)", "Altar_SouthWest (جنوب غرب)", "Altar_NorthWest (شمال غرب)" };
            float[] angles = { 0f, 120f, 240f };

            for (int i = 0; i < 3; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(rad), 0.1f, Mathf.Sin(rad)) * radius;

                GameObject altar = new GameObject(names[i]);
                altar.transform.position = pos;
                altar.transform.SetParent(altarsGroup.transform, true);
                Undo.RegisterCreatedObjectUndo(altar, "Create Altar " + i);

                // NetworkObject
                altar.AddComponent<NetworkObject>();

                // CrystalAltar
                CrystalAltar altarScript = altar.AddComponent<CrystalAltar>();
                altarScript.altarName = names[i];
                altarScript.triggerRadius = 3.5f;

                // منصة المذبح البصرية
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                platform.name = "PlatformMesh";
                platform.transform.SetParent(altar.transform, false);
                platform.transform.localScale = new Vector3(6f, 0.15f, 6f);
                Object.DestroyImmediate(platform.GetComponent<Collider>());

                // Trigger Collider
                SphereCollider col = altar.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 3.5f;

                // ضوء أخضر مميز للمذبح
                GameObject lightObj = new GameObject("AltarLight");
                lightObj.transform.SetParent(altar.transform, false);
                lightObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.2f, 1f, 0.4f); // أخضر زمردي مقدس
                light.range = 7f;
                light.intensity = 2f;
                altarScript.altarLight = light;
            }

            Debug.Log("✅ [BeanstalkWar] تم إنشاء المذابح الثلاثة بنجاح حول قاعدة الشجرة!");
        }
        else
        {
            Debug.Log("ℹ️ [BeanstalkWar] المذابح موجودة بالفعل في المشهد.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
    }
}
#endif
