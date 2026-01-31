using UnityEngine;

/// <summary>
/// 패럴랙스 배경 레이어 컴포넌트
/// 이 스크립트를 각 배경 오브젝트에 붙이고 이동 비율을 설정합니다.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Header("이동 비율")]
    [Range(0f, 1f)]
    [Tooltip("이동 비율 (0 = 움직이지 않음/원경, 1 = 카메라와 동일/근경)")]
    public float parallaxFactor = 0.5f;
    
    [Header("댐핑")]
    [Range(0f, 1f)]
    [Tooltip("댐핑 값 (0 = 즉시 반응, 1 = 매우 부드럽게 / 카메라 떨림 무시)")]
    public float damping = 0.5f;
    
    [Header("옵션")]
    [Tooltip("카메라가 지정되지 않으면 Main Camera를 사용합니다")]
    public Camera targetCamera;
    
    // 내부 변수
    private Vector3 startPosition;
    private Vector3 cameraStartPosition;
    private float currentPositionX;
    private bool isInitialized = false;
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        // 카메라 설정
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera == null)
        {
            Debug.LogWarning($"[ParallaxLayer] {gameObject.name}: 카메라를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        // 시작 위치 저장
        startPosition = transform.position;
        cameraStartPosition = targetCamera.transform.position;
        currentPositionX = startPosition.x;
        isInitialized = true;
    }
    
    private void LateUpdate()
    {
        if (!isInitialized || targetCamera == null) return;
        
        // 카메라 이동량 계산
        Vector3 cameraDelta = targetCamera.transform.position - cameraStartPosition;
        
        // 패럴랙스 효과 적용
        float targetPositionX = startPosition.x + cameraDelta.x * parallaxFactor;
        
        // 댐핑 적용 (카메라 떨림 무시)
        float smoothSpeed = Mathf.Lerp(50f, 2f, damping);
        currentPositionX = Mathf.Lerp(currentPositionX, targetPositionX, Time.deltaTime * smoothSpeed);
        
        // 새 위치 적용 (Y, Z값은 유지)
        transform.position = new Vector3(
            currentPositionX,
            transform.position.y,
            transform.position.z
        );
    }
    
    /// <summary>
    /// 패럴랙스 비율을 런타임에 변경합니다.
    /// </summary>
    public void SetParallaxFactor(float factor)
    {
        parallaxFactor = Mathf.Clamp01(factor);
    }
    
    /// <summary>
    /// 시작 위치를 현재 위치로 재설정합니다.
    /// </summary>
    public void ResetStartPosition()
    {
        if (targetCamera != null)
        {
            startPosition = transform.position;
            cameraStartPosition = targetCamera.transform.position;
        }
    }
}
