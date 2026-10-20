using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// ضريح غنائم الزعيم (Boss Tombstone / Spoils Chest)
/// يظهر في موقع موت الزعيم ويحتوي على الكنز والقوة التي يستطيع فريق القاتل جمعها
/// </summary>
public class BossTombstone : MonoBehaviour
{
    public string bossTitle = "Ancient Boss";
    public PlayerMovement.TeamType rewardedTeam = PlayerMovement.TeamType.Protector;
    public string killerPlayerName = "Warrior";
    public int bonusCoins = 150;
    public int bonusXP = 300;

    private bool isCollected = false;
    private Light chestLight;
    private GameObject floatingCrest;

    public void Setup(string title, PlayerMovement.TeamType team, string killerName, int coins, int xp)
    {
        bossTitle = title;
        rewardedTeam = team;
        killerPlayerName = killerName;
        bonusCoins = coins;
        bonusXP = xp;

        BuildTombstoneVisuals();
    }

    void BuildTombstoneVisuals()
    {
        // 1. قاعدة الضريح الحجري
        GameObject baseStone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseStone.name = "StoneBase";
        baseStone.transform.SetParent(transform);
        baseStone.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        baseStone.transform.localScale = new Vector3(2.4f, 0.4f, 2.4f);
        Collider baseCol = baseStone.GetComponent<Collider>();
        if (baseCol != null) Destroy(baseCol);

        Renderer baseR = baseStone.GetComponent<Renderer>();
        if (baseR != null)
        {
            baseR.material.color = new Color(0.2f, 0.22f, 0.25f);
        }

        // 2. صندوق الكنز الذهبي / التابوت الملكي
        GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chest.name = "GoldenChest";
        chest.transform.SetParent(transform);
        chest.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        chest.transform.localScale = new Vector3(1.4f, 0.9f, 1.0f);
        Collider chestCol = chest.GetComponent<Collider>();
        if (chestCol != null) Destroy(chestCol);

        Color teamColor = rewardedTeam == PlayerMovement.TeamType.Protector 
            ? new Color(0.15f, 0.75f, 1f) 
            : new Color(1f, 0.3f, 0.1f);

        Renderer chestR = chest.GetComponent<Renderer>();
        if (chestR != null)
        {
            chestR.material.color = new Color(0.95f, 0.75f, 0.15f); // ذهبي
            chestR.material.EnableKeyword("_EMISSION");
            chestR.material.SetColor("_EmissionColor", new Color(0.6f, 0.45f, 0.05f));
        }

        // 3. بلورة/تاج يطفو ويدور فوق الصندوق
        floatingCrest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        floatingCrest.name = "FloatingRelic";
        floatingCrest.transform.SetParent(transform);
        floatingCrest.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        floatingCrest.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
        Collider relicCol = floatingCrest.GetComponent<Collider>();
        if (relicCol != null) Destroy(relicCol);

        Renderer relicR = floatingCrest.GetComponent<Renderer>();
        if (relicR != null)
        {
            relicR.material.color = teamColor;
            relicR.material.EnableKeyword("_EMISSION");
            relicR.material.SetColor("_EmissionColor", teamColor * 2f);
        }

        // 4. ضوء هالة ساطعة
        chestLight = gameObject.AddComponent<Light>();
        chestLight.type = LightType.Point;
        chestLight.range = 9f;
        chestLight.color = Color.Lerp(Color.yellow, teamColor, 0.4f);
        chestLight.intensity = 4f;

        // 5. محفز الالتقاط (Trigger)
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2.8f;
    }

    void Update()
    {
        if (isCollected) return;

        // دوران وتموج البلورة الأثيرية
        if (floatingCrest != null)
        {
            floatingCrest.transform.Rotate(0f, 60f * Time.deltaTime, 30f * Time.deltaTime, Space.Self);
            floatingCrest.transform.localPosition = new Vector3(0f, 2.2f + Mathf.Sin(Time.time * 3f) * 0.2f, 0f);
        }

        // نبض الضوء
        if (chestLight != null)
        {
            chestLight.intensity = 3f + Mathf.PingPong(Time.time * 2.5f, 2.5f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        Health h = other.GetComponent<Health>();
        if (h != null && h.isDead) return;

        // التحقق أن اللاعب من نفس الفريق الفائز بالزعيم
        if (pm.team != rewardedTeam)
        {
            return;
        }

        CollectTombstone(pm);
    }

    void CollectTombstone(PlayerMovement collector)
    {
        if (isCollected) return;
        isCollected = true;

        Debug.Log($"🎁 SPOILS CLAIMED: {collector.name} collected the spoils of {bossTitle}!");

        // منح العملات وXP
        if (collector.IsOwner && PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.AddCoins(bonusCoins);
            PlayerProfile.Instance.AddAccountXP(bonusXP);
            PlayerProgressionAndShopUI.Instance?.ShowNotification($"👑 Boss Spoils Claimed: +{bonusCoins} 🪙 | +{bonusXP} XP!");
        }

        // منح بف القوة الجبارة (+50% ضرر وهالة مضيئة لمدة 30 ثانية)
        TitanPowerBuff buff = collector.GetComponent<TitanPowerBuff>();
        if (buff == null)
        {
            buff = collector.gameObject.AddComponent<TitanPowerBuff>();
        }
        buff.ActivateBuff(bossTitle, 45f);

        StartCoroutine(CollectionSequence());
    }

    IEnumerator CollectionSequence()
    {
        if (chestLight != null) chestLight.intensity = 8f;

        float timer = 0.4f;
        Vector3 startScale = transform.localScale;
        while (timer > 0f)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, startScale, timer / 0.4f);
            timer -= Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnGUI()
    {
        if (isCollected || Camera.main == null) return;

        float dist = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (dist > 35f) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.8f);
        if (screenPos.z < 0) return;

        float width = 180f;
        float height = 45f;
        float x = screenPos.x - width / 2f;
        float y = Screen.height - screenPos.y - height;

        // خلفية أنيقة
        GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);

        Color teamColor = rewardedTeam == PlayerMovement.TeamType.Protector 
            ? new Color(0.2f, 0.8f, 1f) 
            : new Color(1f, 0.35f, 0.2f);

        // إطار بلون الفريق
        GUI.color = teamColor;
        GUI.DrawTexture(new Rect(x, y, width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + height - 2, width, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;

        GUI.Label(new Rect(x, y + 2, width, 18), $"👑 SPOILS: {bossTitle}", style);

        style.fontSize = 9;
        style.fontStyle = FontStyle.Normal;
        style.normal.textColor = Color.white;
        string teamName = rewardedTeam == PlayerMovement.TeamType.Protector ? "PROTECTORS" : "DESTROYERS";
        GUI.Label(new Rect(x, y + 20, width, 22), $"For {teamName}\nStep near to claim reward!", style);
    }
}
