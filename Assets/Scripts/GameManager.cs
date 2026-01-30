using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    private Unit player;
    private Unit enemy;
    private Camera mainCam;
    private GameObject starReward;
    private TextMesh gameClearText;

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
        StartCoroutine(GameLoop());
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

        // Light
        if (FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Light");
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

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
        gameClearText = gcObj.AddComponent<TextMesh>();
        gameClearText.text = "GAME CLEAR";
        gameClearText.fontSize = 50;
        gameClearText.anchor = TextAnchor.MiddleCenter;
        gameClearText.color = Color.white;
        gcObj.SetActive(false);
    }

    IEnumerator GameLoop()
    {
        while (true)
        {
            yield return StartCoroutine(MovePhase());

            bool isBoss = (stageCount == bossStage);
            yield return StartCoroutine(BattlePhase(isBoss));

            yield return StartCoroutine(RewardPhase(isBoss));

            if (isBoss) break; // End Game

            stageCount++;
        }
    }

    IEnumerator MovePhase()
    {
        currentState = GameState.Move;
        Debug.Log("Starting Move Phase");

        // Player moves right 3 screens in 5 seconds
        float screenHeight = mainCam.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCam.aspect;

        float totalDist = screenWidth * 3f;
        float duration = 5f;
        float speed = totalDist / duration;

        player.isMovingScenario = true;
        float originalSpeed = player.moveSpeed;
        player.moveSpeed = speed; // Override speed for this phase

        float elapsed = 0;
        while (elapsed < duration)
        {
            // Camera follows player
            Vector3 camPos = mainCam.transform.position;
            camPos.x = player.transform.position.x;
            mainCam.transform.position = camPos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.isMovingScenario = false;
        player.moveSpeed = originalSpeed; // Restore combat speed
    }

    IEnumerator BattlePhase(bool isBoss)
    {
        currentState = GameState.Battle;
        Debug.Log($"Starting Battle Phase (Stage {stageCount})");

        // Spawn Enemy
        // Enemy enters from right side of screen
        float screenHeight = mainCam.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCam.aspect;

        // Spawn slightly offscreen to right relative to camera
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
            enemy.maxHealth = 300; // Boss HP
            enemy.moveSpeed = 2f; // Slower
            enemy.attackSpeed = 2.0f; // Slower attack
            enemy.attackRange = 3f; // Longer range
            enemy.knockbackDist = 2f;
            enemy.attackPower = 20f;
            eObj.GetComponent<Renderer>().material.color = new Color(0.5f, 0, 0.5f); // Purple
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

        // Override visual for Boss if needed (Initialize resets color)
        if(isBoss) eObj.GetComponent<Renderer>().material.color = new Color(0.5f, 0, 0.5f);

        // Assign Targets
        player.target = enemy;
        enemy.target = player;

        // Wait for death
        while (player.currentHealth > 0 && enemy.currentHealth > 0)
        {
             // Camera follows player
             Vector3 cPos = mainCam.transform.position;
             cPos.x = player.transform.position.x;
             mainCam.transform.position = cPos;

             yield return null;
        }

        // Check result
        if (enemy.currentHealth <= 0)
        {
            Debug.Log("Enemy Defeated");
        }
        else if (player.currentHealth <= 0)
        {
            Debug.Log("Player Defeated - Game Over");
            yield break;
        }

        // Cleanup enemy object if not destroyed (Unit disables it)
        if (enemy != null && enemy.gameObject.activeInHierarchy)
            enemy.gameObject.SetActive(false);

        // Clean up reference
        player.target = null;
    }

    IEnumerator RewardPhase(bool isBoss)
    {
        currentState = GameState.Reward;
        Debug.Log("Starting Reward Phase");

        if (isBoss)
        {
            // Game Clear
            gameClearText.gameObject.SetActive(true);
            currentState = GameState.End;
            Debug.Log("GAME CLEAR");
            yield return new WaitForSeconds(5f);
        }
        else
        {
            // Show Star above player
            starReward.transform.position = player.transform.position + Vector3.up * 2f;
            starReward.SetActive(true);

            yield return new WaitForSeconds(2f); // Show for 2 seconds

            starReward.SetActive(false);
        }
    }
}
