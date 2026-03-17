using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager_Hades : MonoBehaviour
{
    public static BGMManager_Hades Instance { get; private set; }

    public AudioClip bgm;
    public AudioClip final;
    public AudioClip boss;
    public AudioClip rs;
    AudioSource audioSource;

    private void Awake()
    {
        Instance = this;
        
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.8f;
        audioSource.loop = true;

        PlaySound(bgm);
    }

    public void PlaySound(AudioClip audioClip)
    {
        if (audioClip == rs) { audioSource.loop = false; audioSource.volume = 1.6f; }
        audioSource.clip = audioClip;
        audioSource.Play();
    }
}