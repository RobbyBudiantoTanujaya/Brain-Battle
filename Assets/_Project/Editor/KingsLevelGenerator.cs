using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BrainBattle.Core.Generators;

namespace BrainBattle.Kings.Editor
{
    public static class KingsLevelGenerator
    {
        private const string OutputPath = "Assets/_Project/ScriptableObjects/Kings/Levels";
        private const int NewLevelsPerCategory = 6;

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
            RenameLegacyAssets();

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
            var levelLoaders = UnityEngine.Object.FindObjectsByType<LevelLoader>(FindObjectsSortMode.None);
            if (levelLoaders.Length == 0) return;

            foreach (LevelLoader loader in levelLoaders)
            {
                var so = new SerializedObject(loader);
                var prop = so.FindProperty("_allLevels");
                prop.arraySize = orderedLevels.Length;
                for (int i = 0; i < orderedLevels.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = orderedLevels[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(loader);
                EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
            }

            EditorSceneManager.SaveOpenScenes();
            UnityEngine.Debug.Log($"[KingsLevelGenerator] Synced {levelLoaders.Length} LevelLoader component(s) with {orderedLevels.Length} ordered level assets.");
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
