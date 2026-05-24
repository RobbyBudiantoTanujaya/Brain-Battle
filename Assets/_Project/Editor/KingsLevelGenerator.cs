using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BrainBattle.Core.Generators;
using BrainBattle.Shared.UI;

namespace BrainBattle.Kings.Editor
{
    public static class KingsLevelGenerator
    {
        private const string OutputPath = "Assets/_Project/ScriptableObjects/Kings/Levels";
        private const int NewLevelsPerCategory = 1;

        private static readonly (string oldName, string newName)[] LegacyRenames =
        {
            ("KingsLevel_01_4x4", "Kings_Beginner_01"),
            ("KingsLevel_02_5x5", "Kings_Beginner_02"),
            ("KingsLevel_03_6x6", "Kings_Expert_01"),
            ("KingsLevel_04_8x8", "Kings_Expert_02"),
            ("KingsLevel_05_10x10", "Kings_Impossible_01"),
        };

        [MenuItem("BrainBattle/Generate Kings Levels")]
        public static void GenerateLevels()
        {
            EnsureDirectory(OutputPath);

            LevelSpec[] newLevels = BuildNewLevels();

            int generated = 0;
            var totalWatch = Stopwatch.StartNew();

            foreach (var level in newLevels)
            {
                var watch = Stopwatch.StartNew();

                LevelData levelData = LevelGeneratorService.GenerateLevel(level.LevelNumber, level.Size, level.Difficulty, level.Seed);
                if (levelData == null)
                {
                    UnityEngine.Debug.LogError($"[KingsLevelGenerator] Failed to generate {level.AssetName} (Level {level.LevelNumber}, {level.Size}x{level.Size}, seed {level.Seed}).");
                    continue;
                }

                SaveAsset(levelData, level.AssetPath);

                watch.Stop();
                UnityEngine.Debug.Log($"[KingsLevelGenerator] Generated {level.AssetName} (Level {level.LevelNumber}, {level.Size}x{level.Size}, seed {level.Seed}) in {watch.ElapsedMilliseconds}ms → {level.AssetPath}");
                generated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SyncOpenSceneLevelLoaders();

            totalWatch.Stop();
            EditorUtility.DisplayDialog(
                "Kings Level Generation Complete",
                $"Generated {generated}/{newLevels.Length} levels in {totalWatch.ElapsedMilliseconds}ms.\n\nSaved to:\n{OutputPath}",
                "OK");
        }

        // ── Duplicate fixer ───────────────────────────────────────────────────────

        [MenuItem("BrainBattle/Fix Duplicate Levels")]
        public static void FixDuplicateLevels()
        {
            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { OutputPath });
            var all = new List<LevelData>();
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) all.Add(asset);
            }
            all.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            // Identify duplicates (keep first occurrence, fix rest)
            var seenFps    = new Dictionary<string, string>();
            var duplicates = new List<LevelData>();
            foreach (var ld in all)
            {
                string fp = BuildFingerprint(ld);
                string first;
                if (seenFps.TryGetValue(fp, out first))
                    duplicates.Add(ld);
                else
                    seenFps[fp] = ld.name ?? "";
            }

            if (duplicates.Count == 0)
            {
                EditorUtility.DisplayDialog("No Duplicates", "All " + all.Count + " levels are unique.", "OK");
                return;
            }

            var usedFps  = new HashSet<string>(seenFps.Keys);
            int fixedCount = 0;
            int baseSeed = unchecked((int)(DateTime.Now.Ticks >> 8));
            var watch    = Stopwatch.StartNew();

            foreach (var ld in duplicates)
            {
                int    levelNum  = ld.LevelNumber;
                int    size      = ld.GridSize;
                string diff      = ld.Difficulty;
                string label     = ld.name ?? "(unnamed)";

                LevelData replacement = null;
                string    newFp       = null;

                for (int attempt = 0; attempt < 50000 && replacement == null; attempt++)
                {
                    int seed = unchecked(baseSeed ^ (fixedCount * 99991 + attempt * 1009));
                    var candidate = LevelGeneratorService.GenerateLevel(levelNum, size, diff, seed);
                    if (candidate == null) continue;

                    string fp = BuildFingerprint(candidate);
                    if (!usedFps.Contains(fp)) { replacement = candidate; newFp = fp; }
                    else UnityEngine.Object.DestroyImmediate(candidate);
                }

                if (replacement == null)
                {
                    UnityEngine.Debug.LogError("[KingsLevelGenerator] Could not find unique replacement for " + label);
                    continue;
                }

                replacement.name = ld.name;
                EditorUtility.CopySerialized(replacement, ld);
                EditorUtility.SetDirty(ld);
                UnityEngine.Object.DestroyImmediate(replacement);
                usedFps.Add(newFp);
                fixedCount++;
                UnityEngine.Debug.Log("[KingsLevelGenerator] Fixed duplicate: " + label);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            watch.Stop();

            EditorUtility.DisplayDialog(
                "Duplicate Fix Complete",
                fixedCount + "/" + duplicates.Count + " duplicates fixed in " + watch.ElapsedMilliseconds + "ms.",
                "OK");
        }

        // Full fingerprint: all queen positions (sorted) + all cell→regionId assignments (sorted).
        // Two levels with the same fingerprint are guaranteed to have identical puzzle content.
        private static string BuildFingerprint(LevelData ld)
        {
            var q      = ld.Solution;
            var qparts = new string[q.Length];
            for (int i = 0; i < q.Length; i++)
                qparts[i] = q[i].x.ToString() + "," + q[i].y.ToString();
            System.Array.Sort(qparts);

            var cells = new List<string>();
            foreach (var reg in ld.Regions)
                foreach (var cell in reg.Cells)
                    cells.Add(cell.x.ToString() + "," + cell.y.ToString() + ":" + reg.RegionId.ToString());
            cells.Sort();

            return string.Join("|", qparts) + "@" + string.Join("|", cells.ToArray());
        }

        // Builds 6 new levels per category (Beginner/Expert/Impossible) on top of what already exists.
        // Seeds are time-derived so each run produces different content.
        private static LevelSpec[] BuildNewLevels()
        {
            int beginnerMax  = MaxDifficultyNumber("Beginner");
            int expertMax    = MaxDifficultyNumber("Expert");
            int impossibleMax = MaxDifficultyNumber("Impossible");
            int totalExisting = CountTotalExisting();

            int levelNumber = totalExisting + 1;

            // Use current time as entropy so each execution generates different seeds.
            // XOR with a large prime and per-difficulty salt to avoid collisions between categories.
            int baseSeed = unchecked((int)(DateTime.Now.Ticks >> 8));

            var levels = new List<LevelSpec>(NewLevelsPerCategory * 3);

            // 6 new Beginner levels — alternate 4x4 / 5x5
            for (int i = 0; i < NewLevelsPerCategory; i++)
            {
                int diffNum = beginnerMax + i + 1;
                int size = (diffNum % 2 == 1) ? 4 : 5;
                int seed = unchecked(baseSeed ^ (10000 + diffNum * 97));
                Add(levels, ref levelNumber, "Beginner", diffNum, size, seed);
            }

            // 6 new Expert levels — alternate 6x6 / 8x8
            for (int i = 0; i < NewLevelsPerCategory; i++)
            {
                int diffNum = expertMax + i + 1;
                int size = (diffNum % 2 == 1) ? 6 : 8;
                int seed = unchecked(baseSeed ^ (20000 + diffNum * 97));
                Add(levels, ref levelNumber, "Expert", diffNum, size, seed);
            }

            // 6 new Impossible levels — all 10x10
            for (int i = 0; i < NewLevelsPerCategory; i++)
            {
                int diffNum = impossibleMax + i + 1;
                int seed = unchecked(baseSeed ^ (30000 + diffNum * 97));
                Add(levels, ref levelNumber, "Impossible", diffNum, 10, seed);
            }

            return levels.ToArray();
        }

        // Returns the highest DifficultyNumber for a given category already on disk (0 if none).
        // Uses System.IO directly — bypasses AssetDatabase so it works even before a Refresh.
        private static int MaxDifficultyNumber(string category)
        {
            string dir = LevelsAbsolutePath();
            if (!Directory.Exists(dir)) return 0;

            string prefix = $"Kings_{category}_";
            int max = 0;
            foreach (string file in Directory.GetFiles(dir, $"Kings_{category}_*.asset"))
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                if (filename.StartsWith(prefix) &&
                    int.TryParse(filename.Substring(prefix.Length), out int num))
                    max = Math.Max(max, num);
            }
            return max;
        }

        // Counts total level assets already saved (to determine next global levelNumber).
        private static int CountTotalExisting()
        {
            string dir = LevelsAbsolutePath();
            if (!Directory.Exists(dir)) return 0;
            return Directory.GetFiles(dir, "Kings_*.asset").Length;
        }

        // Converts the Unity-relative OutputPath to an absolute filesystem path.
        private static string LevelsAbsolutePath()
        {
            // Application.dataPath = "<project>/Assets"
            // OutputPath           = "Assets/_Project/..."
            // Strip leading "Assets/" and join with dataPath.
            string relative = OutputPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relative);
        }

        // Deletes every Kings_*.asset in OutputPath so Generate always starts from a clean slate.
        private static void DeleteAllLevelAssets()
        {
            string dir = LevelsAbsolutePath();
            if (!Directory.Exists(dir)) return;

            string[] files = Directory.GetFiles(dir, "Kings_*.asset");
            int deleted = 0;
            foreach (string file in files)
            {
                // Convert absolute path back to Assets-relative for AssetDatabase.
                string relative = "Assets/" + file.Substring(Application.dataPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                if (AssetDatabase.DeleteAsset(relative))
                    deleted++;
                else
                    UnityEngine.Debug.LogWarning($"[KingsLevelGenerator] Could not delete {relative}");
            }

            if (deleted > 0)
            {
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log($"[KingsLevelGenerator] Deleted {deleted} existing level asset(s) before regeneration.");
            }
        }

        private static void RenameLegacyAssets()
        {
            foreach (var (oldName, newName) in LegacyRenames)
            {
                if (oldName == newName) continue;

                string oldPath = $"{OutputPath}/{oldName}.asset";
                LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(oldPath);
                if (asset == null) continue;

                string targetPath = $"{OutputPath}/{newName}.asset";
                LevelData existingTarget = AssetDatabase.LoadAssetAtPath<LevelData>(targetPath);
                if (existingTarget != null && existingTarget != asset)
                {
                    UnityEngine.Debug.LogWarning($"[KingsLevelGenerator] Skipped rename {oldName} → {newName} because {targetPath} already exists.");
                    continue;
                }

                string error = AssetDatabase.RenameAsset(oldPath, newName);
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError($"[KingsLevelGenerator] Failed to rename {oldName} → {newName}: {error}");
                    continue;
                }

                UnityEngine.Debug.Log($"[KingsLevelGenerator] Renamed {oldName} → {newName}");
            }
        }

        private static void SyncOpenSceneLevelLoaders()
        {
            LevelData[] orderedLevels = LoadOrderedLevels();

            // Save any unsaved changes before switching scenes.
            EditorSceneManager.SaveOpenScenes();
            string originalPath = EditorSceneManager.GetActiveScene().path;

            int synced = 0;
            synced += SyncSceneByPath("Assets/Scenes/SampleScene.unity",  orderedLevels);
            synced += SyncSceneByPath("Assets/_Project/Scenes/LevelSelect.unity",  orderedLevels);

            // Restore the scene the user had open.
            if (!string.IsNullOrEmpty(originalPath) && System.IO.File.Exists(originalPath))
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);

            UnityEngine.Debug.Log($"[KingsLevelGenerator] Synced {synced} component(s) across both scenes with {orderedLevels.Length} level assets.");
        }

        // Opens the scene alone (Single mode — no other scenes loaded, no contamination),
        // updates _allLevels on every LevelLoader and LevelSelectController, then saves.
        private static int SyncSceneByPath(string scenePath, LevelData[] orderedLevels)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                UnityEngine.Debug.LogWarning($"[KingsLevelGenerator] Scene not found, skipping sync: {scenePath}");
                return 0;
            }

            // Single mode closes every other open scene first — prevents cross-scene
            // object contamination that happens with Additive when SaveScene is called.
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            int synced = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var loader in root.GetComponentsInChildren<LevelLoader>(true))
                {
                    ApplyLevels(loader, orderedLevels);
                    synced++;
                }
                foreach (var ctrl in root.GetComponentsInChildren<LevelSelectController>(true))
                {
                    ApplyLevels(ctrl, orderedLevels);
                    synced++;
                }
            }

            if (synced > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return synced;
        }

        private static void ApplyLevels(UnityEngine.Component comp, LevelData[] orderedLevels)
        {
            // Set _allLevels via reflection so the live object state (which has intra-scene
            // references intact) is what gets serialized by SetDirty+SaveScene.
            // SerializedObject.ApplyModifiedPropertiesWithoutUndo loses intra-scene component
            // refs (Button, Image, TMP) in additively-loaded scenes — avoid it here.
            var field = comp.GetType().GetField("_allLevels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;
            field.SetValue(comp, orderedLevels);
            EditorUtility.SetDirty(comp);
        }

        private static LevelData[] LoadOrderedLevels()
        {
            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { OutputPath });
            var levels = new List<LevelData>(guids.Length);
            foreach (string guid in guids)
            {
                LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) levels.Add(asset);
            }

            levels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return levels.ToArray();
        }

        private static void SaveAsset(LevelData levelData, string assetPath)
        {
            LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (existing != null)
            {
                levelData.name = existing.name;
                EditorUtility.CopySerialized(levelData, existing);
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(levelData);
                return;
            }

            AssetDatabase.CreateAsset(levelData, assetPath);
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void Add(List<LevelSpec> levels, ref int levelNumber, string difficulty,
                                 int difficultyNumber, int size, int seed)
        {
            levels.Add(new LevelSpec(levelNumber, difficulty, difficultyNumber, size, seed));
            levelNumber++;
        }

        private readonly struct LevelSpec
        {
            public LevelSpec(int levelNumber, string difficulty, int difficultyNumber, int size, int seed)
            {
                LevelNumber = levelNumber;
                Difficulty = difficulty;
                DifficultyNumber = difficultyNumber;
                Size = size;
                Seed = seed;
            }

            public int LevelNumber { get; }
            public string Difficulty { get; }
            public int DifficultyNumber { get; }
            public int Size { get; }
            public int Seed { get; }
            public string AssetName => $"Kings_{Difficulty}_{DifficultyNumber:D2}";
            public string AssetPath => $"{OutputPath}/{AssetName}.asset";
        }
    }
}
