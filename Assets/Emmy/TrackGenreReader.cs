using UnityEngine;
using SpotifyAPI.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;

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

    // To get features for spawn enemies / gameplay
    public ReccoAudioFeatures CurrentAudioFeatures { get; private set; }

    private MusicGenre MapGenresToMusicGenre(IList<string> genres)
    {
        if (genres == null || genres.Count == 0)
            return MusicGenre.Default;

        bool isRock = false;
        bool isPop = false;
        bool isClassic = false;
        bool isRap = false;
        bool isCountry = false;

        foreach (var g in genres)
        {
            if (string.IsNullOrEmpty(g))
                continue;

            string gl = g.ToLowerInvariant();

            if (gl.Contains("rock")) isRock = true;
            if (gl.Contains("pop")) isPop = true;
            if (gl.Contains("classic")) isClassic = true;   // classical, classic rock etc.
            if (gl.Contains("rap") || gl.Contains("hip hop") || gl.Contains("hip-hop")) isRap = true;
            if (gl.Contains("country")) isCountry = true;
        }

        if (isRock) return MusicGenre.Rock;
        if (isPop) return MusicGenre.Pop;
        if (isClassic) return MusicGenre.Classic;
        if (isRap) return MusicGenre.Rap;
        if (isCountry) return MusicGenre.Country;

        return MusicGenre.Default;
    }

    private async void Start()
    {
        if (!Application.isPlaying)
            return;

        await WaitForSpotifyReady();

        if (!_isRunning)
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

        await MonitorCurrentTrackLoop();
    }

    private void OnDestroy()
    {
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
                    if (track.Id != _lastTrackId)
                    {
                        _lastTrackId = track.Id;
                        await OnTrackChanged(track);
                    }
                }
                else
                {
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
            string trackJson = await _httpClient.GetStringAsync($"track?ids={spotifyTrackId}");
            Debug.Log($"ReccoBeats /track?ids= response for {spotifyTrackId}: {trackJson}");

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
            string artistId = track.Artists[0].Id;
            var artist = await _client.Artists.Get(artistId);

            if (artist.Genres != null && artist.Genres.Count > 0)
            {
                Debug.Log($"Artist: {artist.Name}");
                Debug.Log("Genres: " + string.Join(", ", artist.Genres));

                MusicGenre mg = MapGenresToMusicGenre(artist.Genres);
                if (GenreSceneManager.Instance != null)
                {
                    GenreSceneManager.Instance.SetGenre(mg);
                }
            }
            else
            {
                Debug.Log($"No genre data available for artist: {artist.Name}");
                if (GenreSceneManager.Instance != null)
                {
                    GenreSceneManager.Instance.SetGenre(MusicGenre.Default);
                }
            }

            var features = await GetReccoBeatsFeaturesForSpotifyTrack(track.Id);

            if (features != null)
            {
                CurrentAudioFeatures = features;
            }
            else
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
        }
        catch (APIException apiEx)
        {
            Debug.LogError(
                $"Spotify API error in OnTrackChanged: {apiEx.Message} " +
                $"(HTTP {(int?)apiEx.Response?.StatusCode})\n" +
                $"{apiEx.Response?.Body}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Unexpected error in OnTrackChanged: {ex}");
        }
    }

    private async Task WaitForSpotifyReady()
    {
        while (Application.isPlaying && _isRunning)
        {
            var service = SpotifyService.Instance;
            if (service != null && service.IsConnected)
                break;

            await Task.Yield();
        }
    }
}
