using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Unit player;
    private Unit enemy;
    private Camera mainCam;
    public UIManager uiManager;
    private TextMeshPro gameClearText;

    // States
    public enum GameState { Move, Battle, Reward, End }
    public GameState currentState;

    public int stageCount = 1;
    private const int bossStage = 4; // Should be dynamic based on StageData count ideally

    // Inventory
    public List<MaskData> inventory = new List<MaskData>();
    public int maxInventorySize = 4;
    public int equippedMaskIndex = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeGame()
    {
        GameData.Initialize();

        if (FindObjectOfType<GameManager>() == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupScene();

        // Give default mask
        MaskData defaultMask = GameData.allMasks.Count > 0 ? GameData.allMasks[0].Copy() : new MaskData();
        AddMaskToInventory(defaultMask);
        EquipMask(0);

        GameLoop().Forget();
    }

    void SetupScene()
    {
        // Camera
        mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            mainCam.tag = "MainCamera";
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
        }
        else
        {
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
        }

        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.53f, 0.8f, 0.92f);

        // Light
        if (FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Light");
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // Environment
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -5.5f, 0);
        ground.transform.localScale = new Vector3(1000, 10, 10);
        var groundRend = ground.GetComponent<Renderer>();
        if(groundRend) groundRend.material.color = new Color(0.2f, 0.6f, 0.2f);

        CreateBackgroundProps();

        // UIManager
        GameObject uiObj = new GameObject("UIManager");
        uiManager = uiObj.AddComponent<UIManager>();

        // Create Player
        GameObject pObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pObj.name = "Player";
        player = pObj.AddComponent<Unit>();
        // Initialize will be called after equipping mask
        player.transform.position = Vector3.zero;

        // Game Clear Text
        GameObject gcObj = new GameObject("GameClearText");
        gcObj.transform.SetParent(mainCam.transform);
        gcObj.transform.localPosition = new Vector3(0, 0, 10);
        gameClearText = gcObj.AddComponent<TextMeshPro>();
        gameClearText.text = "GAME CLEAR";
        gameClearText.fontSize = 12;
        gameClearText.alignment = TextAlignmentOptions.Center;
        gameClearText.color = Color.white;
        gcObj.SetActive(false);
    }

    void CreateBackgroundProps()
    {
        for(int i = 0; i < 20; i++)
        {
            float xPos = Random.Range(-10f, 100f);
            float zPos = Random.Range(5f, 15f);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "TreeTrunk";
            trunk.transform.position = new Vector3(xPos, 0, zPos);
            trunk.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
            trunk.GetComponent<Renderer>().material.color = new Color(0.4f, 0.2f, 0.1f);

            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "TreeLeaves";
            leaves.transform.position = new Vector3(xPos, 2.5f, zPos);
            leaves.transform.localScale = Vector3.one * 2.5f;
            leaves.GetComponent<Renderer>().material.color = new Color(0.1f, 0.5f, 0.1f);
        }
    }

    // --- Inventory System ---

    public void EquipMask(int index)
    {
        if (index < 0 || index >= inventory.Count) return;

        equippedMaskIndex = index;
        MaskData mask = inventory[index];
        player.InitializePlayer(mask);

        Debug.Log($"Equipped Mask: {mask.name}");

        // Update UI if needed (via UIManager)
        if(uiManager != null) uiManager.UpdateInventoryUI();
    }

    public bool AddMaskToInventory(MaskData mask)
    {
        if (inventory.Count < maxInventorySize)
        {
            inventory.Add(mask);
            if(uiManager != null) uiManager.UpdateInventoryUI();
            return true;
        }
        return false;
    }

    public void ReplaceMaskInInventory(int index, MaskData newMask)
    {
        if (index < 0 || index >= inventory.Count) return;
        inventory[index] = newMask;
        if (index == equippedMaskIndex)
        {
            EquipMask(index); // Re-equip new one
        }
        if(uiManager != null) uiManager.UpdateInventoryUI();
    }

    public void RemoveMask(int index)
    {
        if (index < 0 || index >= inventory.Count) return;
        inventory.RemoveAt(index);
        if (equippedMaskIndex == index)
        {
            // Equipped mask removed?
            equippedMaskIndex = -1;
            // Fallback to 0 if exists
            if (inventory.Count > 0) EquipMask(0);
        }
        else if (equippedMaskIndex > index)
        {
            equippedMaskIndex--;
        }
        if(uiManager != null) uiManager.UpdateInventoryUI();
    }

    // --- Game Loop ---

    async UniTaskVoid GameLoop()
    {
        while (true)
        {
            await MovePhase();

            // Check Stage Data
            StageData stageData = GameData.GetStage(stageCount);
            if (stageData == null)
            {
                // No more stages? Game Over or Loop?
                // For now, let's just loop or show clear
                Debug.Log("No more stages defined.");
                currentState = GameState.End;
                gameClearText.gameObject.SetActive(true);
                break;
            }

            bool isBoss = (stageCount == 4); // Hardcoded for now based on prompt, or check StageData
            await BattlePhase(stageData);

            if (player.currentHealth <= 0)
            {
                Debug.Log("Game Over");
                currentState = GameState.End;
                break;
            }

            if (isBoss)
            {
                 currentState = GameState.End;
                 gameClearText.gameObject.SetActive(true);
                 gameClearText.transform.DOScale(1.5f, 1f).SetLoops(-1, LoopType.Yoyo);
                 await UniTask.Delay(5000);
                 break;
            }

            await RewardPhase();
            stageCount++;
        }
    }

    async UniTask MovePhase()
    {
        currentState = GameState.Move;
        Debug.Log("Starting Move Phase");

        float screenHeight = mainCam.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCam.aspect;
        float totalDist = screenWidth * 2f;
        float duration = 4f;

        player.isMovingScenario = true;

        float startX = player.transform.position.x;
        float targetX = startX + totalDist;

        await player.transform.DOMoveX(targetX, duration).SetEase(Ease.Linear).AsyncWaitForCompletion();

        player.isMovingScenario = false;
        player.state = UnitState.Idle;
    }

    void LateUpdate()
    {
        if (player != null && mainCam != null)
        {
            Vector3 camPos = mainCam.transform.position;
            camPos.x = player.transform.position.x;
            mainCam.transform.position = camPos;
        }
    }

    async UniTask BattlePhase(StageData stageData)
    {
        currentState = GameState.Battle;
        Debug.Log($"Starting Battle Phase (Stage {stageCount})");

        foreach (string monId in stageData.monsterIds)
        {
            MonsterData mData = GameData.GetMonster(monId);
            if (mData == null) continue;

            // Spawn
            float screenHeight = mainCam.orthographicSize * 2f;
            float screenWidth = screenHeight * mainCam.aspect;
            Vector3 camPos = mainCam.transform.position;
            Vector3 spawnPos = new Vector3(camPos.x + screenWidth / 2f + 2f, 0, 0);

            GameObject eObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eObj.name = mData.name;
            eObj.transform.position = spawnPos;
            enemy = eObj.AddComponent<Unit>();
            enemy.InitializeMonster(mData);

            player.target = enemy;
            enemy.target = player;

            // Move enemy into view? The prompt says "Enemy enters screen".
            // Currently spawnPos is offscreen.
            // Existing logic: "Enemy enters screen" (Move 3x dist?).
            // User Design: "Enemy enters screen... Player Stops... Combat".
            // My Unit code has Move behavior if target is far. So Enemy will move to Player.
            // Player will move to Enemy. They meet and fight.

            // Wait for death
            await UniTask.WaitUntil(() => player.currentHealth <= 0 || enemy.currentHealth <= 0);

            if (player.currentHealth <= 0) return; // Lose

            enemy.state = UnitState.Die;
            player.target = null;
            await UniTask.Delay(1000);
            if (enemy != null) Destroy(enemy.gameObject);
        }
    }

    async UniTask RewardPhase()
    {
        currentState = GameState.Reward;
        Debug.Log("Starting Reward Phase");

        // Generate 3 Options
        List<RewardOption> options = new List<RewardOption>();
        int emptySlots = maxInventorySize - inventory.Count;
        float newMaskChance = 15f + (15f * emptySlots);

        // Probabilities for the remaining percent
        // If NewMask = 30%, Remainder = 70%.
        // Upgrade = 70 * 0.7 = 49%
        // Stat = 70 * 0.3 = 21%

        float upgradeChance = (100f - newMaskChance) * 0.7f;

        // Loop 3 times to generate 3 independent cards
        for(int i=0; i<3; i++)
        {
            float roll = Random.Range(0f, 100f);
            RewardOption opt = new RewardOption();

            if (roll < newMaskChance)
            {
                opt.type = RewardType.NewMask;
                opt.maskData = GameData.GetRandomMask().Copy();
                opt.description = "New Mask";
            }
            else if (roll < newMaskChance + upgradeChance)
            {
                opt.type = RewardType.UpgradeMask;
                opt.description = "Upgrade Equipped Mask";
            }
            else
            {
                opt.type = RewardType.StatBoost;
                opt.statData = GameData.GetRandomStatReward();
                opt.description = opt.statData.name;
            }
            options.Add(opt);
        }

        int selectedIndex = await uiManager.ShowRewardSelection(options);

        if (selectedIndex < 0 || selectedIndex >= options.Count)
        {
            // Default to 0 or skip? Should not happen if UI locks.
            selectedIndex = 0;
        }

        RewardOption choice = options[selectedIndex];

        if (choice.type == RewardType.NewMask)
        {
            bool added = AddMaskToInventory(choice.maskData);
            if (!added)
            {
                Debug.Log("Inventory Full - Showing Replace UI");
                int replaceIdx = await uiManager.ShowReplaceMaskPopup(choice.maskData);

                if (replaceIdx >= 0) // Valid slot selected
                {
                    ReplaceMaskInInventory(replaceIdx, choice.maskData);
                    Debug.Log($"Replaced Mask at slot {replaceIdx}");
                }
                else
                {
                    Debug.Log("Discarded New Mask");
                }
            }
            else
            {
                 Debug.Log($"Added {choice.maskData.name} to Inventory");
            }
        }
        else
        {
            ApplyReward(choice);
        }
    }

    void ApplyReward(RewardOption choice)
    {
        if (choice.type == RewardType.UpgradeMask)
        {
            if (equippedMaskIndex >= 0 && equippedMaskIndex < inventory.Count)
            {
                // Upgrade currently equipped mask
                MaskData m = inventory[equippedMaskIndex];
                m.atkBonus += 2f;
                m.hpBonus += 10f;
                player.ApplyMask(m); // Re-apply to update stats
                Debug.Log("Upgraded Equipped Mask");
            }
        }
        else if (choice.type == RewardType.StatBoost)
        {
            // Apply Stat Boost Effects
            if (choice.statData != null)
            {
                foreach(var kvp in choice.statData.effects)
                {
                    player.ApplyStatBoost(kvp.Key, kvp.Value);
                }
                Debug.Log($"Applied Stat Boost: {choice.statData.name}");
            }
        }
    }
}
