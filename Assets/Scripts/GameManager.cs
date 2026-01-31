using UnityEngine;
using UnityEngine.SceneManagement;
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

        if (FindFirstObjectByType<GameManager>() == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Managers/GameManager");
            if (prefab != null)
            {
                Instantiate(prefab).name = "GameManager";
            }
            else
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }
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
            GameObject camPrefab = Resources.Load<GameObject>("Prefabs/Environment/MainCamera");
            if (camPrefab != null)
            {
                GameObject camObj = Instantiate(camPrefab);
                camObj.name = "Main Camera";
                mainCam = camObj.GetComponent<Camera>();
            }
            else
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                mainCam.tag = "MainCamera";
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5f;
                camObj.transform.position = new Vector3(0, 0, -10);
            }
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
        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightPrefab = Resources.Load<GameObject>("Prefabs/Environment/DirectionalLight");
            if (lightPrefab != null)
            {
                Instantiate(lightPrefab).name = "Light";
            }
            else
            {
                GameObject lightObj = new GameObject("Light");
                Light l = lightObj.AddComponent<Light>();
                l.type = LightType.Directional;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        // Environment
        GameObject groundPrefab = Resources.Load<GameObject>("Prefabs/Environment/Ground");
        GameObject ground;
        if (groundPrefab != null)
        {
            ground = Instantiate(groundPrefab);
            ground.name = "Ground";
            // Ensure position/scale if prefab doesn't have it set correctly?
            // Let's assume prefab is correct, but force position for consistency with code logic if needed.
            // ground.transform.position = new Vector3(0, -5.5f, 0);
        }
        else
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -5.5f, 0);
            ground.transform.localScale = new Vector3(1000, 10, 10);
            var groundRend = ground.GetComponent<Renderer>();
            if (groundRend) groundRend.material.color = new Color(0.2f, 0.6f, 0.2f);
        }

        CreateBackgroundProps();

        // UIManager
        if (uiManager == null) // Check if assigned via Inspector (if GameManager was prefab)
        {
            GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/Managers/UIManager");
            if (uiPrefab != null)
            {
                GameObject uiObj = Instantiate(uiPrefab);
                uiObj.name = "UIManager";
                uiManager = uiObj.GetComponent<UIManager>();
                if (uiManager == null) uiManager = uiObj.AddComponent<UIManager>();
            }
            else
            {
                GameObject uiObj = new GameObject("UIManager");
                uiManager = uiObj.AddComponent<UIManager>();
            }
        }

        // Create Player
        if (player == null) // Check if assigned via Inspector
        {
            GameObject pPrefab = Resources.Load<GameObject>("Prefabs/Units/Player");
            GameObject pObj;
            if (pPrefab != null)
            {
                pObj = Instantiate(pPrefab);
                pObj.name = "Player";
            }
            else
            {
                pObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pObj.name = "Player";
            }

            player = pObj.GetComponent<Unit>();
            if(player == null) player = pObj.AddComponent<Unit>();

            // Initialize will be called after equipping mask
            player.transform.position = Vector3.zero;
        }
    }

    void CreateBackgroundProps()
    {
        GameObject treePrefab = Resources.Load<GameObject>("Prefabs/Environment/Tree");

        for(int i = 0; i < 20; i++)
        {
            float xPos = Random.Range(-10f, 100f);
            float zPos = Random.Range(5f, 15f);

            if (treePrefab != null)
            {
                Instantiate(treePrefab, new Vector3(xPos, 0, zPos), Quaternion.identity);
            }
            else
            {
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

                // Parent leaves to trunk for cleaner hierarchy if programmatic
                leaves.transform.SetParent(trunk.transform, true);
            }
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

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
                // No more stages?
                Debug.Log("No more stages defined.");
                currentState = GameState.End;
                uiManager.ShowGameClear();
                return; // Stop the loop
            }

            bool isBoss = (stageCount == 4); // Hardcoded for now based on prompt, or check StageData
            await BattlePhase(stageData);

            if (player.currentHealth <= 0)
            {
                Debug.Log("Game Over");
                currentState = GameState.End;
                uiManager.ShowGameOver();
                return; // Stop the loop
            }

            if (isBoss)
            {
                 currentState = GameState.End;
                 uiManager.ShowGameClear();
                 return; // Stop the loop
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

            GameObject eObj;
            // Try specific monster prefab first
            GameObject monPrefab = Resources.Load<GameObject>($"Prefabs/Units/{monId}");
            if (monPrefab == null) monPrefab = Resources.Load<GameObject>("Prefabs/Units/Monster");

            if (monPrefab != null)
            {
                eObj = Instantiate(monPrefab, spawnPos, Quaternion.identity);
                eObj.name = mData.name;
            }
            else
            {
                eObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eObj.name = mData.name;
                eObj.transform.position = spawnPos;
            }

            enemy = eObj.GetComponent<Unit>();
            if (enemy == null) enemy = eObj.AddComponent<Unit>();
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

        // Pre-check for unowned masks to prevent duplicates and handle empty pool
        List<string> ownedMaskIds = inventory.ConvertAll(m => m.id);
        List<MaskData> unownedMasks = GameData.allMasks.FindAll(m => !ownedMaskIds.Contains(m.id));

        int emptySlots = maxInventorySize - inventory.Count;
        float baseNewMaskChance = (unownedMasks.Count > 0) ? (15f + (15f * emptySlots)) : 0f;

        // Tracking masks picked in this reward phase to avoid duplicates among the 3 options
        List<string> pickedInThisPhaseIds = new List<string>();

        for(int i=0; i<3; i++)
        {
            float roll = Random.Range(0f, 100f);
            RewardOption opt = new RewardOption();

            // Filter out masks already picked for other slots in this phase
            List<MaskData> availableMasks = unownedMasks.FindAll(m => !pickedInThisPhaseIds.Contains(m.id));

            // If no more unique masks available, set chance to 0
            float currentNewMaskChance = (availableMasks.Count > 0) ? baseNewMaskChance : 0f;
            float currentUpgradeChance = (100f - currentNewMaskChance) * 0.7f;

            if (roll < currentNewMaskChance)
            {
                opt.type = RewardType.NewMask;
                MaskData picked = availableMasks[Random.Range(0, availableMasks.Count)];
                opt.maskData = picked.Copy();
                opt.description = "New Mask";
                pickedInThisPhaseIds.Add(picked.id);
            }
            else if (roll < currentNewMaskChance + currentUpgradeChance)
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
