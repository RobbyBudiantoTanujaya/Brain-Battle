using UnityEditor;
using UnityEngine;
using BrainBattle.Shared;

namespace BrainBattle.Editor
{
    public static class AudioManagerSetup
    {
        [MenuItem("BrainBattle/Setup Audio Manager")]
        private static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Setup Audio Manager",
                    "Exit Play Mode first, then run Setup Audio Manager again.", "OK");
                return;
            }

            var existing = Object.FindFirstObjectByType<AudioManager>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Setup Audio Manager",
                    "[AudioManager] already exists in this scene. No changes made.", "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            Undo.SetCurrentGroupName("Setup Audio Manager");

            // Root GameObject with AudioManager component.
            var rootGO = new GameObject("[AudioManager]");
            Undo.RegisterCreatedObjectUndo(rootGO, "Setup Audio Manager");
            var audioManager = rootGO.AddComponent<AudioManager>();

            // Child [BGMSource] with dedicated AudioSource.
            var bgmGO = new GameObject("[BGMSource]");
            Undo.RegisterCreatedObjectUndo(bgmGO, "Setup Audio Manager");
            bgmGO.transform.SetParent(rootGO.transform, false);
            var bgmSource            = bgmGO.AddComponent<AudioSource>();
            bgmSource.playOnAwake    = false;
            bgmSource.loop           = true;
            bgmSource.spatialBlend   = 0f;

            // Wire _bgmSource via SerializedObject so the assignment is undoable
            // and works with [SerializeField] private access.
            var so = new SerializedObject(audioManager);
            so.FindProperty("_bgmSource").objectReferenceValue = bgmSource;
            so.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            Selection.activeGameObject = rootGO;
            EditorUtility.DisplayDialog("Setup Audio Manager",
                "[AudioManager] created successfully.\n\n" +
                "IMPORTANT: This GameObject must live in your first-loaded scene (LevelSelect, build index 0). " +
                "DontDestroyOnLoad will keep it alive across all scene transitions.\n\n" +
                "Start music from LevelSelectController.Start():\n" +
                "  AudioManager.Instance?.PlayBGM();",
                "OK");
        }
    }
}
