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
        if (detailDesc)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(mask.description);
            sb.Append("\n\n<color=#FFFF00>EQUIP STATS</color>\n");

            var equipStats = new List<string>();
            if (mask.equipAtk != 0) equipStats.Add($"Atk: {mask.equipAtk}");
            if (mask.equipInterval != 0) equipStats.Add($"Interval: {mask.equipInterval}s");
            if (mask.equipDef != 0) equipStats.Add($"Def: {mask.equipDef}%");
            if (mask.equipRange != 0) equipStats.Add($"Range: {mask.equipRange}");
            if (mask.equipKnockback != 0) equipStats.Add($"Knockback: {mask.equipKnockback}");
            sb.Append(string.Join(", ", equipStats));

            sb.Append("\n<color=#00FFFF>PASSIVE STATS (Owned)</color>\n");

            var passiveStats = new List<string>();
            if (mask.passiveHP != 0) passiveStats.Add($"HP: {mask.passiveHP}");
            if (mask.passiveAtkEff != 0) passiveStats.Add($"AtkEff: {mask.passiveAtkEff}%");
            if (mask.passiveSpeed != 0) passiveStats.Add($"Speed: {mask.passiveSpeed}%");
            if (mask.passiveDef != 0) passiveStats.Add($"Def: {mask.passiveDef}%");
            if (mask.passiveRange != 0) passiveStats.Add($"Range: {mask.passiveRange}");
            sb.Append(string.Join(", ", passiveStats));

            detailDesc.text = sb.ToString();
        }
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

        RewardCard rewardCard = card.GetComponent<RewardCard>();
        if (rewardCard != null)
        {
            rewardCard.SetData(opt);
        }
    }

    private string GetStatLine(string key, float val, string unit, bool showPlus)
    {
        if(val == 0)
        {
            return "";
        }

        string iconTag = GetStatIconTag(key);
        string sign = (showPlus && val > 0) ? "+" : "";
        string formattedVal = "";

        if (key.ToLower() == "cooldown") formattedVal = val.ToString("F2");
        else if (key.ToLower() == "range" || key.ToLower() == "kb" || key.ToLower() == "move") formattedVal = val.ToString("F1");
        else formattedVal = val.ToString("F0");

        // Apply % unit for efficiency/accel if not provided
        if (string.IsNullOrEmpty(unit))
        {
            if (key.ToLower() == "atk" || key.ToLower() == "speed" || key.ToLower() == "def") unit = "%";
        }

        return $"{iconTag} {sign}{formattedVal}{unit}\n";
    }

    private string GetStatListForMask(MaskData m)
    {
        string s = "";
        s += GetStatLine("hp", m.passiveHP, "", false);
        s += GetStatLine("atk", m.passiveAtkEff, "%", false);
        s += GetStatLine("speed", m.passiveAtkSpeedAccel, "%", false);
        s += GetStatLine("def", m.passiveDef, "%", false);
        s += GetStatLine("range", m.passiveRange, "", false);
        s += GetStatLine("move", m.passiveSpeed, "", false);
        return s;
    }

    private string GetStatUpgradeList(MaskData initial, int currLvl, int nextLvl)
    {
        string s = "";
        float currMult = 1f + (currLvl - 1) * 0.5f;
        float nextMult = 1f + (nextLvl - 1) * 0.5f;

        s += GetStatUpgradeLine("hp", initial.passiveHP, currMult, nextMult, "");
        s += GetStatUpgradeLine("atk", initial.passiveAtkEff, currMult, nextMult, "%");
        s += GetStatUpgradeLine("speed", initial.passiveAtkSpeedAccel, currMult, nextMult, "%");
        s += GetStatUpgradeLine("def", initial.passiveDef, currMult, nextMult, "%");
        s += GetStatUpgradeLine("range", initial.passiveRange, currMult, nextMult, "");
        s += GetStatUpgradeLine("move", initial.passiveSpeed, currMult, nextMult, "");

        return s;
    }

    private string GetStatUpgradeLine(string key, float baseVal, float cMult, float nMult, string unit)
    {
        if(baseVal == 0)
        {
            return "";
        }

        string iconTag = GetStatIconTag(key);
        float curr = baseVal * cMult;
        float next = baseVal * nMult;

        string fmt = (key.ToLower() == "range" || key.ToLower() == "move") ? "F1" : "F0";
        return $"{iconTag} {curr.ToString(fmt)}{unit} -> <color=#00FF00>{next.ToString(fmt)}{unit}</color>\n";
    }

    private string GetStatIconTag(string key)
    {
        string spriteName = "";
        switch (key.ToLower())
        {
            case "hp": spriteName = "health icon"; break;
            case "atk": spriteName = "attack icon"; break;
            case "baseatk": spriteName = "attack icon"; break;
            case "speed": spriteName = "attack cooltime icon"; break;
            case "cooldown": spriteName = "attack cooltime icon"; break;
            case "def": spriteName = "defense icon"; break;
            case "range": spriteName = "attack range icon"; break;
            case "kb": spriteName = "knockback icon"; break;
            case "move": spriteName = "movement speed icon"; break;
            default: spriteName = "IconName"; break;
        }
        return $"<sprite name=\"{spriteName}\">";
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
}
