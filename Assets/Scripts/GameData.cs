using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum ActionType
{
    Attack,
    Heal,
    AtkBuff,
    SpeedBuff,
    HPBuff
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
    public ActionType actionType;
    public int level = 1;

    // Passive Stats (Inventory-wide)
    public float passiveHP;
    public float passiveDef;
    public float passiveSpeed; // Changed from passiveMoveSpeed to match Unit.cs
    public float passiveAtkEff;
    public float passiveAtkSpeedAccel;
    public float passiveRange;

    // Equip Stats (Only when active)
    public float equipAtk;
    public float equipInterval; // Changed from equipAtkInterval to match Unit.cs
    public float equipDef;
    public float equipRange;
    public float equipKnockback;

    public Color color;
    public string prefabPath;

    public MaskData Copy()
    {
        return (MaskData)this.MemberwiseClone();
    }
}

[System.Serializable]
public class UnitData
{
    public string id;
    public string name;
    public float hp;
    public float atkEff;
    public float atkSpeedAccel;
    public float moveSpeed;
    public float def;
    public float atkInterval;
    public float range;
    public float knockback;
    public float scale;
    public Color color;
    public string prefabPath;
}

[System.Serializable]
public class StageData
{
    public int stageId;
    public List<string> monsterIds = new List<string>();
}

[System.Serializable]
public class StatRewardData
{
    public string id;
    public string name;
    // Dictionary of Stat Code -> Value
    public Dictionary<string, float> effects = new Dictionary<string, float>();
}

public static class GameData
{
    public static List<MaskData> allMasks = new List<MaskData>();
    public static List<UnitData> allUnits = new List<UnitData>();
    public static List<StageData> allStages = new List<StageData>();
    public static List<StatRewardData> allStatRewards = new List<StatRewardData>();

    public static void Initialize()
    {
        LoadMasks();
        LoadUnits();
        LoadStages();
        LoadStatRewards();
    }

    private static void LoadMasks()
    {
        allMasks.Clear();
        List<Dictionary<string, object>> data = CSVReader.Read("Data/MaskData");

        foreach (var row in data)
        {
            MaskData m = new MaskData();
            m.id = row["ID"].ToString();
            m.name = row["Name"].ToString();
            m.description = row["Desc"].ToString();
            m.actionType = (ActionType)System.Enum.Parse(typeof(ActionType), row["ActionType"].ToString());

            m.passiveHP = float.Parse(row["PassiveHP"].ToString());
            m.passiveDef = float.Parse(row["PassiveDef"].ToString());
            m.passiveSpeed = float.Parse(row["PassiveMoveSpeed"].ToString());
            m.passiveAtkEff = float.Parse(row["PassiveAtkEff"].ToString());
            m.passiveAtkSpeedAccel = float.Parse(row["PassiveAtkSpeedAccel"].ToString());
            m.passiveRange = float.Parse(row["PassiveRange"].ToString());

            m.equipAtk = float.Parse(row["EquipAtk"].ToString());
            m.equipInterval = float.Parse(row["EquipAtkInterval"].ToString());
            m.equipDef = float.Parse(row["EquipDef"].ToString());
            m.equipRange = float.Parse(row["EquipRange"].ToString());
            m.equipKnockback = float.Parse(row["EquipKnockback"].ToString());

            ColorUtility.TryParseHtmlString("#" + row["ColorHex"].ToString(), out m.color);
            m.color.a = 1f;

            if (row.ContainsKey("PrefabPath"))
                m.prefabPath = row["PrefabPath"].ToString();

            allMasks.Add(m);
        }
    }

    private static void LoadUnits()
    {
        allUnits.Clear();
        List<Dictionary<string, object>> data = CSVReader.Read("Data/UnitData");

        foreach (var row in data)
        {
            UnitData u = new UnitData();
            u.id = row["ID"].ToString();
            u.name = row["Name"].ToString();
            u.hp = float.Parse(row["HP"].ToString());
            u.atkEff = float.Parse(row["AtkEff"].ToString());
            u.atkSpeedAccel = float.Parse(row["AtkSpeedAccel"].ToString());
            u.moveSpeed = float.Parse(row["MoveSpeed"].ToString());
            u.def = float.Parse(row["Def"].ToString());
            u.atkInterval = float.Parse(row["AtkInterval"].ToString());
            u.range = float.Parse(row["Range"].ToString());
            u.knockback = float.Parse(row["Knockback"].ToString());
            u.scale = float.Parse(row["Scale"].ToString());

            ColorUtility.TryParseHtmlString("#" + row["ColorHex"].ToString(), out u.color);
            u.color.a = 1f;

            if (row.ContainsKey("PrefabPath"))
                u.prefabPath = row["PrefabPath"].ToString();

            allUnits.Add(u);
        }
    }

    private static void LoadStages()
    {
        allStages.Clear();
        List<Dictionary<string, object>> data = CSVReader.Read("Data/StageData");

        foreach (var row in data)
        {
            StageData s = new StageData();
            s.stageId = int.Parse(row["StageID"].ToString());
            string listStr = row["MonsterIDList"].ToString();
            s.monsterIds = listStr.Split(';').ToList();
            allStages.Add(s);
        }
    }

    private static void LoadStatRewards()
    {
        allStatRewards.Clear();
        List<Dictionary<string, object>> data = CSVReader.Read("Data/StatReward");

        foreach (var row in data)
        {
            StatRewardData s = new StatRewardData();
            s.id = row["ID"].ToString();
            s.name = row["Name"].ToString();

            string effectsStr = row["Effects"].ToString(); // "ATK:20;HP:-10"
            string[] pairs = effectsStr.Split(';');
            foreach(var p in pairs)
            {
                string[] kv = p.Split(':');
                if(kv.Length == 2)
                {
                    s.effects[kv[0]] = float.Parse(kv[1]);
                }
            }
            allStatRewards.Add(s);
        }
    }

    public static MaskData GetMask(string id) => allMasks.Find(m => m.id == id);
    public static UnitData GetUnit(string id) => allUnits.Find(u => u.id == id);
    public static StageData GetStage(int id) => allStages.Find(s => s.stageId == id);

    public static MaskData GetRandomMask(List<string> excludeIds = null)
    {
        var pool = allMasks;
        if(excludeIds != null)
        {
            pool = allMasks.Where(m => !excludeIds.Contains(m.id)).ToList();
        }
        if (pool.Count == 0) return allMasks[0]; // Fallback
        return pool[Random.Range(0, pool.Count)];
    }

    public static StatRewardData GetRandomStatReward()
    {
        return allStatRewards[Random.Range(0, allStatRewards.Count)];
    }
}

[System.Serializable]
public class RewardOption
{
    public RewardType type;
    public MaskData maskData; // For New Mask
    public StatRewardData statData; // For Stat Boost
    public string description; // For Upgrade (or generic)
}

public class CSVReader
{
    public static List<Dictionary<string, object>> Read(string file)
    {
        var list = new List<Dictionary<string, object>>();
        TextAsset data = Resources.Load<TextAsset>(file);

        if (data == null)
        {
            Debug.LogError($"CSV file not found: {file}");
            return list;
        }

        string[] lines = data.text.Split('\n');
        if (lines.Length <= 1) return list;

        string[] header = lines[0].Trim().Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');
            var entry = new Dictionary<string, object>();

            for (int j = 0; j < header.Length && j < values.Length; j++)
            {
                if (j < values.Length)
                    entry[header[j]] = values[j];
            }
            list.Add(entry);
        }
        return list;
    }
}
