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

        // إسقاط الكريستالة فوراً إذا كان هذا الكائن يحملها
        if (CrystalItem.Instance != null && CrystalItem.Instance.IsCarrier(gameObject))
        {
            CrystalItem.Instance.DropCrystalServer();
        }

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

    // =========================
    // شريط صحة فوق رأس اللاعب
    // =========================

    void OnGUI()
    {
        if (isDead) return;
        if (Camera.main == null) return;

        // موقع فوق رأس اللاعب
        Vector3 headPos = transform.position + Vector3.up * 2.5f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(headPos);

        // لا ترسم إذا كان اللاعب خلف الكاميرا
        if (screenPos.z < 0) return;

        // حساب المسافة للتحكم بالحجم والإخفاء
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distance > 30f) return; // لا ترسم إذا كان بعيداً جداً

        // حجم شريط الصحة
        float barWidth = 60f;
        float barHeight = 8f;

        // تحويل إحداثيات الشاشة (Unity GUI يقلب Y)
        float x = screenPos.x - barWidth / 2f;
        float y = Screen.height - screenPos.y - barHeight - 5f;

        // خلفية سوداء
        GUI.DrawTexture(new Rect(x - 1, y - 1, barWidth + 2, barHeight + 2), Texture2D.whiteTexture);
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x - 1, y - 1, barWidth + 2, barHeight + 2), Texture2D.whiteTexture);

        // شريط الصحة
        float healthPercent = currentHealth / maxHealth;
        GUI.color = Color.Lerp(Color.red, Color.green, healthPercent);
        GUI.DrawTexture(new Rect(x, y, barWidth * healthPercent, barHeight), Texture2D.whiteTexture);

        // إعادة اللون الافتراضي
        GUI.color = Color.white;

        // عرض نسبة الصحة كنص
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 10;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y - 14f, barWidth, 14f), 
            Mathf.Ceil(currentHealth) + "/" + maxHealth, style);
    }
}