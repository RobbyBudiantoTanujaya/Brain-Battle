using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrainBattle.Shared
{
    public sealed class AudioManager : MonoBehaviour
    {
        // ── Auto-bootstrap ─────────────────────────────────────────────────────────
        // BeforeSceneLoad: creates the singleton before the first scene loads.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("[AudioManager]");
            go.AddComponent<AudioManager>();
            // DontDestroyOnLoad is called inside Awake.
        }

        // AfterSceneLoad: fires after the first scene is fully active — audio engine
        // is guaranteed ready here, so BGM can start safely.
        // sceneLoaded handles all subsequent scene transitions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartBGM()
        {
            Instance?.PlayBGM();
        }

        private const int    PoolSize      = 8;
        private const string PrefSFXVolume = "Audio_SFXVolume";
        private const string PrefBGMVolume = "Audio_BGMVolume";
        private const string PrefSFXMuted  = "Audio_SFXMuted";
        private const string PrefBGMMuted  = "Audio_BGMMuted";

        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource _bgmSource;

        // ── Clips ──────────────────────────────────────────────────────────────────

        private AudioClip _bgm;
        private AudioClip _tapClip;
        private AudioClip _tapVariantClip;
        private AudioClip _autoDotClip;
        private AudioClip _autoDotVariantClip;
        private AudioClip _buttonTapClip;
        private AudioClip _invalidPlaceClip;
        private AudioClip _invalidPlaceVariantClip;
        private AudioClip _victoryClip;

        // ── Pool ───────────────────────────────────────────────────────────────────

        private AudioSource[] _pool;
        private int           _poolIndex;

        // ── Settings ───────────────────────────────────────────────────────────────

        private float _sfxVolume;
        private float _bgmVolume;
        private bool  _sfxMuted;
        private bool  _bgmMuted;

        // ── State ──────────────────────────────────────────────────────────────────

        private bool      _appIsPaused;
        private Coroutine _bgmFadeCoroutine;
        private bool      _clipsLoaded;

        // ── MonoBehaviour ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
            BuildPool();
            InitBGMSource();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Auto-start BGM whenever a scene finishes loading.
        // Runs one frame after the scene is active, so the audio engine is ready.
        // PlayBGM() is idempotent — if BGM is already playing it returns immediately.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayBGM();
        }

        private void OnApplicationPause(bool paused)
        {
            _appIsPaused = paused;
            if (paused) PauseBGM();
            else        ResumeBGM();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)          PauseBGM();
            else if (!_appIsPaused) ResumeBGM();
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        public void PlayTap()          { EnsureClipsLoaded(); PlaySFX(RandomPick(_tapClip, _tapVariantClip), 1f, 1f); }
        public void PlayButtonTap()    { EnsureClipsLoaded(); PlaySFX(_buttonTapClip, 1f, 1f); }
        public void PlayInvalidPlace() { EnsureClipsLoaded(); PlaySFX(RandomPick(_invalidPlaceClip, _invalidPlaceVariantClip), 1f, 1f); }
        public void PlayVictory()      { EnsureClipsLoaded(); PlaySFX(_victoryClip, 1f, 1f); }

        public void PlayAutoDot()
        {
            EnsureClipsLoaded();
            AudioClip clip = RandomPick(_autoDotClip, _autoDotVariantClip);
            if (clip == null || _sfxMuted) return;
            var src = NextPooledSource();
            src.volume = _sfxVolume * 0.4f;
            src.pitch  = Random.Range(0.9f, 1.1f);
            src.clip   = clip;
            src.Play();
        }

        public void PlayBGM()
        {
            EnsureClipsLoaded();
            if (_bgmSource == null || _bgm == null) return;
            // Guard: only skip if the correct clip is already playing.
            // Checking isPlaying alone is not reliable — Unity can report isPlaying=true
            // on a newly created AudioSource with no clip in certain boot scenarios.
            if (_bgmSource.isPlaying && _bgmSource.clip == _bgm) return;

            _bgmSource.clip   = _bgm;
            _bgmSource.loop   = true;
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = StartCoroutine(FadeBGMIn(1f));
        }

        public void StopBGM()
        {
            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
                _bgmFadeCoroutine = null;
            }
            _bgmSource?.Stop();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefSFXVolume, _sfxVolume);
            PlayerPrefs.Save();
        }

        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
            PlayerPrefs.SetFloat(PrefBGMVolume, _bgmVolume);
            PlayerPrefs.Save();
        }

        public void SetSFXMute(bool muted)
        {
            _sfxMuted = muted;
            PlayerPrefs.SetInt(PrefSFXMuted, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetBGMMute(bool muted)
        {
            _bgmMuted              = muted;
            if (_bgmSource != null) _bgmSource.mute = muted;
            PlayerPrefs.SetInt(PrefBGMMuted, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── Private: init ──────────────────────────────────────────────────────────

        // Clips are loaded lazily on first Play call — not in Awake — because
        // RuntimeInitializeOnLoadMethod(BeforeSceneLoad) runs before Unity's audio
        // engine is fully initialised; Resources.Load returns null or unusable clips.
        private void EnsureClipsLoaded()
        {
            if (_clipsLoaded) return;
            _clipsLoaded = true;
            LoadClips();
        }

        private void LoadSettings()
        {
            _sfxVolume = PlayerPrefs.GetFloat(PrefSFXVolume, 1f);
            _bgmVolume = PlayerPrefs.GetFloat(PrefBGMVolume, 0.7f);
            _sfxMuted  = PlayerPrefs.GetInt(PrefSFXMuted, 0) == 1;
            _bgmMuted  = PlayerPrefs.GetInt(PrefBGMMuted, 0) == 1;
        }

        private void LoadClips()
        {
            _bgm                     = Load("audio/BGM/bgm");
            _tapClip                 = Load("audio/SFX/tap_dot");
            _tapVariantClip          = Load("audio/SFX/tap_dot_variant");
            _autoDotClip             = Load("audio/SFX/auto_dot");
            _autoDotVariantClip      = Load("audio/SFX/auto_dot_variant");
            _buttonTapClip           = Load("audio/SFX/button_tap");
            _invalidPlaceClip        = Load("audio/SFX/invalid_place");
            _invalidPlaceVariantClip = Load("audio/SFX/invalid_place_variant");
            _victoryClip             = Load("audio/SFX/victory_sound");
        }

        private static AudioClip Load(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null) Debug.LogError($"[AudioManager] Clip not found at Resources/{path}");
            return clip;
        }

        private void BuildPool()
        {
            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var child            = new GameObject($"SFXSource_{i}");
                child.transform.SetParent(transform);
                var src              = child.AddComponent<AudioSource>();
                src.playOnAwake      = false;
                src.loop             = false;
                src.spatialBlend     = 0f;
                _pool[i]             = src;
            }
        }

        private void InitBGMSource()
        {
            if (_bgmSource == null)
            {
                // Find child GO named [BGMSource] created by setup tool or prior run.
                var existing = transform.Find("[BGMSource]");
                if (existing != null) _bgmSource = existing.GetComponent<AudioSource>();
            }

            if (_bgmSource == null)
            {
                var child        = new GameObject("[BGMSource]");
                child.transform.SetParent(transform);
                _bgmSource       = child.AddComponent<AudioSource>();
            }

            _bgmSource.playOnAwake  = false;
            _bgmSource.loop         = true;
            _bgmSource.spatialBlend = 0f;
            _bgmSource.volume       = _bgmVolume;
            _bgmSource.mute         = _bgmMuted;
        }

        // ── Private: playback ──────────────────────────────────────────────────────

        private void PlaySFX(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _sfxMuted) return;
            var src   = NextPooledSource();
            src.volume = _sfxVolume * volume;
            src.pitch  = pitch;
            src.clip   = clip;
            src.Play();
        }

        private AudioSource NextPooledSource()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                int idx = (_poolIndex + i) % PoolSize;
                if (!_pool[idx].isPlaying)
                {
                    _poolIndex = (idx + 1) % PoolSize;
                    return _pool[idx];
                }
            }
            // All busy — steal next slot.
            var stolen = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % PoolSize;
            return stolen;
        }

        private void PauseBGM()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
                _bgmSource.Pause();
        }

        private void ResumeBGM()
        {
            if (_bgmSource != null && !_bgmSource.isPlaying && _bgmSource.clip != null)
                _bgmSource.UnPause();
        }

        private IEnumerator FadeBGMIn(float duration)
        {
            if (_bgmSource == null) yield break;

            _bgmSource.volume = 0f;
            float elapsed     = 0f;

            while (elapsed < duration)
            {
                elapsed           += Time.unscaledDeltaTime;
                _bgmSource.volume  = Mathf.Lerp(0f, _bgmVolume, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _bgmSource.volume = _bgmVolume;
            _bgmFadeCoroutine = null;
        }

        private static AudioClip RandomPick(AudioClip main, AudioClip variant)
        {
            if (main == null)    return variant;
            if (variant == null) return main;
            return Random.value < 0.5f ? main : variant;
        }
    }
}
