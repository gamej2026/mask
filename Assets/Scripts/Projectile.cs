using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public float knockback;
    public float maxDistance;
    private float distanceTraveled = 0f;
    public Unit target;
    public Unit owner;
    public float collisionRadius = 0.5f; // Radius for collision detection

    private Vector3 direction;
    private Vector3 initialDirection; // Store initial direction to prevent backward movement
    private bool initialized = false;
    private Rigidbody rb;
    private float lastCollisionCheckTime = 0f;
    private const float collisionCheckInterval = 0.05f; // Check collision every 0.05 seconds

    public void Initialize(Unit _owner, Unit _target, float _damage, float _knockback, float _maxDistance)
    {
        owner = _owner;
        target = _target;
        damage = _damage;
        knockback = _knockback;
        maxDistance = _maxDistance;
        rb = GetComponent<Rigidbody>();

        if (target != null)
        {
            direction = (target.transform.position - transform.position).normalized;
            direction.z = 0; // side-scroller (XY plane)
        }
        else
        {
            direction = transform.forward;
        }

        // Store initial direction to prevent backward movement
        initialDirection = direction;

        Destroy(gameObject, 3f);
        initialized = true;
    }

    void FixedUpdate()
    {
        if (!initialized) return;

        // Don't update direction dynamically - use initial direction to prevent backward movement
        // Only keep the initial direction set during Initialize

        float moveStep = speed * Time.fixedDeltaTime;
        distanceTraveled += moveStep;

        if (rb != null)
        {
            Vector3 nextPos = rb.position + initialDirection * moveStep;
            rb.MovePosition(nextPos);
        }
        else
        {
            transform.Translate(initialDirection * moveStep, Space.World);
        }

        if (distanceTraveled >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        // Check collision with enemies (throttled for performance)
        if (Time.time - lastCollisionCheckTime >= collisionCheckInterval)
        {
            lastCollisionCheckTime = Time.time;
            CheckCollisionWithEnemies();
        }
    }

    void CheckCollisionWithEnemies()
    {
        if (owner == null) return;

        Unit[] allUnits = GameObject.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in allUnits)
        {
            if (unit == owner) continue;
            if (unit.team == owner.team) continue;
            if (unit.state == UnitState.Die || !unit.gameObject.activeInHierarchy) continue;

            // Use 3D distance or 2D (XY) distance for side-scroller
            float dist = Vector3.Distance(transform.position, unit.transform.position);
            
            float unitRadius = unit.transform.localScale.x * 0.5f;
            float totalRadius = collisionRadius + unitRadius;
            
            if (dist <= totalRadius)
            {
                Debug.Log($"Projectile Hit {unit.name} for {damage}");
                unit.TakeDamage(damage, knockback);
                Destroy(gameObject);
                return; // Exit after hitting one unit
            }
        }
    }
}
