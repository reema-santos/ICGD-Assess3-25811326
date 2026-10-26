using UnityEngine;

public class Tweener : MonoBehaviour
{
    private Tween tween;
    public bool IsTweening => tween != null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (tween == null) return;

        float t = (Time.time - tween.StartTime) / tween.Duration;

        if (t >= 1f)
        {
            tween.Target.position = tween.EndPos;
            tween = null;
        }
        else
        {
            tween.Target.position = Vector3.Lerp(tween.StartPos, tween.EndPos, t);
        }
    }

    public void AddTween(Transform target, Vector3 startPos, Vector3 endPos, float duration)
    {
        if (tween != null) return;
        tween = new Tween(target, startPos, endPos, Time.time, duration);
    }
}
