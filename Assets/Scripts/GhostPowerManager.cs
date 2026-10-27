using System.Collections;
using TMPro;
using UnityEngine;

public class GhostPowerManager : MonoBehaviour
{
    [SerializeField] private GhostController[] ghosts; // all 4
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip scaredMusic;
    [SerializeField] private GameObject timerUI;
    [SerializeField] private TMP_Text timerText;

    [SerializeField] private float scaredDuration = 10f;
    [SerializeField] private float recoveringAt = 3f;

    private Coroutine routine;

    void Start()
    {
        if (timerUI != null) timerUI.SetActive(false);
    }

    public void StartPowerState()
    {
        // Eating another power pill mid-timer restarts it
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PowerRoutine());
    }

    IEnumerator PowerRoutine()
    {
        foreach (var g in ghosts) g.SetScared();
        PlayMusic(scaredMusic);
        timerUI.SetActive(true);

        float endTime = Time.time + scaredDuration;
        bool recovering = false;

        while (Time.time < endTime)
        {
            float remaining = endTime - Time.time;
            timerText.text = Mathf.CeilToInt(remaining).ToString();

            if (!recovering && remaining <= recoveringAt)
            {
                recovering = true;
                foreach (var g in ghosts) g.SetRecovering();
            }
            yield return null;
        }

        foreach (var g in ghosts) g.SetNormal(); // dead ghosts are skipped inside SetNormal
        PlayMusic(normalMusic);
        timerUI.SetActive(false);
        routine = null;
    }

    void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }
}