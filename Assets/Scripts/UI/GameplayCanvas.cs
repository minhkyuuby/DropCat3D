using UnityEngine;

public class GameplayCanvas : MonoBehaviour
{
    [SerializeField] private GameObject WinPanel;
    [SerializeField] private GameObject LosePanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WinPanel.SetActive(false);
        LosePanel.SetActive(false);
    }

    public void OnWin()
    {
        WinPanel.SetActive(true);
    }

    public void OnLose()
    {
        LosePanel.SetActive(true);
    }
}
