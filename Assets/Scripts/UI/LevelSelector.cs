using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required namespace

public class LevelSelector : MonoBehaviour
{
    public string levelName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(EnterLevel);
    }

    void EnterLevel()
    {
        SceneManager.LoadScene(levelName);
    }
}
