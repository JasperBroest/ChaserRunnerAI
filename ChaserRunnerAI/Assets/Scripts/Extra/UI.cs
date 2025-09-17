using UnityEngine;

public class UIMenu : MonoBehaviour
{
    private static UIMenu _instance;
    public static UIMenu Instance => _instance;

    [SerializeField] private TMPro.TextMeshProUGUI tmpText;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void QuitGame()
    {
        FindFirstObjectByType<Chaser>().EndEpisode();
    }

    public void Output(string outputText)
    {
        tmpText.text += "\n" + outputText;
    }
}
