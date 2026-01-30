using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

public class GameManager : MonoBehaviour
{
    private Unit player;
    private Unit enemy;
    private Camera mainCam;
    private UIManager uiManager;
    private TextMeshPro gameClearText;

    // States
    private enum GameState { Move, Battle, Reward, End }
    private GameState currentState;

    private int stageCount = 1;
    private const int bossStage = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeGame()
    {
        // Check if GameManager exists
        if (FindObjectOfType<GameManager>() == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }
    }

    void Start()
    {
        SetupScene();
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
        mainCam.backgroundColor = new Color(0.53f, 0.8f, 0.92f); // Sky Blue

        // Light
        if (FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Light");
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // Environment: Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -5.5f, 0);
        ground.transform.localScale = new Vector3(1000, 10, 10);
        var groundRend = ground.GetComponent<Renderer>();
        if(groundRend) groundRend.material.color = new Color(0.2f, 0.6f, 0.2f);

        // Environment: Trees
        CreateBackgroundProps();

        // UIManager
        GameObject uiObj = new GameObject("UIManager");
        uiManager = uiObj.AddComponent<UIManager>();

        // Create Player
        GameObject pObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pObj.name = "Player";
        player = pObj.AddComponent<Unit>();
        player.Initialize(Team.Player, MaskDatabase.allMasks[0]); // Default mask
        player.transform.position = Vector3.zero;

        // Create Game Clear Text
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

    async UniTaskVoid GameLoop()
    {
        while (true)
        {
            await MovePhase();

            bool isBoss = (stageCount == bossStage);
            await BattlePhase(isBoss);

            // Boss clear check
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

        float totalDist = screenWidth * 2f; // Shortened a bit for pacing
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

    async UniTask BattlePhase(bool isBoss)
    {
        currentState = GameState.Battle;
        Debug.Log($"Starting Battle Phase (Stage {stageCount})");

        float screenHeight = mainCam.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCam.aspect;

        Vector3 camPos = mainCam.transform.position;
        Vector3 spawnPos = new Vector3(camPos.x + screenWidth / 2f + 2f, 0, 0);

        GameObject eObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eObj.name = isBoss ? "FinalBoss" : "Monster";
        eObj.transform.position = spawnPos;
        enemy = eObj.AddComponent<Unit>();

        // Enemy Stats Scaling
        enemy.baseMaxHealth = 50 + (stageCount * 20);
        enemy.baseAttackPower = 5 + (stageCount * 2);
        enemy.baseMoveSpeed = 3f;

        if (isBoss)
        {
            eObj.transform.localScale = Vector3.one * 2f;
            enemy.baseMaxHealth = 500;
            enemy.baseAttackPower = 25;
            enemy.baseKnockbackDist = 3f;
        }

        enemy.Initialize(Team.Enemy); // Red

        player.target = enemy;
        enemy.target = player;

        await UniTask.WaitUntil(() => player.currentHealth <= 0 || enemy.currentHealth <= 0);

        if (enemy.currentHealth <= 0)
        {
            Debug.Log("Enemy Defeated");
            enemy.state = UnitState.Die;
            player.target = null;
        }
        else if (player.currentHealth <= 0)
        {
            Debug.Log("Player Defeated - Game Over");
            await UniTask.Yield();
            // In a real game, restart. Here we might just hang.
            // Let's just restart the loop or return.
            return;
        }

        // Wait for death animation
        await UniTask.Delay(1000);
        if (enemy != null && enemy.gameObject.activeInHierarchy) enemy.gameObject.SetActive(false);
    }

    async UniTask RewardPhase()
    {
        currentState = GameState.Reward;
        Debug.Log("Starting Reward Phase");

        // Determine Drop
        float roll = Random.Range(0f, 100f);
        RewardType type;
        MaskData maskDrop = null;

        if (roll < 15f) // 15% New Mask
        {
            type = RewardType.NewMask;
            maskDrop = MaskDatabase.GetRandomMask(); // Could implement weighted pool later
        }
        else if (roll < 50f) // 35% Upgrade
        {
            type = RewardType.UpgradeMask;
        }
        else // 50% Stat Boost
        {
            type = RewardType.StatBoost;
        }

        int selection = await uiManager.ShowRewardPopup(type, maskDrop);

        // Apply Selection
        if (type == RewardType.NewMask)
        {
            if (selection == 0) // Equip
            {
                player.ApplyMask(maskDrop);
                Debug.Log($"Equipped {maskDrop.name}");
            }
            else // Salvage
            {
                player.ApplyStatBoost();
                Debug.Log("Salvaged Mask for Stats");
            }
        }
        else if (type == RewardType.UpgradeMask)
        {
            // Upgrade current mask stats slightly
            player.currentMask.atkBonus += 2f;
            player.currentMask.hpBonus += 10f;
            player.RecalculateStats();
            Debug.Log("Upgraded Current Mask");
        }
        else
        {
            player.ApplyStatBoost();
            Debug.Log("Applied Stat Boost");
        }
    }
}
