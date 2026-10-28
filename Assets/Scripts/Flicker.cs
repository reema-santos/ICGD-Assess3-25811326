using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Flicker : MonoBehaviour
{
    [SerializeField] private Light2D targetLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 5f;

    // Update is called once per frame
    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
