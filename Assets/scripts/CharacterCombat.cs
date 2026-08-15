using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class CharacterCombat : NetworkBehaviour
{
    private Vector3 lastAttackDirection = Vector3.forward;

    public enum AttackType
    {
        Melee,
        Ranged
    }

    [Header("Character Data")]
    public CharacterData characterData;

    [Header("Attack Settings")]
    public AttackType attackType = AttackType.Melee;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    public float attackRange = 2.5f;

    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Team")]
    public PlayerMovement.TeamType team;

    private float nextAttackTime = 0f;

    // ✅ زر الهجوم للموبايل
    private bool attackButtonPressed = false;

    void Start()
    {
        if (characterData != null)
        {
            attackDamage = characterData.attackDamage;
            attackCooldown = characterData.attackCooldown;
            attackRange = characterData.attackRange;
            attackType = characterData.attackType;

            PlayerMovement movement = GetComponent<PlayerMovement>();
            Health health = GetComponent<Health>();

            if (movement != null)
                movement.moveSpeed = characterData.moveSpeed;

            if (health != null)
            {
                health.maxHealth = characterData.maxHealth;
                health.currentHealth = characterData.maxHealth;
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // ✅ اللاعب يتوجه دائماً نحو اتجاه الكاميرا
        AlignWithCamera();

        // ✅ هجوم بالماوس (PC)
        bool mouseAttack = Input.GetMouseButtonDown(0);

        // ✅ هجوم بزر الموبايل
        bool mobileAttack = attackButtonPressed;
        attackButtonPressed = false;

        if (mouseAttack || mobileAttack)
        {
            // ✅ تجاهل الضغط على UI في PC فقط
            if (mouseAttack && EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            if (Time.time >= nextAttackTime)
            {
                CalculateDirectionFromCenter();
                PerformAttack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    // ✅ اللاعب يتوجه دائماً نحو الكاميرا أفقياً
    void AlignWithCamera()
    {
        if (Camera.main == null) return;

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;

        if (cameraForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
        }
    }

    // ✅ الاتجاه من منتصف الشاشة (Crosshair)
    void CalculateDirectionFromCenter()
    {
        if (Camera.main == null) return;

        // ✅ نرسل Ray من منتصف الشاشة
        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)
        );

        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, 200f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 100f;
        }

        if (firePoint != null)
            lastAttackDirection = (targetPoint - firePoint.position).normalized;
        else
            lastAttackDirection = ray.direction;
    }

    // ✅ يُستدعى من زر الموبايل في UI
    public void OnAttackButtonPressed()
    {
        attackButtonPressed = true;
    }

    void PerformAttack()
    {
        if (attackType == AttackType.Melee)
            MeleeAttack();
        else
            RangedAttack();
    }

    void MeleeAttack()
    {
        Vector3 attackPoint = transform.position + 
                             transform.forward * attackRange;
        Collider[] hits = Physics.OverlapSphere(attackPoint, 1f);

        foreach (Collider hit in hits)
        {
            TreeGrowth tree = hit.GetComponent<TreeGrowth>();
            if (tree != null)
            {
                if (team == PlayerMovement.TeamType.Destroyer)
                    tree.TakeDamage(attackDamage);
                else
                    tree.Heal(attackDamage);
                continue;
            }

            Health health = hit.GetComponent<Health>();
            if (health != null && hit.gameObject != gameObject)
            {
                CharacterCombat other = hit.GetComponent<CharacterCombat>();
                if (other != null && other.team != this.team)
                    health.TakeDamage(attackDamage);
            }
        }
    }

    void RangedAttack()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        projectile.transform.forward = lastAttackDirection;

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage = attackDamage;
            proj.team = team;
        }
    }
}