using UnityEngine;
using UnityEngine.SceneManagement;


namespace CliffGame
{
    public static class Loader
    {
        public static bool IsHost;

        public enum Scene
        {
            MainMenuScene,
            CliffGameFunPrototype,
            LoadingScene,
        }

        private static Scene _targetScene;

        public static void Load(Scene targetScene)
        {
            _targetScene = targetScene;

            SceneManager.LoadScene(Scene.LoadingScene.ToString());
        }

        public static void LoaderCallback()
        {
            SceneManager.LoadScene(_targetScene.ToString());
        }
    }

}