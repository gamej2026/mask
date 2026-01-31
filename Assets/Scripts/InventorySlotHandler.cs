using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int slotIndex;
    public Image iconImage;

    private bool isPressed = false;
    private float pressTime;
    private bool isLongPressTriggered = false;
    private const float holdDuration = 0.3f; // 0.3s for faster feedback

    void Awake()
    {
        if (iconImage == null)
        {
            Transform t = transform.Find("Icon");
            if (t != null) iconImage = t.GetComponent<Image>();
        }
    }

    public void SetMask(MaskData mask)
    {
        if (mask == null)
        {
            if (iconImage != null)
            {
                iconImage.color = Color.clear;
                iconImage.sprite = null;
            }
            return;
        }

        if (iconImage != null)
        {
            // Default to color
            iconImage.color = mask.color;
            iconImage.sprite = null;

            // Try load icon
            if (!string.IsNullOrEmpty(mask.iconName))
            {
                Sprite s = Resources.Load<Sprite>(mask.iconName);
                if (s != null)
                {
                    iconImage.sprite = s;
                    iconImage.color = Color.white;
                }
            }
        }
    }

    void Update()
    {
        if (isPressed && !isLongPressTriggered)
        {
            if (Time.time - pressTime >= holdDuration)
            {
                isLongPressTriggered = true;

                // Get Mask
                if (slotIndex < GameManager.Instance.inventory.Count)
                {
                    MaskData mask = GameManager.Instance.inventory[slotIndex];
                    if (mask != null)
                    {
                        GameManager.Instance.uiManager.ShowMaskDetail(mask);
                    }
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        pressTime = Time.time;
        isLongPressTriggered = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (isLongPressTriggered)
        {
            GameManager.Instance.uiManager.HideMaskDetail();
        }
        else
        {
            GameManager.Instance.EquipMask(slotIndex);
        }

        isLongPressTriggered = false;
    }
}
