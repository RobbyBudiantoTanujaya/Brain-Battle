using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrainBattle.Kings.Editor
{
    public sealed class KingsLevelGeneratorWizard : ScriptableWizard
    {
        private const string OutputPath   = "Assets/_Project/ScriptableObjects/Kings/Levels";
        private const string PrefKeyCount = "KingsLevelGen_CountPerCategory";

        [Range(1, 20)]
        [Tooltip("Number of new levels to add per difficulty (Beginner / Expert / Impossible).")]
        public int CountPerCategory = 6;

        // Read-only info shown in the wizard body.
        [Space]
        [Header("Currently on disk")]
        [ReadOnly] public int BeginnerExisting;
        [ReadOnly] public int ExpertExisting;
        [ReadOnly] public int ImpossibleExisting;
        [ReadOnly] public int TotalWillGenerate;

        // ── Menu entry ─────────────────────────────────────────────────────────────

        [MenuItem("BrainBattle/Generate Kings Levels")]
        private static void Open()
        {
            var wizard = DisplayWizard<KingsLevelGeneratorWizard>(
                "Generate Kings Levels", "Generate");

            // Restore last-used count from EditorPrefs.
            wizard.CountPerCategory = EditorPrefs.GetInt(PrefKeyCount, 6);
            wizard.RefreshInfo();
        }

        // ── ScriptableWizard callbacks ─────────────────────────────────────────────

        // Called every time a field changes — keeps the info block live.
        private void OnWizardUpdate()
        {
            CountPerCategory = Mathf.Clamp(CountPerCategory, 1, 20);
            RefreshInfo();

            helpString = $"Will add {CountPerCategory} level(s) per difficulty " +
                         $"({CountPerCategory * 3} total) on top of existing assets.";

            isValid = CountPerCategory >= 1;
        }

        // Called when the user clicks "Generate".
        private void OnWizardCreate()
        {
            EditorPrefs.SetInt(PrefKeyCount, CountPerCategory);
            KingsLevelGenerator.GenerateLevels(CountPerCategory);
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private void RefreshInfo()
        {
            BeginnerExisting  = CountAssets("Beginner");
            ExpertExisting    = CountAssets("Expert");
            ImpossibleExisting = CountAssets("Impossible");
            TotalWillGenerate = CountPerCategory * 3;
        }

        private static int CountAssets(string difficulty)
        {
            string absDir = AbsoluteOutputPath();
            if (!Directory.Exists(absDir)) return 0;
            return Directory.GetFiles(absDir, $"Kings_{difficulty}_*.asset").Length;
        }

        private static string AbsoluteOutputPath()
        {
            string relative = OutputPath.Substring("Assets/".Length)
                                        .Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relative);
        }
    }

    // Draws [ReadOnly] fields as greyed-out labels in the wizard inspector.
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    internal sealed class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            bool prev = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(rect, prop, label);
            GUI.enabled = prev;
        }
    }

    internal sealed class ReadOnlyAttribute : PropertyAttribute { }
}
