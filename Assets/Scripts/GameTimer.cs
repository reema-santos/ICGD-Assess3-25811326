using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    public float Elapsed { get; private set; } // seconds since "GO!" disappeared
    public bool Running { get; private set; }

    void Awake()
    {
        if (timerText == null) timerText = GetComponent<TMP_Text>();
    }

    void Start()
    {
        ResetTimer();
    }

    void Update()
    {
        if (!Running) return;

        Elapsed += Time.deltaTime;
        UpdateText();
    }

    public void StartTimer() { Running = true; }
    public void StopTimer()  { Running = false; }

    public void ResetTimer()
    {
        Elapsed = 0f;
        Running = false;
        UpdateText();
    }

    void UpdateText()
    {
        int minutes = (int)(Elapsed / 60f);
        int seconds = (int)(Elapsed % 60f);
        int hundredths = (int)((Elapsed * 100f) % 100f);

        timerText.text = $"{minutes:00}:{seconds:00}:{hundredths:00}";
    }
}