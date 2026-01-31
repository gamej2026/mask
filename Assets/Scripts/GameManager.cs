using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action<int> OnStageStart;

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
    private UnitData playerBaseData;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeGame()
    {
        GameData.Initialize();
        SoundManager.Initialize();

        // Ensure GameManager exists on first load and subscribe to scene reloads
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureGameManagerExists();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-initialize game data if needed and ensure manager exists
        GameData.Initialize();
        EnsureGameManagerExists();
    }

    static void EnsureGameManagerExists()
    {
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

        playerBaseData = GameData.GetUnit("PlayerCharacter");

        // 먼저 플레이어 스탯을 초기화 (스태미나 포함)
        player.InitializePlayer(playerBaseData, new List<MaskData>(), -1);

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
            if (player == null) player = pObj.AddComponent<Unit>();

            // Initialize will be called after equipping mask
            player.transform.position = new Vector3(0, GameOption.Instance.spawnHeight, 0);
        }
    }

    // --- Inventory System ---

    public void EquipMask(int index)
    {
        if (index < 0 || index >= inventory.Count) return;
        if (equippedMaskIndex == index) return; // Already equipped

        MaskData targetMask = inventory[index];
        if (player.currentStamina < targetMask.staminaCost)
        {
            Debug.Log($"Not enough stamina to equip {targetMask.name}");
            return;
        }

        player.currentStamina -= targetMask.staminaCost;
        equippedMaskIndex = index;
        player.InitializePlayer(playerBaseData, inventory, equippedMaskIndex);

        Debug.Log($"Equipped Mask: {targetMask.name}. Remaining Stamina: {player.currentStamina}");

        // Update UI if needed (via UIManager)
        if (uiManager != null) uiManager.UpdateInventoryUI();
    }

    public bool AddMaskToInventory(MaskData mask)
    {
        // Check for duplicate
        MaskData existing = inventory.Find(m => m.id == mask.id);
        if (existing != null)
        {
            UpgradeMask(existing);

            // If this is the equipped mask, update player stats
            int index = inventory.IndexOf(existing);
            if (index == equippedMaskIndex)
            {
                 player.InitializePlayer(playerBaseData, inventory, equippedMaskIndex);
            }

            if (uiManager != null) uiManager.UpdateInventoryUI();
            Debug.Log($"Duplicate mask {mask.name} found. Upgraded to Lv.{existing.level}");
            return true;
        }

        if (inventory.Count < maxInventorySize)
        {
            inventory.Add(mask);
            if (uiManager != null) uiManager.UpdateInventoryUI();
            return true;
        }
        return false;
    }

    public void UpgradeMask(MaskData m)
    {
        m.level++;

        // Passive stats formula: InitialStat * (1 + (Level - 1) * 0.5)
        MaskData initial = GameData.GetMask(m.id);
        if (initial != null)
        {
            float multiplier = 1f + (m.level - 1) * 0.5f;
            m.passiveHP = initial.passiveHP * multiplier;
            m.passiveDef = initial.passiveDef * multiplier;
            m.passiveSpeed = initial.passiveSpeed * multiplier;
            m.passiveAtkEff = initial.passiveAtkEff * multiplier;
            m.passiveAtkSpeedAccel = initial.passiveAtkSpeedAccel * multiplier;
            m.passiveRange = initial.passiveRange * multiplier;
            m.passiveStamina = initial.passiveStamina * multiplier;
        }
    }

    public void ReplaceMaskInInventory(int index, MaskData newMask)
    {
        if (index < 0 || index >= inventory.Count) return;
        inventory[index] = newMask;
        if (index == equippedMaskIndex)
        {
            EquipMask(index); // Re-equip new one
        }
        if (uiManager != null) uiManager.UpdateInventoryUI();
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
        if (uiManager != null) uiManager.UpdateInventoryUI();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TransitionTest(int transitionTestStageLevel)
    {
        OnStageStart?.Invoke(transitionTestStageLevel);
    }

    // --- Game Loop ---

    async UniTaskVoid GameLoop()
    {
        while (true)
        {
            OnStageStart?.Invoke(stageCount);

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

            // 스테이지 BGM 재생
            if (!string.IsNullOrEmpty(stageData.bgm) && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBGM(stageData.bgm);
            }

            await MovePhase();

            bool isBoss = (stageCount == 4); // Hardcoded for now based on prompt, or check StageData
            await BattlePhase(stageData);

            if (player.currentHealth <= 0)
            {
                SoundManager.Instance.PlaySFX("game over sound"); // 게임오버 효과음
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

            // Spawn Reward Box at last enemy position
            if (enemy != null)
            {
                await WaitForBoxClick(enemy.transform.position);
            }

            await RewardPhase();
            stageCount++;
        }
    }

    async UniTask WaitForBoxClick(Vector3 position)
    {
        SoundManager.Instance.PlaySFX("reward box drop sound"); // 보상 드랍 효과음
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "RewardBox";
        box.transform.position = position;
        box.transform.localScale = Vector3.one * 0.8f;
        var rend = box.GetComponent<Renderer>();
        if (rend) rend.material.color = new Color(0.8f, 0.5f, 0.2f); // Wood color

        // Add a simple float effect
        box.transform.DOMoveY(position.y + 0.5f, 1f).SetLoops(-1, LoopType.Yoyo);

        bool clicked = false;
        while (!clicked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                // Use RaycastAll to be sure we hit the box in 2D/3D mix
                RaycastHit[] hits = Physics.RaycastAll(ray);
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject == box)
                    {
                        clicked = true;
                        break;
                    }
                }
            }
            await UniTask.Yield();
        }

        SoundManager.Instance.PlaySFX("reward gain sound"); // 보상 수령 효과음

        box.transform.DOKill();
        Destroy(box);
    }

    async UniTask MovePhase()
    {
        currentState = GameState.Move;
        Debug.Log("Starting Move Phase");

        float screenHeight = mainCam.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCam.aspect;
        // Characters move 3 times the screen width in 5 seconds
        float totalDist = screenWidth * 3f;
        float duration = 5f;

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
        
        SoundManager.Instance.PlaySFX("monster encounter sound"); // 몬스터 조우 시 효과음
        foreach (string monId in stageData.monsterIds)
        {
            string currentMonId = monId;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(GameOption.Instance.forceSpawnMonsterID))
            {
                currentMonId = GameOption.Instance.forceSpawnMonsterID;
                GameOption.Instance.forceSpawnMonsterID = ""; // Reset
            }
#endif

            UnitData uData = GameData.GetUnit(currentMonId);
            if (uData == null)
            {
                Debug.LogError($"Monster ID {currentMonId} not found in GameData.");
                continue;
            }
            // Spawn
            float screenHeight = mainCam.orthographicSize * 2f;
            float screenWidth = screenHeight * mainCam.aspect;
            Vector3 camPos = mainCam.transform.position;
            Vector3 spawnPos = new Vector3(camPos.x + screenWidth / 2f + 2f, GameOption.Instance.spawnHeight, 0);

            GameObject eObj;
            // Try specific monster prefab from Data path, fallback to old logic
            GameObject monPrefab = null;
            if (!string.IsNullOrEmpty(uData.prefabPath))
            {
                monPrefab = Resources.Load<GameObject>(uData.prefabPath);
            } 
            Debug.LogWarning($"Spawning Monster: {currentMonId} at {monPrefab}");


            if (monPrefab == null) monPrefab = Resources.Load<GameObject>($"Prefabs/Units/{currentMonId}");
            if (monPrefab == null) monPrefab = Resources.Load<GameObject>("Prefabs/Units/Monster");

            if (monPrefab != null)
            {
                eObj = Instantiate(monPrefab, spawnPos, Quaternion.identity);
                eObj.name = uData.name;
            }
            else
            {
                eObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eObj.name = uData.name;
                eObj.transform.position = spawnPos;
            }

            enemy = eObj.GetComponent<Unit>();
            if (enemy == null) enemy = eObj.AddComponent<Unit>();
            enemy.InitializeMonster(uData);

            player.target = enemy;
            enemy.target = player;

            // 전투 타이머 시작
            float battleStartTime = Time.time;
            const float battleTimeLimit = 50f;
            float lastBuffTime = 0f;
            
            // 복리 계산을 위한 초기값 저장
            float initialAtkEff = enemy.baseAtkEff;
            float initialMoveSpeed = enemy.baseMoveSpeed;

            // 타이머 UI 표시
            if (uiManager != null) uiManager.ShowBattleTimer(true);

            // Wait for death with overtime buff logic
            while (player.currentHealth > 0 && enemy.currentHealth > 0)
            {
                float elapsed = Time.time - battleStartTime;

                // 타이머 UI 업데이트
                if (uiManager != null) uiManager.UpdateBattleTimer(elapsed, battleTimeLimit);

                // 50초 초과 시 매 초마다 적 버프 (복리)
                if (elapsed > battleTimeLimit)
                {
                    float timeSinceLimit = elapsed - battleTimeLimit;
                    int currentBuffCount = Mathf.FloorToInt(timeSinceLimit);
                    
                    if (currentBuffCount > lastBuffTime)
                    {
                        lastBuffTime = currentBuffCount;
                        
                        // 복리 계산: 초기값 * (1.01^n) - 초기값 = 보너스
                        float compoundMultiplier = Mathf.Pow(1.01f, currentBuffCount);
                        enemy.permAtkEffBonus = initialAtkEff * (compoundMultiplier - 1f);
                        enemy.permMoveSpeedBonus = initialMoveSpeed * (compoundMultiplier - 1f);
                        enemy.RecalculateStats();
                        enemy.ShowText("Power Up!", Color.red);
                        Debug.Log($"Overtime! ({currentBuffCount}s) Enemy buffed - Multiplier: x{compoundMultiplier:F3}");
                    }
                }

                await UniTask.Yield();
            }

            // 타이머 UI 숨김
            if (uiManager != null) uiManager.ShowBattleTimer(false);

            if (player.currentHealth <= 0) return; // Lose

            enemy.state = UnitState.Die;
            player.target = null;

            await UniTask.Delay(1000);
            if (enemy != null) Destroy(enemy.gameObject);
        }

        // Recover 10% of max health after the whole battle phase ends
        player.Heal(player.maxHealth * 0.1f);
    }

    private List<RewardOption> GenerateRewardOptions()
    {
        List<RewardOption> options = new List<RewardOption>();

        // We now allow owned masks to be candidates (for leveling up)
        List<MaskData> candidateMasks = GameData.allMasks;

        int emptySlots = maxInventorySize - inventory.Count;
        // Logic for chance can remain similar, but based on candidate availability
        float baseNewMaskChance = (candidateMasks.Count > 0) ? (15f + (15f * emptySlots)) : 0f;

        // Tracking picked items in this reward phase to avoid duplicates among the 3 options
        List<string> pickedMaskIds = new List<string>();
        List<string> pickedStatIds = new List<string>();
        bool upgradeAlreadyPicked = false;

        for (int i = 0; i < 3; i++)
        {
            RewardOption opt = new RewardOption();

            // Filter out masks already picked for other slots in this phase
            List<MaskData> availableMasks = candidateMasks.FindAll(m => !pickedMaskIds.Contains(m.id));

            // Calculate available reward types and their weights
            float currentNewMaskChance = (availableMasks.Count > 0) ? baseNewMaskChance : 0f;
            float currentUpgradeChance = upgradeAlreadyPicked ? 0f : (100f - currentNewMaskChance) * 0.7f;
            float currentStatBoostChance = 100f - currentNewMaskChance - currentUpgradeChance;

            // Normalize chances (in case some options are exhausted)
            float totalChance = currentNewMaskChance + currentUpgradeChance + currentStatBoostChance;
            if (totalChance <= 0f)
            {
                // Fallback: give a random stat boost
                opt.type = RewardType.StatBoost;
                opt.statData = GameData.GetRandomStatReward(pickedStatIds);
                opt.description = opt.statData != null ? opt.statData.name : "Stat Boost";
                if (opt.statData != null) pickedStatIds.Add(opt.statData.id);
                options.Add(opt);
                continue;
            }

            float roll = UnityEngine.Random.Range(0f, totalChance);

            if (roll < currentNewMaskChance)
            {
                opt.type = RewardType.NewMask;
                MaskData picked = availableMasks[Random.Range(0, availableMasks.Count)];
                opt.maskData = picked.Copy();
                opt.description = "New Mask";
                pickedMaskIds.Add(picked.id);
            }
            else if (roll < currentNewMaskChance + currentUpgradeChance)
            {
                opt.type = RewardType.UpgradeMask;
                opt.description = "Upgrade Equipped Mask";
                upgradeAlreadyPicked = true;
            }
            else
            {
                opt.type = RewardType.StatBoost;
                opt.statData = GameData.GetRandomStatReward(pickedStatIds);
                opt.description = opt.statData != null ? opt.statData.name : "Stat Boost";
                if (opt.statData != null) pickedStatIds.Add(opt.statData.id);
            }
            options.Add(opt);
        }
        return options;
    }

    async UniTask RewardPhase()
    {
        currentState = GameState.Reward;
        Debug.Log("Starting Reward Phase");

        List<RewardOption> options = GenerateRewardOptions();

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
                UpgradeMask(m);

                player.InitializePlayer(playerBaseData, inventory, equippedMaskIndex);
                Debug.Log($"Upgraded Equipped Mask to Lv.{m.level}");
            }
        }
        else if (choice.type == RewardType.StatBoost)
        {
            // Apply Stat Boost Effects
            if (choice.statData != null)
            {
                player.ApplyStatReward(choice.statData);
                Debug.Log($"Applied Stat Boost: {choice.statData.name}");
            }
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1)) HandleCheatMask(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) HandleCheatMask(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) HandleCheatMask(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) HandleCheatMask(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) HandleCheatMask(4);
#endif

        if (Input.GetKeyDown(KeyCode.Q)) EquipMask(0);
        if (Input.GetKeyDown(KeyCode.W)) EquipMask(1);
        if (Input.GetKeyDown(KeyCode.E)) EquipMask(2);
        if (Input.GetKeyDown(KeyCode.R)) EquipMask(3);

        // Update Player Stats UI
        if (player != null && uiManager != null)
        {
            uiManager.UpdatePlayerStatsUI();
        }
    }

#if UNITY_EDITOR
    void HandleCheatMask(int maskIndex)
    {
        if (maskIndex < 0 || maskIndex >= GameData.allMasks.Count)
        {
            Debug.LogWarning($"Cheat: Mask index {maskIndex} out of range.");
            return;
        }

        MaskData mask = GameData.allMasks[maskIndex].Copy();

        bool added = AddMaskToInventory(mask);
        if (added)
        {
             Debug.Log($"Cheat: Added {mask.name} to inventory.");
        }
        else
        {
             Debug.Log($"Cheat: Inventory full. Triggering replace UI for {mask.name}.");
             HandleCheatReplace(mask).Forget();
        }
    }

    async UniTaskVoid HandleCheatReplace(MaskData mask)
    {
        if (uiManager != null)
        {
            int replaceIdx = await uiManager.ShowReplaceMaskPopup(mask);
            if (replaceIdx >= 0)
            {
                 ReplaceMaskInInventory(replaceIdx, mask);
                 Debug.Log($"Cheat: Replaced mask at {replaceIdx} with {mask.name}");
            }
            else
            {
                 Debug.Log("Cheat: Replacement cancelled.");
            }
        }
    }
#endif
}
