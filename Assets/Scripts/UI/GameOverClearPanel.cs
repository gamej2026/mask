using UnityEngine;

public class GameOverClearPanel : MonoBehaviour
{
    public void OnClickMainMenu()
    {
        GameManager.Instance.GoToMain();
    }
}
