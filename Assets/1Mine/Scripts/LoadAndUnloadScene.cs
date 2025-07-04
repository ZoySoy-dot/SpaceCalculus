using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadAndUnloadScene : MonoBehaviour
{
    [Header("Name of the scene to load")]
    public string sceneToLoad;

public void LoadNewScene()
{
    Debug.Log("Play button clicked");
    string currentScene = SceneManager.GetActiveScene().name;
    SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive).completed += (asyncOp) =>
    {
        SceneManager.UnloadSceneAsync(currentScene);
    };
}
}