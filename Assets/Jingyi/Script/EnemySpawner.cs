using System.Collections.Generic;
using UnityEngine;
using static TrackGenreReader;
using UnityEngine.SceneManagement;


public class EnemySpawner : MonoBehaviour
{
    public Transform player;
    
    public GameObject enemyPrefab;

    public TrackGenreReader trackReader;
    private float energy;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;            
    public float spawnRadius = 2f;             // small random offset around each spawn point
    public float spawnInterval = 1.5f;

    float timer = 0f;

    public float minInterval = 3f;
    public float maxInterval = 5f;

    public float minSpawnDistanceBetweenEnemies = 2f;
    

    public MusicGenre currentGenre = MusicGenre.Default;

    Dictionary<MusicGenre, float> spawnMultipliers = new Dictionary<MusicGenre, float>()
    {
        { MusicGenre.Rock, 2.5f },
        { MusicGenre.Pop, 1.5f },
        { MusicGenre.Rap, 1.5f },
        { MusicGenre.Classic, 0.5f },
        { MusicGenre.Country, 1.2f },
        { MusicGenre.Default, 1.0f }
    };

    Dictionary<MusicGenre, float> speedMultipliers = new Dictionary<MusicGenre, float>()
    {
        { MusicGenre.Rock, 4.0f },
        { MusicGenre.Pop, 1.8f },
        { MusicGenre.Rap, 4.0f },
        { MusicGenre.Classic, 1.5f },
        { MusicGenre.Country, 1.5f },
        { MusicGenre.Default, 1.0f }
    };

    [Header("Enemy Prefabs per Genre")]
    public GameObject defaultEnemyPrefab;   // fallback
    public GameObject rockEnemyPrefab;
    public GameObject popEnemyPrefab;
    public GameObject rapEnemyPrefab;
    public GameObject classicEnemyPrefab;
    public GameObject countryEnemyPrefab;

    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAllEnemies();
    }

    private void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var obj in enemies)
        {
            Destroy(obj);
        }
    }
    
    void Update()
    {
        currentGenre = trackReader.getCurrentGenre();
        
        // Automatically find all objects tagged "EnemySpawn"
        GameObject[] points = GameObject.FindGameObjectsWithTag("EnemySpawn");

        spawnPoints = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            spawnPoints[i] = points[i].transform;
        }
        
        if (trackReader.CurrentAudioFeatures != null)
        {
            energy = trackReader.CurrentAudioFeatures.energy;
        }

        // higher energy -> smaller interval -> more frequent spawns
        spawnInterval = Mathf.Lerp(maxInterval, minInterval, energy);

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }

        UpdateAllEnemiesSpeed();
    }

    GameObject GetPrefabForGenre(MusicGenre genre)
    {
        switch (genre)
        {
            case MusicGenre.Rock:
                return rockEnemyPrefab != null ? rockEnemyPrefab : defaultEnemyPrefab;

            case MusicGenre.Pop:
                return popEnemyPrefab != null ? popEnemyPrefab : defaultEnemyPrefab;

            case MusicGenre.Rap:
                return rapEnemyPrefab != null ? rapEnemyPrefab : defaultEnemyPrefab;

            case MusicGenre.Classic:
                return classicEnemyPrefab != null ? classicEnemyPrefab : defaultEnemyPrefab;

            case MusicGenre.Country:
                return countryEnemyPrefab != null ? countryEnemyPrefab : defaultEnemyPrefab;

            case MusicGenre.Default:
            default:
                return defaultEnemyPrefab != null ? defaultEnemyPrefab : enemyPrefab;
        }
    }

    int GetSpawnCount(float energy)
    {
        float baseCount;

        if (energy < 0.7f)
            baseCount = 1 + 2 * energy;
        else
            baseCount = 2 + 3 * energy;

        float genreMultiplier = spawnMultipliers[currentGenre];

        return Mathf.CeilToInt(baseCount * genreMultiplier);
    }


    float GetEnemySpeed(float energy)
    {
        float baseSpeed;

        if (energy < 0.7f)
            baseSpeed = 0.5f + 1f * energy;
        else
            baseSpeed = 1f + 1.5f * energy;

        float genreMultiplier = speedMultipliers[currentGenre];

        return baseSpeed * genreMultiplier;
    }


    void SpawnEnemy()
    {
        
        
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No spawnPoints assigned!");
            return;
        }

        int count = GetSpawnCount(energy);

        // Store positions chosen this batch so we don’t place enemies on top of each other
        System.Collections.Generic.List<Vector3> usedPositions = new System.Collections.Generic.List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            bool foundSpot = false;

            for (int attempts = 0; attempts < 10; attempts++)
            {
                // 1) Pick a random spawn point
                Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

                // 2) Optional small random offset around that spawn point
                Vector2 offset2D = Random.insideUnitCircle * spawnRadius;
                Vector3 candidate = sp.position + new Vector3(offset2D.x, 0f, offset2D.y);

                // Use spawn point's height (so enemy is on ground)
                candidate.y = sp.position.y;

                // 3) Check distance to other enemies spawned in this batch
                bool tooClose = false;
                foreach (var pos in usedPositions)
                {
                    if (Vector3.Distance(pos, candidate) < minSpawnDistanceBetweenEnemies)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    spawnPos = candidate;
                    foundSpot = true;
                    break;
                }
            }

            if (!foundSpot)
            {
                // couldn't find non-overlapping spot after several attempts, skip this enemy
                continue;
            }

            usedPositions.Add(spawnPos);

            GameObject prefabToUse = GetPrefabForGenre(currentGenre);
            if (prefabToUse == null)
            {
                Debug.LogWarning("EnemySpawner: No prefab assigned for current genre, skipping spawn.");
                continue;
            }

            GameObject obj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);


            float speed = GetEnemySpeed(energy);
            Enemy enemyComp = obj.GetComponent<Enemy>();
            if (enemyComp != null)
            {
                enemyComp.SetSpeed(speed);
            }
        }
    }

    void UpdateAllEnemiesSpeed()
    {
        float targetSpeed = GetEnemySpeed(energy);

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject obj in enemies)
        {
            Enemy e = obj.GetComponent<Enemy>();
            if (e != null)
            {
                e.speed = Mathf.Lerp(e.speed, targetSpeed, Time.deltaTime * 3f); // smooth change
            }
        }
    }
}
