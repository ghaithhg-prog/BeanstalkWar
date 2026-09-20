using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class CharacterCombat : NetworkBehaviour
{
    private Vector3 lastAttackDirection =
        Vector3.forward;

    public enum AttackType
    {
        Melee,
        Ranged
    }

    [Header("Character Data")]
    public CharacterData characterData;

    [Header("Attack Settings")]
    public AttackType attackType =
        AttackType.Melee;

    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    public float attackRange = 2.5f;

    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Team")]
    public PlayerMovement.TeamType team;

    private float nextAttackTime = 0f;

    // زر الموبايل
    private bool attackButtonPressed = false;

    // هل هذا الكائن Bot؟
    private bool isBot = false;

    // =========================
    // Start
    // =========================

    void Start()
    {
        // إذا وجد BotController
        // فهذا ليس لاعبًا بشريًا.
        isBot =
            GetComponent<BotController>()
            != null;

        if (characterData != null)
        {
            ApplyCharacterData();
        }
    }

    // =========================
    // Character Data
    // =========================

    void ApplyCharacterData()
    {
        attackDamage =
            characterData.attackDamage;

        attackCooldown =
            characterData.attackCooldown;

        attackRange =
            characterData.attackRange;

        attackType =
            characterData.attackType;

        PlayerMovement movement =
            GetComponent<PlayerMovement>();

        Health health =
            GetComponent<Health>();

        if (movement != null)
        {
            movement.moveSpeed =
                characterData.moveSpeed;
        }

        if (health != null)
        {
            health.maxHealth =
                characterData.maxHealth;

            health.currentHealth =
                characterData.maxHealth;
        }
    }

    // =========================
    // Update
    // =========================

    void Update()
    {
        // BOT ممنوع من قراءة
        // Mouse / Keyboard / Mobile Input.
        if (isBot)
            return;

        // اللاعب يتحكم فقط
        // في شخصيته الخاصة.
        if (!IsOwner)
            return;

        // لا هجوم قبل بداية المباراة.
        if (GameStateManager.Instance == null ||
            !GameStateManager.Instance.IsPlaying())
        {
            return;
        }

        // لا يستطيع حامل الكريستالة استخدام الأسلحة العادية
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && movement.isCarryingCrystal.Value)
        {
            return;
        }

        AlignWithCamera();

        bool mouseAttack =
            Input.GetMouseButtonDown(0);

        bool mobileAttack =
            attackButtonPressed;

        attackButtonPressed = false;

        if (!mouseAttack &&
            !mobileAttack)
        {
            return;
        }

        // لا نهاجم إذا كان الضغط
        // على واجهة المستخدم.
        if (mouseAttack &&
            EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject())
        {
            return;
        }

        TryPlayerAttack();
    }

    // =========================
    // Player Attack
    // =========================

    void TryPlayerAttack()
    {
        if (Time.time <
            nextAttackTime)
        {
            return;
        }

        CalculateDirectionFromCenter();

        PerformAttack();

        nextAttackTime =
            Time.time +
            attackCooldown;
    }

    // =========================
    // AI Attack
    // =========================

    public bool TryAIAttack(
        Transform target)
    {
        // فقط Bot يستطيع استخدام
        // هذه الدالة.
        if (!isBot)
            return false;

        // AI يعمل من السيرفر فقط.
        if (!IsServer)
            return false;

        if (target == null)
            return false;

        if (GameStateManager.Instance == null ||
            !GameStateManager.Instance.IsPlaying())
        {
            return false;
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && movement.isCarryingCrystal.Value)
        {
            return false;
        }

        if (Time.time <
            nextAttackTime)
        {
            return false;
        }

        // =========================
        // Direction
        // =========================

        Vector3 origin =
            firePoint != null
                ? firePoint.position
                : transform.position;

        Vector3 direction =
            target.position -
            origin;

        if (direction.sqrMagnitude <
            0.001f)
        {
            return false;
        }

        lastAttackDirection =
            direction.normalized;

        // تدوير أفقي نحو الهدف.
        Vector3 flatDirection =
            new Vector3(
                direction.x,
                0f,
                direction.z
            );

        if (flatDirection.sqrMagnitude >
            0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    flatDirection
                );
        }

        PerformAttack();

        nextAttackTime =
            Time.time +
            attackCooldown;

        return true;
    }

    // =========================
    // Camera Aim
    // =========================

    void AlignWithCamera()
    {
        if (Camera.main == null)
            return;

        Vector3 cameraForward =
            Camera.main.transform.forward;

        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude >
            0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    cameraForward
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    10f *
                    Time.deltaTime
                );
        }
    }

    // =========================
    // Center Aim
    // =========================

    void CalculateDirectionFromCenter()
    {
        if (Camera.main == null)
            return;

        Ray ray =
            Camera.main.ScreenPointToRay(
                new Vector3(
                    Screen.width / 2f,
                    Screen.height / 2f,
                    0f
                )
            );

        Vector3 targetPoint;

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                200f))
        {
            targetPoint =
                hit.point;
        }
        else
        {
            targetPoint =
                ray.origin +
                ray.direction *
                100f;
        }

        if (firePoint != null)
        {
            lastAttackDirection =
                (
                    targetPoint -
                    firePoint.position
                ).normalized;
        }
        else
        {
            lastAttackDirection =
                ray.direction;
        }
    }

    // =========================
    // Mobile Attack
    // =========================

    public void OnAttackButtonPressed()
    {
        if (isBot)
            return;

        attackButtonPressed = true;
    }

    // =========================
    // Perform Attack
    // =========================

    void PerformAttack()
    {
        if (attackType ==
            AttackType.Melee)
        {
            MeleeAttack();
        }
        else
        {
            RangedAttack();
        }
    }

    // =========================
    // Melee
    // =========================

    void MeleeAttack()
    {
        Vector3 attackPoint =
            transform.position +
            transform.forward *
            attackRange;

        Collider[] hits =
            Physics.OverlapSphere(
                attackPoint,
                1f
            );

        foreach (
            Collider hit in hits)
        {
            // =========================
            // Tree
            // =========================

            TreeGrowth tree =
                hit.GetComponent<TreeGrowth>();

            if (tree != null)
            {
                float levelMultiplier = TreeGrowth.GetHeightMultiplier(transform.position);
                float finalAmount = attackDamage * levelMultiplier;

                if (team ==
                    PlayerMovement.TeamType.Destroyer)
                {
                    tree.TakeDamage(
                        finalAmount
                    );
                }
                else
                {
                    tree.Heal(
                        finalAmount
                    );
                }

                continue;
            }

            // =========================
            // Players / Bots
            // =========================

            Health health =
                hit.GetComponent<Health>();

            if (health == null ||
                hit.gameObject ==
                gameObject)
            {
                continue;
            }

            CharacterCombat other =
                hit.GetComponent<CharacterCombat>();

            if (other != null &&
                other.team != team)
            {
                health.TakeDamage(
                    attackDamage
                );
            }
        }
    }

    // =========================
    // Ranged
    // =========================

    void RangedAttack()
    {
        if (projectilePrefab == null ||
            firePoint == null)
        {
            return;
        }

        GameObject projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.identity
            );

        projectile.transform.forward =
            lastAttackDirection;

        Projectile proj =
            projectile.GetComponent<Projectile>();

        if (proj != null)
        {
            proj.damage =
                attackDamage;

            proj.team =
                team;
        }
    }
}