using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int slotIndex;
    private bool isPressed = false;
    private float pressTime;
    private bool isLongPressTriggered = false;
    private const float holdDuration = 0.3f; // 0.3s for faster feedback

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
