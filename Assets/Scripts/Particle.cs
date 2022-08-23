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

    void Awake() {
    }

    void OnDestroy() {
        foreach (Shape shape in shapes) shape.Release();
    }

    void Start() {
        Manager.Instance.particles.Add(this);

        shapes = new Shape[amount];
        velocities = new Vector2[amount];

        for (int i = 0; i < amount; i++) {
            shapes[i] = Shape.Create(ShapeType.CIRCLE);

            shapes[i].transform.SetParent(transform, false);

            shapes[i].transform.localPosition = Vector3.zero;

            shapes[i].SetColor(color);
            shapes[i].SetScale(Vector2.one * scale);
            shapes[i].Scale(0f, duration);

            // Particles are not recorded.
            shapes[i].ignoreRecorder = true;

            velocities[i] = Random.insideUnitCircle.normalized * speed * Random.Range(0.5f, 1f);
        }

        beginTime = Manager.Instance.time;
    }

    public void DoNextFrame(float dt) {
        for (int i = 0; i < amount; i++) {
            shapes[i].transform.position = shapes[i].transform.position + (Vector3)velocities[i] * dt;
        }

        if (beginTime + duration <= Manager.Instance.time) {
            Destroy(gameObject);
        }
    }

    public static Particle Create() {
        GameObject obj = new GameObject("Particle");
        return obj.AddComponent<Particle>();
    }
}
