using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public int health;
    public float ignoreDamageUntil;

    public UnityEvent<int> onDamaged;

    public void Damage(int damage) {
        if (Time.time <= ignoreDamageUntil) return;
        if (Manager.Instance.invincibleFlag) return;

        health -= Mathf.Min(damage, health);

        onDamaged.Invoke(health);
    }
}
