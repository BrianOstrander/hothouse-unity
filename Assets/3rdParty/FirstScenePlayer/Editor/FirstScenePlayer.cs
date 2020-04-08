// Author : Onur 'Xtro' Er / Atesh Entertainment
// Email : onurer@gmail.com

using System;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
// ReSharper disable once UnusedMember.Global
static class FirstScenePlayer
{
    const string LoadFirstSceneEditorPrefKey = "FirstScenePlayer.LoadFirstScene";
    const string PreviousSceneManagerSetupPrefKey = "FirstScenePlayer.PreviousSceneManagerSetup";

    static bool RestoreScheduled;

    static FirstScenePlayer()
    {
		EditorApplication.playModeStateChanged += PlayModeChanged;
    }

    #region Messages
    [MenuItem("Edit/Load First Scene On Play", true)]
    // ReSharper disable UnusedMember.Local
    static bool ShowLoadMasterOnPlay()
    {
        return !LoadMasterOnPlay;
    }

    [MenuItem("Edit/Load First Scene On Play", priority = 150)]
    static void EnableLoadMasterOnPlay()
    {
        LoadMasterOnPlay = true;
    }

    [MenuItem("Edit/Don't Load First Scene On Play", true)]
    static bool ShowDontLoadMasterOnPlay()
    {
        return LoadMasterOnPlay;
    }

    [MenuItem("Edit/Don't Load First Scene On Play", priority = 150)]
    static void DisableLoadMasterOnPlay()
    {
        LoadMasterOnPlay = false;
    }
    // ReSharper restore UnusedMember.Local
    #endregion

	static void PlayModeChanged(PlayModeStateChange state)
    {
        if (!LoadMasterOnPlay || EditorBuildSettings.scenes.Length < 1) return;

        // If user pressed play button.
        if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            PreviousSceneManagerSetup = EditorSceneManager.GetSceneManagerSetup();

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
            else EditorApplication.isPlaying = false;
        }

        // If user pressed stop button.
        if (EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) RestoreScheduled = true;

        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && RestoreScheduled)
        {
            RestoreScheduled = false;

            EditorSceneManager.RestoreSceneManagerSetup(PreviousSceneManagerSetup);
        }
    }

    static bool LoadMasterOnPlay
    {
        get
        {
            return EditorPrefs.GetBool(LoadFirstSceneEditorPrefKey, false);
        }
        set
        {
            EditorPrefs.SetBool(LoadFirstSceneEditorPrefKey, value);
        }
    }

    static SceneSetup[] PreviousSceneManagerSetup
    {
        get
        {
            var SerializedSetupSplit = EditorPrefs.GetString(PreviousSceneManagerSetupPrefKey).Split(',');
            var Length = Convert.ToInt32(SerializedSetupSplit[0]);

            var Value = new SceneSetup[Length];

            for (var I = 0; I < Length; I++)
            {
                var Offset = I * 3;

                Value[I] = new SceneSetup
                {
                    path = SerializedSetupSplit[Offset + 1],
                    isLoaded = Convert.ToBoolean(SerializedSetupSplit[Offset + 2]),
                    isActive = Convert.ToBoolean(SerializedSetupSplit[Offset + 3])
                };
            }

            return Value;
        }
        set
        {
            var SerializedSetup = value.Length.ToString();

            foreach (var Scene in value)
            {
                SerializedSetup += "," + Scene.path;
                SerializedSetup += "," + Scene.isLoaded;
                SerializedSetup += "," + Scene.isActive;
            }

            EditorPrefs.SetString(PreviousSceneManagerSetupPrefKey, SerializedSetup);
        }
    }
}