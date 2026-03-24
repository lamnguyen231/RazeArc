using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlowManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Canvas endCanvas;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    //[SerializeField] private Button nextLevelButton;
    //[SerializeField] private Button retryButton;

    public bool IsEndScreenOpen { get; private set; }
    private string nextLevelName;

    void Start()
    {
        IsEndScreenOpen = false;
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void EndScreenBehaviour()
    {
        Time.timeScale = 0;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        IsEndScreenOpen = true;

        DisableFullPlayerAction();
    }

    void DisableFullPlayerAction()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var look = player.GetComponentInChildren<PlayerLook>(true);
        if (look) look.enabled = false;

        var movement = player.GetComponentInChildren<PlayerMovement>(true);
        if (movement) movement.enabled = false;

        var switcher = player.GetComponentInChildren<WeaponSwitcher>(true);
        if (switcher) switcher.enabled = false;

        var weapons = player.GetComponentsInChildren<WeaponBase>(true);
        foreach (var weapon in weapons)
        {
            if (weapon) weapon.enabled = false; 
        }
    }

    public void ShowWin(string nextLevel)
    {
        if (!IsEndScreenOpen)
        {
            nextLevelName = nextLevel;
            losePanel.SetActive(false);
            winPanel.SetActive(true);
            EndScreenBehaviour();
        }
    }

    public void ShowLose()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(true);
        EndScreenBehaviour();
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextLevelName);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
