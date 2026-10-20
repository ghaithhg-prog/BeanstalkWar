using UnityEngine;

/// <summary>
/// AnimatedRiverRing:
/// Animates the vast circular enchanted boundary river surrounding the fairytale forest.
/// Continuously scrolls UV texture offsets to create an active, rushing, glistening current,
/// ensuring the water is never stagnant (راكد).
/// </summary>
public class AnimatedRiverRing : MonoBehaviour
{
    [Header("Flow Settings")]
    public float flowSpeed = 0.35f;
    public float swirlSpeed = 0.08f;
    public float waveAmplitude = 0.02f;

    private Material riverMaterial;
    private float currentU = 0f;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            riverMaterial = rend.material;
        }
    }

    void Update()
    {
        if (riverMaterial == null) return;

        float dt = Time.deltaTime;
        currentU += flowSpeed * dt;
        if (currentU > 100f) currentU -= 100f;

        float swirlV = Mathf.Sin(Time.time * 2.5f) * swirlSpeed;

        Vector2 offset = new Vector2(currentU, swirlV);
        riverMaterial.mainTextureOffset = offset;

        if (riverMaterial.HasProperty("_BaseMap"))
        {
            riverMaterial.SetTextureOffset("_BaseMap", offset);
        }
    }

    void OnDestroy()
    {
        if (riverMaterial != null)
        {
            Destroy(riverMaterial);
        }
    }
}
