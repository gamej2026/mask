using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUI : MonoBehaviour
{
    [Header("Top Status Panel")]
    public Image hpBarFill;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI atkCooldownText;
    public TextMeshProUGUI atkRangeText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI knockbackText;

    [Header("Inventory")]
    public Transform inventoryContainer;

    public void UpdateStats(float currentHP, float maxHP, float atk, float cooldown, float range, float def, float speed, float knockback)
    {
        // 1. HP
        if (hpBarFill != null && maxHP > 0)
        {
            hpBarFill.fillAmount = currentHP / maxHP;
        }
        if (hpText != null)
        {
            hpText.text = $"{Mathf.Ceil(currentHP)} / {Mathf.Ceil(maxHP)}";
        }

        // 2. Stats
        if (atkText != null) atkText.text = $"{atk:F1}";
        if (atkCooldownText != null) atkCooldownText.text = $"{cooldown:F2} sec";
        if (atkRangeText != null) atkRangeText.text = $"{range:F1}";
        if (defText != null) defText.text = $"{def * 100:F0}%";
        if (moveSpeedText != null) moveSpeedText.text = $"{speed:F1}";
        if (knockbackText != null) knockbackText.text = $"{knockback:F1}";
    }
}
