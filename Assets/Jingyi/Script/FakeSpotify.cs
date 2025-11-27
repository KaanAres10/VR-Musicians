using UnityEngine;
using static TrackGenreReader;

public class FakeSpotify : MonoBehaviour
{
    [Header("Energy")]
    public float speed = 1f;       
    public float amplitude = 0.5f; 
    public float offset = 0.5f;    

    [HideInInspector]
    public ReccoAudioFeatures fakeFeatures = new ReccoAudioFeatures();

    float timer = 0f;

    void Start()
    {
        
        fakeFeatures = new ReccoAudioFeatures();
        fakeFeatures.energy = offset;
    }

    void Update()
    {
       
        timer += Time.deltaTime * speed;
        float energy = offset + amplitude * Mathf.Sin(timer);
        energy = Mathf.Clamp01(energy);

        fakeFeatures.energy = energy;
    }
}
