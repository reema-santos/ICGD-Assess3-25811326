using System.Collections;
using UnityEngine;

public class CherryController : MonoBehaviour
{
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private SpriteMask levelMask; // sized to the map by this script
    [SerializeField] private LevelGenerator levelGenerator; // falls back to the "Level1" tag
    [SerializeField] private float speed = 3f; // world units per second
    [SerializeField] private float spawnDelay = 5f;
    [SerializeField] private float edgeMargin = 1.5f; // how far outside the map the cherry starts
    [SerializeField] private float zDepth = -0.2f;
    [SerializeField] private int sortingOrder = 100; // above everything else

    private GameObject cherry;
    private Vector3 lastStart;
    private bool hasLastStart;

    [SerializeField] private Transform pacStudent;
    [SerializeField] private PacStudentController pacStudentController;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip cherryEatAudio;
    [SerializeField] private float eatDistance = 0.5f; // in tiles
    [SerializeField] private int cherryPoints = 100;

    void Start()
    {
        if (levelGenerator == null)
            levelGenerator = FindAnyObjectByType<LevelGenerator>();

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            // 5s after the scene starts, and 5s after the previous cherry is gone
            yield return new WaitForSeconds(spawnDelay);

            while (levelGenerator.fullMap == null) yield return null;

            Tweener tweener = SpawnCherry();

            while (cherry != null && tweener.IsTweening)
            {
                if (IsEaten())
                {
                    EatCherry();
                    break;
                }
                yield return null;
            }

            // reached the far side without being eaten: destroy it
            if (cherry != null) Destroy(cherry);
        }
    }

    bool IsEaten()
    {
        if (pacStudent == null) return false;
        Vector2 a = cherry.transform.position;
        Vector2 b = pacStudent.position;
        return Vector2.Distance(a, b) <= eatDistance;  
    }

    void EatCherry()
    {
        pacStudentController.AddPoints(cherryPoints);
        Debug.Log("Eaten cherry");
        if (sfxSource != null && cherryEatAudio != null)
            sfxSource.PlayOneShot(cherryEatAudio);

        Destroy(cherry);
    }

    Tweener SpawnCherry()
    {
        GetMapBounds(out Vector3 center, out Vector2 halfSize);
        UpdateMask(center, halfSize);

        Vector3 start = RandomEdgePoint(center, halfSize);
        Vector3 end = new Vector3(2f * center.x - start.x, 2f * center.y - start.y, zDepth);

        cherry = Instantiate(cherryPrefab, start, Quaternion.identity);

        foreach (SpriteRenderer sr in cherry.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            sr.sortingOrder = sortingOrder;
        }

        Tweener tweener = cherry.GetComponent<Tweener>();
        if (tweener == null) tweener = cherry.AddComponent<Tweener>();

        float duration = Vector3.Distance(start, end) / speed;
        tweener.AddTween(cherry.transform, start, end, duration);
        return tweener;
    }

    void GetMapBounds(out Vector3 center, out Vector2 halfSize)
    {
        int rows = levelGenerator.fullMap.GetLength(0);
        int cols = levelGenerator.fullMap.GetLength(1);

        Vector3 topLeft = levelGenerator.GridToWorld(0, 0);
        Vector3 bottomRight = levelGenerator.GridToWorld(rows - 1, cols - 1);
        float tile = levelGenerator.GridToWorld(0, 1).x - topLeft.x;

        center = (topLeft + bottomRight) / 2f;
        halfSize = new Vector2(cols * tile / 2f, rows * tile / 2f);
    }

    void UpdateMask(Vector3 center, Vector2 halfSize)
    {
        if (levelMask == null || levelMask.sprite == null) return;

        Vector2 spriteSize = levelMask.sprite.bounds.size;
        levelMask.transform.position = new Vector3(center.x, center.y, 0f);
        levelMask.transform.localScale = new Vector3(
            halfSize.x * 2f / spriteSize.x,
            halfSize.y * 2f / spriteSize.y,
            1f);
    }

    Vector3 RandomEdgePoint(Vector3 center, Vector2 halfSize)
    {
        float hx = halfSize.x + edgeMargin;
        float hy = halfSize.y + edgeMargin;
        Vector3 p = Vector3.zero;

        // retry a few times so consecutive cherries don't start in the same spot
        for (int i = 0; i < 10; i++)
        {
            float t = Random.value;
            switch (Random.Range(0, 4))
            {
                case 0:  p = new Vector3(center.x - hx, center.y + Mathf.Lerp(-hy, hy, t), zDepth); break; // left
                case 1:  p = new Vector3(center.x + hx, center.y + Mathf.Lerp(-hy, hy, t), zDepth); break; // right
                case 2:  p = new Vector3(center.x + Mathf.Lerp(-hx, hx, t), center.y + hy, zDepth); break; // top
                default: p = new Vector3(center.x + Mathf.Lerp(-hx, hx, t), center.y - hy, zDepth); break; // bottom
            }

            if (!hasLastStart || Vector3.Distance(p, lastStart) > 3f) break;
        }

        lastStart = p;
        hasLastStart = true;
        return p;
    }
}