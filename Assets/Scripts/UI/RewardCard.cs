using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardCard : MonoBehaviour
{
    public Image rewardIcon;
    public TextMeshProUGUI titleText;

    public GameObject[] equipDetailObj;
    public GameObject[] inBagDetailObj;
    public GameObject[] statDetailObj;

    public TextMeshProUGUI[] equipDetail;
    public TextMeshProUGUI[] inBagDetail;
    public TextMeshProUGUI[] statDetail;

    public Image[] equipDetailImage;
    public Image[] inBagDetailImage;
    public Image[] statDetailImage;

    public GameObject space;
    public GameObject maskEffect;
    public GameObject statEffect;

    // Stat key 순서 (equipDetail, inBagDetail에 맞춰서)
    private readonly string[] equipStatKeys = { "baseatk", "cooldown", "def", "range", "kb" };
    private readonly string[] passiveStatKeys = { "hp", "atk", "speed", "def", "range", "move" };
    private readonly string[] statBoostKeys = { "hp", "atk", "speed", "def", "range", "move" };

    public void SetData(RewardOption opt)
    {
        maskEffect.SetActive(opt.type != RewardType.StatBoost);
        statEffect.SetActive(opt.type == RewardType.StatBoost);

        // 모든 detail 초기화
        HideAllDetails();

        if (opt.type == RewardType.NewMask)
        {
            SetNewMaskData(opt.maskData);
        }
        else if (opt.type == RewardType.UpgradeMask)
        {
            SetUpgradeMaskData();
        }
        else if (opt.type == RewardType.StatBoost)
        {
            SetStatBoostData(opt.statData);
        }
    }

    private void HideAllDetails()
    {
        foreach (var obj in equipDetailObj) if (obj) obj.SetActive(false);
        foreach (var obj in inBagDetailObj) if (obj) obj.SetActive(false);
        foreach (var obj in statDetailObj) if (obj) obj.SetActive(false);
    }

    private void SetNewMaskData(MaskData m)
    {
        titleText.text = $"{m.name} (New!)";

        // 아이콘
        Sprite icon = Resources.Load<Sprite>($"{m.iconName}");
        if (rewardIcon)
        {
            if (icon != null)
            {
                rewardIcon.sprite = icon;
                rewardIcon.color = Color.white;
            }
            else
            {
                rewardIcon.sprite = null;
                rewardIcon.color = m.color;
            }
        }

        // Equip Stats
        float[] equipVals = { m.equipAtk, m.equipInterval, m.equipDef, m.equipRange, m.equipKnockback };
        string[] equipUnits = { "", "s", "%", "", "" };
        SetStatDetails(equipDetailObj, equipDetail, equipDetailImage, equipStatKeys, equipVals, equipUnits, false);

        // Passive Stats
        float[] passiveVals = { m.passiveHP, m.passiveAtkEff, m.passiveAtkSpeedAccel, m.passiveDef, m.passiveRange, m.passiveSpeed };
        string[] passiveUnits = { "", "%", "%", "%", "", "%" };
        SetStatDetails(inBagDetailObj, inBagDetail, inBagDetailImage, passiveStatKeys, passiveVals, passiveUnits, false);
    }

    private void SetUpgradeMaskData()
    {
        if (GameManager.Instance == null || GameManager.Instance.equippedMaskIndex < 0) return;

        var m = GameManager.Instance.inventory[GameManager.Instance.equippedMaskIndex];
        var initial = GameData.GetMask(m.id);

        titleText.text = $"{m.name}\nLv. {m.level} -> <color=#00FF00>Lv. {m.level + 1}</color>";

        // 아이콘
        Sprite icon = Resources.Load<Sprite>($"Icons/{m.iconName}");
        if (rewardIcon)
        {
            if (icon != null)
            {
                rewardIcon.sprite = icon;
                rewardIcon.color = Color.white;
            }
            else
            {
                rewardIcon.sprite = null;
                rewardIcon.color = m.color;
            }
        }

        // Equip Stats (업그레이드시 변경 없음, 현재값만 표시)
        float[] equipVals = { m.equipAtk, m.equipInterval, 0, 0, 0 };
        string[] equipUnits = { "", "s", "%", "", "" };
        SetStatDetails(equipDetailObj, equipDetail, equipDetailImage, equipStatKeys, equipVals, equipUnits, false);

        // Passive Upgrade
        if (initial != null)
        {
            float currMult = 1f + (m.level - 1) * 0.5f;
            float nextMult = 1f + m.level * 0.5f;

            SetUpgradeDetails(inBagDetailObj, inBagDetail, inBagDetailImage, passiveStatKeys, initial, currMult, nextMult);
        }
    }

    private void SetStatBoostData(StatRewardData statData)
    {
        titleText.text = statData.name;

        if (rewardIcon)
        {
            rewardIcon.sprite = null;
            rewardIcon.color = Color.green;
        }

        bool hasPositive = false;
        bool hasNegative = false;
        int firstNegativeIdx = -1;

        // 양수/음수 확인 및 첫 번째 음수 인덱스 찾기
        int idx = 0;
        foreach (var kv in statData.effects)
        {
            if (idx >= statDetailObj.Length) break;

            if (kv.Value >= 0)
                hasPositive = true;
            else
            {
                if (!hasNegative) firstNegativeIdx = idx;
                hasNegative = true;
            }

            SetStatSlot(idx, kv.Key, kv.Value);
            idx++;
        }

        // space 처리
        if (space)
        {
            if (hasPositive && hasNegative)
            {
                space.SetActive(true);
                // 먼저 맨 뒤로 보낸 뒤, 첫 번째 음수 스탯 위치로 이동
                space.transform.SetAsLastSibling();
                if (firstNegativeIdx >= 0 && firstNegativeIdx < statDetailObj.Length && statDetailObj[firstNegativeIdx])
                {
                    int targetIndex = statDetailObj[firstNegativeIdx].transform.GetSiblingIndex();
                    space.transform.SetSiblingIndex(targetIndex);
                }
            }
            else
            {
                space.SetActive(false);
            }
        }
    }

    private void SetStatSlot(int idx, string key, float value)
    {
        string iconName = GetIconName(key);
        string unit = GetUnit(key);
        string sign = value > 0 ? "+" : "";

        if (idx < statDetailObj.Length && statDetailObj[idx])
        {
            statDetailObj[idx].SetActive(true);
        }
        if (idx < statDetail.Length && statDetail[idx])
        {
            statDetail[idx].text = $"{sign}{value:F0}{unit}";
        }
        if (idx < statDetailImage.Length && statDetailImage[idx])
        {
            statDetailImage[idx].sprite = Resources.Load<Sprite>($"Icons/{iconName}");
        }
    }

    private void SetStatDetails(GameObject[] objs, TextMeshProUGUI[] texts, Image[] images, string[] keys, float[] vals, string[] units, bool showPlus)
    {
        for (int i = 0; i < keys.Length && i < objs.Length; i++)
        {
            if (vals[i] == 0)
            {
                if (i < objs.Length && objs[i]) objs[i].SetActive(false);
                continue;
            }

            // 값이 0이 아니면 활성화
            if (i < objs.Length && objs[i]) objs[i].SetActive(true);

            string sign = (showPlus && vals[i] > 0) ? "+" : "";
            string formatted = FormatValue(keys[i], vals[i]);

            if (i < texts.Length && texts[i])
            {
                texts[i].text = $"{sign}{formatted}{units[i]}";
            }
            if (i < images.Length && images[i])
            {
                images[i].sprite = Resources.Load<Sprite>($"Icons/{GetIconName(keys[i])}");
            }
        }
    }

    private void SetUpgradeDetails(GameObject[] objs, TextMeshProUGUI[] texts, Image[] images, string[] keys, MaskData initial, float currMult, float nextMult)
    {
        float[] baseVals = { initial.passiveHP, initial.passiveAtkEff, initial.passiveAtkSpeedAccel, initial.passiveDef, initial.passiveRange, initial.passiveSpeed };
        string[] units = { "", "%", "%", "%", "", "%" };

        for (int i = 0; i < keys.Length && i < objs.Length; i++)
        {
            if (baseVals[i] == 0)
            {
                if (i < objs.Length && objs[i]) objs[i].SetActive(false);
                continue;
            }

            // 값이 0이 아니면 활성화
            if (i < objs.Length && objs[i]) objs[i].SetActive(true);

            float curr = baseVals[i] * currMult;
            float next = baseVals[i] * nextMult;
            string fmt = (keys[i] == "range" || keys[i] == "move") ? "F1" : "F0";

            if (i < texts.Length && texts[i])
            {
                texts[i].text = $"{curr.ToString(fmt)}{units[i]} -> <color=#00FF00>{next.ToString(fmt)}{units[i]}</color>";
            }
            if (i < images.Length && images[i])
            {
                images[i].sprite = Resources.Load<Sprite>($"Icons/{GetIconName(keys[i])}");
            }
        }
    }

    private string FormatValue(string key, float val)
    {
        if (key == "cooldown") return val.ToString("F2");
        if (key == "range" || key == "kb" || key == "move") return val.ToString("F1");
        return val.ToString("F0");
    }

    private string GetIconName(string key)
    {
        switch (key.ToLower())
        {
            case "hp": return "health icon";
            case "atk": case "baseatk": return "attack icon";
            case "speed": case "cooldown": return "attack cooltime icon";
            case "def": return "defense icon";
            case "range": return "attack range icon";
            case "kb": return "knockback icon";
            case "move": return "movement speed icon";
            default: return "health icon";
        }
    }

    private string GetUnit(string key)
    {
        switch (key.ToLower())
        {
            case "atk": case "speed": case "def": return "%";
            default: return "";
        }
    }
}
