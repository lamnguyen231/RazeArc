using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Music Clips")]
    public AudioClip level1Music;
    public AudioClip level2Music;
    public AudioClip level3Music;

    private AudioSource audioSource;
    private string currentScene = "";

    private void Start()
    {
        string startingScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayMusicForScene(startingScene);
    }
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                       UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    void PlayMusicForScene(string sceneName)
    {
        AudioClip targetClip = sceneName switch
        {
            "Lv1" => level1Music,
            "Lv2" => level2Music,
            "Lv3" => level3Music,
            _ => null
        };

        if (targetClip == null || audioSource.clip == targetClip) return;

        audioSource.clip = targetClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    //IEnumerator FadeToNewTrack(AudioClip newClip)
    //{
    //    // Fade out
    //    while (audioSource.volume > 0)
    //    {
    //        audioSource.volume -= Time.deltaTime * 2f;
    //        yield return null;
    //    }

    //    audioSource.clip = newClip;
    //    audioSource.Play();

    //    // Fade in
    //    while (audioSource.volume < 0.5f)
    //    {
    //        audioSource.volume += Time.deltaTime * 2f;
    //        yield return null;
    //    }
    //}
}