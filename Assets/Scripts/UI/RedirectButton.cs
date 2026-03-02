using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RedirectButton : MonoBehaviour
{
    [SerializeField] private Button button;

    void OnValidate()
    {
        button = GetComponent<Button>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button.onClick.AddListener(HandleButtonClicked);
    }

    void HandleButtonClicked()
    {
        SceneManager.LoadScene("MainMenu");   
    }
}
