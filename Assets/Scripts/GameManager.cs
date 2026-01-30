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
    private GameObject starReward;
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
        // Fire and forget
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
            // Reset position
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
        }

        // Environment: Sky
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
        ground.transform.position = new Vector3(0, -5.5f, 0); // Positioned so top is at -0.5 (below player)
        ground.transform.localScale = new Vector3(1000, 10, 10);
        var groundRend = ground.GetComponent<Renderer>();
        if(groundRend) groundRend.material.color = new Color(0.2f, 0.6f, 0.2f); // Grass Green

        // Environment: Trees (Background)
        CreateBackgroundProps();

        // Create Player
        GameObject pObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pObj.name = "Player";
        player = pObj.AddComponent<Unit>();
        // Player Stats
        player.maxHealth = 100;
        player.moveSpeed = 5;
        player.attackSpeed = 1.0f;
        player.attackRange = 1.5f;
        player.knockbackDist = 1.0f;
        player.attackPower = 10f;
        player.Initialize(Team.Player);

        // Ensure player is at 0,0
        player.transform.position = Vector3.zero;

        // Create Reward Object (Star) - hidden initially
        starReward = GameObject.CreatePrimitive(PrimitiveType.Sphere); // Sphere looks a bit like a star if yellow :P
        starReward.name = "RewardStar";
        starReward.GetComponent<Renderer>().material.color = Color.yellow;
        starReward.SetActive(false);

        // Create Game Clear Text
        GameObject gcObj = new GameObject("GameClearText");
        gcObj.transform.SetParent(mainCam.transform);
        gcObj.transform.localPosition = new Vector3(0, 0, 10);

        // Setup TMP
        gameClearText = gcObj.AddComponent<TextMeshPro>();
        gameClearText.text = "GAME CLEAR";
        gameClearText.fontSize = 12; // TMP font sizes are different
        gameClearText.alignment = TextAlignmentOptions.Center;
        gameClearText.color = Color.white;
        // Try to load default font if possible, otherwise it might look pink/invisible
        // gameClearText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        gcObj.SetActive(false);
    }

    void CreateBackgroundProps()
    {
        // Simple procedural trees
        for(int i = 0; i < 20; i++)
        {
            float xPos = Random.Range(-10f, 100f);
            float zPos = Random.Range(5f, 15f);

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "TreeTrunk";
            trunk.transform.position = new Vector3(xPos, 0, zPos);
            trunk.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
            trunk.GetComponent<Renderer>().material.color = new Color(0.4f, 0.2f, 0.1f); // Brown

            // Leaves
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "TreeLeaves";
            leaves.transform.position = new Vector3(xPos, 2.5f, zPos);
            leaves.transform.localScale = Vector3.one * 2.5f;
            leaves.GetComponent<Renderer>().material.color = new Color(0.1f, 0.5f, 0.1f); // Dark Green
        }
    }

    async UniTaskVoid GameLoop()
    {
        while (true)
        {
            await MovePhase();

            bool isBoss = (stageCount == bossStage);
            await BattlePhase(isBoss);

            await RewardPhase(isBoss);

            if (isBoss) break; // End Game

            stageCount++;
        }
    }

    async UniTask MovePhase()
    {
        currentState = GameState.Move;
        Debug.Log("Starting Move Phase");

        // Player moves right 3 screens in 5 seconds
        float screenHeight = mainCam.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCam.aspect;

        float totalDist = screenWidth * 3f;
        float duration = 5f;

        // Use DoTween for movement?
        // We need player to move and camera to follow.
        // It's easier to keep the update loop or use DOTween on player and update camera.

        player.isMovingScenario = true;

        // Calculate target X
        float startX = player.transform.position.x;
        float targetX = startX + totalDist;

        // DoTween
        await player.transform.DOMoveX(targetX, duration).SetEase(Ease.Linear).AsyncWaitForCompletion();

        player.isMovingScenario = false;
    }

    // Since we are using DoTween for movement in MovePhase, we need to handle Camera follow in Update or similar.
    void LateUpdate()
    {
        // Simple camera follow
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

        // Spawn Enemy
        float screenHeight = mainCam.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCam.aspect;

        Vector3 camPos = mainCam.transform.position;
        Vector3 spawnPos = new Vector3(camPos.x + screenWidth / 2f + 2f, 0, 0);

        GameObject eObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eObj.name = isBoss ? "FinalBoss" : "Monster";
        eObj.transform.position = spawnPos;
        enemy = eObj.AddComponent<Unit>();

        // Enemy Stats
        if (isBoss)
        {
            eObj.transform.localScale = Vector3.one * 2f;
            enemy.maxHealth = 300;
            enemy.moveSpeed = 2f;
            enemy.attackSpeed = 2.0f;
            enemy.attackRange = 3f;
            enemy.knockbackDist = 2f;
            enemy.attackPower = 20f;
            eObj.GetComponent<Renderer>().material.color = new Color(0.5f, 0, 0.5f);
        }
        else
        {
            enemy.maxHealth = 50;
            enemy.moveSpeed = 3f;
            enemy.attackSpeed = 1.5f;
            enemy.attackRange = 1.5f;
            enemy.knockbackDist = 0.5f;
            enemy.attackPower = 5f;
        }

        enemy.Initialize(Team.Enemy);
        if(isBoss) eObj.GetComponent<Renderer>().material.color = new Color(0.5f, 0, 0.5f);

        player.target = enemy;
        enemy.target = player;

        // Wait for death
        // Use UniTask.WaitUntil
        await UniTask.WaitUntil(() => player.currentHealth <= 0 || enemy.currentHealth <= 0);

        if (enemy.currentHealth <= 0)
        {
            Debug.Log("Enemy Defeated");
        }
        else if (player.currentHealth <= 0)
        {
            Debug.Log("Player Defeated - Game Over");
            // Stop loop?
            await UniTask.Yield(); // Just yield
            return; // Exit
        }

        if (enemy != null && enemy.gameObject.activeInHierarchy)
            enemy.gameObject.SetActive(false);

        player.target = null;
    }

    async UniTask RewardPhase(bool isBoss)
    {
        currentState = GameState.Reward;
        Debug.Log("Starting Reward Phase");

        if (isBoss)
        {
            gameClearText.gameObject.SetActive(true);
            gameClearText.transform.DOScale(1.5f, 1f).SetLoopType(LoopType.Yoyo).SetLoops(-1); // Pulse effect
            currentState = GameState.End;
            Debug.Log("GAME CLEAR");
            await UniTask.Delay(5000);
        }
        else
        {
            starReward.transform.position = player.transform.position + Vector3.up * 2f;
            starReward.SetActive(true);

            // Pop effect
            starReward.transform.localScale = Vector3.zero;
            starReward.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            starReward.transform.DORotate(new Vector3(0, 360, 0), 2f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);

            await UniTask.Delay(2000);

            starReward.SetActive(false);
            starReward.transform.DOKill();
        }
    }
}
