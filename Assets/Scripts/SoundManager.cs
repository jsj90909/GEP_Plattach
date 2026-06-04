using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private Dictionary<AudioClip, float> lastPlayTimes = new Dictionary<AudioClip, float>();
    private float soundCooldown = 0.05f; // 0.05초 이내 중복 재생 방지

    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;

    // 배경음악 변수
    public AudioClip bgmClip;
    private AudioSource bgmSource;
    private float normalVolume = 0.8f;
    private float shopVolume = 0.4f;

    // 효과음 변수
    public AudioClip pickClip;
    public AudioClip moveClip;
    public AudioClip vanishClip;

    private AudioSource[] sfxSources;
    private int poolSize = 5;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // BGM 오디오 소스 설정
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = normalVolume;
            bgmSource.outputAudioMixerGroup = bgmGroup;

            // 효과음 오디오 소스 풀링 설정
            sfxSources = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                sfxSources[i] = gameObject.AddComponent<AudioSource>();
                sfxSources[i].outputAudioMixerGroup = sfxGroup;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    // 상점 상태에 따른 볼륨 조절 함수
    public void SetShopVolume(bool isShopOpen)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = isShopOpen ? shopVolume : normalVolume;
        }
    }

    public void PlayBlockPick()
    {
        PlaySFX(pickClip);
    }

    public void PlayBlockMove()
    {
        PlaySFX(moveClip);
    }

    public void PlayBlockVanish()
    {
        PlaySFX(vanishClip);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        if (lastPlayTimes.ContainsKey(clip))
        {
            if (Time.time - lastPlayTimes[clip] < soundCooldown)
            {
                return;
            }
        }

        lastPlayTimes[clip] = Time.time;

        AudioSource source = GetAvailableSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
    }

    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < poolSize; i++)
        {
            if (!sfxSources[i].isPlaying)
            {
                return sfxSources[i];
            }
        }
        return sfxSources[0];
    }
}