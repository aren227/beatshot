using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<PoolManager>();
            }
            return _instance;
        }
    }

    static PoolManager _instance;

    public Dictionary<string, ObjectPool> pools = new Dictionary<string, ObjectPool>();
    public Dictionary<GameObject, ObjectPool> spawned = new Dictionary<GameObject, ObjectPool>();

    void Start() {
        // RegisterPool("audioSource", PrefabRegistry.Instance.audioSource);
        RegisterPool("shape", PrefabRegistry.Instance.shape);
    }

    public void RegisterPool(string name, GameObject prefab) {
        pools[name] = new ObjectPool(name, prefab);
    }

    public GameObject Spawn(string name) {
        if (!pools.ContainsKey(name)) return null;

        ObjectPool pool = pools[name];

        GameObject obj = pool.Spawn();

        spawned.Add(obj, pool);

        return obj;
    }

    public void Despawn(GameObject obj) {
        if (!spawned.ContainsKey(obj)) return;

        spawned[obj].Despawn(obj);

        spawned.Remove(obj);
    }

    public void DespawnAll(string name) {
        if (!pools.ContainsKey(name)) return;

        pools[name].DespawnAll();
    }
}
