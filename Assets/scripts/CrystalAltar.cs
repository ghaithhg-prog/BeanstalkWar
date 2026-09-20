using UnityEngine;
using Unity.Netcode;

public class CrystalAltar : NetworkBehaviour
{
    [Header("Altar Info")]
    public string altarName = "Sacred Altar";
    public float triggerRadius = 3.5f;

    [Header("Protection Aura")]
    [Tooltip("نسبة تخفيض الضرر لحامل الكريستالة أثناء وجوده داخل المذبح لمنع الـ Camping")]
    public float damageReductionPercent = 0.5f;

    [Header("Visual & Sound FX")]
    public ParticleSystem auraEffect;
    public ParticleSystem victoryEffect;
    public Light altarLight;
    public AudioSource audioSource;
    public AudioClip victorySound;
    public AudioClip enterAuraSound;

    private bool gameWon = false;

    void Start()
    {
        // التأكد من وجود Collider صلب حتى لا يمر اللاعب من خلال المذبح
        // ويستطيع الوقوف والقفز فوقه
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            // حجم أكبر ليكون واضحاً ويمكن الوقوف عليه
            box.size = new Vector3(3f, 2f, 3f);
            box.center = new Vector3(0f, 1f, 0f);
        }
        else if (existingCollider.isTrigger)
        {
            existingCollider.isTrigger = false;
        }

        // وسم المذبح كـ Ground حتى يستطيع اللاعب القفز عليه مرة أخرى
        gameObject.tag = "Ground";

        if (auraEffect != null && !auraEffect.isPlaying)
            auraEffect.Play();
    }

    void Update()
    {
        if (!IsServer || gameWon) return;

        CheckAltarDeliveryServer();
    }

    void CheckAltarDeliveryServer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius);

        foreach (Collider hit in hits)
        {
            PlayerMovement pm = hit.GetComponent<PlayerMovement>();
            if (pm == null || pm.team != PlayerMovement.TeamType.Protector) continue;

            Health health = hit.GetComponent<Health>();
            if (health != null && health.isDead) continue;

            // هل هذا اللاعب يحمل الكريستالة؟
            if (pm.isCarryingCrystal.Value || 
               (CrystalItem.Instance != null && CrystalItem.Instance.IsCarrier(hit.gameObject)))
            {
                OnCrystalDeliveredServer(pm);
                break;
            }
        }
    }

    void OnCrystalDeliveredServer(PlayerMovement carrier)
    {
        if (gameWon) return;
        gameWon = true;

        Debug.Log($"CRYSTAL DELIVERED TO {altarName} by {carrier.name}! PROTECTORS WIN!");

        // إطلاق تأثيرات الفوز للجميع
        TriggerVictoryFxRpc(transform.position);

        // إنهاء المباراة وإعلان فوز الـ Protectors
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EndGame("Protectors");
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void TriggerVictoryFxRpc(Vector3 altarPos)
    {
        if (victoryEffect != null)
        {
            victoryEffect.transform.position = altarPos + Vector3.up;
            victoryEffect.Play();
        }

        if (altarLight != null)
        {
            altarLight.color = Color.green;
            altarLight.intensity *= 2f;
        }

        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }
    }

    // رسم دائرة المذبح في الـ Scene View للرؤية بوضوح
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
