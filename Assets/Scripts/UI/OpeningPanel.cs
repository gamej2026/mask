using UnityEngine;
using DG.Tweening;

public class OpeningPanel : MonoBehaviour
{
    public GameObject creditObj;
    public float slideDuration = 0.5f;
    
    private bool isCreditOpen = false;
    private RectTransform creditRect;
    private Vector2 hiddenPos;
    private Vector2 shownPos;

    void Start()
    {
        if (creditObj != null)
        {
            creditRect = creditObj.GetComponent<RectTransform>();
            // 현재 위치를 열린 위치로 저장
            shownPos = creditRect.anchoredPosition;
            // 숨겨진 위치 (화면 오른쪽 바깥)
            hiddenPos = new Vector2(shownPos.x + creditRect.rect.width - 2300f, shownPos.y);
            // 초기 상태: 숨김
            creditRect.anchoredPosition = hiddenPos;
        }
    }

    public void OnClickStartGame()
    {
        GameManager.Instance.StartGame();
    }

    public void OnClickEndGame()
    {
        GameManager.Instance.QuitGame();
    }

    public void OnClickCredit()
    {
        if (creditRect == null) return;

        creditRect.DOKill();

        if (isCreditOpen)
        {
            // 닫기: 오른쪽으로 슬라이드 아웃
            creditRect.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InBack);
        }
        else
        {
            // 열기: 왼쪽으로 슬라이드 인
            creditRect.DOAnchorPos(shownPos, slideDuration).SetEase(Ease.OutBack);
        }

        isCreditOpen = !isCreditOpen;
    }
}
