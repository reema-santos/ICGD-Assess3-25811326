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

    [SerializeField] private AudioClip deadMusic;
    [SerializeField] private AudioClip ghostEatAudio;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private float deadDuration = 3f;

    private float endTime;
    public bool PowerActive { get; private set; }
    public float Remaining => PowerActive ? Mathf.Max(0f, endTime - Time.time) : 0f;

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
        PowerActive = true;
        foreach (var g in ghosts) g.SetScared();
        RefreshMusic();
        timerUI.SetActive(true);

        endTime = Time.time + scaredDuration;
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

        PowerActive = false;
        foreach (var g in ghosts) g.SetNormal();
        timerUI.SetActive(false);
        RefreshMusic();
        routine = null;
    }

    // Called by PacStudent when he eats a Scared/Recovering ghost
    public void GhostEaten(GhostController ghost)
    {
        ghost.SetDead();
        if (sfxSource != null && ghostEatAudio != null) sfxSource.PlayOneShot(ghostEatAudio);
        RefreshMusic();
        StartCoroutine(ReviveRoutine(ghost));
    }

    IEnumerator ReviveRoutine(GhostController ghost)
    {
        yield return new WaitForSeconds(deadDuration);
        if (ghost.State != GhostState.Dead) yield break;

        ghost.Revive(); // back to Normal first
        if (PowerActive)
        {
            if (Remaining <= recoveringAt) ghost.SetRecovering();
            else ghost.SetScared();
        }
        RefreshMusic();
    }

    // Priority: any dead ghost > scared timer running > normal
    void RefreshMusic()
    {
        bool anyDead = false;
        foreach (var g in ghosts) if (g.State == GhostState.Dead) anyDead = true;

        AudioClip clip = anyDead ? deadMusic : (PowerActive ? scaredMusic : normalMusic);
        PlayMusic(clip);
    }

    void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return; // don't restart the same track
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void CancelPower()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        PowerActive = false;
        if (timerUI != null) timerUI.SetActive(false);
        RefreshMusic();
    }
}