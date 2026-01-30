using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    private Canvas mainCanvas;

    // Reward UI
    private GameObject rewardPanel;
    private Transform rewardContainer;

    // Inventory UI
    private GameObject inventoryPanel;
    private List<Image> inventorySlots = new List<Image>();
    private List<GameObject> inventoryHighlights = new List<GameObject>();

    // Replacement UI
    private GameObject replacePanel;
    private Transform replaceContainer;

    // Detail UI
    private GameObject detailPanel;
    private TextMeshProUGUI detailName;
    private TextMeshProUGUI detailDesc;

    private int selectedRewardIndex = -1;
    private int selectedReplaceIndex = -1;

    void Awake()
    {
        SetupCanvas();
        SetupInventoryHUD();
        SetupRewardPanel();
        SetupReplacePanel();
        SetupDetailPanel();
    }

    void SetupCanvas()
    {
        GameObject canvasObj = new GameObject("MainCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // --- Inventory HUD ---

    void SetupInventoryHUD()
    {
        inventoryPanel = new GameObject("InventoryHUD");
        inventoryPanel.transform.SetParent(mainCanvas.transform, false);
        RectTransform rect = inventoryPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 50);
        rect.sizeDelta = new Vector2(600, 120);

        HorizontalLayoutGroup layout = inventoryPanel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 4; i++)
        {
            int index = i;
            GameObject slot = new GameObject($"Slot_{i}");
            slot.transform.SetParent(inventoryPanel.transform, false);

            Image bg = slot.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Add Handler
            var handler = slot.AddComponent<InventorySlotHandler>();
            handler.slotIndex = index;

            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(100, 100);

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.rectTransform.sizeDelta = new Vector2(80, 80);
            icon.color = Color.clear;
            inventorySlots.Add(icon);

            GameObject highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(slot.transform, false);
            Image hImg = highlightObj.AddComponent<Image>();
            hImg.color = Color.yellow;
            hImg.rectTransform.sizeDelta = new Vector2(110, 110);
            hImg.raycastTarget = false;
            highlightObj.SetActive(false);
            inventoryHighlights.Add(highlightObj);
        }
    }

    public void UpdateInventoryUI()
    {
        var inv = GameManager.Instance.inventory;
        int equipped = GameManager.Instance.equippedMaskIndex;

        for (int i = 0; i < 4; i++)
        {
            if (i < inv.Count)
            {
                inventorySlots[i].color = inv[i].color;
                inventorySlots[i].gameObject.SetActive(true);
            }
            else
            {
                inventorySlots[i].color = Color.clear;
                inventorySlots[i].gameObject.SetActive(false);
            }

            inventoryHighlights[i].SetActive(i == equipped && i < inv.Count);
        }
    }

    // --- Detail Panel ---

    void SetupDetailPanel()
    {
        // Create panel with custom positioning (not full screen)
        detailPanel = new GameObject("DetailPanel");
        detailPanel.transform.SetParent(mainCanvas.transform, false);
        Image img = detailPanel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.9f);
        
        // Position at bottom with fixed height
        RectTransform rect = detailPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(0, 300); // Fixed height of 300 pixels
        
        detailPanel.SetActive(false);

        // Make it overlay everything
        detailPanel.transform.SetAsLastSibling();

        detailName = CreateText(detailPanel.transform, "Name", "Mask Name", 40, new Vector2(0, 120));
        detailDesc = CreateText(detailPanel.transform, "Desc", "Stats...", 24, new Vector2(0, 20));
    }

    public void ShowMaskDetail(MaskData mask)
    {
        detailPanel.SetActive(true);
        detailPanel.transform.SetAsLastSibling();
        detailName.text = mask.name;
        detailDesc.text = $"{mask.description}\n\n<color=#FFFF00>STATS</color>\nATK: {mask.atkBonus}\nHP: {mask.hpBonus}\nSPD: {mask.moveSpeedBonus}\nRNG: {mask.rangeBonus}";
    }

    public void HideMaskDetail()
    {
        detailPanel.SetActive(false);
    }

    // --- Reward Panel ---

    void SetupRewardPanel()
    {
        rewardPanel = CreatePanel(mainCanvas.transform, "RewardPanel", new Color(0, 0, 0, 0.9f));
        rewardPanel.SetActive(false);

        CreateText(rewardPanel.transform, "Title", "CHOOSE YOUR REWARD", 60, new Vector2(0, 400));

        GameObject container = new GameObject("Container");
        container.transform.SetParent(rewardPanel.transform, false);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1400, 600);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 50;
        layout.childAlignment = TextAnchor.MiddleCenter;

        rewardContainer = container.transform;
    }

    public async UniTask<int> ShowRewardSelection(List<RewardOption> options)
    {
        selectedRewardIndex = -1;
        rewardPanel.SetActive(true);
        rewardPanel.transform.SetAsLastSibling();

        foreach (Transform child in rewardContainer) Destroy(child.gameObject);

        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            RewardOption opt = options[i];
            CreateRewardCard(opt, index);
        }

        await UniTask.WaitUntil(() => selectedRewardIndex != -1);

        rewardPanel.SetActive(false);
        return selectedRewardIndex;
    }

    void CreateRewardCard(RewardOption opt, int index)
    {
        GameObject card = new GameObject($"Card_{index}");
        card.transform.SetParent(rewardContainer, false);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.3f);

        Button btn = card.AddComponent<Button>();
        btn.onClick.AddListener(() => selectedRewardIndex = index);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredWidth = 400;
        le.preferredHeight = 550;

        string titleText = "";
        string descText = opt.description;
        Color visualColor = Color.white;

        if (opt.type == RewardType.NewMask)
        {
            titleText = opt.maskData.name;
            descText = $"{opt.maskData.description}\n\nATK: {opt.maskData.atkBonus}\nSPD: {opt.maskData.moveSpeedBonus}";
            visualColor = opt.maskData.color;
        }
        else if (opt.type == RewardType.UpgradeMask)
        {
            titleText = "UPGRADE MASK";
            visualColor = Color.cyan;
        }
        else if (opt.type == RewardType.StatBoost)
        {
            titleText = "STAT BOOST";
            visualColor = Color.green;
            descText = "Permanent Stats:\n";
            foreach(var kv in opt.statData.effects)
            {
                descText += $"{kv.Key}: {kv.Value}%\n";
            }
        }

        CreateText(card.transform, "Title", titleText, 40, new Vector2(0, 200));
        CreateText(card.transform, "Desc", descText, 24, new Vector2(0, -50));

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(card.transform, false);
        Image iconImg = icon.AddComponent<Image>();
        iconImg.color = visualColor;
        iconImg.rectTransform.anchoredPosition = new Vector2(0, 80);
        iconImg.rectTransform.sizeDelta = new Vector2(150, 150);
    } 

    // --- Replace Mask Panel ---

    void SetupReplacePanel()
    {
        replacePanel = CreatePanel(mainCanvas.transform, "ReplacePanel", new Color(0, 0, 0, 0.95f));
        replacePanel.SetActive(false);

        CreateText(replacePanel.transform, "Title", "INVENTORY FULL!", 50, new Vector2(0, 300));
        CreateText(replacePanel.transform, "Sub", "Select a mask to discard for the new one:", 30, new Vector2(0, 200));

        GameObject container = new GameObject("Container");
        container.transform.SetParent(replacePanel.transform, false);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000, 300);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;

        replaceContainer = container.transform;

        Button cancelBtn = CreateButton(replacePanel.transform, "CancelBtn", "CANCEL (Keep Old)", new Vector2(0, -300));
        cancelBtn.onClick.AddListener(() => selectedReplaceIndex = -2);
    }

    public async UniTask<int> ShowReplaceMaskPopup(MaskData newMask)
    {
        selectedReplaceIndex = -1;
        replacePanel.SetActive(true);
        replacePanel.transform.SetAsLastSibling();

        foreach(Transform t in replaceContainer) Destroy(t.gameObject);

        var inv = GameManager.Instance.inventory;
        for (int i = 0; i < inv.Count; i++)
        {
            int index = i;
            GameObject slot = new GameObject($"Slot_{i}");
            slot.transform.SetParent(replaceContainer, false);

            Image bg = slot.AddComponent<Image>();
            bg.color = inv[i].color;

            Button btn = slot.AddComponent<Button>();
            btn.onClick.AddListener(() => selectedReplaceIndex = index);

            LayoutElement le = slot.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 150;

            CreateText(slot.transform, "Name", inv[i].name, 20, Vector2.zero);
        }

        await UniTask.WaitUntil(() => selectedReplaceIndex != -1);

        replacePanel.SetActive(false);
        return selectedReplaceIndex;
    }


    // --- Helpers ---

    GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, Vector2 anchoredPos)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = size;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform rect = txtObj.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(400, 100);

        return txt;
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = Color.white;

        Button btn = btnObj.AddComponent<Button>();

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(200, 60);

        TextMeshProUGUI txt = CreateText(btnObj.transform, "Label", label, 24, Vector2.zero);
        txt.color = Color.black;
        txt.rectTransform.sizeDelta = new Vector2(200, 60);

        return btn;
    }
}
