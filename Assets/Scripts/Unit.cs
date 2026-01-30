using UnityEngine;
using System.Collections;

public enum Team { Player, Enemy }

public class Unit : MonoBehaviour
{
    // Stats
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float attackSpeed = 1f; // seconds per attack
    public float attackRange = 1.5f;
    public float knockbackDist = 1f;
    public float attackPower = 10f;

    public Team team;

    // State
    public float currentHealth;
    public bool isStunned = false;
    private float lastAttackTime = 0f;

    public Unit target;
    public bool isMovingScenario = false; // For the initial 5s dash

    private TextMesh healthText;
    private Renderer rend;
    private Color originalColor;

    void Awake()
    {
        currentHealth = maxHealth;
        rend = GetComponent<Renderer>();

        // Add Health Text
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.up * 1.5f;
        healthText = textObj.AddComponent<TextMesh>();
        healthText.characterSize = 0.2f;
        healthText.anchor = TextAnchor.MiddleCenter;
        healthText.fontSize = 20;
        healthText.color = Color.white;
    }

    public void Initialize(Team _team)
    {
        team = _team;
        if (rend != null)
        {
            originalColor = (team == Team.Player) ? Color.green : Color.red;
            rend.material.color = originalColor;
        }
        currentHealth = maxHealth;
        UpdateVisuals();
    }

    void Update()
    {
        if (isStunned) return;

        if (isMovingScenario)
        {
            // Move right
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }
        else if (target != null && target.gameObject.activeInHierarchy)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= attackRange)
            {
                // Attack
                if (Time.time >= lastAttackTime + attackSpeed)
                {
                    StartCoroutine(AttackRoutine());
                }
            }
            else
            {
                // Move towards target
                Vector3 dir = (target.transform.position - transform.position).normalized;
                dir.y = 0; // Lock Y
                transform.Translate(dir * moveSpeed * Time.deltaTime);
            }
        }

        UpdateVisuals();
    }

    IEnumerator AttackRoutine()
    {
        lastAttackTime = Time.time;

        // Simple visual punch
        Vector3 originalPos = transform.position;
        Vector3 punchPos = transform.position + (target.transform.position - transform.position).normalized * 0.5f;

        float punchDuration = 0.1f;
        float elapsed = 0;

        while(elapsed < punchDuration)
        {
            transform.position = Vector3.Lerp(originalPos, punchPos, elapsed / punchDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        // Apply Damage if target is still valid
        if (target != null && target.gameObject.activeInHierarchy)
        {
            // Check distance again? Or just hit. Spec says "When in range, attack".
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= attackRange + 0.5f) // forgiveness
            {
                target.TakeDamage(attackPower, knockbackDist);
            }
        }
    }

    public void TakeDamage(float damage, float knockback)
    {
        currentHealth -= damage;
        UpdateVisuals();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            StartCoroutine(KnockbackRoutine(knockback));
        }
    }

    IEnumerator KnockbackRoutine(float distance)
    {
        isStunned = true;

        // Knockback direction is opposite to facing.
        // Assuming side scroller: Player gets knocked Left, Enemy gets knocked Right.
        Vector3 knockDir = (team == Team.Player) ? Vector3.left : Vector3.right;

        float duration = 0.1f;
        float elapsed = 0;
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + knockDir * distance;

        while(elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        isStunned = false;
    }

    void Die()
    {
        gameObject.SetActive(false);
    }

    void UpdateVisuals()
    {
        if(healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
    }
}
