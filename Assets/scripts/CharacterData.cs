using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    public float attackRange = 2.5f;
    public float moveSpeed = 6f;

    [Header("Jump Settings")]
     public float jumpForce = 7f;
     public bool canDoubleJump = false;

    [Header("Attack Type")]
    public CharacterCombat.AttackType attackType;
}