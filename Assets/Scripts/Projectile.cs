using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public float knockback;
    public Unit target;
    public Unit owner;

    private Vector3 direction;
    private bool initialized = false;
    private Rigidbody rb;

    public void Initialize(Unit _owner, Unit _target, float _damage, float _knockback)
    {
        owner = _owner;
        target = _target;
        damage = _damage;
        knockback = _knockback;
        rb = GetComponent<Rigidbody>();

        if (target != null)
        {
            direction = (target.transform.position - transform.position).normalized;
            direction.y = 0;
        }
        else
        {
            direction = transform.forward;
        }

        Destroy(gameObject, 3f);
        initialized = true;
    }

    void FixedUpdate()
    {
        if (!initialized) return;

        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 dest = target.transform.position;
            dest.y = transform.position.y;
            direction = (dest - transform.position).normalized;
        }

        if (rb != null)
        {
            Vector3 nextPos = rb.position + direction * speed * Time.fixedDeltaTime;
            rb.MovePosition(nextPos);
        }
        else
        {
            // Fallback
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;
        if (owner != null && other.gameObject == owner.gameObject) return;

        Unit hitUnit = other.GetComponent<Unit>();
        if (hitUnit != null)
        {
            // Check team
            if (owner != null && hitUnit.team != owner.team)
            {
                Debug.Log($"Projectile Hit {hitUnit.name} for {damage}");
                hitUnit.TakeDamage(damage, knockback);
                Destroy(gameObject);
            }
        }
    }
}
