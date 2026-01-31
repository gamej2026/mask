using UnityEngine;
using UnityEngine.UI;

public class UnitHUD : MonoBehaviour
{
    [SerializeField] private Image hpFill;
    [SerializeField] private Image attackFill;

    public void UpdateStatus(float hpPercent, float attackPercent)
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = Mathf.Clamp01(hpPercent);
        }

        if (attackFill != null)
        {
            attackFill.fillAmount = Mathf.Clamp01(attackPercent);
        }
    }
}
