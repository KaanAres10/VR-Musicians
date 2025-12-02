using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GenreSceneManager : MonoBehaviour
{
    public static GenreSceneManager Instance { get; private set; }

    [Header("Environment Scene Names")]
    public string rockSceneName = "Env_Rock";
    public string popSceneName = "Env_Pop";
    public string classicSceneName = "Env_Classic";
    public string rapSceneName = "Env_Rap";
    public string countrySceneName = "Env_Country";
    public string defaultSceneName = "Env_Default";

    [Header("Skyboxes")]
    public Material rockSkybox;
    public Material popSkybox;
    public Material classicSkybox;
    public Material rapSkybox;
    public Material countrySkybox;
    public Material defaultSkybox;

    [Header("Post-Processing")]
    public Volume globalVolume;
    public VolumeProfile rockProfile;
    public VolumeProfile popProfile;
    public VolumeProfile classicProfile;
    public VolumeProfile rapProfile;
    public VolumeProfile countryProfile;
    public VolumeProfile defaultProfile;

    [Header("Player")]
    public Transform playerTransform;

    [Header("Dynamic Hue (Pop Only)")]
    public bool enablePopDynamicHue = true;
    [Tooltip("How fast hue shifts (degrees per second).")]
    public float popHueSpeed = 50f;
    [Tooltip("Minimum hue value (degrees).")]
    public float popHueMin = -60f;
    [Tooltip("Maximum hue value (degrees).")]
    public float popHueMax = 60f;

    private string _currentEnvScene = "null";
    private bool _isSwitching = false;

    private MusicGenre _currentGenre = MusicGenre.Default;
    private ColorAdjustments _popColorAdjust;


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

    private void Update()
    {
        // Only animate when Pop is active and dynamic hue is enabled
        if (!enablePopDynamicHue) return;
        if (_currentGenre != MusicGenre.Pop) return;
        if (_popColorAdjust == null) return;

        float range = popHueMax - popHueMin;
        if (range <= 0.01f) return;

        // Ping-pong between min and max
        float hue = Mathf.PingPong(Time.time * popHueSpeed, range) + popHueMin;

        // Clamp to valid ColorAdjustments hue range [-180, 180]
        hue = Mathf.Clamp(hue, -180f, 180f);

        _popColorAdjust.hueShift.value = hue;
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

        if (sky != null)
        {
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
        }
        else
        {
            Debug.LogWarning($"GenreSceneManager: Skybox for {genre} is not assigned.");
        }
    }

    private void ApplyPostProcessing(MusicGenre genre)
    {
        if (globalVolume == null)
        {
            Debug.LogWarning("GenreSceneManager: Global Volume is not assigned.");
            return;
        }

        VolumeProfile target = defaultProfile;

        switch (genre)
        {
            case MusicGenre.Rock:
                if (rockProfile != null) target = rockProfile;
                break;
            case MusicGenre.Pop:
                if (popProfile != null) target = popProfile;
                break;
            case MusicGenre.Classic:
                if (classicProfile != null) target = classicProfile;
                break;
            case MusicGenre.Rap:
                if (rapProfile != null) target = rapProfile;
                break;
            case MusicGenre.Country:
                if (countryProfile != null) target = countryProfile;
                break;
            case MusicGenre.Default:
            default:
                if (defaultProfile != null) target = defaultProfile;
                break;
        }

        if (target == null)
        {
            Debug.LogWarning($"GenreSceneManager: Target VolumeProfile for {genre} is null.");
            return;
        }

        globalVolume.profile = target;
        Debug.Log($"[GenreSceneManager] Applied post-processing profile for {genre}");

        // Pop Hue Dynamic
        if (genre == MusicGenre.Pop && enablePopDynamicHue)
        {
            if (!globalVolume.profile.TryGet(out _popColorAdjust))
            {
                _popColorAdjust = null;
                Debug.LogWarning("[GenreSceneManager] Pop profile has no ColorAdjustments override. " +
                                 "Add one and enable Hue Shift.");
            }
        }
        else
        {
            _popColorAdjust = null;
        }
    }

    public void SetGenre(MusicGenre genre)
    {
        if (_isSwitching) return;

        _currentGenre = genre;   

        // Visuals
        ApplySkybox(genre);
        ApplyPostProcessing(genre);

        // Environments (additive scenes)
        string targetScene = GetSceneNameForGenre(genre);
        if (string.IsNullOrEmpty(targetScene)) return;
        if (targetScene == _currentEnvScene) return;

        StartCoroutine(SwitchEnvironmentScene(targetScene));
    }

    private string GetSceneNameForGenre(MusicGenre genre)
    {
        switch (genre)
        {
            case MusicGenre.Rock: return rockSceneName;
            case MusicGenre.Pop: return popSceneName;
            case MusicGenre.Classic: return classicSceneName;
            case MusicGenre.Rap: return rapSceneName;
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
