using UnityEngine;
using SpotifyAPI.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class TrackGenreReader : MonoBehaviour
{
    [System.Serializable]
    public class ReccoTrack
    {
        public string id;
        public string href;
    }

    [System.Serializable]
    public class ReccoTrackSearchResponse
    {
        public ReccoTrack[] content;
    }

    [System.Serializable]
    public class ReccoAudioFeatures
    {
        public string id;
        public string href;

        public float acousticness;
        public float danceability;
        public float energy;
        public float instrumentalness;
        public int key;
        public float liveness;
        public float loudness;
        public int mode;
        public float speechiness;
        public float tempo;
        public float valence;
    }

    [Header("Post-Processing")]
    public Volume globalVolume;
    public VolumeProfile rockProfile;
    public VolumeProfile popProfile;
    public VolumeProfile otherProfile;

    [Tooltip("How often to check Spotify for a new track (seconds).")]
    public float pollIntervalSeconds = 3f;

    private SpotifyClient _client;
    private string _lastTrackId = null;
    private bool _isRunning = true;


    // for reccobeats API
    private static readonly HttpClient _httpClient = new HttpClient
    {
        BaseAddress = new System.Uri("https://api.reccobeats.com/v1/")
    };

    private readonly Dictionary<string, ReccoAudioFeatures> _featuresCache =
        new Dictionary<string, ReccoAudioFeatures>();


    private void ApplyPostProcessingForGenre(IList<string> genres)
    {
        if (globalVolume == null)
        {
            Debug.LogWarning("Global Volume is not assigned on TrackGenreReader.");
            return;
        }

        VolumeProfile targetProfile = otherProfile; // default

        if (genres != null && genres.Count > 0)
        {
            bool isRock = false;
            bool isPop  = false;

            foreach (var g in genres)
            {
                if (string.IsNullOrEmpty(g))
                    continue;

                string gl = g.ToLowerInvariant();
                if (gl.Contains("rock")) isRock = true;
                if (gl.Contains("pop"))  isPop  = true;
            }

            if (isRock && rockProfile != null)
            {
                targetProfile = rockProfile;
                Debug.Log("Applying Rock Profile");
            }
            else if (isPop && popProfile != null)
            {
                targetProfile = popProfile;
                Debug.Log("Applying Pop Profile");
            }
            else
            {
                Debug.Log("Applying Other Profile");
            }
        }
        else
        {
            Debug.Log("Genre: NONE, Applying Other Profile");
        }

        if (targetProfile == null)
        {
            Debug.LogWarning("Target VolumeProfile is null, nothing to apply.");
            return;
        }

        globalVolume.profile = targetProfile;
    }

    private async void Start()
    {
        // Don’t do anything if we’re not in Play mode
        if (!Application.isPlaying)
            return;

        // Wait until SpotifyService is up & authenticated
        await WaitForSpotifyReady();

        if (!_isRunning)  // object might have been destroyed while waiting
            return;

        var service = SpotifyService.Instance;
        if (service == null || !service.IsConnected)
        {
            Debug.LogError("SpotifyService is not connected after waiting.");
            return;
        }

        _client = service.GetSpotifyClient();
        if (_client == null)
        {
            Debug.LogError("Spotify client is null – is auth done?");
            return;
        }

        // Start the monitoring loop
        await MonitorCurrentTrackLoop();
    }

    private void OnDestroy()
    {
        // Stop the loop when this object is destroyed
        _isRunning = false;
    }

    private async Task MonitorCurrentTrackLoop()
    {
        while (_isRunning && Application.isPlaying)
        {
            try
            {
                var playback = await _client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());

                if (playback?.Item is FullTrack track)
                {
                    // New track started?
                    if (track.Id != _lastTrackId)
                    {
                        _lastTrackId = track.Id;
                        await OnTrackChanged(track);
                    }
                }
                else
                {
                    // Nothing playing
                    if (_lastTrackId != null)
                    {
                        _lastTrackId = null;
                        Debug.Log("No track is currently playing.");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error while checking current track: {e}");
            }

            // Wait before next check (don’t spam the API)
            int ms = Mathf.Max(1000, Mathf.RoundToInt(pollIntervalSeconds * 1000f));
            await Task.Delay(ms);
        }
    }

    private async Task<ReccoAudioFeatures> GetReccoBeatsFeaturesForSpotifyTrack(string spotifyTrackId)
    {
        if (_featuresCache.TryGetValue(spotifyTrackId, out var cached))
            return cached;

        try
        {
            // GET /track?ids={spotifyId}
            string trackJson = await _httpClient.GetStringAsync($"track?ids={spotifyTrackId}");
            Debug.Log($"ReccoBeats /track?ids= response for {spotifyTrackId}: {trackJson}");

            // Parse {"content":[...]}
            var searchResponse = JsonUtility.FromJson<ReccoTrackSearchResponse>(trackJson);

            if (searchResponse == null || searchResponse.content == null || searchResponse.content.Length == 0)
            {
                Debug.LogWarning($"ReccoBeats: no track found for Spotify ID {spotifyTrackId}");
                return null;
            }

            string reccoId = searchResponse.content[0].id;
            if (string.IsNullOrEmpty(reccoId))
            {
                Debug.LogWarning($"ReccoBeats: track found but id is empty for Spotify ID {spotifyTrackId}");
                return null;
            }

            // GET /track/{id}/audio-features
            string featJson = await _httpClient.GetStringAsync($"track/{reccoId}/audio-features");
            Debug.Log($"ReccoBeats /track/{{id}}/audio-features response for {reccoId}: {featJson}");

            var features = JsonUtility.FromJson<ReccoAudioFeatures>(featJson);

            if (features == null)
            {
                Debug.LogWarning($"ReccoBeats: failed to parse audio features for Recco ID {reccoId}");
                return null;
            }

            _featuresCache[spotifyTrackId] = features;
            return features;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("ReccoBeats error: " + ex);
            return null;
        }
    }


    private async Task OnTrackChanged(FullTrack track)
    {
        Debug.Log($"New track detected: {track.Name} - {track.Artists[0].Name}");

        try
        {
            // get artist
            string artistId = track.Artists[0].Id;
            var artist = await _client.Artists.Get(artistId);

            if (artist.Genres != null && artist.Genres.Count > 0)
            {
                Debug.Log($"Artist: {artist.Name}");
                Debug.Log("Genres: " + string.Join(", ", artist.Genres));
                
                ApplyPostProcessingForGenre(artist.Genres);

            }
            else
            {
                Debug.Log($"No genre data available for artist: {artist.Name}");
                ApplyPostProcessingForGenre(null);
            }

            // audio features via ReccoBeats
            var features = await GetReccoBeatsFeaturesForSpotifyTrack(track.Id);

            if (features == null)
            {
                Debug.LogWarning("No ReccoBeats audio features returned for this track.");
                return;
            }

            Debug.Log(
                $"ReccoBeats features for {track.Name}:\n" +
                $"  Tempo: {features.tempo}\n" +
                $"  Energy: {features.energy}\n" +
                $"  Valence: {features.valence}\n" +
                $"  Danceability: {features.danceability}\n" +
                $"  Loudness: {features.loudness}\n" +
                $"  Key: {features.key}  Mode: {features.mode}"
            );

            // TODO: send 'features' + 'artist.Genres' to gameplay / environment scripts here
        }
        catch (APIException apiEx)
        {
            // Spotify returned an HTTP error
            Debug.LogError(
                $"Spotify API error in OnTrackChanged: {apiEx.Message} " +
                $"(HTTP {(int?)apiEx.Response?.StatusCode})\n" +
                $"{apiEx.Response?.Body}"
            );
        }
        catch (System.Exception ex)
        {
            // Any other unexpected error
            Debug.LogError($"Unexpected error in OnTrackChanged: {ex}");
        }
    }

    private async Task WaitForSpotifyReady()
    {
        // Only poll while in Play mode AND while this object is alive
        while (Application.isPlaying && _isRunning)
        {
            var service = SpotifyService.Instance;
            if (service != null && service.IsConnected)
                break;

            await Task.Yield();
        }
    }

}
