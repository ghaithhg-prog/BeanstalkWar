using UnityEngine;

/// <summary>
/// Modifier added to a bot to turn it into a boss: increases size, health, damage, and XP.
/// </summary>
public class BossModifier : MonoBehaviour
{
    private bool isBoss = false;
    private Light bossLight;
    private float pulseSpeed = 2f;

    public void InitializeBoss(float newHealth, float damageMulti, float scaleMulti, int xpReward)
    {
        if (isBoss) return;
        isBoss = true;

        // 1. Cap the scale increase to max 1.35x (prevents clipping through levels)
        float clampedScale = Mathf.Min(scaleMulti, 1.35f);
        float previousHeight = transform.localScale.y;
        transform.localScale = Vector3.one * clampedScale;

        // Push boss UP so its feet don't clip through the platform below
        float heightGain = (clampedScale - previousHeight) * 0.5f;
        transform.position += Vector3.up * Mathf.Max(heightGain, 0.3f);

        // 2. Increase health massively
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.maxHealth = newHealth;
            health.currentHealth = newHealth;
        }

        // 3. Increase damage
        CharacterCombat combat = GetComponent<CharacterCombat>();
        if (combat != null)
        {
            combat.attackDamage *= damageMulti;
        }

        // 4. Increase XP reward on kill
        PlayerXP xp = GetComponent<PlayerXP>();
        if (xp != null)
        {
            xp.xpPerKill = xpReward;
        }

        // 5. Modify speed (bosses are slightly slower for balance)
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.walkSpeed *= 0.7f;
            movement.moveSpeed *= 0.7f;
        }

        // 6. Visual effects: Add crimson aura glow without changing base color
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Do not change weapons or crystals
            if (!r.gameObject.CompareTag("Weapon") && !r.gameObject.CompareTag("Crystal"))
            {
                // Enable emission and set to red-orange glow
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", new Color(0.8f, 0.2f, 0.1f) * 0.6f);
            }
        }

        // Add a subtle pulsing red point light
        bossLight = gameObject.AddComponent<Light>();
        bossLight.type = LightType.Point;
        bossLight.color = Color.red;
        bossLight.range = 6f;
        bossLight.intensity = 1.8f;

        // 7. Add boss horns (cylinders scaled thin and tall)
        CreateHorn(new Vector3(0.3f, 1.8f, 0f), new Vector3(0.1f, 0.4f, 0.1f), new Vector3(0f, 0f, -20f));
        CreateHorn(new Vector3(-0.3f, 1.8f, 0f), new Vector3(0.1f, 0.4f, 0.1f), new Vector3(0f, 0f, 20f));

        Debug.Log(gameObject.name + " HAS MUTATED INTO A BOSS!");
    }

    private void CreateHorn(Vector3 localPosition, Vector3 scale, Vector3 rotationAngles)
    {
        GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        
        // Remove collider so it doesn't interfere with gameplay
        Collider col = horn.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }
        
        horn.transform.SetParent(transform, false);
        horn.transform.localPosition = localPosition;
        horn.transform.localScale = scale;
        horn.transform.localRotation = Quaternion.Euler(rotationAngles);
        
        // Give horns the same emission glow
        Renderer r = horn.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = new Color(0.2f, 0.2f, 0.2f); // Dark color for horns
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", new Color(0.8f, 0.2f, 0.1f) * 0.6f);
        }
    }

    private void Update()
    {
        if (isBoss && bossLight != null)
        {
            // Pulse the boss light intensity between 1.2 and 2.5
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            bossLight.intensity = Mathf.Lerp(1.2f, 2.5f, pulse);
        }
    }
}
