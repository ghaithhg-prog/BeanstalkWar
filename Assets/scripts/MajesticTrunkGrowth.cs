using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MajesticTrunkGrowth:
/// يتحكم في نمو وظهور الجذوع والأغصان والتضفيرات الخشبية الأسطورية تدريجياً
/// مع تقدم الشجرة في اللعبة من المستوى الأول ثم الثاني ثم الثالث ثم القمة.
/// كما يضمن أن كل جزء من الجذع يمكن ضربه وتوجيه الضرر لصحة الشجرة.
/// </summary>
public class MajesticTrunkGrowth : MonoBehaviour
{
    public static MajesticTrunkGrowth Instance { get; private set; }

    public struct TrunkElement
    {
        public GameObject gameObject;
        public float minY;
        public float maxY;
        public Vector3 baseScale;
    }

    private readonly List<TrunkElement> elements = new List<TrunkElement>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        float startHeight = (TreeGrowth.Instance != null) ? TreeGrowth.Instance.CurrentHeight : 5.3f;
        UpdateGrowth(startHeight);
    }

    public void RegisterElement(GameObject go, float yHeight)
    {
        RegisterSegment(go, yHeight - 0.5f, yHeight + 0.5f);
    }

    public void RegisterSegment(GameObject go, float minY, float maxY)
    {
        if (go == null) return;

        elements.Add(new TrunkElement
        {
            gameObject = go,
            minY = minY,
            maxY = maxY,
            baseScale = go.transform.localScale
        });

        // ربط وسم الشجرة ومستقبل الضرر على أي كوليدر بالجذع
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            go.tag = "Tree";
            if (go.GetComponent<TreeDamageForwarder>() == null)
            {
                go.AddComponent<TreeDamageForwarder>();
            }
        }

        // إخفاء الأجزاء التي تعلو المستوى الأول في البداية
        // Level 1 surface is around 5.3m
        bool initialVisible = minY <= 5.5f;
        go.SetActive(initialVisible);
    }

    public void UpdateGrowth(float currentTreeHeight)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            var el = elements[i];
            if (el.gameObject == null) continue;

            // إذا كان ارتفاع العنصر أقل من أو يساوي ارتفاع الشجرة الحالي + هامش ظهور مناسب
            bool shouldBeVisible = el.minY <= (currentTreeHeight + 0.25f);
            if (el.gameObject.activeSelf != shouldBeVisible)
            {
                el.gameObject.SetActive(shouldBeVisible);
            }
        }
    }
}
