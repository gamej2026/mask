using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

/*
 * [SoundManager 사용법]
 * 1. BGM 재생: SoundManager.Instance.PlayBGM("파일명");
 *    - Resources/Audio/ 폴더 내의 오디오 파일을 확장자 제외하고 이름으로 호출합니다.
 *    - 기존 BGM이 있으면 FadeOut 후 새로운 BGM이 FadeIn 되며 교체됩니다 (각 1초).
 *
 * 2. 효과음 재생: SoundManager.Instance.PlaySFX("파일명");
 *    - Resources/Audio/ 폴더 내의 오디오 파일을 확장자 제외하고 이름으로 호출합니다.
 *    - 여러 효과음이 중첩되어 재생됩니다.
 *
 * 3. 볼륨 조절 (작동 로직은 추후 구현 가능):
 *    - SoundManager.Instance.SetMasterVolume(float volume);
 *    - SoundManager.Instance.SetBGMVolume(float volume);
 *    - SoundManager.Instance.SetSFXVolume(float volume);
 */

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    private string currentBgmName = "";

    private float masterVolume = 1.0f;
    private float bgmVolume = 1.0f;
    private float sfxVolume = 1.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        EnsureSoundManagerExists();
    }

    public static void EnsureSoundManagerExists()
    {
        if (Instance != null) return;

        SoundManager existing = FindFirstObjectByType<SoundManager>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(Instance.gameObject);
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Managers/SoundManager");
            if (prefab != null)
            {
                Instance = Instantiate(prefab).GetComponent<SoundManager>();
            }
            else
            {
                GameObject obj = new GameObject("SoundManager");
                Instance = obj.AddComponent<SoundManager>();
                Instance.SetupSources();
            }
            DontDestroyOnLoad(Instance.gameObject);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupSources();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void SetupSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    // --- BGM Logic ---

    public void PlayBGM(string audioName)
    {
        if (currentBgmName == audioName) return;

        AudioClip clip = Resources.Load<AudioClip>($"BGM/{audioName}");
        if (clip == null)
        {
            Debug.LogWarning($"BGM Clip not found: BGM/{audioName}");
            return;
        }

        currentBgmName = audioName;
        bgmSource.DOKill();

        if (bgmSource.isPlaying)
        {
            // Fade Out and then Play New
            bgmSource.DOFade(0, fadeDuration).OnComplete(() =>
            {
                bgmSource.Stop();
                bgmSource.clip = clip;
                bgmSource.Play();
                bgmSource.DOFade(bgmVolume * masterVolume, fadeDuration);
            });
        }
        else
        {
            // Just Fade In
            bgmSource.volume = 0;
            bgmSource.clip = clip;
            bgmSource.Play();
            bgmSource.DOFade(bgmVolume * masterVolume, fadeDuration);
        }
    }

    public void StopBGM()
    {
        currentBgmName = "";
        bgmSource.DOKill();
        bgmSource.DOFade(0, fadeDuration).OnComplete(() =>
        {
            bgmSource.Stop();
        });
    }

    // --- SFX Logic ---

    public void PlaySFX(string audioName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/{audioName}");
        if (clip == null)
        {
            Debug.LogWarning($"SFX Clip not found: Audio/{audioName}");
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }

    // --- Volume Control (Stubs) ---

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        // Apply to current BGM
        if (bgmSource.isPlaying)
        {
            bgmSource.volume = bgmVolume * masterVolume;
        }
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource.isPlaying)
        {
            bgmSource.volume = bgmVolume * masterVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
}
