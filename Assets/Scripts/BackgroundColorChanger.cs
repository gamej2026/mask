using UnityEngine;
using DG.Tweening;

public class BackgroundColorChanger : MonoBehaviour
{
    [Header("Renderers")]
    public SpriteRenderer blueSR;
    public SpriteRenderer orangeSR;
    public SpriteRenderer darkRedSR;

    [Header("Settings")]
    public float transitionDuration = 5f;

    private void Start()
    {
#if UNITY_EDITOR
        if(GameOption.Instance.startStageLevel > 0)
        {
            GameManager.Instance.stageCount = GameOption.Instance.startStageLevel;
        }
#endif

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStageStart += HandleStageChange;
            InitializeState(GameManager.Instance.stageCount);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStageStart -= HandleStageChange;
        }
    }

    private void InitializeState(int stage)
    {
        // Reset all first
        if (blueSR) { blueSR.gameObject.SetActive(false); blueSR.color = SetAlpha(blueSR.color, 1); }
        if (orangeSR) { orangeSR.gameObject.SetActive(false); orangeSR.color = SetAlpha(orangeSR.color, 1); }
        if (darkRedSR) { darkRedSR.gameObject.SetActive(false); darkRedSR.color = SetAlpha(darkRedSR.color, 1); }

        if (stage < 6)
        {
            if (blueSR) blueSR.gameObject.SetActive(true);
        }
        else if (stage < 10)
        {
            if (orangeSR) orangeSR.gameObject.SetActive(true);
        }
        else
        {
            if (darkRedSR) darkRedSR.gameObject.SetActive(true);
        }
    }

    private void HandleStageChange(int stage)
    {
        if (stage == 6)
        {
            TransitionBlueToOrange();
        }
        else if (stage == 10)
        {
            TransitionOrangeToDarkRed();
        }
    }

    private void TransitionBlueToOrange()
    {
        if (orangeSR)
        {
            orangeSR.gameObject.SetActive(true);
            orangeSR.color = SetAlpha(orangeSR.color, 1);
        }

        if (blueSR)
        {
            blueSR.DOFade(0f, transitionDuration).OnComplete(() =>
            {
                blueSR.gameObject.SetActive(false);
            });
        }
    }

    private void TransitionOrangeToDarkRed()
    {
        if (darkRedSR)
        {
            darkRedSR.gameObject.SetActive(true);
            darkRedSR.color = SetAlpha(darkRedSR.color, 1);
        }

        if (orangeSR)
        {
            orangeSR.DOFade(0f, transitionDuration).OnComplete(() =>
            {
                orangeSR.gameObject.SetActive(false);
            });
        }
    }

    private Color SetAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }
}
