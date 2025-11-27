using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GenreSceneManager : MonoBehaviour
{
    public static GenreSceneManager Instance { get; private set; }

    [Header("Environment Scene Names")]
    public string rockSceneName    = "Env_Rock";
    public string popSceneName     = "Env_Pop";
    public string classicSceneName = "Env_Classic";
    public string rapSceneName     = "Env_Rap";
    public string countrySceneName = "Env_Country";
    public string defaultSceneName = "Env_Default";

    [Header("Skyboxes")]
    public Material rockSkybox;
    public Material popSkybox;
    public Material classicSkybox;
    public Material rapSkybox;
    public Material countrySkybox;
    public Material defaultSkybox;

    [Header("Player")]
    public Transform playerTransform;

    private string _currentEnvScene = "null";
    private bool _isSwitching = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // keep across scene loads if needed
    }

    private void ApplySkybox(MusicGenre genre)
    {
        Material sky = defaultSkybox;

        switch (genre)
        {
            case MusicGenre.Rock: sky = rockSkybox; break;
            case MusicGenre.Pop: sky = popSkybox; break;
            case MusicGenre.Classic: sky = classicSkybox; break;
            case MusicGenre.Rap: sky = rapSkybox; break;
            case MusicGenre.Country: sky = countrySkybox; break;
        }

        RenderSettings.skybox = sky;

        // optional: force update ambient lighting
        DynamicGI.UpdateEnvironment();
    }

    public void SetGenre(MusicGenre genre)
    {
        if (_isSwitching) return;

        ApplySkybox(genre);

        string targetScene = GetSceneNameForGenre(genre);
        if (string.IsNullOrEmpty(targetScene)) return;
        if (targetScene == _currentEnvScene) return;

        StartCoroutine(SwitchEnvironmentScene(targetScene));
    }

    private string GetSceneNameForGenre(MusicGenre genre)
    {
        switch (genre)
        {
            case MusicGenre.Rock:    return rockSceneName;
            case MusicGenre.Pop:     return popSceneName;
            case MusicGenre.Classic: return classicSceneName;
            case MusicGenre.Rap:     return rapSceneName;
            case MusicGenre.Country: return countrySceneName;
            case MusicGenre.Default: return defaultSceneName;
        }
        return defaultSceneName;
    }

    private IEnumerator SwitchEnvironmentScene(string newScene)
    {
        _isSwitching = true;

        // Unload old env
        if (!string.IsNullOrEmpty(_currentEnvScene))
        {
            var prev = SceneManager.GetSceneByName(_currentEnvScene);
            if (prev.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(_currentEnvScene);
                if (unloadOp != null)
                    yield return unloadOp;
            }
        }

        // Load new env additive so player / XR stay
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
        if (loadOp != null)
            yield return loadOp;

        AlignPlayerToRandomSpawn(newScene);


        _currentEnvScene = newScene;
        _isSwitching = false;
    }

    private void AlignPlayerToRandomSpawn(string sceneName)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("GenreSceneManager: playerTransform not assigned.");
            return;
        }

        Scene envScene = SceneManager.GetSceneByName(sceneName);
        if (!envScene.isLoaded)
        {
            Debug.LogWarning($"GenreSceneManager: scene {sceneName} not loaded yet.");
            return;
        }

        List<Transform> spawnPoints = new List<Transform>();
        GameObject[] roots = envScene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            if (root.name.StartsWith("PlayerSpawn"))
                spawnPoints.Add(root.transform);
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"GenreSceneManager: No PlayerSpawn* found in scene {sceneName}.");
            return;
        }

        // Pick random spawn
        Transform pick = spawnPoints[Random.Range(0, spawnPoints.Count)];

        playerTransform.position = pick.position;
        playerTransform.rotation = pick.rotation;

        Debug.Log($"[GenreSceneManager] Spawned player at {pick.name} in {sceneName}");
    }
}
