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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        endPanel.SetActive(false);
        UpdateScore(0);
        UpdateTimer(0);
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
}
