using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 15f;
    public PlayerMovement.TeamType team;

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        TreeGrowth tree = other.GetComponent<TreeGrowth>();
        if (tree != null)
        {
            if (team == PlayerMovement.TeamType.Destroyer)
                tree.TakeDamage(damage);
            else
                tree.Heal(damage);

            Destroy(gameObject);
            return;
        }

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            CharacterCombat otherCombat = other.GetComponent<CharacterCombat>();
            if (otherCombat != null && otherCombat.team != this.team)
            {
                health.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }

    void Start()
    {
        Destroy(gameObject, 5f);
    }
}