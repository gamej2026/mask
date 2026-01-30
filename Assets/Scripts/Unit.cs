using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

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
    public bool isMovingScenario = false; // Controlled by GameManager via DOTween

    private TextMeshPro healthText;
    private Renderer rend;
    private Color originalColor;

    void Awake()
    {
        currentHealth = maxHealth;
        rend = GetComponent<Renderer>();

        // Add Health Text (TMP)
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.up * 1.5f;
        healthText = textObj.AddComponent<TextMeshPro>();
        healthText.fontSize = 5; // TMP world space size
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;
        // healthText.font = Resources.Load<TMP_FontAsset>("...");
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
            // Handled by DOTween in GameManager
        }
        else if (target != null && target.gameObject.activeInHierarchy)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= attackRange)
            {
                // Attack
                if (Time.time >= lastAttackTime + attackSpeed)
                {
                    AttackRoutine().Forget();
                }
            }
            else
            {
                // Move towards target
                // We use simple translate here for continuous following, DOTween might be choppy if called every frame unless careful.
                // Or we can use DOTween with DOKill.
                Vector3 dir = (target.transform.position - transform.position).normalized;
                dir.y = 0;
                transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
            }
        }

        UpdateVisuals();
    }

    async UniTaskVoid AttackRoutine()
    {
        lastAttackTime = Time.time;

        // DOTween Punch
        Vector3 punchDir = (target.transform.position - transform.position).normalized;
        await transform.DOPunchPosition(punchDir * 0.5f, 0.2f, 10, 1).AsyncWaitForCompletion();

        // Apply Damage if target is still valid
        if (target != null && target.gameObject.activeInHierarchy)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= attackRange + 0.5f)
            {
                target.TakeDamage(attackPower, knockbackDist);
            }
        }
    }

    public void TakeDamage(float damage, float knockback)
    {
        currentHealth -= damage;
        UpdateVisuals();

        // Damage Text Effect (Pop up)
        ShowDamageText(damage);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            KnockbackRoutine(knockback).Forget();
        }
    }

    void ShowDamageText(float damage)
    {
        // Simple floating text
        GameObject dmgObj = new GameObject("DmgText");
        dmgObj.transform.position = transform.position + Vector3.up * 1f;
        var tmp = dmgObj.AddComponent<TextMeshPro>();
        tmp.text = $"-{damage}";
        tmp.fontSize = 4;
        tmp.color = Color.red;
        tmp.alignment = TextAlignmentOptions.Center;

        // Float up and fade
        dmgObj.transform.DOMoveY(dmgObj.transform.position.y + 2f, 1f);
        tmp.DOFade(0, 1f).OnComplete(() => Destroy(dmgObj));
    }

    async UniTaskVoid KnockbackRoutine(float distance)
    {
        isStunned = true;

        Vector3 knockDir = (team == Team.Player) ? Vector3.left : Vector3.right;

        // DOTween Knockback
        await transform.DOMove(transform.position + knockDir * distance, 0.2f).SetEase(Ease.OutBack).AsyncWaitForCompletion();

        isStunned = false;
    }

    void Die()
    {
        // Fade out
        rend.material.DOFade(0, 0.5f).OnComplete(() => gameObject.SetActive(false));
    }

    void UpdateVisuals()
    {
        if(healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
    }
}
