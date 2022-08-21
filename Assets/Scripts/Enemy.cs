using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Enemy : MonoBehaviour
{
    public Entity entity;
    public Shape shape;

    void Awake() {
        entity = GetComponent<Entity>();
        shape = GetComponent<Shape>();

        Health health = GetComponent<Health>();

        health.health = 100;
        health.onDamaged.AddListener(health => {
            if (health <= 0) {
                DestroyImmediate(gameObject);
            }
            else {
                shape.Shake(0.3f, 0.15f);
                shape.Tint(0.2f, Color.white);
            }
        });
    }
}
