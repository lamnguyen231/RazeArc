using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    void Start()
    {
        // Đảm bảo game chạy bình thường lúc mới vào
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Bấm phím P để bật/tắt Pause
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Trả lại thời gian bình thường
        isPaused = false;
        
        // Giấu chuột đi để bắn súng (nếu game bạn là FPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Đóng băng mọi hoạt động trong game
        isPaused = true;

        // Hiện chuột lên để bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SaveAndExit()
    {
        // Lưu lại Màn chơi hiện tại (Lấy số thứ tự trong Build Settings)
        PlayerPrefs.SetInt("SavedLevel", SceneManager.GetActiveScene().buildIndex);
        PlayerPrefs.Save();
        Debug.Log("Đã lưu game tại màn: " + SceneManager.GetActiveScene().buildIndex);

        // BẮT BUỘC: Phải trả lại thời gian về 1 trước khi sang scene khác, nếu không Menu sẽ bị đơ
        Time.timeScale = 1f; 
        
        // Trở về Main Menu (Giả sử Main Menu ở số 0 trong Build Settings)
        SceneManager.LoadScene(0); 
    }
}