using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using BrainBattle.Core.Generators;
using BrainBattle.Kings;

namespace BrainBattle.Kings.Editor
{
    public static class KingsLevelGenerator
    {
        private const string OutputPath = "Assets/_Project/ScriptableObjects/Kings/Levels";

        private static readonly (int levelNumber, int size, string difficulty, int seed)[] Levels =
        {
            (1, 4,  "Beginner",   1001),
            (2, 5,  "Beginner",   1002),
            (3, 6,  "Expert",     1003),
            (4, 8,  "Expert",     1004),
            (5, 10, "Impossible", 1005),
        };

        [MenuItem("BrainBattle/Generate Kings Levels")]
        public static void GenerateLevels()
        {
            EnsureDirectory(OutputPath);

            int generated = 0;
            var totalWatch = Stopwatch.StartNew();

            foreach (var (levelNumber, size, difficulty, seed) in Levels)
            {
                var watch = Stopwatch.StartNew();

                LevelData levelData = LevelGeneratorService.GenerateLevel(levelNumber, size, difficulty, seed);
                if (levelData == null)
                {
                    UnityEngine.Debug.LogError($"[KingsLevelGenerator] Failed to generate Level {levelNumber} ({size}x{size}).");
                    continue;
                }

                string assetPath = $"{OutputPath}/KingsLevel_{levelNumber:D2}_{size}x{size}.asset";
                SaveAsset(levelData, assetPath);

                watch.Stop();
                UnityEngine.Debug.Log($"[KingsLevelGenerator] Generated Level {levelNumber} ({size}x{size}) in {watch.ElapsedMilliseconds}ms → {assetPath}");
                generated++;
            }

            totalWatch.Stop();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Kings Level Generation Complete",
                $"Generated {generated}/{Levels.Length} levels in {totalWatch.ElapsedMilliseconds}ms.\n\nSaved to:\n{OutputPath}",
                "OK");
        }

        private static void SaveAsset(LevelData levelData, string assetPath)
        {
            // Overwrite existing asset if present
            LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(levelData, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(levelData, assetPath);
            }
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
    }
}
