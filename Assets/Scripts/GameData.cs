using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum SkillType
{
    None,
    DoubleStrike,
    KnockbackBoost
}

public enum ActionType
{
    Attack,
    Heal,
    Buff
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
    public SkillType skill;

    public float hpBonus;
    public float atkBonus;
    public float atkSpeedBonus;
    public float moveSpeedBonus;
    public float rangeBonus;

    public Color color;

    public MaskData Copy()
    {
        return (MaskData)this.MemberwiseClone();
    }
}

[System.Serializable]
public class MonsterData
{
    public string id;
    public string name;
    public float hp;
    public float atk;
    public float speed;
    public float range;
    public float knockback;
    public float scale;
    public Color color;
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
    public static List<MonsterData> allMonsters = new List<MonsterData>();
    public static List<StageData> allStages = new List<StageData>();
    public static List<StatRewardData> allStatRewards = new List<StatRewardData>();

    public static void Initialize()
    {
        LoadMasks();
        LoadMonsters();
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
            m.skill = (SkillType)System.Enum.Parse(typeof(SkillType), row["SkillType"].ToString());
            m.hpBonus = float.Parse(row["HPBonus"].ToString());
            m.atkBonus = float.Parse(row["AtkBonus"].ToString());
            m.atkSpeedBonus = float.Parse(row["SpdBonus"].ToString());
            m.rangeBonus = float.Parse(row["RangeBonus"].ToString());

            ColorUtility.TryParseHtmlString("#" + row["ColorHex"].ToString(), out m.color);

            // Fix alpha if needed
            m.color.a = 1f;

            allMasks.Add(m);
        }
    }

    private static void LoadMonsters()
    {
        allMonsters.Clear();
        List<Dictionary<string, object>> data = CSVReader.Read("Data/MonsterData");

        foreach (var row in data)
        {
            MonsterData m = new MonsterData();
            m.id = row["ID"].ToString();
            m.name = row["Name"].ToString();
            m.hp = float.Parse(row["HP"].ToString());
            m.atk = float.Parse(row["Atk"].ToString());
            m.speed = float.Parse(row["Speed"].ToString());
            m.range = float.Parse(row["Range"].ToString());
            m.knockback = float.Parse(row["Knockback"].ToString());
            m.scale = float.Parse(row["Scale"].ToString());

            ColorUtility.TryParseHtmlString("#" + row["ColorHex"].ToString(), out m.color);
            m.color.a = 1f;

            allMonsters.Add(m);
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
    public static MonsterData GetMonster(string id) => allMonsters.Find(m => m.id == id);
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
                entry[header[j]] = values[j];
            }
            list.Add(entry);
        }
        return list;
    }
}
