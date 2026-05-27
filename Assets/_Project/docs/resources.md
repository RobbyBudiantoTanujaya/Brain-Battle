> Folder: `Assets/_Project/Resources/`

# Resources

Assets loaded at runtime via `Resources.Load<T>("path")`.  
Path is relative to `Resources/` and excludes the extension.

---

## audio/BGM/

| File | Loaded by | Usage |
|---|---|---|
| `bgm.mp3` | `AudioManager` (via SerializeField, pre-assigned by scene builders) | Looping background music, starts immediately in both LevelSelect and SampleScene |

---

## audio/SFX/

| File | Event |
|---|---|
| `tap_dot.ogg` | Crown or Dot placed |
| `tap_dot_variant.ogg` | Alternate (50% random) |
| `button_tap.ogg` | Cell cleared / Undo performed |
| `auto_dot.ogg` | Auto-dot batch fired after crown placement |
| `auto_dot_variant.ogg` | Alternate (50% random) |
| `invalid_place.ogg` | Conflict detected |
| `invalid_place_variant.ogg` | Alternate (50% random) |
| `victory_sound.wav` | Win condition met |

All SFX are pre-assigned as SerializeFields in `AudioManager` by both scene builders.  
AudioManager uses `PlayOneShot()` — simultaneous SFX do not cut each other off.

---

## Fonts/

| File | Type | Usage |
|---|---|---|
| `Outfit-Regular.ttf` | Source font | Basis for Outfit SDF |
| `Outfit-Bold.ttf` | Source font | (available for bold variants) |
| `Outfit SDF.asset` | TMP_FontAsset | All body text (HUD labels, timers, counts, menus) |
| `SegoeSym.ttf` | Source font | Basis for HUDIcons SDF |
| `HUDIcons SDF.asset` | TMP_FontAsset | Star ★ glyphs in VictoryPanel star rating |

### Font Atlas Notes

- `Outfit SDF.asset` is in **Dynamic** mode (45 pre-baked glyphs, expands at runtime).
- `HUDIcons SDF.asset` is used only for `_starRatingText` in VictoryPanel.
- Both fonts are applied by `KingsSceneBuilder` after building the scene:
  ```csharp
  // Apply body font to all TMP in canvas, then restore icon font on star rating
  foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
      tmp.font = bodyFont;
  r.VPStarRatingText.font = iconFont;
  ```
- If `Outfit SDF.asset` shows "Font Atlas Texture missing" warning: check that
  `m_AtlasTextures[0]` is linked and `m_Material` is assigned. The builder recreates
  these if broken.

---

## Sprites/

| File | Usage |
|---|---|
| `dot.png` | Cell icon for `Dot` state (loaded by `KingsGridRenderer`) |
| `crown.png` | Multi-sprite sheet: `crown_1` used as crown icon |
| `UIRoundedRect.png` | 9-slice sprite for HUD button backgrounds |

### Sprite Loading in KingsGridRenderer

```csharp
// Awake() fallback if not pre-assigned:
var dots = Resources.LoadAll<Sprite>("Sprites/dot");
_dotSprite = dots.Length > 0 ? dots[0] : null;

var crowns = Resources.LoadAll<Sprite>("Sprites/crown");
// crown_1 = the actual crown shape (crown_0 = small circle, crown_2 = thin bar)
_crownSprite = crowns.Length > 1 ? crowns[1] : (crowns.Length > 0 ? crowns[0] : null);
```

`KingsSceneBuilder` pre-assigns these via SerializeField so the `Resources.Load`
fallback is only needed in edge cases.
