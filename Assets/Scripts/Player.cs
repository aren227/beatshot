using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Vector3 moveDir;
    Vector3 moveDirVel;

    float lastShoot;

    public Entity entity;
    public Health health;
    public Shape shape;

    float lastDamage;
    const float ignoreDamageTime = 1f;

    static Collider2D[] hitColliders = new Collider2D[256];

    CircleCollider2D circleCollider;

    void Awake() {
        entity = GetComponent<Entity>();
        health = GetComponent<Health>();
        shape = GetComponent<Shape>();

        circleCollider = GetComponent<CircleCollider2D>();

        health.health = 1;
        health.onDamaged.AddListener(health => {
            shape.Blink(ignoreDamageTime);
            shape.Shake(0.3f, 0.5f);

            Shape drop = Shape.Create();
            drop.transform.position = transform.position;

            drop.SetColor(new Color(shape.props.color.r, shape.props.color.g, shape.props.color.b, 0.2f));
            drop.Fade(0.5f);
            drop.Scale(3f, 0.5f);

            // Particle
            Particle particle = Particle.Create();

            // particle.transform.SetParent(transform, false);
            particle.transform.position = transform.position;

            particle.amount = 32;
            particle.color = shape.props.color;
            particle.duration = 0.5f;
            particle.scale = 0.3f;
            particle.speed = 3f;

            Debug.Log(health);

            if (health <= 0) {
                DestroyImmediate(gameObject);
                Manager.Instance.RewindGame();
            }
        });
    }

    public void DoNextFrame(float dt) {
        const float speed = 7;

        Vector3 targetMoveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) targetMoveDir += Vector3.up;
        if (Input.GetKey(KeyCode.S)) targetMoveDir += Vector3.down;
        if (Input.GetKey(KeyCode.A)) targetMoveDir += Vector3.left;
        if (Input.GetKey(KeyCode.D)) targetMoveDir += Vector3.right;
        targetMoveDir.Normalize();

        const float moveDirSmoothTime = 0.05f;

        moveDir = Vector3.SmoothDamp(moveDir, targetMoveDir, ref moveDirVel, moveDirSmoothTime);

        transform.position = transform.position + moveDir * dt * speed;

        // Dash
        if (Input.GetKeyDown(KeyCode.Space)) {
            const float dashDistance = 1;
            transform.position = transform.position + moveDir * dashDistance;
        }

        // Shoot
        if (Input.GetMouseButton(0)) {
            const float shootDelay = 0.1f;
            if (Time.time - lastShoot >= shootDelay) {
                lastShoot = Time.time;

                Projectile projectile = Manager.Instance.AddProjectile();
                projectile.transform.position = transform.position;

                Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                Vector2 lookDir = (worldMousePos - (Vector2)transform.position).normalized;

                const float bulletSpeed = 13f;

                projectile.SetRadius(0.2f);
                projectile.velocity = lookDir * bulletSpeed;

                projectile.IgnoreEntity(entity);
            }
        }

        // Damage
        if (Time.time - lastDamage > ignoreDamageTime) {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, circleCollider.radius, hitColliders);

            for (int i = 0; i < count; i++) {
                if (hitColliders[i].GetComponent<Enemy>()) {
                    health.Damage(1);
                    lastDamage = Time.time;
                    break;
                }
            }
        }
    }
}
