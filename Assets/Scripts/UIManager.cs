using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas;

    // Combat UI (Replaces StatsHUD and separate InventoryPanel)
    private CombatUI combatUI;

    // Inventory UI Lists (Managed within CombatUI's container)
    private List<Image> inventorySlots = new List<Image>(); // The Icon Images
    private List<GameObject> inventoryHighlights = new List<GameObject>();
    private List<TextMeshProUGUI> slotHotkeys = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> slotCosts = new List<TextMeshProUGUI>();
    private List<Image> slotBackgrounds = new List<Image>();
    private List<InventorySlotHandler> slotHandlers = new List<InventorySlotHandler>();

    // Reward UI
    [SerializeField] private GameObject rewardPanel;
    private Transform rewardContainer;

    // Replace UI
    [SerializeField] private GameObject replacePanel;
    private Transform replaceContainer;

    // End Game UI
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameClearPanel;

    // Detail UI
    [SerializeField] private GameObject detailPanel;
    private TextMeshProUGUI detailName;
    private TextMeshProUGUI detailDesc;

    // FPS Display
    private FPSDisplay fpsDisplay;

    private int selectedRewardIndex = -1;
    private int selectedReplaceIndex = -1;

    void Awake()
    {
        SetupCanvas();
        SetupCombatUI(); // Instantiate CombatUI first so we have the containers
        SetupInventoryHUD();
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

    void SetupCombatUI()
    {
        if (combatUI != null) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/CombatUI");
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, mainCanvas.transform, false);
            obj.name = "CombatUI";
            combatUI = obj.GetComponent<CombatUI>();
        }
        else
        {
            Debug.LogError("CombatUI Prefab not found!");
        }
    }

    void SetupInventoryHUD()
    {
        inventorySlots.Clear();
        inventoryHighlights.Clear();
        slotHotkeys.Clear();
        slotCosts.Clear();
        slotBackgrounds.Clear();
        slotHandlers.Clear();

        Transform container = null;
        if (combatUI != null && combatUI.inventoryContainer != null)
        {
            container = combatUI.inventoryContainer;
        }
        else
        {
            // Fallback (should not happen if prefab is correct)
            GameObject fallbackPanel = new GameObject("InventoryFallback");
            fallbackPanel.transform.SetParent(mainCanvas.transform, false);
            container = fallbackPanel.transform;
        }

        foreach (Transform child in container) Destroy(child.gameObject);

        GameObject slotPrefab = Resources.Load<GameObject>("Prefabs/UI/InventorySlot");
        string[] hotkeys = { "Q", "W", "E", "R" };

        for (int i = 0; i < 4; i++)
        {
            int index = i;
            GameObject slot;

            if (slotPrefab != null)
            {
                slot = Instantiate(slotPrefab, container, false);
                slot.name = $"Slot_{i}";
            }
            else
            {
                slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(container, false);
                slot.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            Image bg = slot.GetComponent<Image>();
            slotBackgrounds.Add(bg);

            InventorySlotHandler handler = slot.GetComponent<InventorySlotHandler>();
            if (handler == null) handler = slot.AddComponent<InventorySlotHandler>();
            handler.slotIndex = index;
            slotHandlers.Add(handler);

            // Icon is handled by handler, but we need reference for color tinting if needed,
            // or just rely on handler SetMask. We used to keep list of Icons.
            // Handler has 'iconImage' field now. We can assume handler manages the icon sprite.
            // But UpdateInventoryUI logic in previous version did manual coloring.

            // Highlight
            Transform hlTr = slot.transform.Find("Highlight");
            if (hlTr != null)
            {
                inventoryHighlights.Add(hlTr.gameObject);
                hlTr.gameObject.SetActive(false);
            }

            // Hotkey Text
            Transform hkTr = slot.transform.Find("Hotkey");
            if (hkTr) slotHotkeys.Add(hkTr.GetComponent<TextMeshProUGUI>());

            // Cost Text
            Transform costTr = slot.transform.Find("Cost");
            if (costTr) slotCosts.Add(costTr.GetComponent<TextMeshProUGUI>());
        }
    }

    public void UpdateInventoryUI()
    {
        if (GameManager.Instance == null) return;
        var inv = GameManager.Instance.inventory;
        int equipped = GameManager.Instance.equippedMaskIndex;
        float currentStamina = GameManager.Instance.player != null ? GameManager.Instance.player.currentStamina : 0;

        for (int i = 0; i < 4; i++)
        {
            if (i >= slotHandlers.Count) break;

            var handler = slotHandlers[i];

            if (i < inv.Count)
            {
                var mask = inv[i];
                handler.SetMask(mask); // Load Icon

                if (i < slotCosts.Count) slotCosts[i].text = mask.staminaCost.ToString();

                // Visual feedback for stamina/equipped
                bool canEquip = currentStamina >= mask.staminaCost || i == equipped;
                Color baseBgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                Color darkenedBgColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);

                if (i < slotBackgrounds.Count)
                    slotBackgrounds[i].color = canEquip ? baseBgColor : darkenedBgColor;

                // If not equippable, maybe tint the icon too?
                if (!canEquip && handler.iconImage != null)
                {
                    handler.iconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
                else if (handler.iconImage != null)
                {
                    // If sprite is loaded, white. If no sprite (color fallback), mask color.
                    if (handler.iconImage.sprite != null)
                        handler.iconImage.color = Color.white;
                    else
                        handler.iconImage.color = mask.color;
                }

                handler.gameObject.SetActive(true);
            }
            else
            {
                handler.SetMask(null);
                handler.gameObject.SetActive(false); // Hide empty slots? Or just content?
                // Originally we hid the whole slot or parts.
                // "inventorySlots[i].gameObject.SetActive(false)" was hiding the icon.
                // But the slot background remained?
                // "inventorySlots[i]" was the Icon Image in previous code.

                // Let's keep the slot visible but empty
                handler.gameObject.SetActive(true);
                if (i < slotBackgrounds.Count) slotBackgrounds[i].color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                if (i < slotCosts.Count) slotCosts[i].text = "";
            }

            if (i < inventoryHighlights.Count)
                inventoryHighlights[i].SetActive(i == equipped && i < inv.Count);
        }
    }

    public void UpdatePlayerStatsUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null) return;
        if (combatUI == null) return;

        Unit player = GameManager.Instance.player;
        combatUI.UpdateStats(
            player.currentHealth,
            player.maxHealth,
            player.finalAtkPower,
            player.finalAtkInterval,
            player.finalRange,
            player.finalDef,
            player.moveSpeed,
            player.finalKnockback
        );
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
        }
        if (detailPanel)
        {
            detailPanel.SetActive(false);
            detailPanel.transform.SetAsLastSibling();
            Transform nameTr = detailPanel.transform.Find("Name");
            if (nameTr) detailName = nameTr.GetComponent<TextMeshProUGUI>();
            Transform descTr = detailPanel.transform.Find("Desc");
            if (descTr) detailDesc = descTr.GetComponent<TextMeshProUGUI>();
        }
    }

    public void ShowMaskDetail(MaskData mask)
    {
        if (detailPanel == null) return;
        detailPanel.SetActive(true);
        detailPanel.transform.SetAsLastSibling();
        if (detailName) detailName.text = mask.name;
        if (detailDesc) detailDesc.text = $"{mask.description}\n\n" +
                          $"<color=#FFFF00>EQUIP STATS</color>\n" +
                          $"Atk: {mask.equipAtk}, Interval: {mask.equipInterval}s, Def: {mask.equipDef}%\n" +
                          $"Range: {mask.equipRange}, Knockback: {mask.equipKnockback}\n" +
                          $"<color=#00FFFF>PASSIVE STATS (Owned)</color>\n" +
                          $"HP: {mask.passiveHP}, AtkEff: {mask.passiveAtkEff}%, Speed: {mask.passiveSpeed}%\n" +
                          $"Def: {mask.passiveDef}%, Range: {mask.passiveRange}";
    }

    public void HideMaskDetail()
    {
        if (detailPanel) detailPanel.SetActive(false);
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
        }
        if (rewardPanel)
        {
            rewardPanel.SetActive(false);
            Transform containerTr = rewardPanel.transform.Find("Container");
            if (containerTr) rewardContainer = containerTr;
        }
    }

    public async UniTask<int> ShowRewardSelection(List<RewardOption> options)
    {
        if (rewardPanel == null) return -1;
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
        else return;

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

        Transform descTr = card.transform.Find("Desc");
        if (descTr) descTr.GetComponent<TextMeshProUGUI>().text = descText;

        Transform iconTr = card.transform.Find("Icon");
        if (iconTr)
        {
            var img = iconTr.GetComponent<Image>();
            if (img) img.color = visualColor;
        }
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
        }
        if (replacePanel)
        {
            replacePanel.SetActive(false);
            Transform containerTr = replacePanel.transform.Find("Container");
            if (containerTr) replaceContainer = containerTr;

            Transform cancelBtnTr = replacePanel.transform.Find("CancelBtn");
            if (cancelBtnTr)
            {
                Button cancelBtn = cancelBtnTr.GetComponent<Button>();
                if (cancelBtn)
                {
                    cancelBtn.onClick.RemoveAllListeners();
                    cancelBtn.onClick.AddListener(() => selectedReplaceIndex = -2);
                }
            }
        }
    }

    public async UniTask<int> ShowReplaceMaskPopup(MaskData newMask)
    {
        if (replacePanel == null) return -1;
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

            GameObject txtObj = new GameObject("Name");
            txtObj.transform.SetParent(slot.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = inv[i].name;
            txt.fontSize = 20;
            txt.alignment = TextAlignmentOptions.Center;
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
            // Simple creation if prefab missing logic skipped for brevity as we have generator
        }
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    void SetupGameClearPanel()
    {
        if (gameClearPanel == null)
        {
            // Simple creation if prefab missing logic skipped
        }
        if (gameClearPanel) gameClearPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }
    }

    public void ShowGameClear()
    {
        if (gameClearPanel)
        {
            gameClearPanel.SetActive(true);
            gameClearPanel.transform.SetAsLastSibling();
        }
    }

    void SetupFPSDisplay()
    {
        GameObject fpsObj = new GameObject("FPSDisplay");
        fpsObj.transform.SetParent(mainCanvas.transform, false);
        fpsObj.transform.localPosition = new Vector3(-300, 100, 0);
        fpsDisplay = fpsObj.AddComponent<FPSDisplay>();
    }
}
