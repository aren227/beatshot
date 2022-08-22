using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Enemy : MonoBehaviour
{
    public Entity entity;
    public Health health;
    public Shape shape;

    void Awake() {
        entity = GetComponent<Entity>();
        shape = GetComponentInChildren<Shape>();

        health = GetComponent<Health>();

        health.health = 1000;
        health.onDamaged.AddListener(health => {
            if (health <= 0) {
                DestroyImmediate(gameObject);
            }
            else {
                shape.Shake(0.3f, 0.15f);
                shape.Tint(0.2f, Color.white);
            }
        });

        shape.SetScale(new Vector2(3, 3));
    }

    void Start() {
        shape.SetType(ShapeType.BOX);
    }
}
