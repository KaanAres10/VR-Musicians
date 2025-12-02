using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public float gameDuration = 180f;  

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
        score += amount;
        UIManager.Instance?.UpdateScore(score);
        Debug.Log("score: "+score);
    }

    public void EndGame()
    {
        UIManager.Instance?.ShowEndPanel();
        Time.timeScale = 0f;
    }

    public bool IsRunning() => isRunning;
}
