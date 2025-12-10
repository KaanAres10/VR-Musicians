using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public GameObject endPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI tipText;
    public GameObject tipPanel;
    
    [Header("Tip Settings")]
    public float tipDuration = 3f;
    private float tipTimer = 0f;   
    
    void Start()
    {
        HideTip();
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        endPanel.SetActive(false);
        
        UpdateScore(0);
        UpdateTimer(0);
    }
    
    void Update()
    {
        // Handle auto-hide timer
        if (tipPanel != null && tipPanel.activeSelf && tipTimer > 0f)
        {
            tipTimer -= Time.deltaTime;
            if (tipTimer <= 0f)
            {
                HideTip();
            }
        }
    }
    
 
    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (finalScoreText != null)
            finalScoreText.text = $"Score: {score}";
    }

   
    public void UpdateTimer(float remainingTime)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void ShowEndPanel()
    {
        if (endPanel != null)
            endPanel.SetActive(true);
    }
    
    public void ShowLowHealthTip()
    {
        if (tipText == null || tipPanel == null) return;

        tipText.text = "Low Health! Tell DJ to choose a Classic/Country song!";
        tipPanel.SetActive(true);
        tipTimer = tipDuration;   // start/reset timer
    }

    public void ShowRecoveredTip()
    {
        if (tipText == null || tipPanel == null) return;

        tipText.text = "Health Restored! Tell DJ to choose a Rock/Pop/Rap song!";
        tipPanel.SetActive(true);
        tipTimer = tipDuration;   // start/reset timer
    }

    public void HideTip()
    {
        if (tipPanel != null)
            tipPanel.SetActive(false);

        tipTimer = 0f; // stop timer
    }
}
