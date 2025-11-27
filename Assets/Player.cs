using UnityEngine;

public class Player : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public TrackGenreReader trackReader;
    public float energy = 0f;

    public HealthBar healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealth(currentHealth);
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
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void AutoRegenHealth()
    {
        if (currentHealth >= maxHealth)
            return;

        float recoverRate = recoverHealthSpeed(energy); // HP per second
        float regenAmount = recoverRate * Time.deltaTime;

        currentHealth += regenAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        healthBar.SetHealth(currentHealth);
    }

    float recoverHealthSpeed(float energy)
    {
        if (energy < 0.7f)
        {
            if (energy < 0.1f)
            {
                return  1.0f / (2 * energy);
            }
            return 1.0f / energy;      
        }
        else
        {
            return 1.0f / energy;  
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            TakeDamage(1);
        }
    }

    void Die()
    {
        Debug.Log("Player Died");
        GameManager.Instance?.EndGame();
    }
}
