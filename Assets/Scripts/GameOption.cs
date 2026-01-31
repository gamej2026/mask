using UnityEngine;

public class GameOption : MonoBehaviour
{
    public static GameOption Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public float spawnHeight = -1.2f;

    [Header("초당 회복 스태미너")]
    internal float staminaPerSecond = 1;

    [Header("아래는 데이터에서만 작동되는 디버그 옵션")]
    public bool _;

    [Header("몬스터 강제 스폰")]
    [Tooltip("몬스터 강제 스폰 ID (빈 문자열이면 비활성화)")]
    public string forceSpawnMonsterID = "";

    //[Header("게임 시작 레벨")]
    [HideInInspector]
    public int startStageLevel = 1;

    [Header("트렌지션 테스트용 레벨")]
    public int transitionTestStageLevel;

    [ContextMenu("트렌지션 테스트")]
    public void SetTransitionTestStageLevel()
    {
        GameManager.Instance.TransitionTest(transitionTestStageLevel);
    }

}
