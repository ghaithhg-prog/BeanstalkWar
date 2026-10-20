using UnityEngine;

/// <summary>
/// EnchantedRiverHazard:
/// Monitors the boundary river zone (R between 115m and 148m).
/// 1. Shallow Shoreline (R = 115m - 120m):
///    Player wades in ankle-deep water with realistic footstep water splashes!
/// 2. Deep Sacred Moat (R >= 120m):
///    Player drowns in the rushing current and is immediately returned safely
///    to their team sanctuary spawn point with full health (no 10-second wait!).
/// 3. Golden Ratio (Φ = 1.618) styled parchment notification on screen.
/// </summary>
public class EnchantedRiverHazard : MonoBehaviour
{
    [Header("River Boundaries")]
    public float shallowRadius = 115f;
    public float deepRadius = 118f;
    public float outerRadius = 150f;
    public float waterSurfaceY = 0.45f;

    private float checkTimer = 0f;
    private float shallowSplashTimer = 0f;
    private static float bannerTimer = 0f;
    private readonly System.Collections.Generic.HashSet<int> activeDrowningIDs = new System.Collections.Generic.HashSet<int>();

    void Update()
    {
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
        }

        checkTimer += Time.deltaTime;
        if (checkTimer < 0.12f) return;
        checkTimer = 0f;

        CheckEntitiesInRiver();
    }

    void CheckEntitiesInRiver()
    {
        shallowSplashTimer += 0.12f;
        bool canSpawnFootSplash = shallowSplashTimer >= 0.28f;
        if (canSpawnFootSplash) shallowSplashTimer = 0f;

        // 1. فحص جميع اللاعبين
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (players != null)
        {
            foreach (PlayerMovement pm in players)
            {
                if (pm == null) continue;
                Vector3 pos = pm.transform.position;
                float r = new Vector2(pos.x, pos.z).magnitude;

                // نطاق النهر: من 115م فما فوق على مستوى الأرض أو الماء (y <= 4.0m)
                if (r >= shallowRadius && pos.y <= 4.0f && pos.y >= -15.0f)
                {
                    if (r >= deepRadius)
                    {
                        TriggerDrowningRespawn(pm.gameObject, pos, pm.team);
                    }
                    else if (canSpawnFootSplash)
                    {
                        SpawnFootstepRipple(pos);
                    }
                }
            }
        }

        // 2. فحص بقية الكائنات ذات الصحة (مثل البوتات إن لم تكن ضمن PlayerMovement)
        Health[] entities = FindObjectsByType<Health>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (entities != null)
        {
            foreach (Health h in entities)
            {
                if (h == null || h.isDead) continue;
                if (h.GetComponent<PlayerMovement>() != null) continue; // تم فحصه مسبقاً

                Vector3 pos = h.transform.position;
                float r = new Vector2(pos.x, pos.z).magnitude;

                if (r >= shallowRadius && pos.y <= 4.0f && pos.y >= -15.0f)
                {
                    if (r >= deepRadius)
                    {
                        BotController bot = h.GetComponent<BotController>();
                        PlayerMovement.TeamType team = bot != null ? bot.team : PlayerMovement.TeamType.Protector;
                        TriggerDrowningRespawn(h.gameObject, pos, team);
                    }
                    else if (canSpawnFootSplash)
                    {
                        SpawnFootstepRipple(pos);
                    }
                }
            }
        }
    }

    void TriggerDrowningRespawn(GameObject entity, Vector3 pos, PlayerMovement.TeamType team)
    {
        if (entity == null) return;
        int id = entity.GetInstanceID();
        if (activeDrowningIDs.Contains(id)) return;
        activeDrowningIDs.Add(id);

        StartCoroutine(DrowningRoutine(entity, pos, team, id));
    }

    System.Collections.IEnumerator DrowningRoutine(GameObject entity, Vector3 pos, PlayerMovement.TeamType team, int entityId)
    {
        // 1. تأثير رذاذ ماء كثيف عند الدخول في الماء العميق
        SpawnSplashPuff(pos, 3.0f);

        Rigidbody rb = entity.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // إيقاف CharacterController مؤقتاً لضمان النقل النظيف بدون تعثر أو سقوط داخل الأرض
        CharacterController cc = entity.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        UnityEngine.AI.NavMeshAgent navAgent = entity.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.isStopped = true;
        }

        yield return new WaitForSeconds(0.20f);

        if (entity == null)
        {
            activeDrowningIDs.Remove(entityId);
            yield break;
        }

        // 2. تحديد ملاذ الفريق وفريق اللاعب
        Transform spawnPoint = SpawnManager.Instance != null ? SpawnManager.Instance.GetSpawnPoint(team) : null;
        Vector3 targetPos = spawnPoint != null ? spawnPoint.position : (team == PlayerMovement.TeamType.Destroyer ? new Vector3(15f, 1f, 0f) : new Vector3(-15f, 1f, 0f));

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. النقل الآمن إلى نقطة البداية (على ارتفاع 1.2 متر لتجنب الالتصاق بالأرض)
        Vector3 safeRespawnPos = targetPos + Vector3.up * 1.2f;
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.Warp(safeRespawnPos);
            navAgent.isStopped = false;
        }
        else
        {
            entity.transform.position = safeRespawnPos;
        }

        if (cc != null) cc.enabled = true;

        if (spawnPoint != null) entity.transform.rotation = spawnPoint.rotation;

        // 4. استعادة الصحة بالكامل وتفعيل إشعار الشاشة لمدة 7 ثوانٍ كاملة كما طلب غيث
        Health h = entity.GetComponent<Health>();
        if (h != null)
        {
            h.currentHealth = h.maxHealth;
        }
        bannerTimer = 7.0f;

        activeDrowningIDs.Remove(entityId);
        Debug.Log($"🌊 [EnchantedRiverHazard] {entity.name} drowned in deep river! Teleported safely to {team} sanctuary.");
    }

    void SpawnFootstepRipple(Vector3 pos)
    {
        // تم إلغاء إنشاء الأجسام التكرارية لتحقيق أعلى أداء وسلاسة FPS
    }

    void SpawnSplashPuff(Vector3 pos, float scale)
    {
        // تم إلغاء إنشاء الأجسام المؤقتة لمنع تراكم الذاكرة وهبوط الإطارات
    }

    void OnGUI()
    {
        if (bannerTimer <= 0f) return;

        // تصميم شريط الإشعار الملوكي بتطبيق النسبة الذهبية (Golden Ratio Φ = 1.618)
        float sw = Screen.width;
        float bannerWidth = Mathf.Min(sw * 0.618f, 580f); // 0.618 من عرض الشاشة
        float bannerHeight = bannerWidth / 6.8f;
        float bannerX = (sw - bannerWidth) * 0.5f;
        float bannerY = 85f;

        float alpha = Mathf.Clamp01(bannerTimer / 0.6f);

        Color bg = new Color(0.08f, 0.16f, 0.24f, 0.92f * alpha);
        Color border = new Color(1.0f, 0.84f, 0.32f, 0.95f * alpha); // ذهبي ملكي

        GUI.color = border;
        GUI.DrawTexture(new Rect(bannerX - 2, bannerY - 2, bannerWidth + 4, bannerHeight + 4), Texture2D.whiteTexture);

        GUI.color = bg;
        GUI.DrawTexture(new Rect(bannerX, bannerY, bannerWidth, bannerHeight), Texture2D.whiteTexture);

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = Mathf.RoundToInt(bannerHeight * 0.38f);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = new Color(0.95f, 0.98f, 1.0f, alpha);

        string text = LanguageSelectionUI.Localize(
            "🌊 Swept by the Enchanted River! Returned safely to Sanctuary.",
            ar: "🌊 غَرِقْتَ فِي النَّهْرِ السَّاحِرِ! وَعُدْتَ بَسَلَامٍ إِلَى مَلَاذِ فَرِيقِكَ",
            ru: "🌊 Вас унесло волшебной рекой в святилище!",
            fr: "🌊 Emporté par la rivière enchantée vers le sanctuaire !",
            es: "🌊 ¡Arrastrado por el río encantado de vuelta al santuario!",
            de: "🌊 Vom verzauberten Fluss sicher zum Heiligtum zurückgebracht!"
        );

        GUI.Label(new Rect(bannerX, bannerY, bannerWidth, bannerHeight), text, style);
    }
}
