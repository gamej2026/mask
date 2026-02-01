using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameOverClearPanel : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;

    void OnEnable()
    {
        if (fadeImage != null)
        {
            // 알파값 1로 시작
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1f);
            // 점차 0으로 페이드 아웃
            fadeImage.DOFade(0f, fadeDuration).SetEase(Ease.OutQuad);
        }
    }

    public void OnClickMainMenu()
    {
        GameManager.Instance.GoToMain();
    }
}
