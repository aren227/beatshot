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
    public AudioClip preBigExplosion;
    public AudioClip postBigExplosion;

    AudioSource audioSource;

    void Awake() {
        audioSource = GetComponent<AudioSource>();
    }

    public void Play(string sound) {
        float volume = Manager.Instance.globalData.sfxVolume;

        // @Todo: Pooling.
        if (sound == "shoot") {
            audioSource.PlayOneShot(shoot, 0.7f * volume);
        }
        else if (sound == "hit") {
            audioSource.PlayOneShot(hit, 0.3f * volume);
        }
        else if (sound == "preBigExplosion") {
            audioSource.PlayOneShot(preBigExplosion, 1f * volume);
        }
        else if (sound == "postBigExplosion") {
            audioSource.PlayOneShot(postBigExplosion, 1f * volume);
        }
    }
}
