using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX : MonoBehaviour
{
    public static SFX Instance {
        get {
            if (!_instance) {
                _instance = FindObjectOfType<SFX>();
            }
            return _instance;
        }
    }

    static SFX _instance;

    public AudioClip shoot;
    public AudioClip hit;

    AudioSource audioSource;

    void Awake() {
        audioSource = GetComponent<AudioSource>();
    }

    public void Play(string sound) {
        // @Todo: Pooling.
        if (sound == "shoot") {
            audioSource.PlayOneShot(shoot, 0.7f);
        }
        else if (sound == "hit") {
            audioSource.PlayOneShot(hit, 0.3f);
        }
    }
}
