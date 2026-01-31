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
    public TextMeshProUGUI currentCostText;

    [Header("Battle Timer")]
    public GameObject battleTimerObj;
    public TextMeshProUGUI battleTimerText;

    [Header("Inventory")]
    public Transform inventoryContainer;

    private float blinkTimer = 0f;
    private bool blinkState = false;

    public void UpdateStats(float currentHP, float maxHP, float atk, float cooldown, float range, float def, float speed, float knockback, int currentCost)
    {
        currentCostText.text = $"{currentCost}";
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

    public void ShowBattleTimer(bool show)
    {
        if (battleTimerObj != null)
        {
            battleTimerObj.SetActive(show);
        }
    }

    public void UpdateBattleTimer(float elapsedTime, float timeLimit)
    {
        if (battleTimerText == null) return;

        float remainingTime = timeLimit - elapsedTime;
        
        // 시간 표시 포맷 (00:00 형식)
        string timeText;
        if (remainingTime >= 0)
        {
            int totalSeconds = Mathf.CeilToInt(remainingTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timeText = $"{minutes:D2}:{seconds:D2}";
        }
        else
        {
            int totalOvertime = Mathf.FloorToInt(-remainingTime);
            int minutes = totalOvertime / 60;
            int seconds = totalOvertime % 60;
            timeText = $"-{minutes:D2}:{seconds:D2}";
        }

        battleTimerText.text = timeText;

        // 단계별 색상 변화
        Color timerColor;
        float pulseSpeed = 0f;

        if (elapsedTime < 30f)
        {
            // 0~30초: 흰색 (평온)
            timerColor = Color.white;
        }
        else if (elapsedTime < 45f)
        {
            // 30~45초: 노란색 (경고 시작)
            timerColor = Color.yellow;
        }
        else if (elapsedTime < timeLimit)
        {
            // 45~50초: 주황색 + 살짝 펄스
            timerColor = new Color(1f, 0.5f, 0f); // Orange
            pulseSpeed = 2f;
        }
        else
        {
            // 50초+: 빨간색 + 빠른 깜빡임
            timerColor = Color.red;
            pulseSpeed = 6f;
        }

        // 깜빡임 효과
        if (pulseSpeed > 0)
        {
            blinkTimer += Time.deltaTime * pulseSpeed;
            float alpha = 0.5f + Mathf.Sin(blinkTimer * Mathf.PI) * 0.5f;
            timerColor.a = alpha;

            // 스케일 펄스 효과 (50초 이후)
            if (elapsedTime >= timeLimit)
            {
                float scale = 1f + Mathf.Sin(blinkTimer * Mathf.PI) * 0.1f;
                battleTimerText.transform.localScale = Vector3.one * scale;
            }
            else
            {
                battleTimerText.transform.localScale = Vector3.one;
            }
        }
        else
        {
            blinkTimer = 0f;
            battleTimerText.transform.localScale = Vector3.one;
        }

        battleTimerText.color = timerColor;
    }
}
