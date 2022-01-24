using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RPG.UI
{
    public static class UIExtensions
    {
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        public static void ReloadMainMenu()
        {
            var scene = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(scene);

        }
    }

}