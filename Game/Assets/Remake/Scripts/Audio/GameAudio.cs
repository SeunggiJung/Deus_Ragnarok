using System;
using System.Collections.Generic;
using UnityEngine;

// 게임 전체 오디오 : 효과음 재생 + 마스터 볼륨 + 배경음악(BGM) 음량 보정.
// 씬이 바뀌어도 유지되는 오브젝트 하나가 게임 시작 시 자동으로 만들어진다 (씬에 따로 배치할 필요 없음)
//  - 효과음은 SfxSynth가 코드로 합성한 클립을 처음 쓸 때 만들어 캐시 (같은 소리가 한꺼번에 겹치지 않게 최소 간격 적용)
//  - 마스터 볼륨(0~1)은 AudioListener.volume으로 적용하고 PlayerPrefs에 저장 (ESC 메뉴의 슬라이더)
//  - 원작 BGMManager / BGMManager_Hades는 자체적으로 음량을 지정하므로(Start / 하데스 BGM 전환 시), 지정한 값을 감지해
//    BgmScale 만큼 줄여서 다시 적용 (원작 스크립트는 수정하지 않음)
public class GameAudio : MonoBehaviour
{
    private const string VolumeKey = "Deus.MasterVolume";
    private const float BgmScale = 0.7f;      // 원작 BGM 음량의 70% (30% 감소)
    private const int PoolSize = 12;
    private const float MinInterval = 0.045f; // 같은 효과음의 최소 재생 간격

    private static GameAudio instance;
    public static float MasterVolume { get; private set; } = 1f;

    private AudioSource[] pool;
    private int nextSource;
    private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    private readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();

    private readonly Dictionary<AudioSource, float> bgmApplied = new Dictionary<AudioSource, float>();
    private float nextScan;
    private AudioSource[] bgmSources = new AudioSource[0];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameAudio>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        AudioListener.volume = MasterVolume;

        pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            pool[i] = source;
        }
    }

    public static void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
    }

    public static void SaveSettings() => PlayerPrefs.Save();

    #region SFX
    public static void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0.06f)
    {
        if (instance == null) return;
        instance.PlayInternal(sfx, volume, pitchJitter);
    }

    private void PlayInternal(Sfx sfx, float volume, float pitchJitter)
    {
        float now = Time.unscaledTime;
        float minInterval = sfx == Sfx.Fire ? 0.12f : MinInterval;
        if (lastPlayed.TryGetValue(sfx, out float last) && now - last < minInterval) return;
        lastPlayed[sfx] = now;

        if (!clips.TryGetValue(sfx, out AudioClip clip))
        {
            clip = SfxSynth.Create(sfx);
            clips[sfx] = clip;
        }

        AudioSource source = pool[nextSource];
        nextSource = (nextSource + 1) % pool.Length;
        source.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // 맞은 캐릭터에 따라 : 플레이어는 아파하는 소리, 그 외는 타격음 (크리티컬은 금속성 울림 추가)
    public static void PlayDamaged(BaseCharacter target, bool critical)
    {
        if (target is PlayerController) Play(Sfx.Hurt, 0.9f);
        else Play(critical ? Sfx.HitCritical : Sfx.Hit, critical ? 1f : 0.85f);
    }

    public static void PlayDeath(BaseCharacter target)
    {
        if (target is PlayerController) Play(Sfx.PlayerDie, 0.9f, 0.02f);
        else if (target is BossController) Play(Sfx.BossDie, 1f, 0.02f);
        else Play(Sfx.MonsterDie, 0.7f);
    }

    // 충격파 / 폭발 : 퍼지는 폭이 클수록 크고 낮은 소리
    public static void PlayShockwave(float width)
    {
        if (width >= 3.5f) Play(Sfx.Boom, Mathf.Clamp(0.5f + width * 0.06f, 0.5f, 1f));
        else Play(Sfx.BoomSmall, 0.7f);
    }

    public static void PlayTelegraph(TelegraphKind kind)
    {
        Play(Sfx.WarnPound + (int)kind, 0.8f, 0.03f);
    }
    #endregion

    #region BGM
    private void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 1f;
            RefreshBgmSources();
        }

        foreach (AudioSource source in bgmSources)
            ApplyBgmScale(source);
    }

    private void RefreshBgmSources()
    {
        var found = new List<AudioSource>();
        foreach (var bgm in FindObjectsOfType<BGMManager>())
            AddSource(found, bgm.GetComponent<AudioSource>());
        foreach (var bgm in FindObjectsOfType<BGMManager_Hades>())
            AddSource(found, bgm.GetComponent<AudioSource>());
        bgmSources = found.ToArray();

        // 사라진 소스 정리
        var dead = new List<AudioSource>();
        foreach (var key in bgmApplied.Keys)
            if (key == null) dead.Add(key);
        foreach (var key in dead) bgmApplied.Remove(key);
    }

    private static void AddSource(List<AudioSource> list, AudioSource source)
    {
        if (source != null && !list.Contains(source)) list.Add(source);
    }

    // 원작 스크립트가 음량을 새로 지정한 순간(= 우리가 마지막으로 적용한 값과 다름)에만 그 값에 BgmScale을 곱해 적용
    private void ApplyBgmScale(AudioSource source)
    {
        if (source == null) return;
        if (bgmApplied.TryGetValue(source, out float applied) && Mathf.Approximately(source.volume, applied)) return;

        float scaled = source.volume * BgmScale;
        source.volume = scaled;
        bgmApplied[source] = scaled;
    }
    #endregion
}
