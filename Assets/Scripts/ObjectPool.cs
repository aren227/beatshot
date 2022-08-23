using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    public string name;
    public GameObject prefab;

    public List<GameObject> spawned = new List<GameObject>();
    public List<GameObject> idle = new List<GameObject>();

    public ObjectPool(string name, GameObject prefab) {
         this.name = name;
         this.prefab = prefab;
    }

    public GameObject Spawn() {
        if (idle.Count == 0) {
            GameObject cloned = GameObject.Instantiate(prefab);
            cloned.SetActive(false);

            idle.Add(cloned);
        }

        GameObject picked = idle[idle.Count-1];
        idle.RemoveAt(idle.Count-1);

        picked.SetActive(true);

        spawned.Add(picked);

        return picked;
    }

    public void Despawn(GameObject gameObject) {
        gameObject.transform.SetParent(null, false);

        gameObject.SetActive(false);

        spawned.Remove(gameObject);

        idle.Add(gameObject);
    }

    public void DespawnAll() {
        PoolManager poolManager = PoolManager.Instance;

        foreach (GameObject gameObject in spawned) {
            gameObject.transform.SetParent(null, false);

            gameObject.SetActive(false);

            idle.Add(gameObject);

            // @Hack
            poolManager.spawned.Remove(gameObject);
        }

        spawned.Clear();
    }
}
