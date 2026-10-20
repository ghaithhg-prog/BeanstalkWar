using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

/// <summary>
/// نظام أحداث الزعماء: يقوم بتحويل أحد البوتات العادية إلى زعيم ضخم كل فترة زمنية
/// </summary>
public class BossEventSystem : NetworkBehaviour
{
    public static BossEventSystem Instance;

    [Header("Boss Settings")]
    public float bossSpawnInterval = 120f; // كل دقيقتين
    public float bossHealth = 2000f;
    public float bossDamageMultiplier = 3f;
    public float bossScaleMultiplier = 2.5f;
    public int bossXpReward = 500; // XP ضخم عند القتل

    [Header("UI")]
    public TextMeshProUGUI announcementText;

    private float timer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!IsServer) return;

        // لا يظهر الزعماء إلا إذا بدأت اللعبة
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()) return;

        timer += Time.deltaTime;
        if (timer >= bossSpawnInterval)
        {
            timer = 0f;
            TrySpawnBoss();
        }
    }

    void TrySpawnBoss()
    {
        // البحث عن كل البوتات في اللعبة
        BotController[] allBots = FindObjectsByType<BotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        System.Collections.Generic.List<BotController> validBots = new System.Collections.Generic.List<BotController>();

        foreach (BotController bot in allBots)
        {
            Health h = bot.GetComponent<Health>();
            // التأكد أن البوت حي ولم يتحول لزعيم مسبقاً
            if (h != null && !h.isDead && bot.GetComponent<BossModifier>() == null)
            {
                validBots.Add(bot);
            }
        }

        if (validBots.Count > 0)
        {
            // اختيار بوت عشوائي ليصبح زعيماً
            BotController chosenBot = validBots[Random.Range(0, validBots.Count)];
            
            // إضافة سكربت الزعيم إليه
            BossModifier bossMod = chosenBot.gameObject.AddComponent<BossModifier>();
            bossMod.InitializeBoss(bossHealth, bossDamageMultiplier, bossScaleMultiplier, bossXpReward);

            // إرسال تنبيه لجميع اللاعبين
            string teamName = chosenBot.GetComponent<PlayerMovement>().team.ToString();
            AnnounceBossRpc(teamName);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void AnnounceBossRpc(string teamName)
    {
        if (announcementText != null)
        {
            StartCoroutine(ShowAnnouncement("A " + teamName.ToUpper() + " BOSS HAS APPEARED!"));
        }
        else
        {
            Debug.LogWarning("A " + teamName.ToUpper() + " BOSS HAS APPEARED! (No UI Text assigned)");
        }
    }

    IEnumerator ShowAnnouncement(string message)
    {
        announcementText.text = message;
        announcementText.color = Color.red;
        announcementText.gameObject.SetActive(true);
        
        // تكبير وتصغير النص
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one * 1.5f;
        float t = 0;
        
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            announcementText.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        yield return new WaitForSeconds(3f);
        announcementText.gameObject.SetActive(false);
    }
}
