using UnityEngine;

[CreateAssetMenu(
    fileName = "NewCharacter",
    menuName = "Game/Character Data"
)]
public class CharacterData : ScriptableObject
{
    // =========================
    // Identity
    // =========================

    [Header("Identity")]

    [Tooltip("رقم فريد لا يتكرر بين الشخصيات")]
    public int characterID;

    public string characterName;

    [TextArea(2, 4)]
    public string description;

    public Sprite characterIcon;

    // =========================
    // Role
    // =========================

    public enum CharacterRole
    {
        Tank,
        Damage,
        Support,
        Control,
        Specialist
    }

    [Header("Role")]
    public CharacterRole role = CharacterRole.Damage;

    // =========================
    // Selection
    // =========================

    [Header("Selection")]

    [Min(1)]
    [Tooltip("أقصى عدد من هذه الشخصية داخل الفريق الواحد")]
    public int maxPerTeam = 2;

    // =========================
    // Stats
    // =========================

    [Header("Stats")]

    public float maxHealth = 100f;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    public float attackRange = 2.5f;
    public float moveSpeed = 6f;

    // =========================
    // Jump
    // =========================

    [Header("Jump Settings")]

    public float jumpForce = 7f;
    public bool canDoubleJump = false;

    // =========================
    // Attack
    // =========================

    [Header("Attack Type")]

    public CharacterCombat.AttackType attackType;
}