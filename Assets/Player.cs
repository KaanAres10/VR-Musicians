using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    public int maxHealth = 100;
    public int currentHealth;
    

    public TrackGenreReader trackReader;
    public float energy = 0f;

    public HealthBar healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    void Update()
    {
        if (trackReader.CurrentAudioFeatures != null)
        {
            energy = trackReader.CurrentAudioFeatures.energy;
        }


        AutoRegenHealth();
    }

    void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player Died");
        GameManager.Instance?.EndGame();
    }

    float recoverHealthSpeed(float energy)
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

    void AutoRegenHealth()
    {
       
        if (currentHealth >= maxHealth)
            return;

        float recoverRate = recoverHealthSpeed(energy);

    
        float regenAmount = energy * recoverRate * Time.deltaTime;

        currentHealth += Mathf.RoundToInt(regenAmount);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        healthBar.SetHealth(currentHealth);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            TakeDamage(1);
        }
    }


}
