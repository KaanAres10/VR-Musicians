using UnityEngine;

public class ClearSpotifyPkce : MonoBehaviour
{
    void Start()
    {
        PlayerPrefs.DeleteKey("PKCE-credentials");
        PlayerPrefs.Save();
        Debug.Log("Cleared PKCE credentials from PlayerPrefs");
    }
}