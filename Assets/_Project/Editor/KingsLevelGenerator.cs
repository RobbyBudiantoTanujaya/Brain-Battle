using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BrainBattle.Core.Generators;

namespace BrainBattle.Kings.Editor
{
    public static class KingsLevelGenerator
    {
        private const string OutputPath = "Assets/_Project/ScriptableObjects/Kings/Levels";

        private static readonly LevelSpec[] Levels = BuildLevels();
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

            int generated = 0;
            var totalWatch = Stopwatch.StartNew();

            foreach (var level in Levels)
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
                $"Generated {generated}/{Levels.Length} levels in {totalWatch.ElapsedMilliseconds}ms.\n\nSaved to:\n{OutputPath}",
                "OK");
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
            var levelLoaders = Object.FindObjectsByType<LevelLoader>(FindObjectsSortMode.None);
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
                Object.DestroyImmediate(levelData);
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

        private static LevelSpec[] BuildLevels()
        {
            var levels = new List<LevelSpec>(35);
            int levelNumber = 1;

            Add(levels, ref levelNumber, "Beginner", 1, 4, 1001);
            Add(levels, ref levelNumber, "Beginner", 2, 5, 1002);
            Add(levels, ref levelNumber, "Beginner", 3, 4, 3001);
            Add(levels, ref levelNumber, "Beginner", 4, 5, 3002);
            Add(levels, ref levelNumber, "Beginner", 5, 4, 3003);
            Add(levels, ref levelNumber, "Beginner", 6, 5, 3004);
            Add(levels, ref levelNumber, "Beginner", 7, 4, 3005);
            Add(levels, ref levelNumber, "Beginner", 8, 5, 3006);
            Add(levels, ref levelNumber, "Beginner", 9, 4, 3007);
            Add(levels, ref levelNumber, "Beginner", 10, 5, 3008);
            Add(levels, ref levelNumber, "Beginner", 11, 4, 3009);
            Add(levels, ref levelNumber, "Beginner", 12, 5, 3010);

            Add(levels, ref levelNumber, "Expert", 1, 6, 1003);
            Add(levels, ref levelNumber, "Expert", 2, 8, 1004);
            Add(levels, ref levelNumber, "Expert", 3, 6, 4001);
            Add(levels, ref levelNumber, "Expert", 4, 8, 4002);
            Add(levels, ref levelNumber, "Expert", 5, 6, 4003);
            Add(levels, ref levelNumber, "Expert", 6, 8, 4004);
            Add(levels, ref levelNumber, "Expert", 7, 6, 4005);
            Add(levels, ref levelNumber, "Expert", 8, 8, 4006);
            Add(levels, ref levelNumber, "Expert", 9, 6, 4007);
            Add(levels, ref levelNumber, "Expert", 10, 8, 4008);
            Add(levels, ref levelNumber, "Expert", 11, 6, 4009);
            Add(levels, ref levelNumber, "Expert", 12, 8, 4010);

            Add(levels, ref levelNumber, "Impossible", 1, 10, 1005);
            Add(levels, ref levelNumber, "Impossible", 2, 10, 2001);
            Add(levels, ref levelNumber, "Impossible", 3, 10, 2002);
            Add(levels, ref levelNumber, "Impossible", 4, 10, 2003);
            Add(levels, ref levelNumber, "Impossible", 5, 10, 2004);
            Add(levels, ref levelNumber, "Impossible", 6, 10, 2005);
            Add(levels, ref levelNumber, "Impossible", 7, 10, 2006);
            Add(levels, ref levelNumber, "Impossible", 8, 10, 2007);
            Add(levels, ref levelNumber, "Impossible", 9, 10, 2008);
            Add(levels, ref levelNumber, "Impossible", 10, 10, 2009);
            Add(levels, ref levelNumber, "Impossible", 11, 10, 2010);

            return levels.ToArray();
        }

        private static void Add(List<LevelSpec> levels, ref int levelNumber, string difficulty, int difficultyNumber, int size, int seed)
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
