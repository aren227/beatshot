using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    public static Background Instance {
        get {
            if (!_instance) {
                _instance = FindObjectOfType<Background>();
            }
            return _instance;
        }
    }

    static Background _instance;

    List<Entry> entries = new List<Entry>();

    public Color particleColor;

    public void Warmup() {
        float t = Manager.Instance.time;
        // @Hack
        float temp = t;
        float dt = 1 / 4f; // Depends on spawnRate in DoNextFrame().
        // float start = Time.realtimeSinceStartup;
        for (int i = 0; i < 150; i++) {
            Manager.Instance.time = temp;
            DoNextFrame(dt);
            temp += dt;
        }
        // Around 11ms.
        // Debug.Log("Warmup took " + (Time.realtimeSinceStartup - start) * 1000 + "ms." );
        Manager.Instance.time = t;
    }

    public void DoNextFrame(float dt) {
        const float spawnRate = 4;

        int curr = Mathf.FloorToInt(Manager.Instance.time * spawnRate);
        int next = Mathf.FloorToInt((Manager.Instance.time + dt) * spawnRate);

        Vector2 worldMax = Manager.Instance.worldMax;
        Vector2 worldMin = Manager.Instance.worldMin;

        if (curr < next) {
            Shape shape = Shape.Create(ShapeType.BOX);

            float r = Random.Range(0f, 1f);

            shape.SetScale(Vector2.one * Mathf.Lerp(0.1f, 0.3f, r));
            shape.SetColor(particleColor);

            // @Hardcoded: Very very low order (lower than shadow)
            shape.spriteRenderer.sortingOrder = -20;

            shape.transform.position = new Vector2(Manager.Instance.worldMax.x, Random.Range(Manager.Instance.worldMin.y, Manager.Instance.worldMax.y));

            entries.Add(new Entry() { shape = shape, velocity = Mathf.Lerp(0.5f, 1f, r) });
        }

        foreach (Entry entry in entries) {
            Vector2 pos = entry.shape.transform.position;
            if (pos.x < worldMin.x - 0.3f) {
                entry.shape.Release();
            }
            else {
                entry.shape.transform.position = pos - new Vector2(dt * entry.velocity, 0);
            }
        }

        entries.RemoveAll(x => !x.shape.gameObject.activeInHierarchy);
    }

    public void Clear() {
        foreach (Entry entry in entries) {
            entry.shape.Release();
        }
        entries.Clear();
    }
}

class Entry {
    public Shape shape;
    public float velocity;
}