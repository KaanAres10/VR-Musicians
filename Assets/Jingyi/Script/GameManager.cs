using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public float gameDuration = 180f;  
    
    [Header("Score Multiplier")]
    public float defaultScoreMultiplier = 1f;
    public float randomSongsMultiplier = 2f;

    private float currentScoreMultiplier;

    private float remainingTime;

    [SerializeField]
    private int score = 0;
    private bool isRunning = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        remainingTime = gameDuration;
        score = 0;
        isRunning = true;
        
        currentScoreMultiplier = defaultScoreMultiplier;
    }

    void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            EndGame();
        }

        
        UIManager.Instance?.UpdateTimer(remainingTime);
    }

    public void AddScore(int amount)
    {
        int finalAmount = Mathf.RoundToInt(amount * currentScoreMultiplier);
        score += finalAmount;
        UIManager.Instance?.UpdateScore(score);
        Debug.Log("score: "+score);
    }

    public void EndGame()
    {
        UIManager.Instance?.ShowEndPanel();
        Time.timeScale = 0f;
    }
    
    public void SetRandomSongsActive(bool active)
    {
        currentScoreMultiplier = active ? randomSongsMultiplier : defaultScoreMultiplier;
        Debug.Log($"[GameManager] Score multiplier set to {currentScoreMultiplier} (Random Songs active = {active})");
    }

    public bool IsRunning() => isRunning;
}
