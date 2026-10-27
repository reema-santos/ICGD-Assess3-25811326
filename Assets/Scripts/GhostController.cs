using UnityEngine;
using UnityEngine.UIElements;

public enum GhostState { Normal, Scared, Recovering, Dead }
public enum Facing { Up = 0, Left = 1, Down = 2, Right = 3 }

public class GhostController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField] private string[] normalStates = { "Ghost1_UpAnim", "Ghost1_LeftAnim", "Ghost1_DownAnim", "Ghost1_RightAnim" };
    [SerializeField] private string[] scaredStates = { "Scared_UpAnim", "Scared_LeftAnim", "Scared_DownAnim", "Scared_RightAnim" };
    [SerializeField] private string recoveringState = "Ghost1_Recovering";
    [SerializeField] private string deadState = "GhostDead"; // same clip for every ghost

    public GhostState State { get; private set; } = GhostState.Normal;
    public Facing Direction { get; private set; } = Facing.Down;
    public bool Frozen { get; set; }
    private Vector3 originalPosition;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        originalPosition = transform.position;
    }

    // Called by the power pill logic. A dead ghost ignores these.
    public void SetScared() { if (State != GhostState.Dead) Apply(GhostState.Scared); }
    public void SetRecovering() { if (State != GhostState.Dead) Apply(GhostState.Recovering); }
    public void SetNormal() { if (State != GhostState.Dead) Apply(GhostState.Normal); }

    // For later: PacStudent eats a scared ghost / dead ghost gets home
    public void SetDead() { Apply(GhostState.Dead); }
    public void Revive() { Apply(GhostState.Normal); }

    // For later: call this from the ghost's movement code whenever it changes direction
    public void SetFacing(Facing facing)
    {
        if (facing == Direction) return;
        Direction = facing;
        Apply(State); // swap to the same state's clip for the new direction
    }

    public void ResetGhost()
    {
        transform.position = originalPosition;
        Direction = Facing.Down;
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