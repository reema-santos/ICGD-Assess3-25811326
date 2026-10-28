using System.Collections.Generic;
using UnityEngine;

public enum GhostState { Normal, Scared, Recovering, Dead }
public enum Facing { Up = 0, Left = 1, Down = 2, Right = 3 }

public class GhostController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string[] normalStates = { "Ghost1_UpAnim", "Ghost1_LeftAnim", "Ghost1_DownAnim", "Ghost1_RightAnim" };
    [SerializeField] private string[] scaredStates = { "Scared_UpAnim", "Scared_LeftAnim", "Scared_DownAnim", "Scared_RightAnim" };
    [SerializeField] private string recoveringState = "Ghost1_Recovering";
    [SerializeField] private string deadState = "GhostDead";

    [SerializeField] private int ghostNumber = 1; // 1 to 4
    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private GhostPowerManager powerManager;
    [SerializeField] private Vector2Int topExitCell = new Vector2Int(11, 13); // just outside the top gap (row, col)
    [SerializeField] private Vector2Int bottomExitCell = new Vector2Int(17, 13); // just outside the bottom gap

    public GhostState State { get; private set; } = GhostState.Normal;
    public Facing Direction { get; private set; } = Facing.Down;
    public bool Frozen { get; set; }

    // Directions in clockwise order as (row, col): up, right, down, left
    static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1)
    };
    static readonly Facing[] FacingOf = { Facing.Up, Facing.Right, Facing.Down, Facing.Left };

    private Tweener tweener;
    private int[,] fullMap;
    private Vector3 originalPosition;
    private Vector2Int gridPos;
    private int lastDir; // index into Dirs of the last move
    private bool inHouse = true; // in the spawn area
    private int leaveStage;
    private bool deadTweenStarted;
    private bool following;
    private int followHeading;

    bool UsesTopExit => ghostNumber == 1 || ghostNumber == 3;
    Vector2Int ExitCell => UsesTopExit ? topExitCell : bottomExitCell;
    int ExitDir => UsesTopExit ? 0 : 2;

    float NormalSpeed => pacStudent.Speed * 0.9f;
    float CurrentSpeed => State == GhostState.Normal ? NormalSpeed : NormalSpeed * 0.5f; // scared/recovering/dead = 50%

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        tweener = GetComponent<Tweener>();
        if (tweener == null) tweener = gameObject.AddComponent<Tweener>();
        originalPosition = transform.position;
        if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();
    }

    void Update()
    {
        if (fullMap == null)
        {
            fullMap = levelGenerator.fullMap;
            if (fullMap == null) return;
        }

        if (Frozen) { tweener.CancelTween(); return; }

        if (State == GhostState.Dead) { UpdateDead(); return; }
        if (tweener.IsTweening) return;

        if (inHouse) UpdateLeaving();
        else ChooseNextMove();
    }

    // dead: straight line home, through walls
    void UpdateDead()
    {
        if (!deadTweenStarted)
        {
            tweener.CancelTween();
            deadTweenStarted = true;
            inHouse = true;
            leaveStage = 0;
            following = false;
            MoveTo(originalPosition);
            return;
        }

        if (tweener.IsTweening) return;

        // arrived at the starting position
        transform.position = originalPosition;
        deadTweenStarted = false;
        Revive(); // Normal first
        if (powerManager != null) powerManager.GhostRespawned(this); // then Scared/Recovering + music
    }

    // leaving spawn area
    void UpdateLeaving()
    {
        Vector3 exitWorld = CellToWorld(ExitCell);

        if (leaveStage == 0)
        {
            leaveStage = 1;
            Vector3 aligned = new Vector3(exitWorld.x, transform.position.y, transform.position.z);
            if (Mathf.Abs(aligned.x - transform.position.x) > 0.01f) { MoveTo(aligned); return; }
        }
        if (leaveStage == 1)
        {
            leaveStage = 2;
            MoveTo(exitWorld); // straight through the gap
            return;
        }

        // reached the cell outside the gap: from now on it follows the grid rules
        inHouse = false;
        leaveStage = 0;
        gridPos = ExitCell;
        lastDir = ExitDir;
        ChooseNextMove();
    }

    // grid movement
    void ChooseNextMove()
    {
        var options = new List<int>();
        int back = (lastDir + 2) % 4;

        for (int i = 0; i < 4; i++)
            if (i != back && IsOpen(gridPos + Dirs[i])) options.Add(i);

        if (options.Count == 0 && IsOpen(gridPos + Dirs[back])) options.Add(back);  // dead end: no other choice
        if (options.Count == 0) return;

        int chosen = PickDirection(options);
        gridPos += Dirs[chosen];
        lastDir = chosen;
        SetFacing(FacingOf[chosen]);
        MoveTo(CellToWorld(gridPos));
    }

    int PickDirection(List<int> options)
    {
        int behaviour = State == GhostState.Normal ? ghostNumber : 1;  // scared/recovering use Ghost 1
        if (behaviour != 4) following = false;

        switch (behaviour)
        {
            case 1: return PickByDistance(options, true); // not closer to PacStudent
            case 2: return PickByDistance(options, false); // not further from PacStudent
            case 4: return PickFollow(options);
            default: return options[Random.Range(0, options.Count)];
        }
    }

    int PickByDistance(List<int> options, bool further)
    {
        Vector2 pac = pacStudent.PacTransform.position;
        float current = Vector2.Distance(CellToWorld(gridPos), pac);

        var good = new List<int>();
        foreach (int d in options)
        {
            float dist = Vector2.Distance(CellToWorld(gridPos + Dirs[d]), pac);
            if (further ? dist >= current - 0.001f : dist <= current + 0.001f) good.Add(d);
        }

        List<int> pool = good.Count > 0 ? good : options;   // nothing qualifies: any valid direction
        return pool[Random.Range(0, pool.Count)];
    }

    // Ghost 4: left-hand wall follower on the outer wall = clockwise around the map
    int PickFollow(List<int> options)
    {
        if (!following)
        {
            // wander until next to the outer wall, then latch on
            for (int i = 0; i < 4; i++)
            {
                if (IsOuterWall(gridPos + Dirs[i]))
                {
                    following = true;
                    followHeading = (i + 1) % 4; // heading with that wall on the left
                    break;
                }
            }
            if (!following) return options[Random.Range(0, options.Count)];
        }

        // prefer: left, straight, right, back (relative to heading)
        int[] prefs = { (followHeading + 3) % 4, followHeading, (followHeading + 1) % 4, (followHeading + 2) % 4 };
        foreach (int p in prefs)
        {
            if (options.Contains(p)) { followHeading = p; return p; }
        }
        return options[0];
    }

    bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.x < fullMap.GetLength(0) && c.y >= 0 && c.y < fullMap.GetLength(1);

    // Ghosts walk where PacStudent walks, but the ghost gate and the tunnel ends count as walls
    bool IsOpen(Vector2Int c)
    {
        if (!InBounds(c)) return false;
        if (c.y == 0 || c.y == fullMap.GetLength(1) - 1) return false; // teleporter cells
        int t = fullMap[c.x, c.y];
        return t == 0 || t == 5 || t == 6;
    }

    bool IsOuterWall(Vector2Int c)
    {
        if (!InBounds(c)) return false;
        int t = fullMap[c.x, c.y];
        return t == 1 || t == 2 || t == 7;
    }

    Vector3 CellToWorld(Vector2Int c)
    {
        Vector3 p = levelGenerator.GridToWorld(c.x, c.y);
        p.z = originalPosition.z;
        return p;
    }

    void MoveTo(Vector3 target)
    {
        Vector3 start = transform.position;
        target.z = start.z;

        Vector3 delta = target - start;
        if (delta.sqrMagnitude > 0.0001f)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) SetFacing(delta.x > 0 ? Facing.Right : Facing.Left);
            else SetFacing(delta.y > 0 ? Facing.Up : Facing.Down);
        }

        float duration = Mathf.Max(delta.magnitude / CurrentSpeed, 0.0001f); // constant speed, frame-rate independent
        tweener.AddTween(transform, start, target, duration);
    }

    // States and animation
    public void SetScared() { if (State != GhostState.Dead) Apply(GhostState.Scared); }
    public void SetRecovering() { if (State != GhostState.Dead) Apply(GhostState.Recovering); }
    public void SetNormal() { if (State != GhostState.Dead) Apply(GhostState.Normal); }
    public void SetDead() { Apply(GhostState.Dead); }
    public void Revive() { Apply(GhostState.Normal); }

    public void SetFacing(Facing facing)
    {
        if (facing == Direction) return;
        Direction = facing;
        // recovering and dead have no directional clips, so don't restart them
        if (State == GhostState.Normal || State == GhostState.Scared) Apply(State);
    }

    public void ResetGhost()
    {
        tweener.CancelTween();
        transform.position = originalPosition;
        Direction = Facing.Down;
        inHouse = true;
        leaveStage = 0;
        deadTweenStarted = false;
        following = false;
        Frozen = false;
        Apply(GhostState.Normal);
    }

    void Apply(GhostState newState)
    {
        State = newState;
        int d = (int)Direction;

        switch (newState)
        {
            case GhostState.Normal: animator.Play(normalStates[d]); break;
            case GhostState.Scared: animator.Play(scaredStates[d]); break;
            case GhostState.Recovering: animator.Play(recoveringState); break;
            case GhostState.Dead: animator.Play(deadState); break;
        }
    }
}