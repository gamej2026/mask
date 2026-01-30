using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

public enum Team { Player, Enemy }
public enum UnitState { Idle, Move, Attack, Hit, Die }

public class Unit : MonoBehaviour
{
    // Base Stats
    public float baseMaxHealth = 100f;
    public float baseMoveSpeed = 5f;
    public float baseAttackSpeed = 1f; // delay between attacks
    public float baseAttackRange = 1.5f;
    public float baseKnockbackDist = 1f;
    public float baseAttackPower = 10f;

    // Current Effective Stats
    public float maxHealth;
    public float moveSpeed;
    public float attackSpeed;
    public float attackRange;
    public float knockbackDist;
    public float attackPower;

    public Team team;
    public UnitState state = UnitState.Idle;

    public float currentHealth;
    private float lastAttackTime = 0f;
    private float stunDuration = 0.5f; // Default hit stun

    public Unit target;
    public bool isMovingScenario = false; // Controlled by GameManager via DOTween

    private TextMeshPro healthText;
    private Renderer rend;
    private Color originalColor;

    // Mask Logic
    public MaskData currentMask;

    void Awake()
    {
        rend = GetComponent<Renderer>();

        // Add Health Text (TMP)
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.up * 1.5f;
        healthText = textObj.AddComponent<TextMeshPro>();
        healthText.fontSize = 5;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;
    }

    public void Initialize(Team _team, MaskData mask = null)
    {
        team = _team;

        // Set Default Mask if none
        if (mask == null)
            currentMask = MaskDatabase.allMasks[0]; // Default
        else
            currentMask = mask;

        RecalculateStats();

        currentHealth = maxHealth;
        state = UnitState.Idle;

        if (rend != null)
        {
            // Use mask color for player, Red for enemy
            if (team == Team.Player)
                originalColor = currentMask.color;
            else
                originalColor = Color.red;

            rend.material.color = originalColor;
        }

        UpdateVisuals();
    }

    public void RecalculateStats()
    {
        // Start with base
        maxHealth = baseMaxHealth + currentMask.hpBonus;
        moveSpeed = baseMoveSpeed + currentMask.moveSpeedBonus;
        attackSpeed = baseAttackSpeed + currentMask.atkSpeedBonus;
        if (attackSpeed < 0.1f) attackSpeed = 0.1f; // Cap speed

        attackRange = baseAttackRange + currentMask.rangeBonus;
        attackPower = baseAttackPower + currentMask.atkBonus;
        knockbackDist = baseKnockbackDist; // Could be modified by mask too

        // Skill modifiers
        if (currentMask.skill == SkillType.KnockbackBoost)
        {
            knockbackDist += 1.0f;
        }
    }

    public void ApplyMask(MaskData newMask)
    {
        currentMask = newMask;
        RecalculateStats();

        // Update visual color
        if (team == Team.Player)
        {
            originalColor = currentMask.color;
            rend.material.color = originalColor;
        }
    }

    public void ApplyStatBoost()
    {
        // Simple permanent boost to base stats
        baseAttackPower += 2f;
        baseMaxHealth += 10f;
        RecalculateStats();
        currentHealth += 10f; // Heal a bit
        UpdateVisuals();
    }

    void Update()
    {
        if (state == UnitState.Die) return;

        // FSM
        switch (state)
        {
            case UnitState.Hit:
                // Do nothing, waiting for stun to end
                break;

            case UnitState.Attack:
                // Managed by Attack coroutine mostly
                // If animation was here, we'd wait.
                // Currently Attack is instant + cooldown, so we go back to Idle/Move immediately?
                // Let's keep logic simple: if cooldown ready and in range -> Attack.
                break;

            case UnitState.Move:
            case UnitState.Idle:
                LogicLoop();
                break;
        }
    }

    void LogicLoop()
    {
        if (isMovingScenario)
        {
            state = UnitState.Move;
            return;
        }

        if (target == null || !target.gameObject.activeInHierarchy || target.state == UnitState.Die)
        {
            state = UnitState.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackSpeed)
            {
                state = UnitState.Attack;
                AttackRoutine().Forget();
            }
            else
            {
                // Waiting for cooldown, stay idle (don't move)
                state = UnitState.Idle;
            }
        }
        else
        {
            state = UnitState.Move;
            MoveTowardsTarget();
        }
    }

    void MoveTowardsTarget()
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        dir.y = 0;
        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
    }

    async UniTaskVoid AttackRoutine()
    {
        lastAttackTime = Time.time;

        // Visual
        Vector3 punchDir = (target.transform.position - transform.position).normalized;
        await transform.DOPunchPosition(punchDir * 0.5f, 0.2f, 10, 1).AsyncWaitForCompletion();

        // Check range again before damage
        if (target != null && target.state != UnitState.Die)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= attackRange + 0.5f)
            {
                target.TakeDamage(attackPower, knockbackDist);

                // Double Strike Skill
                if (currentMask.skill == SkillType.DoubleStrike)
                {
                    await UniTask.Delay(100);
                    if(target != null && target.state != UnitState.Die)
                        target.TakeDamage(attackPower * 0.5f, 0); // Mini hit
                }
            }
        }

        // Return to Idle to re-evaluate
        if(state != UnitState.Hit && state != UnitState.Die)
            state = UnitState.Idle;
    }

    public void TakeDamage(float damage, float knockback)
    {
        if (state == UnitState.Die) return;

        currentHealth -= damage;
        UpdateVisuals();

        ShowDamageText(damage);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            // Enter Hit/Stun state
            HitRoutine(knockback).Forget();
        }
    }

    async UniTaskVoid HitRoutine(float distance)
    {
        state = UnitState.Hit;

        Vector3 knockDir = (team == Team.Player) ? Vector3.left : Vector3.right;

        // Knockback
        transform.DOMove(transform.position + knockDir * distance, 0.2f).SetEase(Ease.OutBack);

        // Stiff color
        rend.material.color = Color.white;

        await UniTask.Delay(System.TimeSpan.FromSeconds(stunDuration)); // Stun duration

        if (state != UnitState.Die)
        {
            rend.material.color = originalColor;
            state = UnitState.Idle;
        }
    }

    void Die()
    {
        state = UnitState.Die;
        // Fade out
        rend.material.DOFade(0, 0.5f).OnComplete(() => gameObject.SetActive(false));
    }

    void ShowDamageText(float damage)
    {
        GameObject dmgObj = new GameObject("DmgText");
        dmgObj.transform.position = transform.position + Vector3.up * 1f;
        var tmp = dmgObj.AddComponent<TextMeshPro>();
        tmp.text = $"-{damage:F0}";
        tmp.fontSize = 4;
        tmp.color = Color.red;
        tmp.alignment = TextAlignmentOptions.Center;

        dmgObj.transform.DOMoveY(dmgObj.transform.position.y + 2f, 1f);
        tmp.DOFade(0, 1f).OnComplete(() => Destroy(dmgObj));
    }

    void UpdateVisuals()
    {
        if(healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
    }
}
