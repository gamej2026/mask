using UnityEngine;

/// <summary>
/// 버튼 OnClick이나 애니메이션 이벤트에서 효과음을 재생할 때 사용하는 컴포넌트
/// 사용법:
/// 1. 오브젝트에 이 컴포넌트 추가
/// 2. 버튼 OnClick 또는 Animation Event에서 PlaySound("사운드이름") 호출
/// </summary>
public class SFXPlayer : MonoBehaviour
{
    [Header("Optional: 기본 사운드 (PlayDefaultSound용)")]
    public string defaultSound;

    /// <summary>
    /// 지정한 이름의 효과음을 재생합니다.
    /// Animation Event나 Button OnClick에서 사용 가능
    /// </summary>
    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(soundName);
        }
    }

    /// <summary>
    /// Inspector에서 설정한 defaultSound를 재생합니다.
    /// Button OnClick에서 파라미터 없이 호출할 때 유용
    /// </summary>
    public void PlayDefaultSound()
    {
        PlaySound(defaultSound);
    }
}
