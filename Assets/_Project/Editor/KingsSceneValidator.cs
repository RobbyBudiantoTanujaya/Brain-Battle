using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BrainBattle.Games.Kings.Logic;
using BrainBattle.Games.Kings.UI;
using BrainBattle.Kings;
using BrainBattle.Shared;
using BrainBattle.Shared.UI;

namespace BrainBattle.Editor
{
    /// <summary>
    /// BrainBattle → Validate Kings Scene
    /// Checks that every required SerializeField in SampleScene is wired.
    /// Run this after any manual scene change and before building.
    /// </summary>
    public static class KingsSceneValidator
    {
        [MenuItem("BrainBattle/Validate Kings Scene")]
        public static void ValidateKingsScene()
        {
            var errors   = new List<string>();
            var warnings = new List<string>();

            // ── Require SampleScene to be open ────────────────────────────────────
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (scene.name != "SampleScene")
            {
                // Try to open it
                string path = "Assets/Scenes/SampleScene.unity";
                if (!System.IO.File.Exists(path))
                {
                    errors.Add("SampleScene not found at Assets/Scenes/SampleScene.unity");
                    Report(errors, warnings);
                    return;
                }
                EditorSceneManager.OpenScene(path);
                scene = EditorSceneManager.GetActiveScene();
            }

            // ── GameManager ───────────────────────────────────────────────────────
            CheckComponent<KingsGameManager>    ("GameManager",     errors, warnings, kgm =>
            {
                CheckField(kgm, "_gridRenderer",       "KingsGameManager._gridRenderer",       errors);
                CheckField(kgm, "_tutorialController", "KingsGameManager._tutorialController",  warnings); // optional
            });

            CheckComponent<KingsSceneBootstrap> ("GameManager",     errors, warnings, b =>
            {
                CheckField(b, "_levelLoader", "KingsSceneBootstrap._levelLoader", errors);
                CheckField(b, "_gameManager", "KingsSceneBootstrap._gameManager", errors);
            });

            // ── HUD buttons ───────────────────────────────────────────────────────
            CheckComponent<RestartButton>("RestartButton", errors, warnings, rb =>
            {
                CheckField(rb, "_gameManager",  "RestartButton._gameManager",  errors);
                CheckField(rb, "_button",       "RestartButton._button",       errors);
                CheckField(rb, "_confirmPanel", "RestartButton._confirmPanel", errors);
            });

            CheckComponent<TipsButton>("TipsButton", errors, warnings, tb =>
            {
                CheckField(tb, "_gameManager", "TipsButton._gameManager", errors);
                CheckField(tb, "_button",      "TipsButton._button",      errors);
                CheckField(tb, "_tipsPanel",   "TipsButton._tipsPanel",   errors);
                CheckField(tb, "_tipsText",    "TipsButton._tipsText",    errors);
                CheckField(tb, "_closeButton", "TipsButton._closeButton", errors);
            });

            // ── VictoryPanel ──────────────────────────────────────────────────────
            CheckComponent<VictoryPanel>("VictoryPanel", errors, warnings, vp =>
            {
                CheckField(vp, "_panel",           "VictoryPanel._panel",           errors);
                CheckField(vp, "_gameManager",     "VictoryPanel._gameManager",     errors);
                CheckField(vp, "_sceneBootstrap",  "VictoryPanel._sceneBootstrap",  errors);
                CheckField(vp, "_timeText",        "VictoryPanel._timeText",        errors);
                CheckField(vp, "_moveCountText",   "VictoryPanel._moveCountText",   errors);
                CheckField(vp, "_starRatingText",  "VictoryPanel._starRatingText",  errors);
                CheckField(vp, "_nextLevelButton", "VictoryPanel._nextLevelButton", errors);
                CheckField(vp, "_restartButton",   "VictoryPanel._restartButton",   errors);
                CheckField(vp, "_mainMenuButton",  "VictoryPanel._mainMenuButton",  errors);
            });

            // ── GridContainer / KingsGridRenderer ─────────────────────────────────
            CheckComponent<KingsGridRenderer>("GridContainer", errors, warnings, gr =>
            {
                CheckField(gr, "_dotSprite",   "KingsGridRenderer._dotSprite",   warnings); // loaded at runtime if null
                CheckField(gr, "_crownSprite", "KingsGridRenderer._crownSprite", warnings);
            });

            // ── TutorialController ────────────────────────────────────────────────
            CheckComponent<TutorialController>("TutorialOverlay", errors, warnings, tc =>
            {
                CheckField(tc, "_overlayPanel", "TutorialController._overlayPanel", errors);
            });

            Report(errors, warnings);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        static void CheckComponent<T>(string goName, List<string> errors, List<string> warnings,
                                      System.Action<T> inspect = null)
            where T : Component
        {
            var go = GameObject.Find(goName);
            if (go == null)
            {
                errors.Add($"GameObject '{goName}' not found in scene.");
                return;
            }
            var comp = go.GetComponent<T>();
            if (comp == null)
            {
                errors.Add($"Component {typeof(T).Name} missing on '{goName}'.");
                return;
            }
            inspect?.Invoke(comp);
        }

        static void CheckField(object obj, string fieldName, string label, List<string> target)
        {
            var f = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f == null) return; // field doesn't exist in this version, skip

            var value = f.GetValue(obj);
            if (value == null || value.Equals(null))
                target.Add($"  NULL: {label}");
        }

        static void Report(List<string> errors, List<string> warnings)
        {
            bool ok = errors.Count == 0;

            if (errors.Count > 0)
            {
                Debug.LogError($"[KingsSceneValidator] {errors.Count} error(s) found:\n" +
                               string.Join("\n", errors));
            }
            if (warnings.Count > 0)
            {
                Debug.LogWarning($"[KingsSceneValidator] {warnings.Count} warning(s):\n" +
                                 string.Join("\n", warnings));
            }

            string summary = ok
                ? $"✓ Kings Scene valid — {warnings.Count} warning(s)."
                : $"✗ Kings Scene has {errors.Count} error(s). Fix before building.";

            EditorUtility.DisplayDialog(
                "Kings Scene Validation",
                summary + (errors.Count > 0 ? "\n\nSee Console for details." : ""),
                "OK");

            if (ok) Debug.Log($"[KingsSceneValidator] {summary}");
        }
    }
}
