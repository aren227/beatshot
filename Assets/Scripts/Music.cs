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

    public AudioSource audioSource1;
    public AudioSource audioSource2;

    void LateUpdate() {
        // audioSource.pitch = Time.timeScale;
    }
}
