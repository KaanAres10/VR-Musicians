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
    
    public string CurrentPlaylistName { get; private set; }
    public bool IsInRandomSongsPlaylist { get; private set; }

    private const string RandomSongsPlaylistName = "Random Songs(2X Score)";
    
    // Expanded keyword lists for genre matching
    private static readonly string[] RockKeywords = new[]
    {
        "rock", "hard rock", "alt rock", "alternative rock", "classic rock", "indie rock",
        "metal", "heavy metal", "punk rock", "garage rock", "grunge", "post-rock",
        "progressive rock", "prog rock", "psychedelic rock"
    };

    private static readonly string[] PopKeywords = new[]
    {
        "pop", "soft pop", "electropop", "teen pop", "dance pop", "post-teen pop",
        "k-pop", "j-pop", "indie pop", "bedroom pop", "hyperpop"
    };

    private static readonly string[] ClassicKeywords = new[]
    {
        "classical", "classic", "soundtrack", "orchestra", "orchestral", "film score",
        "movie score", "score", "opera", "symphony", "instrumental soundtrack",
        "classical performance", "chamber orchestra", "soundtrack"
    };

    private static readonly string[] RapKeywords = new[]
    {
        "rap", "hip hop", "hip-hop", "trap", "drill", "gangsta rap", "boom bap",
        "underground hip hop", "alternative hip hop", "r&b rap", "cloud rap"
    };

    private static readonly string[] CountryKeywords = new[]
    {
        "country", "alt-country", "country pop", "country rock", "bluegrass",
        "folk country", "americana", "modern country", "folk"
    };
    
    [Header("Auto Skip / Time Limit")]
    [Tooltip("If true, limit how long a single track can play before we force a switch.")]
    public bool enableAutoSkip = true;

    [Tooltip("Max seconds a single track is allowed to play before we force next.")]
    public float maxTrackPlaySeconds = 45f;  

    private bool _autoSkipTriggeredForCurrentTrack = false;
    private float _currentTrackPlaySeconds = 0f;

    private MusicGenre finalGenre = MusicGenre.Default;

    public MusicGenre getCurrentGenre()
    {
        return finalGenre;
    }

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

            // Rock
            foreach (var key in RockKeywords)
                if (gl.Contains(key)) isRock = true;

            // Pop
            foreach (var key in PopKeywords)
                if (gl.Contains(key)) isPop = true;

            // Classic
            foreach (var key in ClassicKeywords)
                if (gl.Contains(key)) isClassic = true;

            // Rap 
            foreach (var key in RapKeywords)
                if (gl.Contains(key)) isRap = true;

            // Country
            foreach (var key in CountryKeywords)
                if (gl.Contains(key)) isCountry = true;
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
                var playback = await _client.Player.GetCurrentPlayback();

                if (playback?.Item is FullTrack track)
                {
                    await EnsureShuffleEnabled(playback);

                    if (track.Id != _lastTrackId)
                    {
                        _lastTrackId = track.Id;
                        _currentTrackPlaySeconds = 0f;
                        _autoSkipTriggeredForCurrentTrack = false;

                        
                        string playlistName = await TryGetPlaylistName(playback);
                        
                        await OnTrackChanged(track, playlistName);
                    }
                    else
                    {
                        if (playback.IsPlaying)
                        {
                            _currentTrackPlaySeconds += pollIntervalSeconds;
                        }

                        await CheckAutoSkipTrack();
                    }
                }
                else
                {
                    if (_lastTrackId != null)
                    {
                        _lastTrackId = null;
                        _currentTrackPlaySeconds = 0f;
                        _autoSkipTriggeredForCurrentTrack = false;
                        Debug.Log("No track is currently playing.");
                    }
                }
            }
            catch (APIException apiEx)
            {
                Debug.LogError($"Spotify API error while checking current track: {apiEx}");

                if (IsInvalidGrant(apiEx))
                {
                    Debug.LogWarning("Got invalid_grant. Forcing re-auth via SpotifyService...");

                    bool ok = await TryReauthenticateAndUpdateClient();
                    if (!ok)
                    {
                        Debug.LogError("Re-authentication failed. Stopping monitoring loop.");
                        _isRunning = false;
                        break;
                    }

                    continue;
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
    
    private async Task CheckAutoSkipTrack()
    {
        if (!enableAutoSkip) return;
        if (_autoSkipTriggeredForCurrentTrack) return;

        float remaining = maxTrackPlaySeconds - _currentTrackPlaySeconds;

        if (remaining <= 0f)
        {
            await DoAutoSkipNow();
            return;
        }
    }
    
    private async Task DoAutoSkipNow()
    {
        if (_autoSkipTriggeredForCurrentTrack) return;

        _autoSkipTriggeredForCurrentTrack = true;
        Debug.Log("[TrackGenreReader] Performing auto-skip now…");

        try
        {
            await _client.Player.SkipNext(); 
        }
        catch (APIException apiEx)
        {
            Debug.LogError($"[TrackGenreReader] Auto-skip API error: {apiEx}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TrackGenreReader] Auto-skip unexpected error: {ex}");
        }
    }
    
    private async Task EnsureShuffleEnabled(CurrentlyPlayingContext playback)
    {
        try
        {
            bool shuffleOn = playback.ShuffleState;

            if (!shuffleOn)
            {
                Debug.Log("[TrackGenreReader] Shuffle is OFF, enabling shuffle…");
                
                await _client.Player.SetShuffle(new PlayerShuffleRequest(true));

                Debug.Log("[TrackGenreReader] Shuffle enabled on current device.");
            }
        }
        catch (APIException apiEx)
        {
            Debug.LogError($"[TrackGenreReader] Failed to enable shuffle: {apiEx}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TrackGenreReader] Unexpected error enabling shuffle: {ex}");
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

    private async Task OnTrackChanged(FullTrack track, string playlistName)
    {
        _autoSkipTriggeredForCurrentTrack = false;  
        _currentTrackPlaySeconds = 0f;
        
        Debug.Log($"New track detected: {track.Name} - {track.Artists[0].Name}");
        
        CurrentPlaylistName = playlistName;

        bool isRandomSongs =
            !string.IsNullOrEmpty(playlistName) &&
            string.Equals(playlistName, RandomSongsPlaylistName, System.StringComparison.OrdinalIgnoreCase);

        IsInRandomSongsPlaylist = isRandomSongs;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetRandomSongsActive(isRandomSongs);
        }

        if (isRandomSongs)
        {
            Debug.Log("[TrackGenreReader] Random Songs playlist detected – score will be doubled.");
        }

        try
        {
            string artistId = track.Artists[0].Id;
            var artist = await _client.Artists.Get(artistId);

            finalGenre = MusicGenre.Default;
            string spotifyGenresDebug = "(none)";

            if (artist.Genres != null && artist.Genres.Count > 0)
            {
                spotifyGenresDebug = string.Join(", ", artist.Genres);
                finalGenre = MapGenresToMusicGenre(artist.Genres);
            }

            Debug.Log($"Artist: {artist.Name}");
            Debug.Log("Spotify genres: " + spotifyGenresDebug);
            Debug.Log($"Genre from Spotify mapping: {finalGenre}");

            // Fallback: if no clear genre from Spotify, try artist-name-based mapping
            if (finalGenre == MusicGenre.Default)
            {
                var fromName = MapArtistNameToMusicGenre(artist.Name);
                if (fromName != MusicGenre.Default)
                {
                    finalGenre = fromName;
                    Debug.Log($"[TrackGenreReader] Using artist-name fallback for '{artist.Name}': {finalGenre}");
                }
                else
                {
                    Debug.Log("[TrackGenreReader] No genre from Spotify or artist-name table, using Default.");
                }
            }

            var features = await GetReccoBeatsFeaturesForSpotifyTrack(track.Id);

            if (features != null)
            {
                CurrentAudioFeatures = features;
            }
            else
            {
                CurrentAudioFeatures = GetDefaultFeaturesForGenre(finalGenre, track.Id);
                Debug.LogWarning("[TrackGenreReader] No ReccoBeats features, using genre presets.");
            }

            if (CurrentAudioFeatures != null)
            {
                var f = CurrentAudioFeatures;
                Debug.Log(
                    $"Effective features for {track.Name}:\n" +
                    $"  Tempo: {f.tempo}\n" +
                    $"  Energy: {f.energy}\n" +
                    $"  Valence: {f.valence}\n" +
                    $"  Danceability: {f.danceability}\n" +
                    $"  Loudness: {f.loudness}\n" +
                    $"  Acousticness: {f.acousticness}\n" +
                    $"  Instrumentalness: {f.instrumentalness}\n" +
                    $"  Speechiness: {f.speechiness}\n" +
                    $"  Key: {f.key}  Mode: {f.mode}"
                );
            }
            
            if (GenreSceneManager.Instance != null)
            {
                Debug.Log($"[TrackGenreReader] Applying final genre to scene: {finalGenre}");
                
                GenreSceneManager.Instance.SetGenre(finalGenre);
            }
            else
            {
                Debug.LogWarning("[TrackGenreReader] GenreSceneManager.Instance is null, cannot apply genre.");
            }
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

        private bool IsInvalidGrant(APIException ex)
    {
        if (ex.Message != null && ex.Message.Contains("invalid_grant"))
            return true;

        var bodyObj = ex.Response?.Body;
        string body = bodyObj?.ToString();

        if (!string.IsNullOrEmpty(body) && body.Contains("invalid_grant"))
            return true;

        return false;
    }


    private async Task<bool> TryReauthenticateAndUpdateClient()
    {
        var service = SpotifyService.Instance;
        if (service == null)
        {
            Debug.LogError("SpotifyService.Instance is null, cannot re-authenticate.");
            return false;
        }

        try
        {
            // clear tokens + start login flow again
            service.ResetAndReauthorize(removeSavedAuth: true);

            // wait until SpotifyService reports that it’s connected again
            await WaitForSpotifyReady();

            if (!service.IsConnected)
            {
                Debug.LogError("SpotifyService is still not connected after re-authentication.");
                return false;
            }

            _client = service.GetSpotifyClient();
            if (_client == null)
            {
                Debug.LogError("Spotify client is null after re-authentication.");
                return false;
            }

            Debug.Log("Re-authentication successful, SpotifyClient updated.");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to re-authenticate: {e}");
            return false;
        }
    }
    
    private async Task<string> TryGetPlaylistName(CurrentlyPlayingContext playback)
    {
        try
        {
            var ctx = playback.Context;
            if (ctx == null || string.IsNullOrEmpty(ctx.Uri))
                return null;

            // We only care if context is a playlist
            if (!string.Equals(ctx.Type, "playlist", System.StringComparison.OrdinalIgnoreCase))
                return null;

            string uri = ctx.Uri; 
            string playlistId = null;

            var parts = uri.Split(':');
            if (parts.Length >= 3 && parts[1] == "playlist")
            {
                playlistId = parts[2];
            }
            else if (parts.Length >= 5 && parts[3] == "playlist")
            {
                playlistId = parts[4];
            }

            if (string.IsNullOrEmpty(playlistId))
                return null;

            var playlist = await _client.Playlists.Get(playlistId);
            string name = playlist?.Name;
            if (!string.IsNullOrEmpty(name))
            {
                Debug.Log($"[TrackGenreReader] Current playlist: {name}");
            }
            return name;
        }
        catch (APIException ex)
        {
            Debug.LogError($"Spotify API error in TryGetPlaylistName: {ex}");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in TryGetPlaylistName: {ex}");
            return null;
        }
    }
    
    private MusicGenre MapArtistNameToMusicGenre(string artistName)
    {
        if (string.IsNullOrEmpty(artistName))
            return MusicGenre.Default;

        string nameLower = artistName.ToLowerInvariant();

        foreach (var entry in ArtistNameGenreMap)
        {
            if (nameLower.Contains(entry.name))
                return entry.genre;
        }

        return MusicGenre.Default;
    }
    
    
    private static readonly (string name, MusicGenre genre)[] ArtistNameGenreMap = new[]
{
    // POP
    ("taylor swift", MusicGenre.Pop),
    ("rihanna", MusicGenre.Pop),
    ("dua lipa", MusicGenre.Pop),
    ("ed sheeran", MusicGenre.Pop),
    ("katy perry", MusicGenre.Pop),
    ("lady gaga", MusicGenre.Pop),
    ("ariana grande", MusicGenre.Pop),
    ("the weeknd", MusicGenre.Pop),
    ("justin bieber", MusicGenre.Pop),
    ("selena gomez", MusicGenre.Pop),
    ("billie eilish", MusicGenre.Pop),
    ("olivia rodrigo", MusicGenre.Pop),
    ("bruno mars", MusicGenre.Pop),
    ("charlie puth", MusicGenre.Pop),
    ("calvin harris", MusicGenre.Pop),
    ("will.i.am", MusicGenre.Pop),
    ("jennifer lopez", MusicGenre.Pop),
    ("pitbull", MusicGenre.Pop),
    ("lmfao", MusicGenre.Pop),
    ("jessie j", MusicGenre.Pop),
    ("sia", MusicGenre.Pop),
    

    // ROCK
    ("metallica", MusicGenre.Rock),
    ("ac/dc", MusicGenre.Rock),
    ("acdc", MusicGenre.Rock),
    ("nirvana", MusicGenre.Rock),
    ("queen", MusicGenre.Rock),
    ("led zeppelin", MusicGenre.Rock),
    ("pink floyd", MusicGenre.Rock),
    ("green day", MusicGenre.Rock),
    ("foo fighters", MusicGenre.Rock),
    ("linkin park", MusicGenre.Rock),
    ("red hot chili peppers", MusicGenre.Rock),
    ("imagine dragons", MusicGenre.Rock),

    // RAP / HIP-HOP
    ("eminem", MusicGenre.Rap),
    ("drake", MusicGenre.Rap),
    ("kendrick lamar", MusicGenre.Rap),
    ("kanye west", MusicGenre.Rap),
    ("ye", MusicGenre.Rap),
    ("travisscott", MusicGenre.Rap),
    ("travis scott", MusicGenre.Rap),
    ("lil wayne", MusicGenre.Rap),
    ("lil nas x", MusicGenre.Rap),
    ("nicki minaj", MusicGenre.Rap),
    ("50 cent", MusicGenre.Rap),
    ("snoop dogg", MusicGenre.Rap),
    ("cardi b", MusicGenre.Rap),
    ("post malone", MusicGenre.Rap),
    ("soulja boy", MusicGenre.Rap),

    // COUNTRY
    ("luke combs", MusicGenre.Country),
    ("morgan wallen", MusicGenre.Country),
    ("chris stapleton", MusicGenre.Country),
    ("blake shelton", MusicGenre.Country),
    ("carrie underwood", MusicGenre.Country),
    ("keith urban", MusicGenre.Country),
    ("dolly parton", MusicGenre.Country),
    ("shania twain", MusicGenre.Country),
    ("rednex", MusicGenre.Country),

    // CLASSIC / SOUNDTRACK
    ("hans zimmer", MusicGenre.Classic),
    ("john williams", MusicGenre.Classic),
    ("ludwig van beethoven", MusicGenre.Classic),
    ("beethoven", MusicGenre.Classic),
    ("mozart", MusicGenre.Classic),
    ("johann sebastian bach", MusicGenre.Classic),
    ("bach", MusicGenre.Classic),
    ("tchaikovsky", MusicGenre.Classic),
    ("yiruma", MusicGenre.Classic),
    ("max richter", MusicGenre.Classic),
    ("ennio morricone", MusicGenre.Classic),
    ("carlos rafael rivera", MusicGenre.Classic),
};
    
  private ReccoAudioFeatures GetDefaultFeaturesForGenre(MusicGenre genre, string trackId = null)
{
    switch (genre)
    {
        case MusicGenre.Pop:
            return new ReccoAudioFeatures
            {
                id = trackId,
                acousticness = 0.2f,
                danceability = 0.85f,
                energy = 0.8f,
                instrumentalness = 0.1f,
                liveness = 0.2f,
                loudness = -6f,
                mode = 1,
                speechiness = 0.12f,
                tempo = 118f,
                valence = 0.75f,
                key = 0,
            };

        case MusicGenre.Rock:
            return new ReccoAudioFeatures
            {
                id = trackId,
                acousticness = 0.15f,
                danceability = 0.55f,
                energy = 0.85f,
                instrumentalness = 0.1f,
                liveness = 0.25f,
                loudness = -5f,
                mode = 1,
                speechiness = 0.08f,
                tempo = 130f,
                valence = 0.5f,
                key = 7,
            };

        case MusicGenre.Rap:
            return new ReccoAudioFeatures
            {
                id = trackId,
                acousticness = 0.1f,
                danceability = 0.8f,
                energy = 0.75f,
                instrumentalness = 0.05f,
                liveness = 0.3f,
                loudness = -7f,
                mode = 1,
                speechiness = 0.45f,
                tempo = 92f,
                valence = 0.6f,
                key = 9,
            };

        case MusicGenre.Country:
            return new ReccoAudioFeatures
            {
                id = trackId,
                acousticness = 0.7f,
                danceability = 0.55f,
                energy = 0.6f,
                instrumentalness = 0.2f,
                liveness = 0.25f,
                loudness = -10f,
                mode = 1,
                speechiness = 0.12f,
                tempo = 100f,
                valence = 0.7f,
                key = 2,
            };

        case MusicGenre.Classic:
            return new ReccoAudioFeatures
            {
                id = trackId,
                acousticness = 0.95f,
                danceability = 0.1f,
                energy = 0.3f,
                instrumentalness = 0.98f,
                liveness = 0.2f,
                loudness = -18f,
                mode = 1,
                speechiness = 0.02f,
                tempo = 80f,
                valence = 0.4f,
                key = 4,
            };

        case MusicGenre.Default:
        default:
            // Neutral-ish fallback so gameplay always has something
            return new ReccoAudioFeatures
            {
                id = trackId,
                acousticness = 0.4f,
                danceability = 0.5f,
                energy = 0.5f,
                instrumentalness = 0.3f,
                liveness = 0.2f,
                loudness = -12f,
                mode = 1,
                speechiness = 0.1f,
                tempo = 110f,
                valence = 0.5f,
                key = 0,
            };
    }
}
}
