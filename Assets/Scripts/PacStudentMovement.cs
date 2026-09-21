using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class PacStudentMovement : MonoBehaviour
{
    [SerializeField] private Vector3[] waypoints;
    private float moveSpeed = 4f;
    private Animator animator;
    private AudioSource movementAudio;
    private int currentWaypointIndex = 0;
    private int currentAnim = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        movementAudio = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogError("At least 2 waypoints need to be assigned.");
            return;
        }
        if (movementAudio != null && !movementAudio.isPlaying)
        {
            movementAudio.loop = true;
            movementAudio.Play();
        }

        transform.position = waypoints[0];
        StartCoroutine(FollowPathCoroutine());
    }

    private IEnumerator FollowPathCoroutine()
    {
        while (true) {
            int nextWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            Vector3 startPos = waypoints[currentWaypointIndex];
            Vector3 nextPos = waypoints[nextWaypointIndex];

            float dist = Vector3.Distance(startPos, nextPos);
            float duration = dist / moveSpeed;
            float elapsedTime = 0f;

            UpdateVisual(startPos, nextPos);
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float percentageComplete = elapsedTime / duration;

                transform.position = Vector3.Lerp(startPos, nextPos, percentageComplete);

                yield return null;
            }

            transform.position = nextPos;
            currentWaypointIndex = nextWaypointIndex;
        }

    }

    private void UpdateVisual(Vector3 start, Vector3 next)
    {
        Vector3 direction = (next - start).normalized;

        currentAnim++;

        if (direction.x > 0.1f) animator.Play("PacStudent_Right");
        else if (direction.x < -0.1f) animator.Play("PacStudent_Left");
        else if (direction.y > 0.1f) animator.Play("PacStudent_Up");
        else if (direction.y < -0.1f) animator.Play("PacStudent_Down");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
