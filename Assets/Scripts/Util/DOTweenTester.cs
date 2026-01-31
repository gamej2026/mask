using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// 인스펙터에서 DOTween 애니메이션을 쉽게 설정하고 테스트할 수 있는 컴포넌트입니다.
/// 게임 오브젝트에 추가하고 인스펙터에서 애니메이션 속성을 설정한 후 Play 버튼으로 테스트하세요.
/// Unity Play Mode가 아니어도 에디터에서 바로 테스트할 수 있습니다.
/// </summary>
[ExecuteAlways]
public class DOTweenTester : MonoBehaviour
{
    [Header("재생 설정")]
    [Tooltip("시작 시 자동 재생")]
    public bool playOnStart = false;
    
    [Tooltip("애니메이션 재생 중")]
    [SerializeField] private bool _isPlaying = false;
    
    [Header("애니메이션 타입")]
    public TweenType tweenType = TweenType.Move;
    
    [Header("기본 설정")]
    public float duration = 1f;
    public float delay = 0f;
    public Ease easeType = Ease.OutQuad;
    
    [Header("Move 설정")]
    public bool useLocalPosition = true;
    public Vector3 targetPosition = Vector3.zero;
    public bool useRelative = false;
    
    [Header("Scale 설정")]
    public Vector3 targetScale = Vector3.one;
    
    [Header("Rotate 설정")]
    public Vector3 targetRotation = Vector3.zero;
    public RotateMode rotateMode = RotateMode.Fast;
    
    [Header("Fade 설정")]
    [Tooltip("CanvasGroup 또는 SpriteRenderer 필요")]
    [Range(0f, 1f)]
    public float targetAlpha = 0f;
    
    [Header("Color 설정")]
    [Tooltip("SpriteRenderer 또는 Image 필요")]
    public Color targetColor = Color.white;
    
    [Header("Shake 설정")]
    public float shakeStrength = 1f;
    public int shakeVibrato = 10;
    public float shakeRandomness = 90f;
    
    [Header("Punch 설정")]
    public Vector3 punchDirection = Vector3.up;
    public int punchVibrato = 10;
    public float punchElasticity = 1f;
    
    [Header("루프 설정")]
    [Tooltip("-1 = 무한 반복, 0 = 루프 없음, 1+ = 해당 횟수만큼 반복")]
    public int loopCount = 0;
    public LoopType loopType = LoopType.Restart;
    
    [Header("초기 상태 저장")]
    [SerializeField] private Vector3 originalPosition;
    [SerializeField] private Vector3 originalLocalPosition;
    [SerializeField] private Vector3 originalScale;
    [SerializeField] private Quaternion originalRotation;
    [SerializeField] private float originalAlpha;
    [SerializeField] private Color originalColor;

    private Tween currentTween;
    private CanvasGroup canvasGroup;
    private SpriteRenderer spriteRenderer;
    private UnityEngine.UI.Image uiImage;
    
    public bool IsPlaying 
    { 
        get => _isPlaying; 
        set => _isPlaying = value;
    }
    
    public Tween CurrentTween
    {
        get => currentTween;
        set => currentTween = value;
    }

    public enum TweenType
    {
        Move,
        Scale,
        Rotate,
        Fade,
        Color,
        ShakePosition,
        ShakeRotation,
        ShakeScale,
        PunchPosition,
        PunchRotation,
        PunchScale
    }

    private void Awake()
    {
        CacheComponents();
        SaveOriginalState();
    }

    private void Start()
    {
        if (playOnStart && Application.isPlaying)
        {
            Play();
        }
    }

    public void CacheComponents()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<UnityEngine.UI.Image>();
    }

    /// <summary>
    /// 현재 상태를 원본 상태로 저장합니다.
    /// </summary>
    [ContextMenu("Save Original State")]
    public void SaveOriginalState()
    {
        originalPosition = transform.position;
        originalLocalPosition = transform.localPosition;
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
        
        CacheComponents();
        
        if (canvasGroup != null)
            originalAlpha = canvasGroup.alpha;
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        if (uiImage != null)
            originalColor = uiImage.color;
    }

    /// <summary>
    /// 저장된 원본 상태로 복원합니다.
    /// </summary>
    [ContextMenu("Restore Original State")]
    public void RestoreOriginalState()
    {
        Stop();
        
        transform.position = originalPosition;
        transform.localPosition = originalLocalPosition;
        transform.localScale = originalScale;
        transform.rotation = originalRotation;
        
        CacheComponents();
        
        if (canvasGroup != null)
            canvasGroup.alpha = originalAlpha;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        if (uiImage != null)
            uiImage.color = originalColor;
    }

    /// <summary>
    /// 애니메이션을 재생합니다.
    /// </summary>
    [ContextMenu("Play Animation")]
    public void Play()
    {
        Stop();
        _isPlaying = true;
        
        currentTween = CreateTween();
        
        if (currentTween != null)
        {
            currentTween.SetDelay(delay);
            currentTween.SetEase(easeType);
            
            if (loopCount != 0)
            {
                currentTween.SetLoops(loopCount, loopType);
            }
            
            currentTween.OnComplete(() => _isPlaying = false);
            currentTween.OnKill(() => _isPlaying = false);
        }
    }

    /// <summary>
    /// 애니메이션을 정지합니다.
    /// </summary>
    [ContextMenu("Stop Animation")]
    public void Stop()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
        _isPlaying = false;
    }

    /// <summary>
    /// 애니메이션을 일시 정지합니다.
    /// </summary>
    [ContextMenu("Pause Animation")]
    public void Pause()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Pause();
        }
    }

    /// <summary>
    /// 일시 정지된 애니메이션을 재개합니다.
    /// </summary>
    [ContextMenu("Resume Animation")]
    public void Resume()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Play();
        }
    }

    public Tween CreateTween()
    {
        CacheComponents();
        Tween tween = null;
        
        switch (tweenType)
        {
            case TweenType.Move:
                if (useLocalPosition)
                {
                    tween = transform.DOLocalMove(targetPosition, duration);
                }
                else
                {
                    tween = transform.DOMove(targetPosition, duration);
                }
                if (useRelative && tween != null)
                {
                    tween.SetRelative(true);
                }
                break;
                
            case TweenType.Scale:
                tween = transform.DOScale(targetScale, duration);
                break;
                
            case TweenType.Rotate:
                tween = transform.DORotate(targetRotation, duration, rotateMode);
                break;
                
            case TweenType.Fade:
                if (canvasGroup != null)
                {
                    tween = canvasGroup.DOFade(targetAlpha, duration);
                }
                else if (spriteRenderer != null)
                {
                    tween = spriteRenderer.DOFade(targetAlpha, duration);
                }
                else if (uiImage != null)
                {
                    tween = uiImage.DOFade(targetAlpha, duration);
                }
                else
                {
                    Debug.LogWarning("[DOTweenTester] Fade requires CanvasGroup, SpriteRenderer, or Image component!");
                }
                break;
                
            case TweenType.Color:
                if (spriteRenderer != null)
                {
                    tween = spriteRenderer.DOColor(targetColor, duration);
                }
                else if (uiImage != null)
                {
                    tween = uiImage.DOColor(targetColor, duration);
                }
                else
                {
                    Debug.LogWarning("[DOTweenTester] Color requires SpriteRenderer or Image component!");
                }
                break;
                
            case TweenType.ShakePosition:
                tween = transform.DOShakePosition(duration, shakeStrength, shakeVibrato, shakeRandomness);
                break;
                
            case TweenType.ShakeRotation:
                tween = transform.DOShakeRotation(duration, shakeStrength, shakeVibrato, shakeRandomness);
                break;
                
            case TweenType.ShakeScale:
                tween = transform.DOShakeScale(duration, shakeStrength, shakeVibrato, shakeRandomness);
                break;
                
            case TweenType.PunchPosition:
                tween = transform.DOPunchPosition(punchDirection, duration, punchVibrato, punchElasticity);
                break;
                
            case TweenType.PunchRotation:
                tween = transform.DOPunchRotation(punchDirection, duration, punchVibrato, punchElasticity);
                break;
                
            case TweenType.PunchScale:
                tween = transform.DOPunchScale(punchDirection, duration, punchVibrato, punchElasticity);
                break;
        }
        
        return tween;
    }

    private void OnDestroy()
    {
        Stop();
    }

    private void OnDisable()
    {
        Stop();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 현재 위치를 타겟으로 설정합니다.
    /// </summary>
    [ContextMenu("Set Current Position as Target")]
    private void SetCurrentPositionAsTarget()
    {
        targetPosition = useLocalPosition ? transform.localPosition : transform.position;
    }

    /// <summary>
    /// 에디터에서 현재 스케일을 타겟으로 설정합니다.
    /// </summary>
    [ContextMenu("Set Current Scale as Target")]
    private void SetCurrentScaleAsTarget()
    {
        targetScale = transform.localScale;
    }

    /// <summary>
    /// 에디터에서 현재 회전을 타겟으로 설정합니다.
    /// </summary>
    [ContextMenu("Set Current Rotation as Target")]
    private void SetCurrentRotationAsTarget()
    {
        targetRotation = transform.eulerAngles;
    }
#endif
}
