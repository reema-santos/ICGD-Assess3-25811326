using System;
// using Unity.VisualScripting;
// using UnityEditor.PackageManager;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private GameObject pacStudent;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float zDepth = -0.1f;
    [SerializeField] private Vector2Int startGridPos = new Vector2Int(1, -1);
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private AudioClip movementAudio;
    [SerializeField] private AudioClip eatingPelletAudio;
    [SerializeField] private AudioClip wallCollideAudio;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private ParticleSystem collidePs;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private List<UnityEngine.UI.Image> lives;
    [SerializeField] private GhostPowerManager ghostManager;
    [SerializeField] private GhostController[] ghosts;
    [SerializeField] private ParticleSystem deathPs;
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] private float deathDuration = 1.5f;
    [SerializeField] private float dieDistance = 0.5f;
    [SerializeField] private int ghostPoints = 300;
    [SerializeField] private GameOverController gameOverController;

    private Tweener tweener;
    private int[,] fullMap;
    private KeyCode? lastInput;
    private KeyCode? currentInput;
    private Vector2Int gridPos;
    private Animator animator;
    private AudioSource audioSource;
    private bool wallSoundPlayed;
    private int points; // current score 
    private int highScore = 0;
    private float recordTime = 0;
    private const string HighScoreKey = "HighScore";
    private const string RecordTimeKey = "RecordTime";
    private bool isDying;
    public bool ControlEnabled { get; set; }   // false until the countdown finishes

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tweener = GetComponent<Tweener>();
        if (levelGenerator == null)
            levelGenerator = FindAnyObjectByType<LevelGenerator>();

        gridPos = startGridPos;
        pacStudent.transform.position = GridToWorld(gridPos);

        animator = pacStudent.GetComponent<Animator>();
        if (audioSource == null) audioSource = pacStudent.GetComponent<AudioSource>();

        if (pointsText != null) pointsText.text = "000000";

        if (ghosts == null || ghosts.Length == 0)
            ghosts = FindObjectsByType<GhostController>();
        
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0); // 0 if doesn't exist
        recordTime = PlayerPrefs.GetFloat(RecordTimeKey, 0);
        Debug.Log($"Loaded: high score is {highScore} at {recordTime} seconds");
    }

    // Update is called once per frame
    void Update()
    {
        if (fullMap == null)
        {
            fullMap = levelGenerator.fullMap;
            if (fullMap == null) return;
            pacStudent.transform.position = GridToWorld(gridPos);
        }

        if (!ControlEnabled) return;
        
        if (isDying) return;

        if (TouchingNormalGhost())
        {
            StartCoroutine(DeathSequence());
            return;
        }

        EatTouchingGhosts();

        if (Input.GetKeyDown(KeyCode.W)) lastInput = KeyCode.W;
        if (Input.GetKeyDown(KeyCode.A)) lastInput = KeyCode.A;
        if (Input.GetKeyDown(KeyCode.S)) lastInput = KeyCode.S;
        if (Input.GetKeyDown(KeyCode.D)) lastInput = KeyCode.D;

        if (!tweener.IsTweening)
        {
            int tile = GetTile(gridPos);
            if (IsNormalPellet(tile)) {
                AddPoints(10);
                Debug.Log("Added 10 points");
            }

            if (IsPowerPellet(tile)) {
                AddPoints(50);
                ghostManager.StartPowerState();
                Debug.Log("Added 50 points");
            }
  
            levelGenerator.ChangePelletToEmpty(gridPos);
            TryTeleport();
            MovePacStudent(speed);
        }

        if (points > highScore)
        {
            highScore = points;
            recordTime = gameTimer.Elapsed;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.SetFloat(RecordTimeKey, recordTime);
            PlayerPrefs.Save();
        }
    }

    public IEnumerator DeathSequence()
    {
        isDying = true;

        // freeze everything
        tweener.CancelTween();
        foreach (GhostController g in ghosts) g.Frozen = true;
        audioSource.Stop();

        // lose a life straight away so the UI updates with the death
        if (lives.Count > 0)
        {
            UnityEngine.UI.Image life = lives[lives.Count - 1];
            if (life != null) Destroy(life.gameObject);
            lives.RemoveAt(lives.Count - 1);
        }

        // animation, particles and sound all start together
        animator.Play("PacStudent_Dead");
        if (deathPs != null)
        {
            deathPs.transform.position = pacStudent.transform.position;
            deathPs.Play();
        }
        if (deathAudio != null) sfxSource.PlayOneShot(deathAudio);

        yield return new WaitForSeconds(deathDuration);

        if (lives.Count == 0)
        {
            // Game Over is a later requirement: hook it in here and stay frozen
            gameOverController.StartCoroutine("GameOver");
            yield break;
        }

        // respawn
        ResetPacStudent();
        foreach (GhostController g in ghosts) g.ResetGhost();
        ghostManager.CancelPower();
        isDying = false;
    }
    
    void EatTouchingGhosts()
    {
        Vector2 p = pacStudent.transform.position;
        foreach (GhostController ghost in ghosts)
        {
            if (ghost.State != GhostState.Scared && ghost.State != GhostState.Recovering) continue;
            if (Vector2.Distance(ghost.transform.position, p) > dieDistance) continue;

            AddPoints(ghostPoints);
            ghostManager.GhostEaten(ghost);
        }
    }
    public void AddPoints(int amount)
    {
        points += amount;
        UpdatePointsUI(points);
    }

    void UpdatePointsUI(int points)
    {
        int digits = CountDigits(points);
        int numZeros = 6 - digits;

        string pointAmount = "";

        for (int i = 0; i < numZeros; i++)
        {
            pointAmount += "0";
        }
        pointAmount += points.ToString();
        pointsText.text = pointAmount;
    }

    private int CountDigits(int num)
    {
        if (num == 0) return 1;

        int count = 0;

        while (num > 0)
        {
            long lastDigit = num % 10;
            count++;
            num /= 10;
        }

        return count;
    }

    void MovePacStudent(float speed)
    {
        if (TryMove(lastInput, speed))
        {
            currentInput = lastInput;
            PlayDirection(currentInput);
            PlayClip();
            wallSoundPlayed = false;
            return;
        }

        if (TryMove(currentInput, speed)) { wallSoundPlayed = false; return; }

        // else, can't move
        if (!wallSoundPlayed && KeyToDirection(lastInput) != Vector2Int.zero)
        {
            audioSource.Stop();
            sfxSource.PlayOneShot(wallCollideAudio);
            collidePs.Play();
            wallSoundPlayed = true;
        }
    }

    void PlayDirection(KeyCode? key)
    {
        switch (key)
        {
            case KeyCode.W: animator.Play("PacStudent_Up"); break;
            case KeyCode.A: animator.Play("PacStudent_Left"); break;
            case KeyCode.S: animator.Play("PacStudent_Down"); break;
            case KeyCode.D: animator.Play("PacStudent_Right"); break;
        }
    }

    void PlayClip()
    {
        audioSource.Stop(); // Stop if already playing something
        audioSource.Play();
    }

    bool TryMove(KeyCode? key, float speed)
    {
        Vector2Int dir = KeyToDirection(key);
        if (dir == Vector2Int.zero) return false;

        Vector2Int next = gridPos + dir;
        int previousTile = GetTile(gridPos);
        int nextTile = GetTile(next);

        if (!IsWalkable(nextTile)) {
            return false; 
        }

        audioSource.resource = (IsPellet(nextTile) ? eatingPelletAudio : movementAudio);

        Vector3 start = pacStudent.transform.position;
        Vector3 end = GridToWorld(next);
        float duration = Vector3.Distance(start, end) / speed;

        tweener.AddTween(pacStudent.transform, start, end, duration);
        gridPos = next;

        return true;
    }

    bool TryTeleport()
    {
        if (fullMap == null) return false;

        int lastCol = fullMap.GetLength(1) - 1;
        int tile = GetTile(gridPos);
        if (!IsWalkable(tile)) return false;

        if (gridPos.y == 0) // left end -> reappear on the right
        {
            gridPos = new Vector2Int(gridPos.x, lastCol);
            currentInput = KeyCode.A; // inwards from the right side is left
        }
        else if (gridPos.y == lastCol) // right end -> reappear on the left
        {
            gridPos = new Vector2Int(gridPos.x, 0);
            currentInput = KeyCode.D; // inwards from the left side is right
        }
        else return false;

        pacStudent.transform.position = GridToWorld(gridPos);
        return true;
    }

    Vector2Int KeyToDirection(KeyCode? key)
    {
        switch (key)
        {
            case KeyCode.W: return new Vector2Int(-1, 0);
            case KeyCode.A: return new Vector2Int(0, -1);
            case KeyCode.S: return new Vector2Int(1, 0);
            case KeyCode.D: return new Vector2Int(0, 1);
            default: return Vector2Int.zero;
        }
    }

    void ResetPacStudent()
    {
        gridPos = startGridPos;
        pacStudent.transform.position = GridToWorld(gridPos);
        animator.Play("PacStudent_Right");
        lastInput = null; // wait for fresh player input
        currentInput = null;
        wallSoundPlayed = false;
    }

    void GameOver()
    {
        
    }
    bool TouchingNormalGhost()
    {
        Vector2 p = pacStudent.transform.position;
        foreach (GhostController ghost in ghosts)
        {
            if (ghost.State != GhostState.Normal) continue; // scared/recovering/dead ghosts are harmless
            if (Vector2.Distance(ghost.transform.position, p) <= dieDistance) return true;
        }
        return false;
    }

    bool IsWalkable(int tile)
    {
        return tile == 0 || tile == 5 || tile == 6;
    }

    bool IsPellet(int tile)
    {
        return tile == 5 || tile == 6;
    }

    bool IsNormalPellet(int tile) { return tile == 5;}
    bool IsPowerPellet(int tile) { return tile == 6; }

    int GetTile(Vector2Int cell)
    {
        if (fullMap == null) return -1;
        if (cell.x < 0 || cell.x >= fullMap.GetLength(0) ||
            cell.y < 0 || cell.y >= fullMap.GetLength(1)) return -1;

        return fullMap[cell.x, cell.y];
    }

    Vector3 GridToWorld(Vector2Int cell)
    {
        Vector3 p = levelGenerator.GridToWorld(cell.x, cell.y);
        p.z = zDepth;
        return p;
    }

    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.DeleteKey(RecordTimeKey);
        highScore = 0;
        recordTime = 0;
    }
}
