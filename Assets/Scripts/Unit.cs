using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

public enum Team { Player, Enemy }
public enum UnitState { Idle, Move, Attack, Hit, Die }

public class Unit : MonoBehaviour
{
    // Base Stats (from UnitData)
    [Header("Base Stats")]
    public float baseMaxHealth;
    public float baseAtkEff;
    public float baseAtkSpeedAccel;
    public float baseMoveSpeed;
    public float baseDef;
    public float baseAtkInterval;
    public float baseRange;
    public float baseKnockback;
    public float baseMaxStamina;

    // Permanent Buffs (from Reward or Action Buffs)
    [Header("Permanent Buffs")]
    public float permAtkEffBonus = 0;
    public float permAtkSpeedAccelBonus = 0;
    public float permHPBonus = 0;
    public float permStaminaBonus = 0;

    // Current Effective Stats (Calculated)
    [Header("Effective Stats")]
    public float maxHealth;
    public float maxStamina;
    public float moveSpeed;
    public float finalAtkInterval;
    public float finalRange;
    public float finalAtkPower;
    public float finalDef;
    public float finalKnockback;

    public Team team;
    public UnitState state = UnitState.Idle;

    public float currentHealth;
    public float currentStamina;
    private float lastAttackTime = 0f;
    private float hitDuration = 0.1f;

    public Unit target;
    public bool isMovingScenario = false;

    private TextMeshPro healthText;
    private Renderer rend;
    private Color originalColor;
    private GameObject currentMaskObject;
    private Transform maskPosTransform;

    // Active Mask
    public MaskData equippedMask;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        CreateHealthText();
    }

    void CreateHealthText()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/HealthText");
        GameObject textObj;
        if (prefab != null)
        {
            textObj = Instantiate(prefab, transform);
            textObj.name = "HealthText";
            textObj.transform.localPosition = Vector3.up * 1.5f;
        }
        else
        {
            textObj = new GameObject("HealthText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = Vector3.up * 1.5f;
        }

        healthText = textObj.GetComponent<TextMeshPro>();
        if (healthText == null)
        {
            healthText = textObj.AddComponent<TextMeshPro>();
            healthText.fontSize = 5;
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.color = Color.white;
        }
    }

    public void InitializePlayer(UnitData data, List<MaskData> inventory, int equippedIndex)
    {
        team = Team.Player;
        if (data == null)
        {
            // Fallback
            baseMaxHealth = 100; baseAtkEff = 100; baseAtkSpeedAccel = 100;
            baseMoveSpeed = 2; baseDef = 0; baseRange = 1.5f; baseKnockback = 1;
        }
        else
        {
            baseMaxHealth = data.hp;
            baseAtkEff = data.atkEff;
            baseAtkSpeedAccel = data.atkSpeedAccel;
            baseMoveSpeed = data.moveSpeed;
            baseDef = data.def;
            baseRange = data.range;
            baseKnockback = data.knockback;
            baseMaxStamina = data.maxStamina;
        }

        if (equippedIndex >= 0 && equippedIndex < inventory.Count)
            equippedMask = inventory[equippedIndex];
        else
            equippedMask = null;

        RecalculateStats();

        if (currentHealth <= 0 || currentHealth > maxHealth)
            currentHealth = maxHealth;

        if (currentStamina <= 0 || currentStamina > maxStamina)
            currentStamina = maxStamina;

        if (rend != null && equippedMask != null)
        {
            originalColor = equippedMask.color;
            rend.material.color = originalColor;
        }
        else if (rend != null && data != null)
        {
            originalColor = data.color;
            rend.material.color = originalColor;
        }

        // Find MaskPos if not already found
        if (maskPosTransform == null)
        {
            maskPosTransform = transform.Find("MaskPos");
            if (maskPosTransform == null)
            {
                // Recursive search
                foreach (Transform t in GetComponentsInChildren<Transform>())
                {
                    if (t.name == "MaskPos")
                    {
                        maskPosTransform = t;
                        break;
                    }
                }
            }
        }

        UpdateMaskVisuals();
        UpdateVisuals();
    }

    public void InitializeMonster(UnitData data)
    {
        team = Team.Enemy;
        baseMaxHealth = data.hp;
        baseAtkEff = data.atkEff;
        baseAtkSpeedAccel = data.atkSpeedAccel;
        baseMoveSpeed = data.moveSpeed;
        baseDef = data.def;
        baseAtkInterval = data.atkInterval;
        baseRange = data.range;
        baseKnockback = data.knockback;
        baseMaxStamina = data.maxStamina;
        transform.localScale = Vector3.one * data.scale;

        equippedMask = null;
        RecalculateStats();
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        state = UnitState.Idle;

        if (rend != null)
        {
            originalColor = data.color;
            rend.material.color = originalColor;
        }
        UpdateVisuals();
    }

    public void RecalculateStats()
    {
        float passiveHP = 0;
        float passiveStamina = 0;
        float passiveDef = 0;
        float passiveMoveSpeed = 0;
        float passiveAtkEff = 0;
        float passiveAtkSpeedAccel = 0;
        float passiveRange = 0;

        if (team == Team.Player && GameManager.Instance != null)
        {
            foreach (var mask in GameManager.Instance.inventory)
            {
                passiveHP += mask.passiveHP;
                passiveStamina += mask.passiveStamina;
                passiveDef += mask.passiveDef;
                passiveMoveSpeed += mask.passiveSpeed;
                passiveAtkEff += mask.passiveAtkEff;
                passiveAtkSpeedAccel += mask.passiveAtkSpeedAccel;
                passiveRange += mask.passiveRange;
            }
        }

        // Apply Equipped Mask Overrides or Defaults
        float equipAtk = 10;
        float equipInterval = 1;
        float equipDefAdd = 0;
        float equipRange = baseRange;
        float equipKnockback = baseKnockback;

        if (equippedMask != null)
        {
            equipAtk = equippedMask.equipAtk;
            equipInterval = equippedMask.equipInterval;
            equipDefAdd = equippedMask.equipDef;
            equipRange = equippedMask.equipRange;
            equipKnockback = equippedMask.equipKnockback;
        }
        else if (team == Team.Enemy)
        {
            equipAtk = baseAtkEff;
            equipInterval = baseAtkInterval;
            equipRange = baseRange;
            equipKnockback = baseKnockback;
        }

        // Final Calculations based on formulas
        maxHealth = (baseMaxHealth + passiveHP + permHPBonus);
        maxStamina = (baseMaxStamina + passiveStamina + permStaminaBonus);
        moveSpeed = (baseMoveSpeed + passiveMoveSpeed);

        finalAtkPower = equipAtk * (baseAtkEff + passiveAtkEff + permAtkEffBonus) / 100f;

        float totalAccel = (baseAtkSpeedAccel + passiveAtkSpeedAccel + permAtkSpeedAccelBonus) / 100f;
        finalAtkInterval = equipInterval / Mathf.Max(0.1f, totalAccel);

        finalRange = equipRange + passiveRange;
        finalDef = (baseDef + passiveDef + equipDefAdd) / 100f; // As % reduction
        finalKnockback = equipKnockback;

        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (currentStamina > maxStamina) currentStamina = maxStamina;
    }

    public void ApplyMask(MaskData mask)
    {
        equippedMask = mask;
        RecalculateStats();
        if (team == Team.Player)
        {
            originalColor = equippedMask.color;
            rend.material.color = originalColor;
            UpdateMaskVisuals();
        }
    }

    private void UpdateMaskVisuals()
    {
        if (currentMaskObject != null)
        {
            Destroy(currentMaskObject);
            currentMaskObject = null;
        }

        if (equippedMask != null && !string.IsNullOrEmpty(equippedMask.prefabPath))
        {
            GameObject maskPrefab = Resources.Load<GameObject>(equippedMask.prefabPath);
            if (maskPrefab != null)
            {
                if (team == Team.Player && maskPosTransform != null)
                {
                    currentMaskObject = Instantiate(maskPrefab, maskPosTransform);
                    currentMaskObject.transform.localPosition = Vector3.zero;
                    currentMaskObject.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    currentMaskObject = Instantiate(maskPrefab, transform);
                    // Assume mask prefab is centered or properly offset.
                    currentMaskObject.transform.localPosition = Vector3.zero;
                }
            }
        }
    }

    public void ApplyStatReward(StatRewardData reward)
    {
        foreach (var effect in reward.effects)
        {
            switch (effect.Key)
            {
                case "Atk": permAtkEffBonus += effect.Value; break;
                case "Speed": permAtkSpeedAccelBonus += effect.Value; break;
                case "Hp": permHPBonus += effect.Value; break;
                case "Stamina": permStaminaBonus += effect.Value; break;
            }
        }
        RecalculateStats();
        UpdateVisuals();
    }

    private float staminaTimer = 0f;
    void Update()
    {
        if (state == UnitState.Die) return;

        // Stamina Recovery: 1 per 5 seconds (continuous)
        if (maxStamina > 0 && currentStamina < maxStamina)
        {
            currentStamina += Time.deltaTime / 5f;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }

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
        if (dist <= finalRange)
        {
            if (Time.time >= lastAttackTime + finalAtkInterval)
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

        if (team == Team.Player)
        {
            // Execute based on ActionType
            ActionType action = equippedMask != null ? equippedMask.actionType : ActionType.Attack;

            switch (action)
            {
                case ActionType.Attack:
                    SpawnProjectile();
                    break;
                case ActionType.Heal:
                    Heal(finalAtkPower * 2f);
                    break;
                case ActionType.AtkBuff:
                    permAtkEffBonus += 1f;
                    RecalculateStats();
                    ShowText("ATK UP!", Color.red);
                    break;
                case ActionType.SpeedBuff:
                    permAtkSpeedAccelBonus += 1f;
                    RecalculateStats();
                    ShowText("SPD UP!", Color.yellow);
                    break;
                case ActionType.HPBuff:
                    permHPBonus += 1f;
                    RecalculateStats();
                    ShowText("HP UP!", Color.green);
                    break;
            }
        }
        else
        {
            // Enemies always attack with projectile as per new spec
            SpawnProjectile();
        }

        await UniTask.Delay(100);
        if(state != UnitState.Hit && state != UnitState.Die)
            state = UnitState.Idle;
    }

    void SpawnProjectile()
    {
        if (target == null) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/Projectiles/Projectile");
        GameObject projObj;

        if (prefab != null)
        {
            projObj = Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
        else
        {
            projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projObj.transform.position = transform.position + Vector3.up * 0.5f;
            projObj.transform.localScale = Vector3.one * 0.3f;
        }

        // Color
        var r = projObj.GetComponent<Renderer>();
        if (r) r.material.color = originalColor;

        Projectile p = projObj.GetComponent<Projectile>();
        if (p == null) p = projObj.AddComponent<Projectile>();

        p.Initialize(this, target, finalAtkPower, finalKnockback, finalRange);
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateVisuals();
        ShowText($"+{amount:F0}", Color.green);
    }

    public void TakeDamage(float damage, float knockback)
    {
        if (state == UnitState.Die) return;

        // Formula: damage * (1 - defense)
        float reducedDamage = damage * (1f - finalDef);
        currentHealth -= reducedDamage;
        UpdateVisuals();
        ShowText($"-{reducedDamage:F0}", Color.red);

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

        Vector3 knockDir = (team == Team.Player) ? Vector3.left : Vector3.right;
        transform.DOMove(transform.position + knockDir * distance, hitDuration).SetEase(Ease.OutQuad);

        if (rend) rend.material.color = Color.white;

        await UniTask.Delay(System.TimeSpan.FromSeconds(hitDuration));

        if (state != UnitState.Die)
        {
            if (rend) rend.material.color = originalColor;
            state = UnitState.Idle;
        }
    }

    void Die()
    {
        state = UnitState.Die;
        if (rend) rend.material.DOFade(0, 0.5f).OnComplete(() => gameObject.SetActive(false));
        else gameObject.SetActive(false);
    }

    void ShowText(string msg, Color col)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/PopupText");
        GameObject txtObj;
        TextMeshPro tmp;

        if (prefab != null)
        {
            txtObj = Instantiate(prefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        }
        else
        {
            txtObj = new GameObject("PopupText");
            txtObj.transform.position = transform.position + Vector3.up * 1f;
        }

        tmp = txtObj.GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            tmp = txtObj.AddComponent<TextMeshPro>();
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        tmp.text = msg;
        tmp.color = col;

        txtObj.transform.DOMoveY(txtObj.transform.position.y + 2f, 1f);
        tmp.DOFade(0, 1f).OnComplete(() => Destroy(txtObj));
    }

    public void UpdateVisuals()
    {
        if(healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{Mathf.Ceil(maxHealth)}";
    }
}
