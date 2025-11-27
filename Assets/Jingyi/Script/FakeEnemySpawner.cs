using UnityEngine;
using static TrackGenreReader;

public class FakeEnemySpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;

    public FakeSpotify trackReader;
    private float energy;


    public float spawnRadius = 10f;
    public float spawnInterval = 1.5f;

    float timer = 0f;



    void Update()
    {
        if (trackReader.fakeFeatures != null)
        {
            energy = trackReader.fakeFeatures.energy;
        }



        float minInterval = 0.5f;
        float maxInterval = 1.5f;
        spawnInterval = Mathf.Lerp(maxInterval, minInterval, energy); // higher energy - spawn enermy faster and increase moving speed, and more enermies

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }

        UpdateAllEnemiesSpeed();

    }

    int GetSpawnCount(float energy)
    {
        if (energy < 0.7f)
        {
            return Mathf.CeilToInt(5 + 5 * energy);
        }
        else
        {
            return Mathf.CeilToInt(8 + 8 * energy);
        }
    }

    float GetEnemySpeed(float energy)
    {
        if (energy < 0.7f)
        {
            return 1f + 2f * energy;      
        }
        else
        {
            return 1.5f + 2f * energy;     
        }
    }


    void SpawnEnemy()
    {
        int count = GetSpawnCount(energy);

        for (int i = 0; i < count; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = player.position + new Vector3(randomDir.x, randomDir.y, 0) * spawnRadius;

            GameObject obj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            float speed = GetEnemySpeed(energy);
            obj.GetComponent<Enemy>().SetSpeed(speed);

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
                //e.SetSpeed(speed);
                e.speed = Mathf.Lerp(e.speed, targetSpeed, Time.deltaTime * 3f); // change moving speed smoothly
            }
        }
    }



}
