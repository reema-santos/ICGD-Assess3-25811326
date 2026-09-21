using System;
using System.Collections;
using UnityEngine;

public class BackgroundMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip ghostNormalMusic;

    private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        StartCoroutine(PlayMusicSequence());
    }

    // Update is called once per frame
    private IEnumerator PlayMusicSequence()
    {
        audioSource.clip = introMusic;
        audioSource.Play();

        float waitTime = Mathf.Min(introMusic.length, 3f);

        yield return new WaitForSeconds(waitTime);

        audioSource.clip = ghostNormalMusic;
        audioSource.loop = true;
        audioSource.Play();
    }
    void Update()
    {
        
    }
}
