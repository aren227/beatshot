using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public int health;

    public UnityEvent<int> onDamaged;

    public void Damage(int damage) {
        health -= Mathf.Min(damage, health);

        onDamaged.Invoke(health);
    }
}
