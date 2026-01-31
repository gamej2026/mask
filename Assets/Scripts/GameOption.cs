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


    public int transitionTestStageLevel;
    [ContextMenu("트렌지션 테스트")]
    public void SetTransitionTestStageLevel()
    {
        GameManager.Instance.TransitionTest(transitionTestStageLevel);
    }

}
