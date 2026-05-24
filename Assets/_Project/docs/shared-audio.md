> Folder: `Assets/_Project/Scripts/Shared/Audio/`

# Shared/Audio — Audio System

Namespace: `BrainBattle.Shared`

---

## AudioManager.cs

`DontDestroyOnLoad` singleton MonoBehaviour.  
Exists in **both** `LevelSelect` and `SampleScene` (added by scene builders).  
The second instance that Awake() fires simply calls `Destroy(gameObject)`.

### Singleton Pattern

```csharp
private static AudioManager s_instance;

private void Awake()
{
    if (s_instance != null && s_instance != this) { Destroy(gameObject); return; }
    s_instance = this;
    DontDestroyOnLoad(gameObject);
    // create AudioSources, start BGM
}
```

### SerializeFields (wired by both scene builders)

| Field | File | Description |
|---|---|---|
| `_tapDotClip` | `audio/SFX/tap_dot.ogg` | Place dot/crown |
| `_tapDotVariantClip` | `audio/SFX/tap_dot_variant.ogg` | Alternate for variety |
| `_buttonTapClip` | `audio/SFX/button_tap.ogg` | Clear cell / undo |
| `_autoDotClip` | `audio/SFX/auto_dot.ogg` | Auto-dot batch |
| `_autoDotVariantClip` | `audio/SFX/auto_dot_variant.ogg` | Alternate |
| `_invalidPlaceClip` | `audio/SFX/invalid_place.ogg` | Conflict detected |
| `_invalidPlaceVariantClip` | `audio/SFX/invalid_place_variant.ogg` | Alternate |
| `_victoryClip` | `audio/SFX/victory_sound.wav` | Win |
| `_bgmClip` | `audio/BGM/bgm.mp3` | Looping background music |

### Dynamic KingsGameManager Subscription

`_gameManager` is **not** a SerializeField — it is found dynamically:

```csharp
private void Start()       => TrySubscribe();        // initial scene
private void OnEnable()    => SceneManager.sceneLoaded += OnSceneLoaded;
private void OnDisable()   => SceneManager.sceneLoaded -= OnSceneLoaded;

private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    Unsubscribe();
    TrySubscribe();   // FindFirstObjectByType<KingsGameManager>()
}
```

This means:
- In `LevelSelect`: no `KingsGameManager` found → only BGM plays.
- In `SampleScene`: `KingsGameManager` found → SFX events subscribed.
- On returning to `LevelSelect`: unsubscribed automatically, BGM continues.

### Event → SFX Mapping

| Event | Sound |
|---|---|
| `OnCellPlaced(Crown or Dot)` | `PickVariant(_tapDotClip, _tapDotVariantClip)` |
| `OnCellPlaced(Empty)` | `_buttonTapClip` |
| `OnUndoPerformed` | `_buttonTapClip` |
| `OnAutoDotsBatch` | `PickVariant(_autoDotClip, _autoDotVariantClip)` |
| `OnConflictDetected` | `PickVariant(_invalidPlaceClip, _invalidPlaceVariantClip)` |
| `OnWin` | `_victoryClip` |

`PickVariant(a, b)` returns `a` or `b` at 50% random chance. Falls back to whichever is non-null.

### AudioSource Setup

Two `AudioSource` components added in `Awake()`:
- `_sfxSource`: `playOnAwake=false`, one-shot via `PlayOneShot(clip)`
- `_bgmSource`: `playOnAwake=false`, `loop=true`, plays `_bgmClip` immediately on awake

No `AudioMixerGroup` used in this project — routes through default `AudioListener`.

### Adding New SFX

1. Drop audio file into `Assets/_Project/Resources/audio/SFX/`
2. Add `[SerializeField] private AudioClip _myClip;` to `AudioManager`
3. Add `AssignClip(audioComp, "_myClip", SfxBase + "myfile.ogg")` in both `KingsSceneBuilder.WireAudioClips()` and `LevelSelectSceneBuilder.WireAudioClips()`
4. Run **BrainBattle → Build Kings Scene** + **BrainBattle → Build Level Select Scene**
