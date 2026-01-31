using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas;

    // Reward UI
    [SerializeField] private GameObject rewardPanel;
    private Transform rewardContainer;

    // Inventory UI
    [SerializeField] private GameObject inventoryPanel;
    private List<Image> inventorySlots = new List<Image>();
    private List<GameObject> inventoryHighlights = new List<GameObject>();

    // Replacement UI
    [SerializeField] private GameObject replacePanel;
    private Transform replaceContainer;

    // End Game UI
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameClearPanel;

    // Player Stats HUD
    private TextMeshProUGUI hpText;
    private TextMeshProUGUI atkSpeedText;

    // Detail UI
    [SerializeField] private GameObject detailPanel;
    private TextMeshProUGUI detailName;
    private TextMeshProUGUI detailDesc;

    // FPS Display - kept for potential future control (e.g., toggle on/off)
    private FPSDisplay fpsDisplay;

    private int selectedRewardIndex = -1;
    private int selectedReplaceIndex = -1;

    void Awake()
    {
        SetupCanvas();
        SetupInventoryHUD();
        SetupStatsHUD();
        SetupRewardPanel();
        SetupReplacePanel();
        SetupDetailPanel();
        SetupGameOverPanel();
        SetupGameClearPanel();
        SetupFPSDisplay();
    }

    void SetupCanvas()
    {
        if (mainCanvas != null) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/MainCanvas");
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = "MainCanvas";
            mainCanvas = obj.GetComponent<Canvas>();
            if (mainCanvas == null) mainCanvas = obj.AddComponent<Canvas>();
        }
        else
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

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
        if (inventoryPanel == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/InventoryPanel");
            if (prefab != null)
            {
                inventoryPanel = Instantiate(prefab, mainCanvas.transform, false);
                inventoryPanel.name = "InventoryHUD";
            }
            else
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
            }
        }

        // Clear existing children to ensure clean state or assume they are correct?
        // For this refactor, let's clear and rebuild using slot prefab to ensure correct setup.
        foreach (Transform child in inventoryPanel.transform) Destroy(child.gameObject);
        inventorySlots.Clear();
        inventoryHighlights.Clear();

        GameObject slotPrefab = Resources.Load<GameObject>("Prefabs/UI/InventorySlot");

        for (int i = 0; i < 4; i++)
        {
            int index = i;
            GameObject slot;

            if (slotPrefab != null)
            {
                slot = Instantiate(slotPrefab, inventoryPanel.transform, false);
                slot.name = $"Slot_{i}";
            }
            else
            {
                slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(inventoryPanel.transform, false);

                Image bg = slot.AddComponent<Image>();
                bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                RectTransform slotRect = slot.GetComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(100, 100);
            }

            // Add Handler
            var handler = slot.GetComponent<InventorySlotHandler>();
            if (handler == null) handler = slot.AddComponent<InventorySlotHandler>();
            handler.slotIndex = index;

            // Setup Icon
            Transform iconTr = slot.transform.Find("Icon");
            Image icon;
            if (iconTr != null)
            {
                icon = iconTr.GetComponent<Image>();
            }
            else
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slot.transform, false);
                icon = iconObj.AddComponent<Image>();
                icon.rectTransform.sizeDelta = new Vector2(80, 80);
                icon.color = Color.clear;
            }
            inventorySlots.Add(icon);

            // Setup Highlight
            Transform hlTr = slot.transform.Find("Highlight");
            GameObject highlightObj;
            if (hlTr != null)
            {
                highlightObj = hlTr.gameObject;
            }
            else
            {
                highlightObj = new GameObject("Highlight");
                highlightObj.transform.SetParent(slot.transform, false);
                Image hImg = highlightObj.AddComponent<Image>();
                hImg.color = Color.yellow;
                hImg.rectTransform.sizeDelta = new Vector2(110, 110);
                hImg.raycastTarget = false;
            }
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

    // --- Stats HUD ---

    void SetupStatsHUD()
    {
        GameObject statsObj = new GameObject("StatsHUD");
        statsObj.transform.SetParent(mainCanvas.transform, false);
        RectTransform rect = statsObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(50, -50);
        rect.sizeDelta = new Vector2(400, 150);

        hpText = CreateText(statsObj.transform, "HPText", "HP: 0/0", 30, new Vector2(100, 0));
        hpText.alignment = TextAlignmentOptions.Left;

        atkSpeedText = CreateText(statsObj.transform, "AtkSpeedText", "Atk Interval: 0", 30, new Vector2(100, -50));
        atkSpeedText.alignment = TextAlignmentOptions.Left;
    }

    public void UpdatePlayerStatsUI(float currentHP, float maxHP, float atkInterval)
    {
        if (hpText != null) hpText.text = $"HP: {Mathf.Ceil(currentHP)} / {Mathf.Ceil(maxHP)}";
        if (atkSpeedText != null) atkSpeedText.text = $"Atk Interval: {atkInterval:F2}s";
    }

    // --- Detail Panel ---

    void SetupDetailPanel()
    {
        if (detailPanel == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/DetailPanel");
            if (prefab != null)
            {
                detailPanel = Instantiate(prefab, mainCanvas.transform, false);
                detailPanel.name = "DetailPanel";
            }
            else
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
            }
        }

        detailPanel.SetActive(false);
        detailPanel.transform.SetAsLastSibling();

        // Find Components
        Transform nameTr = detailPanel.transform.Find("Name");
        if (nameTr) detailName = nameTr.GetComponent<TextMeshProUGUI>();
        if (detailName == null) detailName = CreateText(detailPanel.transform, "Name", "Mask Name", 36, new Vector2(0, 60));

        Transform descTr = detailPanel.transform.Find("Desc");
        if (descTr) detailDesc = descTr.GetComponent<TextMeshProUGUI>();
        if (detailDesc == null) detailDesc = CreateText(detailPanel.transform, "Desc", "Stats...", 22, new Vector2(0, -50));
    }

    public void ShowMaskDetail(MaskData mask)
    {
        detailPanel.SetActive(true);
        detailPanel.transform.SetAsLastSibling();
        detailName.text = mask.name;
        detailDesc.text = $"{mask.description}\n\n" +
                          $"<color=#FFFF00>EQUIP STATS</color>\n" +
                          $"Atk: {mask.equipAtk}, Interval: {mask.equipInterval}s, Def: {mask.equipDef}%\n" +
                          $"Range: {mask.equipRange}, Knockback: {mask.equipKnockback}\n" +
                          $"<color=#00FFFF>PASSIVE STATS (Owned)</color>\n" +
                          $"HP: {mask.passiveHP}, AtkEff: {mask.passiveAtkEff}%, Speed: {mask.passiveSpeed}%\n" +
                          $"Def: {mask.passiveDef}%, Range: {mask.passiveRange}";
    }

    public void HideMaskDetail()
    {
        detailPanel.SetActive(false);
    }

    // --- Reward Panel ---

    void SetupRewardPanel()
    {
        if (rewardPanel == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/RewardPanel");
            if (prefab != null)
            {
                rewardPanel = Instantiate(prefab, mainCanvas.transform, false);
                rewardPanel.name = "RewardPanel";
            }
            else
            {
                rewardPanel = CreatePanel(mainCanvas.transform, "RewardPanel", new Color(0, 0, 0, 0.9f));
                CreateText(rewardPanel.transform, "Title", "CHOOSE YOUR REWARD", 60, new Vector2(0, 400));
            }
        }

        rewardPanel.SetActive(false);

        Transform containerTr = rewardPanel.transform.Find("Container");
        if (containerTr == null)
        {
            GameObject container = new GameObject("Container");
            container.transform.SetParent(rewardPanel.transform, false);
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1400, 600);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 50;
            layout.childAlignment = TextAnchor.MiddleCenter;
            containerTr = container.transform;
        }
        rewardContainer = containerTr;
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
        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/RewardCard");
        GameObject card;

        if (prefab != null)
        {
            card = Instantiate(prefab, rewardContainer, false);
            card.name = $"Card_{index}";
        }
        else
        {
            card = new GameObject($"Card_{index}");
            card.transform.SetParent(rewardContainer, false);

            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f);

            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredWidth = 400;
            le.preferredHeight = 550;
        }

        Button btn = card.GetComponent<Button>();
        if (btn == null) btn = card.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => selectedRewardIndex = index);

        string titleText = "";
        string descText = opt.description;
        Color visualColor = Color.white;

        if (opt.type == RewardType.NewMask)
        {
            titleText = opt.maskData.name;
            descText = $"{opt.maskData.description}\n\n" +
                       $"Equip Atk: {opt.maskData.equipAtk}\n" +
                       $"Passive HP: {opt.maskData.passiveHP}";
            visualColor = opt.maskData.color;
        }
        else if (opt.type == RewardType.UpgradeMask)
        {
            titleText = "UPGRADE MASK";
            visualColor = Color.cyan;
            if (GameManager.Instance.equippedMaskIndex >= 0)
            {
                var m = GameManager.Instance.inventory[GameManager.Instance.equippedMaskIndex];
                descText = $"{m.name}\nLv. {m.level} -> Lv. {m.level + 1}";
            }
        }
        else if (opt.type == RewardType.StatBoost)
        {
            titleText = "STAT BOOST";
            visualColor = Color.green;
            descText = "Permanent Stats:\n";
            foreach (var kv in opt.statData.effects)
            {
                descText += $"{kv.Key}: {kv.Value}%\n";
            }
        }

        Transform titleTr = card.transform.Find("Title");
        if (titleTr) titleTr.GetComponent<TextMeshProUGUI>().text = titleText;
        else CreateText(card.transform, "Title", titleText, 40, new Vector2(0, 200));

        Transform descTr = card.transform.Find("Desc");
        if (descTr) descTr.GetComponent<TextMeshProUGUI>().text = descText;
        else CreateText(card.transform, "Desc", descText, 24, new Vector2(0, -50));

        Transform iconTr = card.transform.Find("Icon");
        Image iconImg;
        if (iconTr)
        {
            iconImg = iconTr.GetComponent<Image>();
        }
        else
        {
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(card.transform, false);
            iconImg = icon.AddComponent<Image>();
            iconImg.rectTransform.anchoredPosition = new Vector2(0, 80);
            iconImg.rectTransform.sizeDelta = new Vector2(150, 150);
        }
        iconImg.color = visualColor;
    }

    // --- Replace Mask Panel ---

    void SetupReplacePanel()
    {
        if (replacePanel == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/ReplacePanel");
            if (prefab != null)
            {
                replacePanel = Instantiate(prefab, mainCanvas.transform, false);
                replacePanel.name = "ReplacePanel";
            }
            else
            {
                replacePanel = CreatePanel(mainCanvas.transform, "ReplacePanel", new Color(0, 0, 0, 0.95f));
                CreateText(replacePanel.transform, "Title", "INVENTORY FULL!", 50, new Vector2(0, 300));
                CreateText(replacePanel.transform, "Sub", "Select a mask to discard for the new one:", 30, new Vector2(0, 200));
            }
        }

        replacePanel.SetActive(false);

        Transform containerTr = replacePanel.transform.Find("Container");
        if (containerTr == null)
        {
            GameObject container = new GameObject("Container");
            container.transform.SetParent(replacePanel.transform, false);
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1000, 300);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30;
            layout.childAlignment = TextAnchor.MiddleCenter;
            containerTr = container.transform;
        }
        replaceContainer = containerTr;

        Transform cancelBtnTr = replacePanel.transform.Find("CancelBtn");
        Button cancelBtn;
        if (cancelBtnTr)
        {
            cancelBtn = cancelBtnTr.GetComponent<Button>();
        }
        else
        {
            cancelBtn = CreateButton(replacePanel.transform, "CancelBtn", "CANCEL (Keep Old)", new Vector2(0, -300));
        }
        cancelBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.AddListener(() => selectedReplaceIndex = -2);
    }

    public async UniTask<int> ShowReplaceMaskPopup(MaskData newMask)
    {
        selectedReplaceIndex = -1;
        replacePanel.SetActive(true);
        replacePanel.transform.SetAsLastSibling();

        foreach (Transform t in replaceContainer) Destroy(t.gameObject);

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

    // --- End Game Panels ---

    void SetupGameOverPanel()
    {
        if (gameOverPanel == null)
        {
            gameOverPanel = CreatePanel(mainCanvas.transform, "GameOverPanel", new Color(0.5f, 0, 0, 0.8f));
            CreateText(gameOverPanel.transform, "Title", "GAME OVER", 80, new Vector2(0, 100));

            Button restartBtn = CreateButton(gameOverPanel.transform, "RestartBtn", "RESTART", new Vector2(0, -100));
            restartBtn.onClick.AddListener(() => GameManager.Instance.RestartGame());
        }
        gameOverPanel.SetActive(false);
    }

    void SetupGameClearPanel()
    {
        if (gameClearPanel == null)
        {
            gameClearPanel = CreatePanel(mainCanvas.transform, "GameClearPanel", new Color(0, 0.5f, 0, 0.8f));
            CreateText(gameClearPanel.transform, "Title", "GAME CLEAR!", 80, new Vector2(0, 100));

            Button restartBtn = CreateButton(gameClearPanel.transform, "RestartBtn", "PLAY AGAIN", new Vector2(0, -100));
            restartBtn.onClick.AddListener(() => GameManager.Instance.RestartGame());
        }
        gameClearPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();
    }

    public void ShowGameClear()
    {
        gameClearPanel.SetActive(true);
        gameClearPanel.transform.SetAsLastSibling();
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

    // --- FPS Display ---

    void SetupFPSDisplay()
    {
        GameObject fpsObj = new GameObject("FPSDisplay");
        fpsObj.transform.SetParent(mainCanvas.transform, false);
        fpsDisplay = fpsObj.AddComponent<FPSDisplay>();
    }
}
