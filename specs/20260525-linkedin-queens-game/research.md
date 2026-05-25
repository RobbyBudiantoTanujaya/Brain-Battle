# Research: LinkedIn Queens Game Enhancements

**Feature**: `linkedin-queens-game`
**Date**: 2026-05-25
**Scope**: M3 (Polish/Audio) → M6 (Monetization/Analytics)

---

## Technology Decisions

### 1. Audio System Architecture

**Decision**: Singleton AudioManager with RuntimeInitializeOnLoadMethod bootstrap

**Rationale**:
- DontDestroyOnLoad ensures BGM continuity across scene transitions
- 8-source SFX pool prevents GC allocation during gameplay
- Lazy clip loading avoids null clips during BeforeSceneLoad phase
- Fade-in coroutine uses `Time.unscaledDeltaTime` for UI-independence

**Alternatives Considered**:
- Per-scene AudioSources: Rejected — BGM stops on scene change, complex handoff
- Addressables for audio: Rejected — overkill for ~15 clips, Resources.Load sufficient
- One-shot PlayClipAtPoint: Rejected — no pooling, creates GC pressure

**Implementation Pattern**:
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void AutoCreate() { /* create singleton */ }

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void AutoStartBGM() { /* first scene BGM */ }
```

**Key Gotcha**: Never guard `PlayBGM()` with `isPlaying` alone — Unity 6 reports `isPlaying=true` on fresh AudioSource with null clip. Correct guard: `if (source.isPlaying && source.clip == expectedClip) return;`

---

### 2. Design System Token Expansion

**Decision**: Expand existing `DesignSystem` static class with audio and analytics tokens

**Rationale**:
- Current tokens cover colors, sizes, spacing, grid
- New tokens needed: audio volume defaults, BGM fade duration, analytics event names
- Static class avoids MonoBehaviour overhead for constants

**Alternatives Considered**:
- ScriptableObject config: Rejected — constants don't need Inspector editing
- JSON config file: Rejected — adds parsing overhead, no runtime flexibility needed

**New Tokens to Add**:
```csharp
// Audio
public const float BGMDefaultVolume = 0.7f;
public const float SFXDefaultVolume = 1.0f;
public const float BGMFadeDuration = 1.0f;
public const int SFXPoolSize = 8;

// Analytics Events
public const string EventLevelStart = "level_start";
public const string EventLevelComplete = "level_complete";
public const string EventLevelFail = "level_fail";
public const string EventAdWatched = "ad_watched";
```

---

### 3. Daily Challenge Implementation

**Decision**: Seeded System.Random from date hash, PlayerPrefs streak storage

**Rationale**:
- `System.Random(int seed)` produces identical sequences across Android/iOS
- Date string → hash code provides deterministic daily seed
- Same puzzle for all players globally (no server dependency)
- Streak tracking: PlayerPrefs with date comparison for reset logic

**Alternatives Considered**:
- Server-side daily puzzle: Rejected — requires backend, offline support broken
- Unity.Random: Rejected — state is global, harder to control determinism
- DateTimeOffset: Rejected — unnecessary complexity, DateTime.UtcNow.Date sufficient

**Implementation Pattern**:
```csharp
int GetDailySeed() {
    var today = DateTime.UtcNow.Date;
    return today.GetHashCode(); // Same for all players on same date
}

void GenerateDailyChallenge() {
    var rng = new System.Random(GetDailySeed());
    // Generate puzzle with seeded RNG
}
```

**Streak Logic**:
```csharp
// On completion
if (lastPlayedDate == DateTime.UtcNow.Date.AddDays(-1))
    currentStreak++; // Consecutive day
else if (lastPlayedDate != DateTime.UtcNow.Date)
    currentStreak = 1; // Broken streak, start new

// On skip (check on app open)
if (lastPlayedDate < DateTime.UtcNow.Date.AddDays(-1))
    currentStreak = 0; // Missed a day
```

---

### 4. AdMob Integration

**Decision**: Google Mobile Ads Unity Plugin with interstitial + rewarded ads

**Rationale**:
- Official Unity plugin, supports Android + iOS
- Interstitial: between levels (configurable frequency)
- Rewarded: hint unlock (player-initiated, guaranteed reward)
- Test device configuration via AdMob dashboard

**Alternatives Considered**:
- Unity Ads: Rejected — lower fill rates, less control
- IronSource: Rejected — heavier SDK, more integration work

**Integration Points**:
- Interstitial: `VictoryPanel.OnGameComplete()` → show if level % frequency == 0
- Rewarded: `TipsButton.OnClick()` → show ad → unlock hint on completion

**Key Gotcha**: Initialize AdMob early (splash or LevelSelect), not at first ad request — initialization can take 1-2 seconds.

---

### 5. Firebase Analytics & Crashlytics

**Decision**: Firebase Unity SDK with Analytics + Crashlytics modules

**Rationale**:
- Analytics: level_start, level_complete, level_fail, ad_watched events
- Crashlytics: automatic crash reporting, stack traces, device info
- Integration with AdMob for ad revenue analytics

**Alternatives Considered**:
- Unity Analytics: Rejected — less detailed, Unity dashboard only
- Custom backend: Rejected — reinventing the wheel, no Crashlytics equivalent

**Event Logging Pattern**:
```csharp
void LogLevelStart(int levelNumber, string difficulty) {
    Firebase.Analytics.FirebaseAnalytics.LogEvent(
        DesignSystem.EventLevelStart,
        new Parameter[] {
            new Parameter("level", levelNumber),
            new Parameter("difficulty", difficulty)
        }
    );
}
```

---

## Existing Architecture Patterns (Reuse)

### Event-Driven Gameplay Coordination

`KingsGameManager` already exposes `OnGameComplete`, `OnConflictDetected`, `OnUndoStackChanged`. Audio and analytics subscribe to these events:

```csharp
// AudioManager subscribes to play SFX
KingsGameManager.OnConflictDetected += () => PlayInvalidPlace();

// AnalyticsManager subscribes to log events
KingsGameManager.OnGameComplete += (time, moves) => LogLevelComplete(time, moves);
```

### Editor-Authored Scene Setup

`KingsSceneBuilder` already owns scene hierarchy. Audio wiring added via builder:

```csharp
// In KingsSceneBuilder.WireAll()
gameManager.GetComponent<AudioListener>(); // Verify not duplicated
// AudioManager auto-creates, no manual wiring needed
```

### Logic-First Boundaries

New audio/analytics code follows same pattern:
- `AudioManager`: singleton adapter for Unity audio APIs
- `AnalyticsManager`: singleton adapter for Firebase APIs
- Neither contains game logic — they respond to events from game code

---

## Platform-Specific Considerations

### Android

| Issue | Solution |
|-------|----------|
| TMP text disappearing | Static atlas + pre-baked chars + `m_ClearDynamicDataOnBuild=0` |
| BGM memory pressure | `AudioClip.loadType = Streaming` for BGM |
| AdMob initialization | Initialize in LevelSelect scene, not gameplay |
| Firebase delay | Non-blocking init, queue events until ready |

### iOS

| Issue | Solution |
|-------|----------|
| BGM interruption | Subscribe to `AudioSettings.phoneMode` (future) |
| AdMob consent | Use UMP SDK for GDPR/CCPA (future) |
| Firebase delay | Same as Android — non-blocking init |

---

## Performance Targets

| Metric | Target | Validation Method |
|--------|--------|-------------------|
| Frame rate | 60fps sustained | Unity Profiler during gameplay |
| Load time | <3s to LevelSelect | Stopwatch from splash to interactive |
| Memory | <100MB peak | Profiler memory snapshot |
| Audio latency | <50ms SFX | Perceived responsiveness test |
| Ad load | <2s interstitial | AdMob callback timing |

---

## References

- [Unity Audio Best Practices](https://docs.unity3d.com/Manual/AudioOverview.html)
- [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup)
- [Google Mobile Ads Unity](https://developers.google.com/admob/unity/quick-start)
- [TextMeshPro Atlas Modes](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/FontAsset.html)
