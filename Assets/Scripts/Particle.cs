using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public Shape[] shapes;
    public Vector2[] velocities;

    public int amount;
    public Color color;
    public float duration;
    public float scale;
    public float speed;

    float beginTime;

    void Start() {
        shapes = new Shape[amount];
        velocities = new Vector2[amount];

        for (int i = 0; i < amount; i++) {
            shapes[i] = Shape.Create();

            shapes[i].transform.SetParent(transform, false);

            shapes[i].SetColor(color);
            shapes[i].transform.localScale = Vector3.one * scale;
            shapes[i].Scale(0f, duration);

            // Particles are not recorded.
            shapes[i].ignoreRecorder = true;

            velocities[i] = Random.insideUnitCircle.normalized * speed * Random.Range(0.5f, 1f);
        }

        beginTime = Manager.Instance.time;
    }

    void Update() {
        for (int i = 0; i < amount; i++) {
            shapes[i].transform.position = shapes[i].transform.position + (Vector3)velocities[i] * Time.deltaTime;
        }

        if (beginTime + duration <= Manager.Instance.time) {
            DestroyImmediate(gameObject);
        }
    }

    public static Particle Create() {
        GameObject obj = new GameObject("Particle");
        return obj.AddComponent<Particle>();
    }
}
