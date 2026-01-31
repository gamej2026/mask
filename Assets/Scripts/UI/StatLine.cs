using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatLine : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI valueText;

    public void SetData(Sprite iconSprite, string value)
    {
        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.gameObject.SetActive(iconSprite != null);
        }

        if (valueText != null)
        {
            valueText.text = value;
        }
    }
}
