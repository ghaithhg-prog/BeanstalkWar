using UnityEngine;

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
    public int pathsPerSection = 3;

    [Min(0.5f)]
    public float pathWidth = 3f;

    [Range(8, 100)]
    public int segments = 40;

    [Tooltip("مقدار دوران الممر حول الشجرة بين مستويين")]
    [Range(30f, 240f)]
    public float arcDegrees = 110f;

    [Tooltip("إبعاد منتصف الممر إلى الخارج حتى لا يدخل في الشجرة")]
    public float outwardBulge = 4f;

    [Tooltip("يدخل طرف الممر قليلاً داخل حافة المنصة لمنع وجود فجوة")]
    public float platformOverlap = 0.7f;

    [Header("Appearance")]
    public Material pathMaterial;

    [Header("Collision")]
    public string climbLayerName = "Climb";

    void Start()
    {
        RebuildPaths();
    }

    [ContextMenu("Rebuild Paths")]
    public void RebuildPaths()
    {
        ClearGeneratedPaths();

        BuildSection(level0, level1, 0);
        BuildSection(level1, level2, 1);
        BuildSection(level2, level3, 2);
        BuildSection(level3, topLevel, 3);

        // في البداية نُظهر فقط الطريق المؤدي إلى Level_1
          SetSectionUnlocked(0, true);
          SetSectionUnlocked(1, false);
          SetSectionUnlocked(2, false);
          SetSectionUnlocked(3, false);
    }

    void BuildSection(Transform from, Transform to, int sectionIndex)
    {
        if (from == null || to == null)
            return;

        GameObject section = new GameObject(
            $"Paths_{from.name}_To_{to.name}"
        );

        section.transform.SetParent(transform, false);

        float angleStep = 360f / pathsPerSection;

        // نجعل كل طبقة مختلفة قليلاً حتى لا تصبح الممرات فوق بعضها بصرياً.
        float sectionRotation = sectionIndex * 35f;

        for (int i = 0; i < pathsPerSection; i++)
        {
            float startAngle =
                sectionRotation + i * angleStep;

            CreateCurvedPath(
                section.transform,
                from,
                to,
                startAngle,
                i
            );
        }
    }

    void CreateCurvedPath(
        Transform parent,
        Transform from,
        Transform to,
        float startAngle,
        int pathIndex)
    {
        if (!TryGetPlatformData(
                from,
                out Vector3 fromCenter,
                out float fromRadius,
                out float fromTopY))
            return;

        if (!TryGetPlatformData(
                to,
                out Vector3 toCenter,
                out float toRadius,
                out float toTopY))
            return;

        GameObject pathObject =
            new GameObject($"CurvedPath_{pathIndex + 1}");

        pathObject.transform.SetParent(parent, false);

        int layer = LayerMask.NameToLayer(climbLayerName);
        if (layer >= 0)
            pathObject.layer = layer;

        MeshFilter meshFilter =
            pathObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            pathObject.AddComponent<MeshRenderer>();

        MeshCollider meshCollider =
            pathObject.AddComponent<MeshCollider>();

        if (pathMaterial != null)
            meshRenderer.sharedMaterial = pathMaterial;

        Vector3[] vertices =
            new Vector3[(segments + 1) * 2];

        Vector2[] uvs =
            new Vector2[(segments + 1) * 2];

        int[] triangles =
            new int[segments * 6];

        Vector3 previousCenter = Vector3.zero;

        for (int s = 0; s <= segments; s++)
        {
            float t = (float)s / segments;

            // SmoothStep يجعل البداية والنهاية أكثر نعومة.
            float smoothT = t * t * (3f - 2f * t);

            float angle =
                startAngle + arcDegrees * smoothT;

            float angleRad = angle * Mathf.Deg2Rad;

            Vector3 radialDirection =
                new Vector3(
                    Mathf.Cos(angleRad),
                    0f,
                    Mathf.Sin(angleRad)
                );

            Vector3 currentCenter =
                Vector3.Lerp(
                    fromCenter,
                    toCenter,
                    smoothT
                );

            float currentRadius =
                Mathf.Lerp(
                    fromRadius - platformOverlap,
                    toRadius - platformOverlap,
                    smoothT
                );

            // في منتصف المسار نخرجه إلى الخارج.
            // عند البداية والنهاية تصبح القيمة صفراً لكي يلامس المنصة.
            float bulge =
                Mathf.Sin(t * Mathf.PI) * outwardBulge;

            currentRadius += bulge;

            float y =
                Mathf.Lerp(
                    fromTopY + 0.03f,
                    toTopY + 0.03f,
                    smoothT
                );

            Vector3 center =
                currentCenter +
                radialDirection * currentRadius;

            center.y = y;

            Vector3 forward;

            if (s == 0)
            {
                float nextT =
                    1f / segments;

                forward =
                    GetApproximateDirection(
                        fromCenter,
                        toCenter,
                        fromRadius,
                        toRadius,
                        fromTopY,
                        toTopY,
                        startAngle,
                        nextT
                    );
            }
            else
            {
                forward =
                    (center - previousCenter).normalized;
            }

            // نحصل على الاتجاه العرضي الحقيقي للممر.
            Vector3 side =
                Vector3.Cross(
                    Vector3.up,
                    forward
                ).normalized;

            // حماية من حالة اتجاه رأسي شبه كامل.
            if (side.sqrMagnitude < 0.001f)
            {
                side = new Vector3(
                    -radialDirection.z,
                    0f,
                    radialDirection.x
                );
            }

            vertices[s * 2] =
                pathObject.transform.InverseTransformPoint(
                    center - side * pathWidth * 0.5f
                );

            vertices[s * 2 + 1] =
                pathObject.transform.InverseTransformPoint(
                    center + side * pathWidth * 0.5f
                );

            uvs[s * 2] =
                new Vector2(0f, t * 4f);

            uvs[s * 2 + 1] =
                new Vector2(1f, t * 4f);

            previousCenter = center;
        }

        int triangleIndex = 0;

        for (int s = 0; s < segments; s++)
        {
            int a = s * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;

            triangles[triangleIndex++] = a;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = b;

            triangles[triangleIndex++] = b;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = d;
        }

        Mesh mesh = new Mesh();
        mesh.name = pathObject.name + "_Mesh";

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    Vector3 GetApproximateDirection(
        Vector3 fromCenter,
        Vector3 toCenter,
        float fromRadius,
        float toRadius,
        float fromTopY,
        float toTopY,
        float startAngle,
        float t)
    {
        float smoothT =
            t * t * (3f - 2f * t);

        float angle =
            startAngle + arcDegrees * smoothT;

        float angleRad =
            angle * Mathf.Deg2Rad;

        Vector3 radial =
            new Vector3(
                Mathf.Cos(angleRad),
                0f,
                Mathf.Sin(angleRad)
            );

        Vector3 center =
            Vector3.Lerp(
                fromCenter,
                toCenter,
                smoothT
            );

        float radius =
            Mathf.Lerp(
                fromRadius - platformOverlap,
                toRadius - platformOverlap,
                smoothT
            );

        radius +=
            Mathf.Sin(t * Mathf.PI) *
            outwardBulge;

        Vector3 point =
            center + radial * radius;

        point.y =
            Mathf.Lerp(
                fromTopY,
                toTopY,
                smoothT
            );

        float startRad =
            startAngle * Mathf.Deg2Rad;

        Vector3 startRadial =
            new Vector3(
                Mathf.Cos(startRad),
                0f,
                Mathf.Sin(startRad)
            );

        Vector3 start =
            fromCenter +
            startRadial *
            (fromRadius - platformOverlap);

        start.y = fromTopY;

        return (point - start).normalized;
    }

    bool TryGetPlatformData(
        Transform platform,
        out Vector3 center,
        out float radius,
        out float topY)
    {
        center = platform.position;
        radius = 1f;
        topY = platform.position.y;

        Renderer renderer =
            platform.GetComponent<Renderer>();

        Collider collider =
            platform.GetComponent<Collider>();

        Bounds bounds;

        if (renderer != null)
        {
            bounds = renderer.bounds;
        }
        else if (collider != null)
        {
            bounds = collider.bounds;
        }
        else
        {
            Debug.LogWarning(
                $"{platform.name} يحتاج Renderer أو Collider لكي نحسب حجمه."
            );

            return false;
        }

        center = bounds.center;
        center.y = platform.position.y;

        // المنصات دائرية، فنأخذ نصف أكبر قطر أفقي.
        radius =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.z
            );

        topY = bounds.max.y;

        return true;
    }

    void ClearGeneratedPaths()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child =
                transform.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

     public void SetSectionUnlocked(int sectionIndex, bool unlocked)
{
    // sectionIndex:
    // 0 = Level_0 -> Level_1
    // 1 = Level_1 -> Level_2
    // 2 = Level_2 -> Level_3
    // 3 = Level_3 -> TopLevel

    if (sectionIndex < 0 || sectionIndex >= transform.childCount)
        return;

    transform.GetChild(sectionIndex).gameObject.SetActive(unlocked);
}

}