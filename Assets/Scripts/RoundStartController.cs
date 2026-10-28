using System.Collections;
using TMPro;
using UnityEngine;

public class RoundStartController : MonoBehaviour
{
    [SerializeField] private GameObject blocker;
    [SerializeField] private TMP_Text countdownText;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip normalMusic;

    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private GhostController[] ghosts;
    [SerializeField] private GameTimer gameTimer;

    [SerializeField] private float stepDuration = 1f;

    void Start()
    {
        if (ghosts == null || ghosts.Length == 0)
            ghosts = FindObjectsByType<GhostController>();

        StartCoroutine(RoundStart());
    }

    IEnumerator RoundStart()
    {
        // lock everything
        pacStudent.ControlEnabled = false;
        foreach (var g in ghosts) g.Frozen = true;

        blocker.SetActive(true);
        countdownText.gameObject.SetActive(true);

        // intro audio plays for the whole countdown
        if (musicSource != null && introMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = introMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        string[] steps = { "3", "2", "1", "GO!" };
        foreach (string s in steps)
        {
            countdownText.text = s;
            yield return new WaitForSeconds(stepDuration);
        }

        // hide the UI and start the game
        countdownText.gameObject.SetActive(false);
        blocker.SetActive(false);

        gameTimer.StartTimer();

        if (musicSource != null)
        {
            musicSource.Stop(); // stops the intro
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        pacStudent.ControlEnabled = true;
        foreach (var g in ghosts) g.Frozen = false;
    }
}