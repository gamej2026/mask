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
    public float baseAttackSpeed = 1f;
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
    private float stunDuration = 0.5f;

    public Unit target;
    public bool isMovingScenario = false;

    private TextMeshPro healthText;
    private Renderer rend;
    private Color originalColor;

    // Mask Logic
    public MaskData currentMask;

    // Buff Logic
    private int buffStacks = 0;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        CreateHealthText();
    }

    void CreateHealthText()
    {
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.up * 1.5f;
        healthText = textObj.AddComponent<TextMeshPro>();
        healthText.fontSize = 5;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;
    }

    // Initialize for Player
    public void InitializePlayer(MaskData mask)
    {
        team = Team.Player;
        currentMask = mask;
        buffStacks = 0;

        RecalculateStats();
        currentHealth = maxHealth;
        state = UnitState.Idle;

        if (rend != null)
        {
            originalColor = currentMask.color;
            rend.material.color = originalColor;
        }
        UpdateVisuals();
    }

    // Initialize for Monster
    public void InitializeMonster(MonsterData data)
    {
        team = Team.Enemy;
        currentMask = null;

        baseMaxHealth = data.hp;
        baseAttackPower = data.atk;
        baseMoveSpeed = data.speed;
        baseAttackRange = data.range;
        baseKnockbackDist = data.knockback;
        transform.localScale = Vector3.one * data.scale;

        RecalculateStats(); // Basically just sets base to current
        currentHealth = maxHealth;
        state = UnitState.Idle;

        if (rend != null)
        {
            originalColor = data.color;
            rend.material.color = originalColor;
        }
        
        // Adjust collider height to ensure projectiles can hit regardless of scale
        // Projectiles fly at 0.5 units above ground, so collider must extend to that height
        AdjustColliderForProjectileHit();
        
        UpdateVisuals();
    }
    
    void AdjustColliderForProjectileHit()
    {
        // Projectile flight height is 0.5 units above ground (transform.position.y)
        float projectileHeight = 0.5f;
        
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            // Calculate current collider height based on scale
            float currentColliderHeight = boxCollider.size.y * transform.localScale.y;
            
            // Calculate minimum collider height needed (projectileHeight * 2 to cover from center)
            float minColliderHeight = projectileHeight * 2f;
            
            // If current collider is shorter than needed, extend it
            if (currentColliderHeight < minColliderHeight)
            {
                // Adjust collider size to reach projectile height
                Vector3 newSize = boxCollider.size;
                newSize.y = minColliderHeight / transform.localScale.y;
                boxCollider.size = newSize;
                
                // Adjust collider center to keep bottom at ground level
                Vector3 newCenter = boxCollider.center;
                newCenter.y = projectileHeight / transform.localScale.y;
                boxCollider.center = newCenter;
            }
        }
    }

    // Fallback/Legacy Initialize
    public void Initialize(Team _team, MaskData mask = null)
    {
        if (_team == Team.Player) InitializePlayer(mask ?? GameData.allMasks[0]);
        else InitializeMonster(new MonsterData { hp=100, atk=10, speed=2, range=1.5f, knockback=1, scale=1, color=Color.red });
    }

    public void RecalculateStats()
    {
        float hpBonus = 0;
        float moveSpeedBonus = 0;
        float atkSpeedBonus = 0;
        float rangeBonus = 0;
        float atkBonus = 0;

        if (currentMask != null)
        {
            hpBonus = currentMask.hpBonus;
            moveSpeedBonus = currentMask.moveSpeedBonus;
            atkSpeedBonus = currentMask.atkSpeedBonus;
            rangeBonus = currentMask.rangeBonus;
            atkBonus = currentMask.atkBonus;
        }

        // Apply Buffs (1 Stack = +1 Attack)
        atkBonus += buffStacks;

        maxHealth = baseMaxHealth + hpBonus;
        moveSpeed = baseMoveSpeed + moveSpeedBonus;

        attackSpeed = baseAttackSpeed + atkSpeedBonus;
        if (attackSpeed < 0.1f) attackSpeed = 0.1f;

        attackRange = baseAttackRange + rangeBonus;
        attackPower = baseAttackPower + atkBonus;
        knockbackDist = baseKnockbackDist;

        if (currentMask != null && currentMask.skill == SkillType.KnockbackBoost)
        {
            knockbackDist += 1.0f;
        }
    }

    public void ApplyMask(MaskData newMask)
    {
        currentMask = newMask;
        buffStacks = 0; // Reset buffs on switch? Usually yes.
        RecalculateStats();

        if (team == Team.Player)
        {
            originalColor = currentMask.color;
            rend.material.color = originalColor;
        }
    }

    public void ApplyStatBoost(string stat, float value)
    {
        // Value is percentage (e.g. 20 means +20% base)
        // Or we can modify base stats permanently as per previous logic.
        // Prompt said: "Stat Upgrade... Atk+20%, HP-10%"
        // Implementation: Modify Base Stats.

        float multiplier = 1f + (value / 100f);

        if (stat == "HP")
        {
            baseMaxHealth *= multiplier;
            currentHealth *= multiplier; // Adjust current HP too
        }
        else if (stat == "ATK")
        {
            baseAttackPower *= multiplier;
        }
        else if (stat == "ASP" || stat == "SPD") // Attack Speed (Acceleration logic in doc: 100 accel -> higher speed)
        {
            // Doc: Attack Speed Acceleration.
            // Current implementation uses 'delay' (lower is faster).
            // If value is +20 (Speed Up), delay should decrease.
            // Simple: baseAttackSpeed /= multiplier;
            baseAttackSpeed /= multiplier;
        }

        RecalculateStats();
        UpdateVisuals();
    }

    void Update()
    {
        if (state == UnitState.Die) return;
        if (state == UnitState.Hit) return;

        LogicLoop();
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

        // Attack Animation (Punch)
        // For Player (Projectile), maybe punch isn't needed, but keeps it responsive.
        await transform.DOPunchPosition(punchDir * 0.2f, 0.1f, 10, 1).AsyncWaitForCompletion();

        if (team == Team.Player)
        {
            SpawnProjectile();

            // Mask Action Effects (Triggered on Attack)
            if (currentMask != null)
            {
                if (currentMask.actionType == ActionType.Heal)
                {
                    Heal(attackPower * 2f);
                }
                else if (currentMask.actionType == ActionType.Buff)
                {
                    AddBuff();
                }
            }

            // Double Strike Skill (fires second projectile)
            if (currentMask != null && currentMask.skill == SkillType.DoubleStrike)
            {
                await UniTask.Delay(100);
                SpawnProjectile(0.5f); // 50% damage
            }
        }
        else // Enemy (Melee / HitScan)
        {
            if (target != null && target.state != UnitState.Die)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (dist <= attackRange + 0.5f)
                {
                    target.TakeDamage(attackPower, knockbackDist);
                }
            }
        }

        if(state != UnitState.Hit && state != UnitState.Die)
            state = UnitState.Idle;
    }

    void SpawnProjectile(float damageMultiplier = 1.0f)
    {
        if (target == null) return;

        GameObject projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projObj.name = "Projectile";
        projObj.transform.position = transform.position + Vector3.up * 0.5f; // Center
        projObj.transform.localScale = Vector3.one * 0.5f;

        // Physics Setup
        var col = projObj.GetComponent<Collider>();
        if(col) col.isTrigger = true;

        var rb = projObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Color
        var rend = projObj.GetComponent<Renderer>();
        rend.material.color = currentMask != null ? currentMask.color : Color.yellow;

        Projectile p = projObj.AddComponent<Projectile>();
        p.Initialize(this, target, attackPower * damageMultiplier, knockbackDist);
    }

    void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateVisuals();
        ShowText($"+{amount:F0}", Color.green);
    }

    void AddBuff()
    {
        buffStacks++;
        RecalculateStats();
        ShowText("BUFF!", Color.cyan);
    }

    public void TakeDamage(float damage, float knockback)
    {
        if (state == UnitState.Die) return;

        currentHealth -= damage;
        UpdateVisuals();
        ShowText($"-{damage:F0}", Color.red);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            HitRoutine(knockback).Forget();
        }
    }

    async UniTaskVoid HitRoutine(float distance)
    {
        state = UnitState.Hit;

        // Direction away from attacker? Or Fixed?
        // Simple: Player knocked left, Enemy knocked right.
        Vector3 knockDir = (team == Team.Player) ? Vector3.left : Vector3.right;

        transform.DOMove(transform.position + knockDir * distance, 0.2f).SetEase(Ease.OutBack);
        rend.material.color = Color.white;

        await UniTask.Delay(System.TimeSpan.FromSeconds(stunDuration));

        if (state != UnitState.Die)
        {
            rend.material.color = originalColor;
            state = UnitState.Idle;
        }
    }

    void Die()
    {
        state = UnitState.Die;
        rend.material.DOFade(0, 0.5f).OnComplete(() => gameObject.SetActive(false));
    }

    void ShowText(string msg, Color col)
    {
        GameObject txtObj = new GameObject("PopupText");
        txtObj.transform.position = transform.position + Vector3.up * 1f;
        var tmp = txtObj.AddComponent<TextMeshPro>();
        tmp.text = msg;
        tmp.fontSize = 4;
        tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Center;

        txtObj.transform.DOMoveY(txtObj.transform.position.y + 2f, 1f);
        tmp.DOFade(0, 1f).OnComplete(() => Destroy(txtObj));
    }

    void UpdateVisuals()
    {
        if(healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
    }
}
