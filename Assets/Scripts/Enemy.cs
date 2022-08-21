using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Entity entity;

    void Awake() {
        entity = GetComponent<Entity>();

        Health health = GetComponent<Health>();

        health.health = 10;
        health.onDamaged.AddListener(health => {
            if (health <= 0) Destroy(gameObject);
        });
    }
}
