using TMPro;
using UnityEngine;

public class GameplayCountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameManager gameManager;

    private void OnEnable()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.RegisterTimeListener(UpdateTime);
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.UnregisterTimeListener(UpdateTime);
        }
    }

    private void UpdateTime(float timeRemaining)
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
    
}
