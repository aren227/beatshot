using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Enemy : MonoBehaviour
{
    public Entity entity { get; private set; }
    public Health health { get; private set; }
    public Shape shape { get; private set; }
    // public Shape innerShape;

    public int maxHealth;

    public float scale;

    void Awake() {
        entity = GetComponent<Entity>();
        health = GetComponent<Health>();
        shape = GetComponentInChildren<Shape>();

        maxHealth = 550;
        health.health = maxHealth;

        scale = 3;

        health.onDamaged.AddListener(health => {
            if (health <= 0) {
                if (Manager.Instance.boss != this) {
                    // If this is not the actual boss, then just destroy this.

                    // Particle
                    {
                        Particle particle = Particle.Create();

                        particle.transform.position = transform.position;

                        particle.amount = 48;
                        particle.color = shape.props.color;
                        particle.duration = 1f;
                        particle.scale = 0.3f * scale;
                        particle.speed = 4f;
                    }

                    Destroy(gameObject);
                }
                else {
                    // You win.
                    Manager.Instance.WinGame();
                }
            }
            else {
                shape.Shake(0.3f, 0.15f);
                shape.Tint(0.2f, Color.Lerp(shape.props.color, Color.white, 0.7f));

                SFX.Instance.Play("hit");
            }
        });
    }

    void Start() {
        // shape.SetScale(new Vector2(3, 3));
        // innerShape.SetScale(new Vector2(3, 3));

        shape.SetType(ShapeType.BOX);
        // innerShape.SetType(ShapeType.BOX);

        shape.SetShadow(PrefabRegistry.Instance.shadowOffset, PrefabRegistry.Instance.shadowColor);
    }

    void Update() {
        // shape.SetScale(Vector2.one * scale * Mathf.Lerp(0.5f, 1f, ((float)health.health / maxHealth)));
    }
}
