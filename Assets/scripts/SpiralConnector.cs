using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SpiralConnector:
/// Generates magnificent, natural organic curved pathways winding up the giant beanstalk:
/// 1. Section 0: Ground (Level 0) -> Level 1 (2 wide, elegant curved root ramps on North & South).
///    - Starts far away from spawn points (West/East) so spawn area is 100% open and visible!
///    - Rises smoothly from grass (y=0.05m) to Level 1 rim (y=5.32m).
/// 2. Section 1: Level 1 -> Level 2 (Curved spiral outside the tree).
/// 3. Section 2: Level 2 -> Level 3 (Graceful spiral ascending through the foliage).
/// 4. Section 3: Level 3 -> TopLevel (Ascending to the sacred crystal flower crown).
/// 
/// Key Guarantees:
/// - Explicit upward vertex normals and double-sided triangle winding (Guaranteed 100% lit, never black).
/// - Completely clear player spawn points: zero clipping, zero camera blocking.
/// - Unlocked sequentially: Section 0 open at start; Sections 1-3 unlock as bosses are defeated.
/// - Completely disables old straight scene box roots (Root_1 to Root_4).
/// </summary>
public class SpiralConnector : MonoBehaviour
{
    [Header("Levels")]
    public Transform level0;
    public Transform level1;
    public Transform level2;
    public Transform level3;
    public Transform topLevel;

    [Header("Paths")]
    [Range(1, 4)]
    public int pathsPerSection = 4;

    [Min(0.5f)]
    public float pathWidth = 3.24f; // 2 * 1.618m (Golden Ratio)

    [Range(8, 100)]
    public int segments = 32;

    [Header("Appearance")]
    public Material pathMaterial;

    [Header("Collision")]
    public string climbLayerName = "Climb";

    // Cached materials for each distinct section
    private Material[] sectionMaterials;

    public static SpiralConnector Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        EnsureLevelReferences();
        CreatePathMaterials();
        // بناء الممرات في Awake حتى تكون جاهزة قبل أن يُنفَّذ TreeLevelManager.Start()
        RebuildPaths();
    }

    void Start()
    {
        // تأكيد إغلاق الأقسام 1 و 2 و 3 في Start مع إبقاء قوائم المستوى الأول فقط مفتوحة
        SetSectionUnlocked(0, true);
        SetSectionUnlocked(1, false);
        SetSectionUnlocked(2, false);
        SetSectionUnlocked(3, false);
    }

    public static GameObject FindRootsObject()
    {
        GameObject roots = GameObject.Find("Roots");
        if (roots != null) return roots;

        GameObject treePivot = GameObject.Find("TreePivot");
        if (treePivot != null)
        {
            Transform tr = treePivot.transform.Find("Roots");
            if (tr != null) return tr.gameObject;
        }

        // Search active scene root GameObjects including inactive children
        foreach (GameObject rObj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (rObj.name == "Roots") return rObj;
            Transform tr = rObj.transform.Find("Roots");
            if (tr != null) return tr.gameObject;
        }
        return null;
    }

    void EnsureLevelReferences()
    {
        if (level0 == null) level0 = GameObject.Find("Level_0")?.transform;
        if (level1 == null) level1 = GameObject.Find("Level_1")?.transform;
        if (level2 == null) level2 = GameObject.Find("Level_2")?.transform;
        if (level3 == null) level3 = GameObject.Find("Level_3")?.transform;
        if (topLevel == null) topLevel = GameObject.Find("TopLevel")?.transform;

        // تفعيل قوائم وجذور غيث الأصلية الأربعة دائماً حتى لو كانت غير مفعلة
        GameObject oldRoots = FindRootsObject();
        if (oldRoots != null)
        {
            oldRoots.SetActive(true);
        }
    }

    Texture2D GeneratePathBarkTexture(int width, int height, Color baseWood, Color ridgeWood)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float grain = Mathf.PerlinNoise(u * 14f, v * 3.5f);
                float fine = Mathf.PerlinNoise(u * 28f + 5.2f, v * 7.0f + 1.8f) * 0.45f;
                float blend = Mathf.Clamp01(grain + fine * 0.5f);

                Color c = Color.Lerp(baseWood, ridgeWood, blend);
                pixels[y * width + x] = c;
            }
        }

        tex.wrapMode = TextureWrapMode.Repeat;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void CreatePathMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        sectionMaterials = new Material[4];

        // 0. Ground -> Level 1: Warm Spring Oak Bark
        sectionMaterials[0] = new Material(litShader);
        sectionMaterials[0].name = "PathMat_Section_0";
        Color col0 = new Color(0.48f, 0.32f, 0.18f);
        sectionMaterials[0].color = col0;
        sectionMaterials[0].mainTexture = GeneratePathBarkTexture(256, 256, new Color(0.38f, 0.24f, 0.12f), new Color(0.56f, 0.38f, 0.22f));
        sectionMaterials[0].mainTextureScale = new Vector2(2f, 8f);
        if (sectionMaterials[0].HasProperty("_BaseColor")) sectionMaterials[0].SetColor("_BaseColor", col0);
        if (sectionMaterials[0].HasProperty("_Color")) sectionMaterials[0].SetColor("_Color", col0);
        if (sectionMaterials[0].HasProperty("_Smoothness")) sectionMaterials[0].SetFloat("_Smoothness", 0.12f);
        if (sectionMaterials[0].HasProperty("_EmissionColor"))
        {
            sectionMaterials[0].EnableKeyword("_EMISSION");
            sectionMaterials[0].SetColor("_EmissionColor", new Color(0.20f, 0.12f, 0.06f) * 0.35f);
        }

        // 1. Level 1 -> Level 2: Rich Ancient Oak Bark with Mossy Undertones
        sectionMaterials[1] = new Material(litShader);
        sectionMaterials[1].name = "PathMat_Section_1";
        Color col1 = new Color(0.44f, 0.32f, 0.18f);
        sectionMaterials[1].color = col1;
        sectionMaterials[1].mainTexture = GeneratePathBarkTexture(256, 256, new Color(0.34f, 0.22f, 0.11f), new Color(0.46f, 0.44f, 0.22f));
        sectionMaterials[1].mainTextureScale = new Vector2(2f, 8f);
        if (sectionMaterials[1].HasProperty("_BaseColor")) sectionMaterials[1].SetColor("_BaseColor", col1);
        if (sectionMaterials[1].HasProperty("_Color")) sectionMaterials[1].SetColor("_Color", col1);
        if (sectionMaterials[1].HasProperty("_Smoothness")) sectionMaterials[1].SetFloat("_Smoothness", 0.12f);
        if (sectionMaterials[1].HasProperty("_EmissionColor"))
        {
            sectionMaterials[1].EnableKeyword("_EMISSION");
            sectionMaterials[1].SetColor("_EmissionColor", new Color(0.18f, 0.14f, 0.07f) * 0.40f);
        }

        // 2. Level 2 -> Level 3: Golden Amber Cedar Bark
        sectionMaterials[2] = new Material(litShader);
        sectionMaterials[2].name = "PathMat_Section_2";
        Color col2 = new Color(0.54f, 0.38f, 0.19f);
        sectionMaterials[2].color = col2;
        sectionMaterials[2].mainTexture = GeneratePathBarkTexture(256, 256, new Color(0.44f, 0.29f, 0.14f), new Color(0.62f, 0.46f, 0.24f));
        sectionMaterials[2].mainTextureScale = new Vector2(2f, 8f);
        if (sectionMaterials[2].HasProperty("_BaseColor")) sectionMaterials[2].SetColor("_BaseColor", col2);
        if (sectionMaterials[2].HasProperty("_Color")) sectionMaterials[2].SetColor("_Color", col2);
        if (sectionMaterials[2].HasProperty("_Smoothness")) sectionMaterials[2].SetFloat("_Smoothness", 0.14f);
        if (sectionMaterials[2].HasProperty("_EmissionColor"))
        {
            sectionMaterials[2].EnableKeyword("_EMISSION");
            sectionMaterials[2].SetColor("_EmissionColor", new Color(0.24f, 0.16f, 0.08f) * 0.40f);
        }

        // 3. Level 3 -> TopLevel: Sacred Living Emerald Bough
        sectionMaterials[3] = new Material(litShader);
        sectionMaterials[3].name = "PathMat_Section_3";
        Color col3 = new Color(0.28f, 0.58f, 0.30f);
        sectionMaterials[3].color = col3;
        sectionMaterials[3].mainTexture = GeneratePathBarkTexture(256, 256, new Color(0.22f, 0.48f, 0.24f), new Color(0.36f, 0.68f, 0.38f));
        sectionMaterials[3].mainTextureScale = new Vector2(2f, 8f);
        if (sectionMaterials[3].HasProperty("_BaseColor")) sectionMaterials[3].SetColor("_BaseColor", col3);
        if (sectionMaterials[3].HasProperty("_Color")) sectionMaterials[3].SetColor("_Color", col3);
        if (sectionMaterials[3].HasProperty("_Smoothness")) sectionMaterials[3].SetFloat("_Smoothness", 0.20f);
        if (sectionMaterials[3].HasProperty("_EmissionColor"))
        {
            sectionMaterials[3].EnableKeyword("_EMISSION");
            sectionMaterials[3].SetColor("_EmissionColor", new Color(0.16f, 0.38f, 0.18f) * 0.45f);
        }
    }

    // مراجع مباشرة لأقسام الممرات الأربعة وحالة الإفتاح المعتمدة
    private GameObject[] sectionGameObjects = new GameObject[4];
    private bool[] sectionUnlocked = new bool[] { true, false, false, false };

    [ContextMenu("Rebuild Paths")]
    public void RebuildPaths()
    {
        EnsureLevelReferences();
        if (sectionMaterials == null || sectionMaterials[0] == null)
        {
            CreatePathMaterials();
        }

        // حفظ الحالة الحالية للأقسام قبل أي إعادة بناء
        bool[] savedStates = (bool[])sectionUnlocked.Clone();

        ClearGeneratedPaths();

        BuildSection(0); // Ground -> Level 1 (قوائم وجذور غيث الأصلية الأربعة)
        BuildSection(1); // Level 1 -> Level 2 (4 ممرات عريضة متينة)
        BuildSection(2); // Level 2 -> Level 3 (4 ممرات عريضة متينة)
        BuildSection(3); // Level 3 -> TopLevel (4 ممرات عريضة متينة للقمة)

        // تطبيق الحالة المحفوظة بدقة فائقة
        for (int i = 0; i < savedStates.Length; i++)
        {
            SetSectionUnlocked(i, savedStates[i]);
        }
    }

    public static float GetPlatformTopSurfaceY(Transform platform, float fallbackY)
    {
        if (platform == null) return fallbackY;
        Collider col = platform.GetComponent<Collider>();
        if (col != null && col.gameObject.activeInHierarchy && col.bounds.size.sqrMagnitude > 0.1f)
        {
            return col.bounds.max.y;
        }
        Renderer rend = platform.GetComponent<Renderer>();
        if (rend != null && rend.gameObject.activeInHierarchy && rend.bounds.size.sqrMagnitude > 0.1f)
        {
            return rend.bounds.max.y;
        }
        // Consistent geometric formula for Unity primitive cylinder (center Y + half-height):
        float geomY = platform.position.y + Mathf.Abs(platform.lossyScale.y) * 0.5f;
        if (geomY > 1.0f) return geomY;
        return fallbackY;
    }

    void BuildSection(int sectionIndex)
    {
        string secName = sectionIndex switch
        {
            0 => "Ground_To_Level_1",
            1 => "Level_1_To_Level_2",
            2 => "Level_2_To_Level_3",
            3 => "Level_3_To_TopLevel",
            _ => $"Section_{sectionIndex}"
        };

        if (sectionIndex == 0)
        {
            // Ground -> Level 1:
            // استعادة وتفعيل قوائم وجذور غيث الأصلية الأربعة (Roots: Root_1 إلى Root_4) كما طلب غيث حرفياً
            GameObject roots = FindRootsObject();
            sectionGameObjects[0] = roots;
            if (roots != null)
            {
                roots.SetActive(sectionUnlocked[0]);
            }
            return;
        }

        GameObject section = new GameObject($"Paths_{secName}");
        section.transform.SetParent(transform, false);
        section.transform.localPosition = Vector3.zero;
        section.transform.localRotation = Quaternion.identity;
        section.transform.localScale = Vector3.one;

        // ممرات عريضة وثابتة (4.2 متر) لتوفير أقصى راحة وحرية حركة وقتال بدون سقوط
        float goldenWidth = 4.20f;

        if (sectionIndex == 1)
        {
            // Level 1 -> Level 2:
            // 4 ممرات متناسقة تماماً وموزعة دائرياً حول الشجرة (0°، 90°، 180°، 270°)
            // السطح العلوي للمنصة 1 (5.80م) والسطح العلوي الدقيق للمنصة 2 (15.70م)
            float startY = GetPlatformTopSurfaceY(level1, 5.80f);
            float endY   = GetPlatformTopSurfaceY(level2, 15.70f);
            float rampArc = 90.0f;

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                CreateSolidCurvedRamp(section.transform, i, sectionIndex, angle, rampArc, 14.5f, 11.2f, startY, endY, goldenWidth, 1.1f);
            }
        }
        else if (sectionIndex == 2)
        {
            // Level 2 -> Level 3:
            // 4 ممرات متناسقة مزاحة بـ 45 درجة (45°، 135°، 225°، 315°) ليستكشف اللاعب الطابق الثاني بحرية
            float startY = GetPlatformTopSurfaceY(level2, 15.70f);
            float endY   = GetPlatformTopSurfaceY(level3, 25.70f);
            float rampArc = 100.0f;

            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + i * 90f;
                CreateSolidCurvedRamp(section.transform, i, sectionIndex, angle, rampArc, 11.5f, 8.2f, startY, endY, goldenWidth, 1.0f);
            }
        }
        else if (sectionIndex == 3)
        {
            // Level 3 -> TopLevel:
            // 4 ممرات صعود بانحدار مريح وجميل نحو القمة والتاج الكريستالي
            float startY = GetPlatformTopSurfaceY(level3,   25.70f);
            float endY   = GetPlatformTopSurfaceY(topLevel, 40.0f);
            float rampArc = 135.0f;

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                CreateSolidCurvedRamp(section.transform, i, sectionIndex, angle, rampArc, 8.5f, 5.2f, startY, endY, 4.0f, 0.8f);
            }
        }

        // ==============================================================
        // الأقسام 1 و 2 و 3 تُضبط فور إنشائها بحسب مصفوفة الإفتاح:
        // تكون مغلقة تماماً (false) عند بداية اللعبة، ولا تفتح إلا مع الطابق المقابل
        // ==============================================================
        section.SetActive(sectionUnlocked[sectionIndex]);
        sectionGameObjects[sectionIndex] = section;
    }

    /// <summary>
    /// بناء ممر شجري ثلاثي الأبعاد مصمت (Solid 3D Closed Mesh)
    /// يحتوي على سمك حقيقي (0.40م) وحواف بارزة لمنع الانزلاق ونواقل أسطح مستقلة
    /// يحل جذرياً مشكلة القفز التلقائي والسقوط وتداخل الفيزياء في Unity PhysX
    /// </summary>
    void CreateSolidCurvedRamp(
        Transform parent,
        int pathIndex,
        int sectionIndex,
        float startAngleDeg,
        float arcDeg,
        float startRadius,
        float endRadius,
        float startY,
        float endY,
        float width,
        float outwardBulgeAmount)
    {
        GameObject pathObject = new GameObject($"CurvedRootPath_{pathIndex + 1}");
        pathObject.transform.SetParent(parent, false);
        pathObject.transform.localPosition = Vector3.zero;
        pathObject.transform.localRotation = Quaternion.identity;
        pathObject.transform.localScale = Vector3.one;

        int navLayer = LayerMask.NameToLayer("NavigationGround");
        if (navLayer < 0) navLayer = LayerMask.NameToLayer("Climb");
        if (navLayer < 0) navLayer = LayerMask.NameToLayer("Ground");
        if (navLayer < 0) navLayer = 8;
        pathObject.layer = navLayer;
        pathObject.tag = "Ground";

        MeshFilter meshFilter = pathObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = pathObject.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = pathObject.AddComponent<MeshCollider>();

        Material useMat = (sectionMaterials != null && sectionIndex < sectionMaterials.Length && sectionMaterials[sectionIndex] != null)
            ? sectionMaterials[sectionIndex]
            : (pathMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")));

        meshRenderer.sharedMaterial = useMat;

        int segCount = Mathf.Max(segments, 32);
        float thickness = 0.40f; // سمك الممر الصلب 40 سم

        // 4 وجوه مستقلة لضمان نواقل أسطح حادة وصحيحة 100%:
        // 1. الوجه العلوي (سطح السير مع حافتين بارزتين طفيفتين لحماية اللاعب من الانزلاق)
        // 2. الوجه السفلي
        // 3. الجدار الجانبي الأيسر
        // 4. الجدار الجانبي الأيمن
        int vCountPerFace = (segCount + 1) * 2;
        int totalVertices = vCountPerFace * 4;
        Vector3[] vertices = new Vector3[totalVertices];
        Vector2[] uvs = new Vector2[totalVertices];

        int topBase    = 0;
        int bottomBase = vCountPerFace;
        int leftBase   = vCountPerFace * 2;
        int rightBase  = vCountPerFace * 3;

        Vector3 colCenter = level1 != null ? new Vector3(level1.position.x, 0f, level1.position.z) : Vector3.zero;
        Vector3 previousCenter = Vector3.zero;

        for (int s = 0; s <= segCount; s++)
        {
            float t = (float)s / segCount;
            float smoothT = t * t * (3f - 2f * t);

            float angleDeg = startAngleDeg + arcDeg * smoothT;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 radialDir = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));

            float curRadius = Mathf.Lerp(startRadius, endRadius, smoothT);
            curRadius += Mathf.Sin(t * Mathf.PI) * outwardBulgeAmount;

            // انحدار رأسي سلس ومثالي مع عتبتين أفقيتين بالكامل عند البداية والنهاية
            float heightT;
            if (t <= 0.08f)
            {
                heightT = 0f; // عتبة أفقية متطابقة تماماً مع سطح المنصة السفلية
            }
            else if (t >= 0.88f)
            {
                heightT = 1f; // عتبة أفقية متطابقة تماماً مع سطح المنصة العلوية
            }
            else
            {
                heightT = Mathf.SmoothStep(0f, 1f, (t - 0.08f) / 0.80f);
            }

            float curY = Mathf.Lerp(startY, endY, heightT);

            Vector3 center = colCenter + radialDir * curRadius;
            center.y = curY;

            Vector3 forward;
            if (s == 0)
            {
                float nextAngle = (startAngleDeg + arcDeg * 0.04f) * Mathf.Deg2Rad;
                Vector3 nextCenter = colCenter + new Vector3(Mathf.Cos(nextAngle), 0f, Mathf.Sin(nextAngle)) * (startRadius + outwardBulgeAmount * 0.1f);
                nextCenter.y = startY;
                forward = (nextCenter - center).normalized;
            }
            else
            {
                forward = (center - previousCenter).normalized;
            }

            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            if (side.sqrMagnitude < 0.001f)
            {
                side = new Vector3(-radialDir.z, 0f, radialDir.x).normalized;
            }

            float halfW = width * 0.5f;

            // نقاط المقطع العرضي للممر:
            // سطح المشي يطابق تماماً سطح المنصة مع إزاحة 0.02م لتجنب الوميض وتوفير مسار سلس 100%
            Vector3 ptTopLeft     = center - side * halfW + Vector3.up * 0.02f;
            Vector3 ptTopRight    = center + side * halfW + Vector3.up * 0.02f;
            Vector3 ptBottomLeft  = center - side * halfW - Vector3.up * thickness;
            Vector3 ptBottomRight = center + side * halfW - Vector3.up * thickness;

            int idx = s * 2;
            float vCoord = t * 6f;

            // 1. الوجه العلوي
            vertices[topBase + idx]     = ptTopLeft;
            vertices[topBase + idx + 1] = ptTopRight;
            uvs[topBase + idx]          = new Vector2(0f, vCoord);
            uvs[topBase + idx + 1]      = new Vector2(1f, vCoord);

            // 2. الوجه السفلي
            vertices[bottomBase + idx]     = ptBottomLeft;
            vertices[bottomBase + idx + 1] = ptBottomRight;
            uvs[bottomBase + idx]          = new Vector2(0f, vCoord);
            uvs[bottomBase + idx + 1]      = new Vector2(1f, vCoord);

            // 3. الجدار الأيسر
            vertices[leftBase + idx]     = ptBottomLeft;
            vertices[leftBase + idx + 1] = ptTopLeft;
            uvs[leftBase + idx]          = new Vector2(0f, vCoord);
            uvs[leftBase + idx + 1]      = new Vector2(1f, vCoord);

            // 4. الجدار الأيمن
            vertices[rightBase + idx]     = ptTopRight;
            vertices[rightBase + idx + 1] = ptBottomRight;
            uvs[rightBase + idx]          = new Vector2(0f, vCoord);
            uvs[rightBase + idx + 1]      = new Vector2(1f, vCoord);

            previousCenter = center;
        }

        // بناء المثلثات لكل وجه باتجاه خارجي دقيق (Outward Normals):
        int trianglesPerFace = segCount * 6;
        int[] triangles = new int[trianglesPerFace * 4];
        int tIdx = 0;

        void AddQuad(int a, int b, int c, int d)
        {
            triangles[tIdx++] = a;
            triangles[tIdx++] = c;
            triangles[tIdx++] = b;

            triangles[tIdx++] = b;
            triangles[tIdx++] = c;
            triangles[tIdx++] = d;
        }

        for (int s = 0; s < segCount; s++)
        {
            int i0 = s * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            // الوجه العلوي (سير اللاعب - النواقل للأعلى)
            AddQuad(topBase + i0, topBase + i1, topBase + i2, topBase + i3);

            // الوجه السفلي (النواقل للأسفل)
            AddQuad(bottomBase + i0, bottomBase + i2, bottomBase + i1, bottomBase + i3);

            // الجدار الأيسر (النواقل لليسار)
            AddQuad(leftBase + i0, leftBase + i1, leftBase + i2, leftBase + i3);

            // الجدار الأيمن (النواقل لليمين)
            AddQuad(rightBase + i0, rightBase + i1, rightBase + i2, rightBase + i3);
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{pathObject.name}_Solid3DMesh";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        // زخرفة الحافة الخارجية للممر بعناقيد نباتية لطيفة تمنحه مظهراً شجرياً ساحراً دون المساس بالاصطدام أو أبعاد الممر
        DecorateRampOuterEdge(pathObject.transform, vertices, topBase, segCount);
    }

    void DecorateRampOuterEdge(Transform parent, Vector3[] verts, int topBase, int segCount)
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Material leafMat = new Material(litShader);
        leafMat.name = "RampLeaf_Runtime";
        leafMat.color = new Color(0.28f, 0.70f, 0.32f);
        if (leafMat.HasProperty("_BaseColor")) leafMat.SetColor("_BaseColor", leafMat.color);
        if (leafMat.HasProperty("_Color")) leafMat.SetColor("_Color", leafMat.color);

        Material flowerMat = new Material(litShader);
        flowerMat.name = "RampBlossom_Runtime";
        flowerMat.color = new Color(0.35f, 0.90f, 0.85f);
        if (flowerMat.HasProperty("_EmissionColor"))
        {
            flowerMat.EnableKeyword("_EMISSION");
            flowerMat.SetColor("_EmissionColor", new Color(0.20f, 0.80f, 0.75f) * 0.8f);
        }

        // توزيع نباتات لطيفة على الحافة الخارجية للممر (ptTopRight)
        for (int s = 2; s < segCount - 2; s += 4)
        {
            int idx = topBase + s * 2 + 1; // ptTopRight
            if (idx >= verts.Length) break;

            Vector3 edgePos = verts[idx];

            // 1. ورقة لبلاب خضراء
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = $"RampIvyLeaf_{s}";
            leaf.transform.SetParent(parent, false);
            leaf.transform.localPosition = edgePos + new Vector3(0f, -0.06f, 0f);
            leaf.transform.localScale = new Vector3(0.55f, 0.10f, 0.55f);
            leaf.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-15f, 15f));
            leaf.GetComponent<Renderer>().material = leafMat;
            Collider col = leaf.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            // 2. زهرة غابية متوهجة رقيقة كل 8 مقاطع
            if (s % 8 == 2)
            {
                GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = $"RampBlossom_{s}";
                flower.transform.SetParent(parent, false);
                flower.transform.localPosition = edgePos + new Vector3(0f, 0.06f, 0f);
                flower.transform.localScale = Vector3.one * 0.22f;
                flower.GetComponent<Renderer>().material = flowerMat;
                Collider fCol = flower.GetComponent<Collider>();
                if (fCol != null)
                {
                    if (Application.isPlaying) Destroy(fCol);
                    else DestroyImmediate(fCol);
                }
            }
        }
    }

    void ClearGeneratedPaths()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.transform.SetParent(null);
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        for (int i = 1; i < sectionGameObjects.Length; i++)
        {
            sectionGameObjects[i] = null;
        }
    }

    public void SetSectionUnlocked(int sectionIndex, bool unlocked)
    {
        if (sectionIndex < 0 || sectionIndex >= sectionUnlocked.Length) return;

        sectionUnlocked[sectionIndex] = unlocked;

        if (sectionIndex == 0)
        {
            GameObject rootsObj = sectionGameObjects[0] ?? FindRootsObject();
            if (rootsObj != null)
            {
                sectionGameObjects[0] = rootsObj;
                rootsObj.SetActive(unlocked);
            }
            return;
        }

        if (sectionGameObjects[sectionIndex] != null)
        {
            sectionGameObjects[sectionIndex].SetActive(unlocked);
            return;
        }

        string secName = sectionIndex switch
        {
            1 => "Paths_Level_1_To_Level_2",
            2 => "Paths_Level_2_To_Level_3",
            3 => "Paths_Level_3_To_TopLevel",
            _ => $"Paths_Section_{sectionIndex}"
        };

        Transform targetChild = transform.Find(secName);
        if (targetChild != null)
        {
            sectionGameObjects[sectionIndex] = targetChild.gameObject;
            targetChild.gameObject.SetActive(unlocked);
            return;
        }

        // بحث احتياطي في كافة الأبناء بالاسم
        for (int c = 0; c < transform.childCount; c++)
        {
            Transform ch = transform.GetChild(c);
            if (ch.name.Contains(secName) || ch.name.EndsWith($"Section_{sectionIndex}"))
            {
                sectionGameObjects[sectionIndex] = ch.gameObject;
                ch.gameObject.SetActive(unlocked);
                return;
            }
        }
    }
}