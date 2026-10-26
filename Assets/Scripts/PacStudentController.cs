using System;
using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private GameObject pacStudent;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float zDepth = -0.1f;
    [SerializeField] private Vector2Int startGridPos = new Vector2Int(1, -1);

    private Tweener tweener;
    private LevelGenerator levelGenerator;
    private int[,] fullMap;
    private KeyCode lastInput;
    private KeyCode currentInput;
    private Vector2Int gridPos;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tweener = GetComponent<Tweener>();
        levelGenerator = GameObject.FindGameObjectWithTag("Level1").GetComponent<LevelGenerator>();
        fullMap = levelGenerator.fullMap;

        gridPos = startGridPos;
        pacStudent.transform.position = GridToWorld(gridPos);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) lastInput = KeyCode.W;
        if (Input.GetKeyDown(KeyCode.A)) lastInput = KeyCode.A;
        if (Input.GetKeyDown(KeyCode.S)) lastInput = KeyCode.S;
        if (Input.GetKeyDown(KeyCode.D)) lastInput = KeyCode.D;

        if (!tweener.IsTweening)
        {
            MovePacStudent(speed);
        }
    }

    void MovePacStudent(float speed)
    {
        if (TryMove(lastInput, speed))
        {
            currentInput = lastInput;
            return;
        }

        if (TryMove(currentInput, speed)) return;
    }

    bool TryMove(KeyCode key, float speed)
    {
        Vector2Int dir = KeyToDirection(key);
        if (dir == Vector2Int.zero) return false;

        Vector2Int next = gridPos + dir;
        if (!IsWalkable(next)) return false;

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

    bool IsWalkable(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= fullMap.GetLength(0) ||
            cell.y < 0 || cell.y >= fullMap.GetLength(1)) return false;

        int tile = fullMap[cell.x, cell.y];
        return tile == 0 || tile == 5 || tile == 6;
    }

    Vector3 GridToWorld(Vector2Int cell)
    {
        Vector3 p = levelGenerator.GridToWorld(cell.x, cell.y);
        p.z = zDepth;
        return p;
    }
}
