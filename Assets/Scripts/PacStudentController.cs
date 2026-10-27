using System;
// using Unity.VisualScripting;
// using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using System.Drawing;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private GameObject pacStudent;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float zDepth = -0.1f;
    [SerializeField] private Vector2Int startGridPos = new Vector2Int(1, -1);

    private Tweener tweener;
    [SerializeField] private LevelGenerator levelGenerator;
    private int[,] fullMap;
    private KeyCode lastInput;
    private KeyCode currentInput;
    private Vector2Int gridPos;

    private Animator animator;
    private AudioSource audioSource;
    [SerializeField] private AudioClip movementAudio;
    [SerializeField] private AudioClip eatingPelletAudio;
    [SerializeField] private AudioClip wallCollideAudio;
    [SerializeField] private AudioSource sfxSource;
    private bool wallSoundPlayed;

    [SerializeField] private ParticleSystem collidePs;

    private int previousTile;

    private int points;
    [SerializeField] private TMP_Text pointsText;

    [SerializeField] private GhostPowerManager ghostPower;


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
        points = 0;

        if (pointsText != null) pointsText.text = "000000";
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
                ghostPower.StartPowerState();
                Debug.Log("Added 50 points");
            }
  
            levelGenerator.ChangePelletToEmpty(gridPos);
            MovePacStudent(speed);
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

    void PlayDirection(KeyCode key)
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

    bool TryMove(KeyCode key, float speed)
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

    Vector2Int KeyToDirection(KeyCode key)
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
}
