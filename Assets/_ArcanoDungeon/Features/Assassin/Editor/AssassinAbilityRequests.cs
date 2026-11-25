using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcanoDungeon.Features.Assassin.Editor
{
    // File requests let the current editor import this feature without closing the user's project.
    [InitializeOnLoad]
    public static class AssassinAbilityRequests
    {
        const string Request = "AssassinValidation/REQUEST_ABILITIES.txt";
        static double nextCheck;
        static AssassinAbilityRequests() => EditorApplication.update += Poll;
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextCheck = EditorApplication.timeSinceStartup + .5;
            if (!File.Exists(Request)) return;
            var command = File.ReadAllText(Request).Trim();
            if (EditorApplication.isPlaying && command != "status") return;
            File.Delete(Request);
            Directory.CreateDirectory("AssassinValidation");
            File.WriteAllText("AssassinValidation/editor-state.json", JsonUtility.ToJson(new EditorState {
                playing = EditorApplication.isPlaying,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty
            }, true));
            if (command == "status") return;
            if (command == "setup")
            {
                try { AssassinAbilitySetup.Setup(); }
                catch (System.Exception error) { File.WriteAllText("AssassinValidation/abilities-error.txt", error.ToString()); Debug.LogException(error); }
            }
            if (command == "test")
            {
                try { AssassinAbilityValidation.Run(); }
                catch (System.Exception error) { File.WriteAllText("AssassinValidation/abilities-error.txt", error.ToString()); Debug.LogException(error); }
            }
            if (command == "presentation")
            {
                try { ArcanoDungeon.Features.HUD.Editor.GamePresentationSetup.Apply(); }
                catch (System.Exception error) { File.WriteAllText("AssassinValidation/presentation-error.txt", error.ToString()); Debug.LogException(error); }
            }
            if (command == "presentation-test")
            {
                try { ArcanoDungeon.Features.HUD.Editor.GamePresentationValidation.Run(); }
                catch (System.Exception error) { File.WriteAllText("AssassinValidation/presentation-error.txt", error.ToString()); Debug.LogException(error); }
            }
        }
        [System.Serializable] sealed class EditorState { public bool playing; public string scene; public bool dirty; }
    }
}
