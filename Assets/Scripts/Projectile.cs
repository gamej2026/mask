using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public float knockback;
    public Unit target;
    public Unit owner;

    private Vector3 initialDirection; // Store initial direction to prevent backward movement
    private bool initialized = false;
    private Rigidbody rb;
    private bool hasHit = false; // Prevent multiple hits

    public void Initialize(Unit _owner, Unit _target, float _damage, float _knockback)
    {
        owner = _owner;
        target = _target;
        damage = _damage;
        knockback = _knockback;
        rb = GetComponent<Rigidbody>();

        if (target != null)
        {
            initialDirection = (target.transform.position - transform.position).normalized;
            initialDirection.y = 0;
        }
        else
        {
            initialDirection = transform.forward;
        }

        Destroy(gameObject, 3f);
        initialized = true;
    }

    void FixedUpdate()
    {
        if (!initialized || hasHit) return;

        if (rb != null)
        {
            Vector3 nextPos = rb.position + initialDirection * speed * Time.fixedDeltaTime;
            rb.MovePosition(nextPos);
        }
        else
        {
            // Fallback
            transform.Translate(initialDirection * speed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit || !initialized || owner == null) return;

        // Try to get Unit component from the collided object
        Unit unit = other.GetComponent<Unit>();
        if (unit == null) return;

        // Skip if it's the owner
        if (unit == owner) return;

        // Skip if same team
        if (unit.team == owner.team) return;

        // Skip if unit is dead or inactive
        if (unit.state == UnitState.Die || !unit.gameObject.activeInHierarchy) return;

        // Hit the unit
        hasHit = true;
        Debug.Log($"Projectile Hit {unit.name} for {damage}");
        unit.TakeDamage(damage, knockback);
        Destroy(gameObject);
    }
}
