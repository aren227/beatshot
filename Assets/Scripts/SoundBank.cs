using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundBank : MonoBehaviour
{
    public static SoundBank Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<SoundBank>();
            }
            return _instance;
        }
    }

    static SoundBank _instance;

    public List<SoundCollection> soundCollections;

    Dictionary<string, SoundCollection> soundCollectionsDict = new Dictionary<string, SoundCollection>();

    void Awake() {
        foreach (SoundCollection soundCollection in soundCollections) {
            soundCollectionsDict[soundCollection.name] = soundCollection;
        }
    }

    public AudioClip GetSound(string name) {
        if (!soundCollectionsDict.ContainsKey(name)) return null;

        SoundCollection soundCollection = soundCollectionsDict[name];
        return soundCollection.clips[Random.Range(0, soundCollection.clips.Count)];
    }

    public void PlaySound(string name, Vector3 at, float volume = 0.5f, float minDist = 25, float maxDist = 200) {
        AudioClip clip = GetSound(name);
        if (clip == null) return;

        GameObject obj = PoolManager.Instance.Spawn("audioSource");

        obj.transform.position = at;

        AudioSource audioSource = obj.GetComponent<AudioSource>();

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(0.7f, 1.3f);

        audioSource.minDistance = minDist;
        audioSource.maxDistance = maxDist;

        audioSource.Play();
    }
}

[System.Serializable]
public class SoundCollection {
    public string name;
    public List<AudioClip> clips;
}