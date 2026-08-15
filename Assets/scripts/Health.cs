using UnityEngine;
using System.Collections;
using TMPro;

public class Health : MonoBehaviour
{
    public TextMeshProUGUI respawnText;

    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;

    private bool initialized = false;

    void Start()
    {
        StartCoroutine(LateInitialize());
    }

   IEnumerator LateInitialize()
{
    yield return null;

    if (!initialized)
    {
        initialized = true;
        currentHealth = maxHealth;
        isDead = false;

        // ✅ ابحث عن RespawnText وأخفه
        if (respawnText == null)
        {
            GameObject obj = GameObject.Find("RespawnText");
            if (obj != null)
                respawnText = obj.GetComponent<TextMeshProUGUI>();
        }

        // ✅ تأكد أنه مخفي دائماً عند البداية
        if (respawnText != null)
            respawnText.gameObject.SetActive(false);
    }
}

    public void TakeDamage(float amount)
    {
        if (!initialized) return;
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " died");

        PlayerMovement player = GetComponent<PlayerMovement>();
        if (player != null)
            player.enabled = false;

        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        float respawnTime = 10f;

        if (respawnText == null)
        {
            GameObject obj = GameObject.Find("RespawnText");
            if (obj != null)
                respawnText = obj.GetComponent<TextMeshProUGUI>();
        }

        if (respawnText != null)
            respawnText.gameObject.SetActive(true);

        while (respawnTime > 0)
        {
            if (respawnText != null)
                respawnText.text = "Respawning in " + 
                Mathf.Ceil(respawnTime).ToString();

            yield return new WaitForSeconds(1f);
            respawnTime--;
        }

        PlayerMovement player = GetComponent<PlayerMovement>();

        if (SpawnManager.Instance != null && player != null)
        {
            Transform spawnPoint = 
            SpawnManager.Instance.GetSpawnPoint(player.team);
            if (spawnPoint != null)
                transform.position = spawnPoint.position;
        }

        currentHealth = maxHealth;
        isDead = false;

        if (player != null)
            player.enabled = true;

        if (respawnText != null)
            respawnText.gameObject.SetActive(false);
    }
}