# Quickstart: LinkedIn Queens Game Implementation

**Feature**: `linkedin-queens-game`
**Date**: 2026-05-25

---

## Prerequisites

- Unity 6.0.75f1 LTS installed
- Android SDK configured (for Android builds)
- Xcode configured (for iOS builds, Mac only)
- Firebase project created (for Analytics/Crashlytics)
- AdMob account created (for monetization)

---

## Project Setup

### 1. Import Firebase SDK

```bash
# Download Firebase Unity SDK from:
# https://firebase.google.com/download/unity

# Import packages:
# - FirebaseAnalytics.unitypackage
# - FirebaseCrashlytics.unitypackage
```

In Unity: `Assets → Import Package → Custom Package`

### 2. Import AdMob SDK

```bash
# Download Google Mobile Ads Unity Plugin from:
# https://github.com/googleads/googleads-mobile-unity/releases

# Import: GoogleMobileAds.unitypackage
```

### 3. Configure Firebase

1. In Firebase Console, create Android and iOS apps
2. Download `google-services.json` (Android) → `Assets/StreamingAssets/`
3. Download `GoogleService-Info.plist` (iOS) → `Assets/StreamingAssets/`
4. Run `Firebase → iOS Settings → Generate Info.plist` (Mac only)

### 4. Configure AdMob

1. In AdMob Console, create ad units:
   - Interstitial ad unit ID
   - Rewarded ad unit ID
2. Add to `Assets/_Project/Resources/admob_config.json`:
```json
{
  "interstitialAdUnitId": "ca-app-pub-xxx/yyy",
  "rewardedAdUnitId": "ca-app-pub-xxx/zzz",
  "testDeviceIds": ["your-test-device-id"]
}
```

---

## Implementation Order

### Phase 1: Audio System (M3)

**Files to create**:
```
Assets/_Project/Scripts/Shared/Audio/
├── AudioManager.cs
└── AudioClips.cs (constants)
```

**Steps**:
1. Create `AudioManager.cs` with singleton pattern
2. Add `RuntimeInitializeOnLoadMethod` bootstrap
3. Implement SFX pool (8 AudioSources)
4. Implement BGM with fade-in coroutine
5. Add lazy clip loading (`EnsureClipsLoaded()`)
6. Wire to existing `KingsGameManager` events

**Test**:
- Play in editor → BGM fades in
- Tap cell → SFX plays
- Scene transition → BGM continues

---

### Phase 2: Design System Expansion (M3)

**File to modify**: `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`

**Add tokens**:
```csharp
// Audio
public const float BGMDefaultVolume = 0.7f;
public const float SFXDefaultVolume = 1.0f;
public const float BGMFadeDuration = 1.0f;
public const int SFXPoolSize = 8;

// Analytics
public const string EventLevelStart = "level_start";
public const string EventLevelComplete = "level_complete";
// ... etc
```

---

### Phase 3: Daily Challenge (M4)

**Files to create**:
```
Assets/_Project/Scripts/Games/Kings/Logic/
└── DailyChallengeManager.cs

Assets/_Project/Scripts/Games/Kings/UI/
└── DailyChallengeUI.cs
```

**Steps**:
1. Implement `GetDailySeed()` from date hash
2. Generate daily puzzle with seeded RNG
3. Implement streak tracking with PlayerPrefs
4. Add Daily Challenge button to LevelSelect
5. Create DailyChallengeUI scene/panel

**Test**:
- Complete daily → streak = 1
- Change system date → new puzzle
- Same date on different device → same puzzle

---

### Phase 4: AdMob Integration (M5)

**Files to create**:
```
Assets/_Project/Scripts/Shared/Monetization/
└── AdManager.cs
```

**Steps**:
1. Initialize AdMob in LevelSelect scene
2. Load interstitial ad (preload)
3. Show interstitial between levels (configurable frequency)
4. Load rewarded ad on hint request
5. Unlock hint on rewarded ad completion

**Test**:
- Use test ad unit IDs
- Verify interstitial shows after N levels
- Verify hint unlocks after rewarded ad

---

### Phase 5: Firebase Analytics (M6)

**Files to create**:
```
Assets/_Project/Scripts/Shared/Analytics/
├── AnalyticsManager.cs
└── AnalyticsEvent.cs
```

**Steps**:
1. Initialize Firebase in splash/LevelSelect
2. Subscribe to `KingsGameManager` events
3. Log events with parameters (level, difficulty, time)
4. Add Crashlytics for crash reporting

**Test**:
- Play level → check Firebase Console for events
- Force crash → verify Crashlytics report

---

## Verification Checklist

### After Audio Implementation

- [ ] BGM plays on app launch
- [ ] BGM continues across scene transitions
- [ ] SFX plays on tap/crown placement
- [ ] No console warnings about duplicate AudioListener
- [ ] Audio settings persist (mute/unmute)

### After Daily Challenge

- [ ] Daily puzzle is same on different devices
- [ ] Streak increments on consecutive days
- [ ] Streak resets on skipped day
- [ ] Daily completion tracked in PlayerPrefs

### After AdMob Integration

- [ ] Interstitial shows between levels
- [ ] Rewarded ad unlocks hint
- [ ] No ads in editor (test mode)
- [ ] Ads respect test device configuration

### After Firebase Integration

- [ ] Events appear in Firebase Console
- [ ] level_start, level_complete, level_fail logged
- [ ] ad_watched event includes reward type
- [ ] Crashlytics captures test crash

---

## Common Issues

### BGM Silent on First Scene

**Cause**: `isPlaying` guard triggered on fresh AudioSource  
**Fix**: Guard with `source.isPlaying && source.clip == expectedClip`

### Audio Clips Null

**Cause**: `Resources.Load` called during `BeforeSceneLoad`  
**Fix**: Lazy load in `EnsureClipsLoaded()` at first Play call

### TMP Text Missing on Android

**Cause**: Dynamic atlas + `m_ClearDynamicDataOnBuild=1`  
**Fix**: Static atlas + pre-baked chars + `m_ClearDynamicDataOnBuild=0`

### Daily Puzzle Different on iOS

**Cause**: DateTime hash differs by timezone  
**Fix**: Use `DateTime.UtcNow.Date` for UTC consistency

---

## Build Configuration

### Android

```
Player Settings:
- Scripting Backend: IL2CPP
- Target Architecture: ARM64
- Internet Access: Required (for ads/analytics)

TMP Settings:
- m_ClearDynamicDataOnBuild: 0
```

### iOS

```
Player Settings:
- Scripting Backend: IL2CPP
- Target Device: iPhone + iPad
- Requires ARCamera: No

Firebase:
- Add GoogleService-Info.plist to Xcode project
```

---

## Next Steps

After completing this quickstart:

1. Run `/adk:sdd:tasks` to generate detailed task breakdown
2. Implement tasks in priority order (M3 → M4 → M5 → M6)
3. Update `Milestones.md` after each milestone completion
4. Review scene wiring and build readiness before each build
