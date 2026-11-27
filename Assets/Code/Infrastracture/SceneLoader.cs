using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Infrastracture
{
    public class SceneLoader
    {
        private ICoroutineRunner _coroutineRunner;

        public SceneLoader(ICoroutineRunner coroutineRunner) => 
            _coroutineRunner = coroutineRunner;

        public void Load(string name, Action onLoaded = null) =>
            _coroutineRunner.StartCoroutine(LoadSceneLoader(name, onLoaded));
        
        private IEnumerator LoadSceneLoader(string nextScene, Action onLoaded = null)
        {
            if (SceneManager.GetActiveScene().name == nextScene)
            {
                onLoaded?.Invoke();
                yield break;
            }
            
            AsyncOperation waitNewScene = SceneManager.LoadSceneAsync(nextScene);
            
            while (!waitNewScene.isDone)
                yield  return null;
            
            onLoaded?.Invoke();
        }
    }
}