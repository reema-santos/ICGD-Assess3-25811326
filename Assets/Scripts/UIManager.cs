using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadFirstLevel()
    {
        SceneManager.LoadScene("MainLevel");
    }

    public void QuitGame()
    {
        #if UNITY_STANDALONE
            Application.Quit();
        #endif
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int firstLevelSceneIndex = SceneUtility.GetBuildIndexByScenePath("MainLevel");
        int startSceneIndex = SceneUtility.GetBuildIndexByScenePath("StartScene");
        if (scene.buildIndex == firstLevelSceneIndex)
        {
            GameObject quitBtnObj = GameObject.FindGameObjectWithTag("ExitGame");
            Button quitBtn = quitBtnObj.GetComponent<Button>();
            quitBtn.onClick.AddListener(QuitGame);
        }

        if (scene.buildIndex == startSceneIndex)
        {
            GameObject highScore = GameObject.FindGameObjectWithTag("HighScore");
            GameObject time = GameObject.FindGameObjectWithTag("RecordTime");
            TMP_Text highScoreText = highScore.GetComponent<TMP_Text>();
            TMP_Text timeText = time.GetComponent<TMP_Text>();

            highScoreText.text = $"{PlayerPrefs.GetInt("HighScore", 0):D6}";

            float recordTime = PlayerPrefs.GetFloat("RecordTime", 0);
            timeText.text = TimeToString(recordTime);

            Debug.Log("Loaded Start Scene");
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    string TimeToString(float time)
    {
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);
        int hundredths = (int)((time * 100f) % 100f);

        return $"{minutes:00}:{seconds:00}:{hundredths:00}";
    }
}
