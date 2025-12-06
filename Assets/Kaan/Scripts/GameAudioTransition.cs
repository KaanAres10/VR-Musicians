using UnityEngine;
using System.Collections;

public class GameAudioTransition : MonoBehaviour
{
    public static GameAudioTransition Instance { get; private set; }

    [Header("Volumes")]
    [Range(0f, 1f)] public float normalVolume = 1f;
    [Range(0f, 1f)] public float duckedVolume = 0.3f;

    [Header("Timings")]
    public float fadeOutTime = 0.4f;
    public float fadeInTime = 0.6f;

    private Coroutine _activeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Duck game audio, hold it low during the switch, then fade back up.
    /// holdLowSeconds is roughly how long until the next song kicks in.
    /// </summary>
    public void PlayTransition(float holdLowSeconds)
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(DoTransition(holdLowSeconds));
    }

    private IEnumerator DoTransition(float holdLowSeconds)
    {
        float startVol = AudioListener.volume;

        // Fade down
        float t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(startVol, duckedVolume, t / fadeOutTime);
            yield return null;
        }
        AudioListener.volume = duckedVolume;

        // Hold low until auto-skip happens
        if (holdLowSeconds > 0f)
            yield return new WaitForSecondsRealtime(holdLowSeconds);

        // Fade back up
        t = 0f;
        startVol = AudioListener.volume;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(startVol, normalVolume, t / fadeInTime);
            yield return null;
        }
        AudioListener.volume = normalVolume;

        _activeRoutine = null;
    }
}