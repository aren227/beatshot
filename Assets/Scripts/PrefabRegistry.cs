using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabRegistry : MonoBehaviour
{
    public static PrefabRegistry Instance {
        get {
            if (!_instance) {
                _instance = FindObjectOfType<PrefabRegistry>();
            }
            return _instance;
        }
    }

    static PrefabRegistry _instance;

    public GameObject player;
    public GameObject enemy;
    public GameObject projectile;
}
