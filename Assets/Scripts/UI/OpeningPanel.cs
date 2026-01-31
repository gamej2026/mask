using UnityEngine;

public class OpeningPanel : MonoBehaviour
{
    public void OnClickStartGame()
    {
        GameManager.Instance.StartGame();
    }

    public void OnClickEndGame()
    {

    }

    public void OnClickCredit()
    {
        GameManager.Instance.QuitGame();
    }
}
