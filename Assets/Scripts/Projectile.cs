using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public float knockback;
    public Unit target;
    public Unit owner; // To attribute heal/buff logic if needed, though usually handled at launch

    private Vector3 direction;
    private bool initialized = false;

    public void Initialize(Unit _owner, Unit _target, float _damage, float _knockback)
    {
        owner = _owner;
        target = _target;
        damage = _damage;
        knockback = _knockback;

        // Determine direction at launch
        if (target != null)
        {
            direction = (target.transform.position - transform.position).normalized;
            direction.y = 0; // Keep flat
        }
        else
        {
            direction = transform.forward;
        }

        Destroy(gameObject, 3f); // Safety destroy
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        // If target is moving, maybe home in?
        // Doc says: "Projectile flies for attack range... Speed 10unit/s"
        // User didn't specify homing. Simple straight line is usually safer for projectiles unless homing specified.
        // However, standard RPG logic usually homes or goes to last known position.
        // Let's go towards current target position to ensure hit if in range.

        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 dest = target.transform.position;
            dest.y = transform.position.y;
            direction = (dest - transform.position).normalized;
        }

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Check distance/collision manually if collider not reliable or simple distance check
        // But we will use OnTriggerEnter with a Collider on the prefab
    }

    void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;

        // Prevent hitting self
        if (other.gameObject == owner.gameObject) return;

        Unit hitUnit = other.GetComponent<Unit>();
        if (hitUnit != null && hitUnit == target)
        {
            // Hit!
            hitUnit.TakeDamage(damage, knockback);
            Destroy(gameObject);
        }
    }
}
