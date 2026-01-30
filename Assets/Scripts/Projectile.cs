using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public float knockback;
    public Unit target;
    public Unit owner;
    public float collisionRadius = 0.5f; // Radius for collision detection

    private Vector3 direction;
    private Vector3 initialDirection; // Store initial direction to prevent backward movement
    private bool initialized = false;
    private Rigidbody rb;
    private float lastCollisionCheckTime = 0f;
    private const float collisionCheckInterval = 0.05f; // Check collision every 0.05 seconds

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

        // Find all Units in the scene
        Unit[] allUnits = GameObject.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in allUnits)
        {
            // Skip if it's the owner
            if (unit == owner) continue;
            
            // Skip if same team
            if (unit.team == owner.team) continue;
            
            // Skip if unit is dead or inactive
            if (unit.state == UnitState.Die || !unit.gameObject.activeInHierarchy) continue;

            // Calculate distance on X axis (primary) and Z axis, ignoring Y for 2D-style gameplay
            float projectileX = transform.position.x;
            float projectileZ = transform.position.z;
            float unitX = unit.transform.position.x;
            float unitZ = unit.transform.position.z;
            
            // Get unit's scale as radius approximation
            float unitRadius = unit.transform.localScale.x * 0.5f;
            float totalRadius = collisionRadius + unitRadius;
            
            // Check collision using X and Z position with combined radius
            float distanceX = Mathf.Abs(projectileX - unitX);
            float distanceZ = Mathf.Abs(projectileZ - unitZ);
            
            // Use 2D distance for more accurate collision detection on the XZ plane
            float distance2D = Mathf.Sqrt(distanceX * distanceX + distanceZ * distanceZ);
            
            if (distance2D <= totalRadius)
            {
                Debug.Log($"Projectile Hit {unit.name} for {damage}");
                unit.TakeDamage(damage, knockback);
                Destroy(gameObject);
                return; // Exit after hitting one unit
            }
        }
    }
}
