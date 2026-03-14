using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuSaveLoader : MonoBehaviour
{
    public void LoadSavedGame()
    {
        // Lấy lại số màn chơi đã lưu. Nếu chưa lưu bao giờ thì mặc định load màn số 1.
        int levelToLoad = PlayerPrefs.GetInt("SavedLevel", 1);
        SceneManager.LoadScene(levelToLoad);
    }
}