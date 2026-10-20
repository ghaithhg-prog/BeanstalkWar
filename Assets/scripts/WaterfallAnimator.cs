using UnityEngine;

/// <summary>
/// Dynamic Waterfall Animator:
/// 1. Continuously scrolls water texture UVs downwards at high speed to simulate rushing falling water.
/// 2. Scrolls the elevated mountain river bed forward toward the cliff edge.
/// 3. Undulates cascade width and depth with organic wave oscillations.
/// 4. Animates frothing impact foam, rising mist puffs, and water ripples at the plunge basin.
/// 5. Drives an integrated 3D water droplet and mist particle system.
/// </summary>
public class WaterfallAnimator : MonoBehaviour
{
    [Header("Flow Animation Speeds")]
    public float verticalFallSpeed = 3.6f;
    public float riverFlowSpeed = 1.4f;
    public float waveFrequency = 7.0f;
    public float waveAmplitude = 0.06f;

    [Header("Elements to Animate")]
    public Renderer[] verticalWaterRenderers;
    public Renderer[] riverWaterRenderers;
    public Transform[] foamImpactPuffs;
    public Transform poolTransform;

    private float verticalUV = 0f;
    private float riverUV = 0f;
    private Vector3[] initialVerticalScales;
    private Vector3 initialPoolScale;
    private Material[] cachedVerticalMats;
    private Material[] cachedRiverMats;

    void Start()
    {
        if (verticalWaterRenderers != null && verticalWaterRenderers.Length > 0)
        {
            initialVerticalScales = new Vector3[verticalWaterRenderers.Length];
            cachedVerticalMats = new Material[verticalWaterRenderers.Length];
            for (int i = 0; i < verticalWaterRenderers.Length; i++)
            {
                if (verticalWaterRenderers[i] != null)
                {
                    initialVerticalScales[i] = verticalWaterRenderers[i].transform.localScale;
                    cachedVerticalMats[i] = verticalWaterRenderers[i].material;
                }
            }
        }

        if (riverWaterRenderers != null && riverWaterRenderers.Length > 0)
        {
            cachedRiverMats = new Material[riverWaterRenderers.Length];
            for (int i = 0; i < riverWaterRenderers.Length; i++)
            {
                if (riverWaterRenderers[i] != null)
                {
                    cachedRiverMats[i] = riverWaterRenderers[i].material;
                }
            }
        }

        if (poolTransform != null)
        {
            initialPoolScale = poolTransform.localScale;
        }

        SetupWaterDropletsParticleSystem();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1. Rushing downward UV scroll for vertical waterfall cascades
        verticalUV -= verticalFallSpeed * dt;
        if (verticalUV < -100f) verticalUV += 100f;

        if (cachedVerticalMats != null)
        {
            for (int i = 0; i < cachedVerticalMats.Length; i++)
            {
                Material mat = cachedVerticalMats[i];
                if (mat == null) continue;

                Vector2 offset = new Vector2(0f, verticalUV + (i * 0.23f));
                mat.mainTextureOffset = offset;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTextureOffset("_BaseMap", offset);
                }

                // Subtle organic surging wave pulse
                if (initialVerticalScales != null && i < initialVerticalScales.Length && verticalWaterRenderers != null && verticalWaterRenderers[i] != null)
                {
                    float wave = Mathf.Sin(Time.time * waveFrequency + i * 1.8f) * waveAmplitude;
                    Vector3 baseS = initialVerticalScales[i];
                    verticalWaterRenderers[i].transform.localScale = new Vector3(baseS.x * (1f + wave), baseS.y, baseS.z * (1f + wave * 0.4f));
                }
            }
        }

        // 2. Mountain river flow UV scroll forward toward the cliff edge
        riverUV -= riverFlowSpeed * dt;
        if (riverUV < -100f) riverUV += 100f;

        if (cachedRiverMats != null)
        {
            for (int i = 0; i < cachedRiverMats.Length; i++)
            {
                Material mat = cachedRiverMats[i];
                if (mat == null) continue;

                Vector2 offset = new Vector2(0f, riverUV);
                mat.mainTextureOffset = offset;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTextureOffset("_BaseMap", offset);
                }
            }
        }

        // 3. Frothing impact foam and rising splash mist at the plunge pool
        if (foamImpactPuffs != null)
        {
            for (int i = 0; i < foamImpactPuffs.Length; i++)
            {
                Transform foam = foamImpactPuffs[i];
                if (foam == null) continue;

                float pulse = 0.85f + Mathf.PingPong(Time.time * 4.5f + i * 0.7f, 0.4f);
                foam.localScale = Vector3.one * pulse;
                foam.Rotate(0f, 40f * dt * (i % 2 == 0 ? 1f : -1f), 0f);
            }
        }

        // 4. Subtle pool surface ripple oscillation
        if (poolTransform != null)
        {
            float ripple = 1f + Mathf.Sin(Time.time * 3f) * 0.03f;
            poolTransform.localScale = new Vector3(initialPoolScale.x * ripple, initialPoolScale.y, initialPoolScale.z * ripple);
        }
    }

    void SetupWaterDropletsParticleSystem()
    {
        // Procedural particle system for mist & droplets at waterfall crest and base
        GameObject psObj = new GameObject("WaterfallSplashSprayFX");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = new Vector3(0f, 1.2f, 3.2f);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 1.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
        main.startColor = new Color(1.0f, 1.0f, 1.0f, 0.65f);
        main.maxParticles = 60;
        main.gravityModifier = -0.05f; // Gentle upward mist drift

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.5f, 0.3f, 2.5f);

        // Particle material
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material pMat = new Material(unlitShader);
        pMat.color = new Color(0.92f, 0.97f, 1.0f, 0.55f);
        if (pMat.HasProperty("_BaseColor")) pMat.SetColor("_BaseColor", pMat.color);

        ParticleSystemRenderer psr = psObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.material = pMat;
        }

        ps.Play();
    }
}
