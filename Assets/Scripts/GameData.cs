using UnityEngine;
using System.Collections.Generic;

public enum SkillType
{
    None,
    DoubleStrike,
    Heal,
    KnockbackBoost
}

public enum RewardType
{
    NewMask,
    UpgradeMask,
    StatBoost
}

[System.Serializable]
public class MaskData
{
    public string id;
    public string name;
    public string description;

    // Base Stats
    public float hpBonus;
    public float atkBonus;
    public float atkSpeedBonus; // Multiplier? Or flat? Let's assume flat addition or percentage. Let's use multipliers for simplicity where 1.0 is base.
    public float moveSpeedBonus;
    public float rangeBonus;

    public SkillType skill;
    public Color color; // Visual representation since we lack sprites

    public MaskData Copy()
    {
        return (MaskData)this.MemberwiseClone();
    }
}

public static class MaskDatabase
{
    public static List<MaskData> allMasks = new List<MaskData>();

    static MaskDatabase()
    {
        // 1. Default Mask (Steampunk Basic)
        allMasks.Add(new MaskData
        {
            id = "mask_default",
            name = "Iron Mask",
            description = "A standard steampunk mask.",
            hpBonus = 0,
            atkBonus = 0,
            atkSpeedBonus = 0,
            moveSpeedBonus = 0,
            rangeBonus = 0,
            skill = SkillType.None,
            color = new Color(0.6f, 0.6f, 0.6f) // Grey
        });

        // 2. Cat Mask (Speed)
        allMasks.Add(new MaskData
        {
            id = "mask_cat",
            name = "Cat Mask",
            description = "Increases attack speed and movement.",
            hpBonus = -10f,
            atkBonus = -2f,
            atkSpeedBonus = -0.3f, // Lower is faster (delay)
            moveSpeedBonus = 2f,
            rangeBonus = -0.2f,
            skill = SkillType.DoubleStrike,
            color = new Color(0.1f, 0.1f, 0.1f) // Black
        });

        // 3. Half Mask (Power)
        allMasks.Add(new MaskData
        {
            id = "mask_half",
            name = "Half Mask",
            description = "High damage but low defense.",
            hpBonus = -30f,
            atkBonus = 15f,
            atkSpeedBonus = 0.2f, // Slower
            moveSpeedBonus = 0f,
            rangeBonus = 0.5f,
            skill = SkillType.KnockbackBoost,
            color = new Color(0.8f, 0.6f, 0.2f) // Gold/Bronze
        });
    }

    public static MaskData GetMask(string id)
    {
        return allMasks.Find(m => m.id == id);
    }

    public static MaskData GetRandomMask()
    {
        return allMasks[Random.Range(0, allMasks.Count)];
    }
}
