using UnityEngine;
using SpotifyAPI.Web;
using System.Threading.Tasks;

public class TrackGenreReader : MonoBehaviour
{
    [Tooltip("How often to check Spotify for a new track (seconds).")]
    public float pollIntervalSeconds = 3f;

    private SpotifyClient _client;
    private string _lastTrackId = null;
    private bool _isRunning = true;

    private async void Start()
    {
        // Don’t do anything if we’re not actually in Play mode
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

    private async Task OnTrackChanged(FullTrack track)
    {
        Debug.Log($"New track detected: {track.Name} - {track.Artists[0].Name}");

        // Get main artist
        string artistId = track.Artists[0].Id;
        var artist = await _client.Artists.Get(artistId);

        if (artist.Genres != null && artist.Genres.Count > 0)
        {
            Debug.Log($"Artist: {artist.Name}");
            Debug.Log("Genres: " + string.Join(", ", artist.Genres));
        }
        else
        {
            Debug.Log($"No genre data available for artist: {artist.Name}");
        }

        // Here you can also trigger your VR visuals / logic:
        // UpdateVisualsFromGenre(artist.Genres);
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
