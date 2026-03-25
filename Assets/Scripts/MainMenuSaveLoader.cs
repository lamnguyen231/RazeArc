using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuSaveLoader : MonoBehaviour
{
    private string saveKey = "SavedLevel"; 

    public void LoadSavedGame()
    {
        Time.timeScale = 1f; 
        int levelToLoad = PlayerPrefs.GetInt(saveKey, 1); 
        Debug.Log("Đang load lại màn: " + levelToLoad);
        SceneManager.LoadScene(levelToLoad);
    }

    public void StartNewGame()
    {
        Time.timeScale = 1f;
        PlayerPrefs.DeleteKey(saveKey); 
        SceneManager.LoadScene(1); 
    }
}