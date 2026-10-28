using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject blocker;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private GhostController[] ghosts;
    [SerializeField] private float duration = 3.0f;
    private bool started;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (ghosts == null || ghosts.Length == 0)
            ghosts = FindObjectsByType<GhostController>();
    }

    IEnumerator GameOver()
    {
        // lock everything
        pacStudent.ControlEnabled = false;
        foreach (var g in ghosts) g.Frozen = true;

        blocker.SetActive(true);
        gameOverText.gameObject.SetActive(true);
        gameTimer.StopTimer();
        yield return new WaitForSeconds(duration);

        SceneManager.LoadScene("StartScene");
    }

    public void BeginGameOver()
    {
        if (started) return;
        started = true;
        StartCoroutine(GameOver());
    }
}
