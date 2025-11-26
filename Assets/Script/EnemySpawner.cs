using UnityEngine;
using static TrackGenreReader;

public class EnemySpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;

    public TrackGenreReader trackReader;
    private float energy;


    public float spawnRadius = 10f;
    public float spawnInterval = 1.5f;

    float timer = 0f;


    public float minInterval = 3f;
    public float maxInterval = 5f;


    void Update()
    {
        if (trackReader.CurrentAudioFeatures != null)
        {
            energy = trackReader.CurrentAudioFeatures.energy;
        }



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
            return Mathf.CeilToInt(1 + 3 * energy);
        }
        else
        {
            return Mathf.CeilToInt(2+ 5 * energy);
        }
    }

    float GetEnemySpeed(float energy)
    {
        if (energy < 0.7f)
        {
            return 1f + 1f * energy;      // 3 ~ 6.5
        }
        else
        {
            return 1.5f + 1.5f * energy;      // 8.5 ~ 10
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
