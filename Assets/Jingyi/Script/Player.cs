using UnityEngine;

public class Player : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public TrackGenreReader trackReader;
    public float energy = 0f;

    public HealthBar healthBar;
    
    private bool isHealthLow = false;
    
    public float lowHealthThreshold = 50f;
    public float maxHealthThreshold = 90f;

    

    
    
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
        CheckHealthState();
    }

    public void TakeDamage(float damage)
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
        MusicGenre currentGenre = trackReader.getCurrentGenre();
        if (currentGenre == MusicGenre.Rock || currentGenre == MusicGenre.Pop || currentGenre == MusicGenre.Rap)
        {
            return;
        }
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
        if (energy < 0.8f)
        {
            if (energy < 0.1f)
            {
                return  1.0f / (10 * energy);
            }
            return 1.0f / (energy);
        }
        else
        {
            return  1.0f / (2 * energy);
        }
    }

   /* void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(1);
        }
    }*/

    void Die()
    {
        Debug.Log("Player Died");
        GameManager.Instance?.EndGame();
    }
    
    void CheckHealthState()
    {
        // Enter low health
        if (!isHealthLow && currentHealth <= lowHealthThreshold)
        {
            isHealthLow = true;
            UIManager.Instance.ShowLowHealthTip();
        }
        // Exit low health
        else if (isHealthLow && currentHealth >= maxHealthThreshold)
        {
            isHealthLow = false;
            UIManager.Instance.ShowRecoveredTip();
        }
    }
}
