using UnityEngine;
using UnityEngine.UI;

public class TreeGrowth : MonoBehaviour
{
    [Header("UI")]
    public Image healthBarFill;

    [Header("Growth")]
    public float growthSpeed = 0.5f;
    public float maxHeight = 18f;

    [Header("Shake Settings")]
    public float shakeDuration = 2f;
    public float shakeAmount = 0.1f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Critical State")]
    public float criticalDuration = 4f;
    public float rescueHealThreshold = 30f;

    [Header("Fall Settings")]
    public float fallSpeed = 120f;

    private bool isMature = false;
    private bool isShaking = false;
    private bool isCritical = false;
    private bool criticalUsed = false;
    private bool isDead = false;
    private bool isFalling = false;

    private float shakeTimer = 0f;
    private float criticalTimer = 0f;
    private float healDuringCritical = 0f;
    private float currentFallAngle = 0f;

    private Vector3 originalPivotPosition;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        UpdateHealthBar();

        if (!isDead)
        {
            GrowTree();
            CheckDeath();
        }

        if (isCritical)
        {
            HandleCriticalState();
        }

        if (isShaking)
        {
            ShakeTree();
        }

        if (isFalling)
        {
            HandleFall();
        }
    }

    // =========================
    // Growth
    // =========================

    void GrowTree()
    {
        if (!isMature)
        {
            float growth = growthSpeed * Time.deltaTime;

            transform.localScale += new Vector3(0, growth, 0);
            transform.position += new Vector3(0, growth / 2f, 0);

            if (transform.localScale.y >= maxHeight)
            {
                transform.localScale = new Vector3(
                    transform.localScale.x,
                    maxHeight,
                    transform.localScale.z
                );

                isMature = true;
                StartShake(); // ✅ اهتزاز عند النضج
            }
        }
    }

    // =========================
    // Critical System
    // =========================

    void CheckDeath()
{
    if (currentHealth <= 0 && !isDead)
    {
        // ✅ إذا كنا داخل الحالة الحرجة لا نفعل شيئًا
        if (isCritical)
            return;

        if (!criticalUsed)
        {
            EnterCriticalState();
        }
        else
        {
            FinalDeath();
        }
    }
}

    void EnterCriticalState()
    {
        isCritical = true;
        criticalUsed = true;
        criticalTimer = criticalDuration;
        healDuringCritical = 0f;
        currentHealth = 0f;

        StartShake(); // ✅ اهتزاز أثناء الحالة الحرجة

        Debug.Log("Tree entered Critical State!");
    }

    void HandleCriticalState()
    {
        if (criticalTimer > 0f)
        {
            criticalTimer -= Time.deltaTime;
        }
        else
        {
            isCritical = false;
            FinalDeath();
        }
    }

    void ExitCriticalState()
    {
        isCritical = false;
        currentHealth = maxHealth * 0.25f; // ✅ تعود بـ 25%

        Debug.Log("Tree rescued from Critical State!");
    }

    // =========================
    // Shake
    // =========================

    void StartShake()
    {
        isShaking = true;
        shakeTimer = shakeDuration;
        originalPivotPosition = transform.parent.position;
    }

    void ShakeTree()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;

            float offsetX = Random.Range(-shakeAmount, shakeAmount);
            float offsetZ = Random.Range(-shakeAmount, shakeAmount);

            transform.parent.position =
                originalPivotPosition + new Vector3(offsetX, 0f, offsetZ);
        }
        else
        {
            isShaking = false;
            transform.parent.position = originalPivotPosition;

            if (isMature && !isCritical)
                Debug.Log("Tree reached Top Level. Crystal Ready!");
        }
    }

    // =========================
    // Fall
    // =========================

    void FinalDeath()
    {
        isDead = true;
        isFalling = true;

        Debug.Log("Tree permanently destroyed!");
        GameStateManager.Instance.EndGame("Destroyers");
    }

    void HandleFall()
    {
        float fallStep = fallSpeed * Time.deltaTime;

        if (currentFallAngle < 90f)
        {
            transform.parent.Rotate(Vector3.forward, fallStep);
            currentFallAngle += fallStep;
        }
    }

    // =========================
    // Health
    // =========================

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        if (isCritical)
        {
            healDuringCritical += amount;

            if (healDuringCritical >= rescueHealThreshold)
            {
                ExitCriticalState();
            }

            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = currentHealth / maxHealth;
            healthBarFill.fillAmount = healthPercent;
            healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
        }
    }
}