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

    const float ignoreDamageTime = 1f;

    static Collider2D[] hitColliders = new Collider2D[256];

    public bool shootFlag;
    public Vector2 shootDirection;

    public PlayerRecorder playback;

    void Awake() {
        entity = GetComponent<Entity>();
        health = GetComponent<Health>();
        shape = GetComponentInChildren<Shape>();

        health.health = 1;
        health.onDamaged.AddListener(TakeDamage);
    }

    void Start() {
        shape.SetRadius(0.25f);
    }

    public void MakeInvincible(float time) {
        shape.Blink(time);
        health.ignoreDamageUntil = Time.time + time;
    }

    public void TakeDamage(int health) {
        shape.Blink(ignoreDamageTime);
        shape.Shake(0.3f, 0.5f);

        this.health.ignoreDamageUntil = Time.time + ignoreDamageTime;

        Shape drop = Shape.Create();
        drop.transform.position = transform.position;

        drop.SetColor(new Color(shape.props.color.r, shape.props.color.g, shape.props.color.b, 0.2f));
        drop.Fade(0.5f);
        drop.Scale(3f, 0.5f);

        // Particle
        {
            Particle particle = Particle.Create();

            particle.transform.position = transform.position;

            particle.amount = 32;
            particle.color = shape.props.color;
            particle.duration = 0.5f;
            particle.scale = 0.3f;
            particle.speed = 3f;
        }

        Debug.Log(health);

        if (health <= 0) {
            DestroyImmediate(gameObject);

            if (this == Manager.Instance.currentPlayer) {
                Manager.Instance.RewindGame();
            }
        }
    }

    public void Shoot(Vector2 direction) {
        lastShoot = Time.time;
        shootFlag = true;
        shootDirection = direction;

        Projectile projectile = Manager.Instance.AddProjectile();
        projectile.transform.position = transform.position;

        const float bulletSpeed = 13f;

        projectile.shape.SetRadius(0.2f);
        projectile.velocity = direction * bulletSpeed;

        projectile.IgnoreEntity(entity);

        if (this != Manager.Instance.currentPlayer) {
            // Dark bullet for cloned player.
            projectile.GetComponentInChildren<Shape>().SetColor(Color.yellow * 0.5f);
        }
    }

    public void DoNextFrame(float dt) {
        // Controlled by user.
        if (playback == null) {
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
            shootFlag = false;
            if (Input.GetMouseButton(0)) {
                const float shootDelay = 0.1f;
                if (Time.time - lastShoot >= shootDelay) {
                    Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                    Vector2 lookDir = (worldMousePos - (Vector2)transform.position).normalized;
                    Shoot(lookDir);
                }
            }

            // Damage
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, shape.GetRadius(), hitColliders, LayerMask.GetMask("Enemy"));

            // for (int i = 0; i < count; i++) {
            //     if (hitColliders[i].GetComponent<Enemy>()) {
            //         health.Damage(1);
            //         lastDamage = Time.time;
            //         break;
            //     }
            // }

            if (count > 0) {
                health.Damage(1);
            }
        }
        // Play recorded inputs.
        else {
            // End of record, kill.
            if (playback.lastProcessedFrame >= playback.snapshots.Count-1) {
                TakeDamage(0);
            }
            else {
                while (playback.lastProcessedFrame < playback.snapshots.Count-1) {
                    PlayerSnapshot snapshot = playback.snapshots[playback.lastProcessedFrame+1];

                    // Not play yet.
                    if (snapshot.time > Manager.Instance.time) break;

                    transform.position = snapshot.position;

                    if (snapshot.hasShoot) {
                        Shoot(snapshot.shootDirection);
                    }

                    playback.lastProcessedFrame++;
                }
            }
        }
    }
}
