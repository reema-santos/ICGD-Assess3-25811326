using UnityEngine;
using UnityEngine.UI;

public class DotBorder : MonoBehaviour
{
    [SerializeField] RectTransform target; // the element to trace
    [SerializeField] RectTransform dotPrefab;
    [SerializeField] int dotCount = 70;
    [SerializeField] float speed = 90f;  // how fast dots travel around
    [SerializeField] Vector2 padding = new Vector2(40f, 30f); // gap between target and dots
    [SerializeField] float chaseSpeed = 4f;  // speed of the pulse running through the dots
    [SerializeField] float chaseDensity = 0.35f; // how tightly packed the pulses are
    [SerializeField] Color dimColor = new Color(0.1f, 0.45f, 0.5f);
    [SerializeField] Color brightColor = new Color(1f, 0.6f, 0.1f);

    RectTransform[] dots;
    Image[] images;
    readonly Vector3[] corners = new Vector3[4];

    void Start()
    {
        dots = new RectTransform[dotCount];
        images = new Image[dotCount];
        for (int i = 0; i < dotCount; i++)
        {
            dots[i] = Instantiate(dotPrefab, transform);
            images[i] = dots[i].GetComponent<Image>();
        }
    }

    void LateUpdate()
    {
        // Get the target's rectangle in this object's local space (works with any anchors or scaling)
        target.GetWorldCorners(corners);
        Vector3 bl = transform.InverseTransformPoint(corners[0]);
        Vector3 tr = transform.InverseTransformPoint(corners[2]);

        float xMin = bl.x - padding.x, xMax = tr.x + padding.x;
        float yMin = bl.y - padding.y, yMax = tr.y + padding.y;
        float w = xMax - xMin, h = yMax - yMin;
        float perimeter = 2f * (w + h);
        float spacing = perimeter / dotCount;

        for (int i = 0; i < dotCount; i++)
        {
            float d = (i * spacing + Time.time * speed) % perimeter;

            // Walk the outline: bottom -> right -> top -> left
            Vector2 p;
            if (d < w)             p = new Vector2(xMin + d, yMin);
            else if ((d -= w) < h) p = new Vector2(xMax, yMin + d);
            else if ((d -= h) < w) p = new Vector2(xMax - d, yMax);
            else                   p = new Vector2(xMin, yMax - (d - w));

            dots[i].localPosition = p;

            // A pulse of light chases around the dots
            float t = Mathf.Clamp01(Mathf.Sin(i * chaseDensity - Time.time * chaseSpeed) * 0.5f + 0.5f);
            t *= t;
            images[i].color = Color.Lerp(dimColor, brightColor, t);
            dots[i].localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, t);
        }
    }
}