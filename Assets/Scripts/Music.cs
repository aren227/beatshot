using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Music : MonoBehaviour
{
    public static Music Instance {
        get {
            if (!_instance) {
                _instance = FindObjectOfType<Music>();
            }
            return _instance;
        }
    }

    static Music _instance;

    public AudioSource audioSource;

    void Awake() {
        audioSource = GetComponent<AudioSource>();
    }

    void LateUpdate() {
        // audioSource.pitch = Time.timeScale;
    }
}
