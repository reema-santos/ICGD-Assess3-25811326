using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        if (scene.buildIndex == firstLevelSceneIndex)
        {
            GameObject quitBtnObj = GameObject.FindGameObjectWithTag("ExitGame");
            Button quitBtn = quitBtnObj.GetComponent<Button>();
            quitBtn.onClick.AddListener(QuitGame);
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
}
