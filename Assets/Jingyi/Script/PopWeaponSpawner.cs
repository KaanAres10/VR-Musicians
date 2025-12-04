using System.Collections.Generic;
using UnityEngine;

public class PopWeaponSpawner : MonoBehaviour
{
    [Header("Ball Settings")]
    [Tooltip("Ball prefab with Rigidbody, XRGrabInteractable, PopWeapon, etc.")]
    public GameObject ballPrefab;

    [Tooltip("How many balls to spawn around the player initially.")]
    public int initialBallCount = 6;

    [Tooltip("Maximum number of balls alive at once (for respawn).")]
    public int maxBalls = 10;

    [Header("Spawn Area")]
    [Tooltip("Center for spawning. If null, will use this GameObject's transform.")]
    public Transform playerCenter;

    [Tooltip("Horizontal radius from the player.")]
    public float radius = 5.0f;

    [Tooltip("Height offset from player center (e.g. chest height).")]
    public float heightOffset = 0.5f;

    [Header("Respawn")]
    [Tooltip("Automatically respawn balls if count drops below maxBalls.")]
    public bool enableRespawn = true;

    [Tooltip("Seconds between respawn checks.")]
    public float respawnInterval = 2.0f;

    private List<GameObject> spawnedBalls = new List<GameObject>();
    private float respawnTimer = 0f;

    private void Start()
    {
        if (playerCenter == null)
            playerCenter = transform; // fallback

        SpawnInitialBalls();
    }

    private void Update()
    {
        if (!enableRespawn)
            return;

        // Clean up destroyed/null balls from the list
        for (int i = spawnedBalls.Count - 1; i >= 0; i--)
        {
            if (spawnedBalls[i] == null)
                spawnedBalls.RemoveAt(i);
        }

        // Respawn logic
        respawnTimer += Time.deltaTime;
        if (respawnTimer >= respawnInterval)
        {
            respawnTimer = 0f;

            int currentCount = spawnedBalls.Count;
            if (currentCount < maxBalls)
            {
                int toSpawn = maxBalls - currentCount;
                for (int i = 0; i < toSpawn; i++)
                    SpawnOneBall();
            }
        }
    }

    private void SpawnInitialBalls()
    {
        for (int i = 0; i < initialBallCount; i++)
        {
            SpawnOneBall(i, initialBallCount);
        }
    }

    private void SpawnOneBall(int index = -1, int total = 0)
    {
        if (ballPrefab == null || playerCenter == null)
            return;

        Vector3 center = playerCenter.position + Vector3.up * heightOffset;

        float angle;
        if (index >= 0 && total > 0)
        {
            // evenly spaced for initial spawn
            angle = (index / (float)total) * Mathf.PI * 2f;
        }
        else
        {
            // random angle for respawn
            angle = Random.Range(0f, Mathf.PI * 2f);
        }

        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        Vector3 spawnPos = center + offset;

        GameObject ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
        spawnedBalls.Add(ball);
    }

    // 🔥 Call this whenever the player has been teleported to a new environment
    public void RespawnAroundPlayer()
    {
        // Destroy all existing balls
        for (int i = spawnedBalls.Count - 1; i >= 0; i--)
        {
            if (spawnedBalls[i] != null)
                Destroy(spawnedBalls[i]);
        }

        spawnedBalls.Clear();
        respawnTimer = 0f;

        // Spawn fresh balls around the *current* playerCenter position
        SpawnInitialBalls();
    }
}
