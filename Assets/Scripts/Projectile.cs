using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector2 velocity;

    void Update() {
        transform.position = transform.position + (Vector3)velocity * Time.deltaTime;
    }
}
