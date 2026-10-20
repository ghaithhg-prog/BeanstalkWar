using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MajesticTrunkGrowth:
/// يتحكم في نمو الشجرة العملاقة بشكل سينمائي وطبيعي مستمر،
/// بحيث تنمو المقاطع وتستطيل بسلاسة كالأفعى المتصاعدة أو كفيديوهات تصوير نمو النباتات (Time-lapse)،
/// بدلاً من ظهور قوالب مفاجئة كل متر.
/// يتضمن رأساً حيوياً صاعداً كالأفعى (Ascending Serpent Sprout Head) في قمة الشجرة يواكب صعودها ويزهر تدريجياً.
/// </summary>
public class MajesticTrunkGrowth : MonoBehaviour
{
    public static MajesticTrunkGrowth Instance { get; private set; }

    public enum ElementType
    {
        ExtrudingSegment, // مقطع اسطواني ينمو ويستطيل سينمائياً من p0 إلى p1
        NodeJoint,        // مفصل كروي ينمو ويتسع قطره عند وصول القمة
        FoliageSprout     // ورقة أو طحلب أو تجويف يتفتح تدريجياً من برعم صغير
    }

    public class TrunkElement
    {
        public GameObject gameObject;
        public ElementType type;
        public Vector3 p0;
        public Vector3 p1;
        public float minY;
        public float maxY;
        public Vector3 baseLocalPosition;
        public Quaternion baseLocalRotation;
        public Vector3 baseLocalScale;
        public float fullDistance;
    }

    private readonly List<TrunkElement> elements = new List<TrunkElement>();

    // رأس البرعم المتصاعد كالأفعى في القمة
    private GameObject sproutHeadObj;
    private Transform sproutTendrilContainer;
    private Light sproutGlowLight;
    private readonly List<Transform> sproutTendrils = new List<Transform>();
    private float topPlatformHeight = 40.0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateAscendingSerpentSproutHead();

        float startHeight = (TreeGrowth.Instance != null) ? TreeGrowth.Instance.CurrentHeight : 5.80f;
        UpdateGrowth(startHeight);
    }

    public static Vector3 GetBeanstalkCenterAtHeight(float y, float topHeight = 40.0f)
    {
        float swayX = Mathf.Sin(y * 0.16f) * 1.6f + Mathf.Sin(y * 0.35f) * 0.6f;
        float swayZ = Mathf.Cos(y * 0.18f) * 1.5f + Mathf.Cos(y * 0.40f) * 0.5f;

        float groundAnchor = Mathf.Clamp01(y / 4.0f);
        float topAnchor = Mathf.Clamp01((topHeight - y) / 7.0f);
        float totalAnchor = groundAnchor * topAnchor;

        return new Vector3(swayX * totalAnchor, y, swayZ * totalAnchor);
    }

    /// <summary>
    /// تسجيل مقطع اسطواني ينمو ويستطيل سينمائياً من p0 إلى p1
    /// </summary>
    public void RegisterExtrudingSegment(GameObject go, Vector3 p0, Vector3 p1)
    {
        if (go == null) return;

        EnsureTreeCombatSetup(go);

        Vector3 lower = p0.y <= p1.y ? p0 : p1;
        Vector3 upper = p0.y <= p1.y ? p1 : p0;
        float dist = Vector3.Distance(lower, upper);

        elements.Add(new TrunkElement
        {
            gameObject = go,
            type = ElementType.ExtrudingSegment,
            p0 = lower,
            p1 = upper,
            minY = lower.y,
            maxY = upper.y,
            baseLocalPosition = go.transform.localPosition,
            baseLocalRotation = go.transform.localRotation,
            baseLocalScale = go.transform.localScale,
            fullDistance = dist
        });

        // إخفاء الأجزاء التي تعلو المستوى الأول في البداية
        bool initialVisible = lower.y <= 5.80f;
        go.SetActive(initialVisible);
    }

    /// <summary>
    /// تسجيل مفصل كروي ينمو حجمه عند وصول قمة النمو
    /// </summary>
    public void RegisterNodeJoint(GameObject go, Vector3 position, float yHeight)
    {
        if (go == null) return;

        EnsureTreeCombatSetup(go);

        elements.Add(new TrunkElement
        {
            gameObject = go,
            type = ElementType.NodeJoint,
            p0 = position,
            p1 = position,
            minY = yHeight - 0.45f,
            maxY = yHeight + 0.15f,
            baseLocalPosition = go.transform.localPosition,
            baseLocalRotation = go.transform.localRotation,
            baseLocalScale = go.transform.localScale,
            fullDistance = 0f
        });

        bool initialVisible = yHeight <= 5.80f;
        go.SetActive(initialVisible);
    }

    /// <summary>
    /// تسجيل أوراق اللبلاب ورفوف الطحالب لتتفتح وتنمو طبيعياً من براعم
    /// </summary>
    public void RegisterFoliage(GameObject go, float yHeight)
    {
        if (go == null) return;

        EnsureTreeCombatSetup(go);

        elements.Add(new TrunkElement
        {
            gameObject = go,
            type = ElementType.FoliageSprout,
            p0 = go.transform.localPosition,
            p1 = go.transform.localPosition,
            minY = yHeight,
            maxY = yHeight + 1.4f,
            baseLocalPosition = go.transform.localPosition,
            baseLocalRotation = go.transform.localRotation,
            baseLocalScale = go.transform.localScale,
            fullDistance = 0f
        });

        bool initialVisible = yHeight <= 5.80f;
        go.SetActive(initialVisible);
    }

    // دوال التوافق القديمة
    public void RegisterElement(GameObject go, float yHeight)
    {
        RegisterFoliage(go, yHeight);
    }

    public void RegisterSegment(GameObject go, float minY, float maxY)
    {
        if (go == null) return;
        EnsureTreeCombatSetup(go);

        elements.Add(new TrunkElement
        {
            gameObject = go,
            type = ElementType.ExtrudingSegment,
            p0 = go.transform.localPosition - Vector3.up * (Mathf.Abs(maxY - minY) * 0.5f),
            p1 = go.transform.localPosition + Vector3.up * (Mathf.Abs(maxY - minY) * 0.5f),
            minY = minY,
            maxY = maxY,
            baseLocalPosition = go.transform.localPosition,
            baseLocalRotation = go.transform.localRotation,
            baseLocalScale = go.transform.localScale,
            fullDistance = Mathf.Abs(maxY - minY)
        });

        bool initialVisible = minY <= 5.80f;
        go.SetActive(initialVisible);
    }

    private void EnsureTreeCombatSetup(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            go.tag = "Tree";
            if (go.GetComponent<TreeDamageForwarder>() == null)
            {
                go.AddComponent<TreeDamageForwarder>();
            }
        }
    }

    /// <summary>
    /// إنشاء رأس البرعم المتصاعد كالأفعى في قمة الشجرة (The Ascending Serpent Growth Sprout)
    /// </summary>
    void CreateAscendingSerpentSproutHead()
    {
        if (sproutHeadObj != null) return;

        sproutHeadObj = new GameObject("AscendingSerpentSproutHead");
        sproutHeadObj.transform.SetParent(transform, false);

        sproutTendrilContainer = new GameObject("Tendrils").transform;
        sproutTendrilContainer.SetParent(sproutHeadObj.transform, false);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // مادة البرعم الأخضر الزمردي الغض
        Material sproutMat = new Material(litShader);
        sproutMat.name = "SproutEmeraldVineMat";
        sproutMat.color = new Color(0.25f, 0.95f, 0.4f);
        if (sproutMat.HasProperty("_Smoothness")) sproutMat.SetFloat("_Smoothness", 0.6f);
        if (sproutMat.HasProperty("_EmissionColor"))
        {
            sproutMat.EnableKeyword("_EMISSION");
            sproutMat.SetColor("_EmissionColor", new Color(0.18f, 0.85f, 0.35f) * 1.5f);
        }

        // أطراف لولبية خضراء تتلوى كالأفعى في القمة
        int tendrilCount = 4;
        for (int i = 0; i < tendrilCount; i++)
        {
            float angle = (i / (float)tendrilCount) * Mathf.PI * 2f;
            GameObject tendril = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tendril.name = $"SproutTendril_{i}";
            tendril.transform.SetParent(sproutTendrilContainer, false);
            tendril.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.45f, 0.6f + i * 0.15f, Mathf.Sin(angle) * 0.45f);
            tendril.transform.localRotation = Quaternion.Euler(15f, angle * Mathf.Rad2Deg + 45f, 20f);
            tendril.transform.localScale = new Vector3(0.18f, 0.65f, 0.18f);
            tendril.GetComponent<Renderer>().material = sproutMat;
            Destroy(tendril.GetComponent<Collider>());
            sproutTendrils.Add(tendril.transform);

            // برعم ورقة صغيرة في طرف كل خيط لولبي
            GameObject budLeaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            budLeaf.name = $"BudLeaf_{i}";
            budLeaf.transform.SetParent(tendril.transform, false);
            budLeaf.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            budLeaf.transform.localScale = new Vector3(1.6f, 0.4f, 1.2f);
            budLeaf.GetComponent<Renderer>().material = sproutMat;
            Destroy(budLeaf.GetComponent<Collider>());
        }

        // ضوء هالة البرعم الزمردي المتصاعد
        GameObject lightObj = new GameObject("SproutGlowLight");
        lightObj.transform.SetParent(sproutHeadObj.transform, false);
        lightObj.transform.localPosition = Vector3.up * 0.5f;
        sproutGlowLight = lightObj.AddComponent<Light>();
        sproutGlowLight.type = LightType.Point;
        sproutGlowLight.color = new Color(0.25f, 1.0f, 0.45f);
        sproutGlowLight.range = 8.5f;
        sproutGlowLight.intensity = 3.0f;
    }

    /// <summary>
    /// التحديث المستمر السينمائي لنمو الشجرة:
    /// كل مقطع يستطيل بدقة وبسلاسة مع ارتفاع الشجرة بدون أي قفزات مفاجئة!
    /// </summary>
    public void UpdateGrowth(float currentTreeHeight)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            var el = elements[i];
            if (el.gameObject == null) continue;

            if (el.type == ElementType.ExtrudingSegment)
            {
                if (currentTreeHeight <= el.minY)
                {
                    if (el.gameObject.activeSelf) el.gameObject.SetActive(false);
                }
                else if (currentTreeHeight >= el.maxY)
                {
                    if (!el.gameObject.activeSelf) el.gameObject.SetActive(true);
                    el.gameObject.transform.localPosition = el.baseLocalPosition;
                    el.gameObject.transform.localScale = el.baseLocalScale;
                    el.gameObject.transform.localRotation = el.baseLocalRotation;
                }
                else
                {
                    // المقطع ينمو ويستطيل الآن في هذه اللحظة بشكل سينمائي متصل!
                    if (!el.gameObject.activeSelf) el.gameObject.SetActive(true);

                    float progress = Mathf.Clamp01((currentTreeHeight - el.minY) / (el.maxY - el.minY));
                    Vector3 curTop = Vector3.Lerp(el.p0, el.p1, progress);
                    Vector3 curMid = (el.p0 + curTop) * 0.5f;
                    float curDist = Vector3.Distance(el.p0, curTop);

                    el.gameObject.transform.localPosition = curMid;
                    el.gameObject.transform.localRotation = el.baseLocalRotation;

                    float scaleY = Mathf.Max(curDist * 0.5f, 0.05f);
                    // تدرج سمك انسيابي طبيعي (Tapering) عند قمة النمو لتبدو الشجرة حية وغضة في طرفها الصاعد
                    float tipTaper = Mathf.Lerp(0.72f, 1.0f, progress);
                    el.gameObject.transform.localScale = new Vector3(el.baseLocalScale.x * tipTaper, scaleY, el.baseLocalScale.z * tipTaper);
                }
            }
            else if (el.type == ElementType.NodeJoint)
            {
                if (currentTreeHeight < el.minY)
                {
                    if (el.gameObject.activeSelf) el.gameObject.SetActive(false);
                }
                else if (currentTreeHeight >= el.maxY)
                {
                    if (!el.gameObject.activeSelf) el.gameObject.SetActive(true);
                    el.gameObject.transform.localScale = el.baseLocalScale;
                }
                else
                {
                    float progress = Mathf.Clamp01((currentTreeHeight - el.minY) / (el.maxY - el.minY));
                    if (progress > 0.02f)
                    {
                        if (!el.gameObject.activeSelf) el.gameObject.SetActive(true);
                        el.gameObject.transform.localScale = el.baseLocalScale * Mathf.SmoothStep(0f, 1f, progress);
                    }
                    else
                    {
                        if (el.gameObject.activeSelf) el.gameObject.SetActive(false);
                    }
                }
            }
            else // FoliageSprout
            {
                if (currentTreeHeight < el.minY)
                {
                    if (el.gameObject.activeSelf) el.gameObject.SetActive(false);
                }
                else
                {
                    float progress = Mathf.Clamp01((currentTreeHeight - el.minY) / 1.5f);
                    if (progress > 0.02f)
                    {
                        if (!el.gameObject.activeSelf) el.gameObject.SetActive(true);
                        el.gameObject.transform.localScale = el.baseLocalScale * Mathf.SmoothStep(0f, 1f, progress);
                    }
                    else
                    {
                        if (el.gameObject.activeSelf) el.gameObject.SetActive(false);
                    }
                }
            }
        }

        // تحديث حركة رأس البرعم المتصاعد كالأفعى في القمة
        UpdateSerpentSproutHead(currentTreeHeight);
    }

    void UpdateSerpentSproutHead(float currentTreeHeight)
    {
        if (sproutHeadObj == null) return;

        // إخفاء رأس البرعم عند اكتمال نمو الشجرة وبلوغها القمة (40م) لأنها نضجت وتفتحت الوردة الكبرى
        if (currentTreeHeight >= topPlatformHeight - 0.2f)
        {
            if (sproutHeadObj.activeSelf) sproutHeadObj.SetActive(false);
            return;
        }

        if (!sproutHeadObj.activeSelf) sproutHeadObj.SetActive(true);

        Vector3 center = GetBeanstalkCenterAtHeight(currentTreeHeight, topPlatformHeight);
        sproutHeadObj.transform.localPosition = center;

        // حركة دوران وتلوٍ لولبية حية (Circumnutation) تشبه نمو الشجرة الطبيعي في الفيديوهات
        float circumnutation = currentTreeHeight * 22f + Time.time * 35f;
        sproutHeadObj.transform.localRotation = Quaternion.Euler(
            Mathf.Sin(Time.time * 2.2f) * 12f,
            circumnutation,
            Mathf.Cos(Time.time * 2.6f) * 12f
        );

        // تمويج الخيوط اللولبية
        for (int t = 0; t < sproutTendrils.Count; t++)
        {
            if (sproutTendrils[t] != null)
            {
                float wave = Mathf.Sin(Time.time * 3f + t * 1.5f) * 10f;
                sproutTendrils[t].localRotation = Quaternion.Euler(15f + wave, (t * 90f) + circumnutation * 0.3f, 20f);
            }
        }

        // نبض الضوء الزمردي
        if (sproutGlowLight != null)
        {
            sproutGlowLight.intensity = 2.4f + Mathf.PingPong(Time.time * 2f, 1.2f);
        }
    }
}
