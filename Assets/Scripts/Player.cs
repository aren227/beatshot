using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Vector3 moveDir;
    Vector3 moveDirVel;

    float lastShoot;

    public Entity entity { get; private set; }
    public Health health { get; private set; }
    public Shape shape { get; private set; }

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
        shape.SetShadow(PrefabRegistry.Instance.shadowOffset * 0.7f, PrefabRegistry.Instance.shadowColor);
    }

    public void MakeInvincible(float time) {
        shape.Blink(time);
        health.ignoreDamageUntil = Time.time + time;
    }

    public void TakeDamage(int health) {
        shape.Blink(ignoreDamageTime);
        shape.Shake(0.3f, 0.5f);

        this.health.ignoreDamageUntil = Time.time + ignoreDamageTime;

        Shape drop = Shape.Create(ShapeType.CIRCLE);
        drop.transform.position = transform.position;

        drop.SetColor(new Color(shape.props.color.r, shape.props.color.g, shape.props.color.b, 0.2f));
        drop.Fade(0.5f);
        drop.Scale(4f, 0.5f);

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

        if (health <= 0) {
            Destroy(gameObject);

            if (this == Manager.Instance.currentPlayer) {
                Manager.Instance.RewindGame(musicEnded: false);
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
            // projectile.GetComponentInChildren<Shape>().SetColor(Color.yellow * 0.5f);

            // White bullet for cloned players.
            projectile.GetComponentInChildren<Shape>().SetColor(Color.white * 0.7f);
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

            // Clamp position to bounds.
            Vector2 worldMin = Manager.Instance.worldMin;
            Vector2 worldMax = Manager.Instance.worldMax;
            float radius = shape.GetRadius();

            transform.position = new Vector2(
                Mathf.Clamp(transform.position.x, worldMin.x + radius, worldMax.x - radius),
                Mathf.Clamp(transform.position.y, worldMin.y + radius, worldMax.y - radius)
            );

            // Dash
            // if (Input.GetKeyDown(KeyCode.Space)) {
            //     const float dashDistance = 1;
            //     transform.position = transform.position + moveDir * dashDistance;
            // }

            // Shoot
            shootFlag = false;
            if (Input.GetMouseButton(0)) {
                float shootPeriod = 0.25f;
                float offset = -0.1f;
                float currentBeat = Manager.Instance.beamTime + offset;
                float nextBeat = Manager.Instance.beamTime + Manager.Instance.bps * Manager.Instance.deltaTime + offset;

                bool canShoot = false;

                if (Mathf.FloorToInt(currentBeat / shootPeriod) < Mathf.FloorToInt(nextBeat / shootPeriod)) {
                    canShoot = true;
                }

                if (canShoot) {
                    Vector2 worldMousePos = Camera.main.ViewportToWorldPoint(Input.mousePosition / new Vector2(Screen.width, Screen.height));

                    Vector2 lookDir = (worldMousePos - (Vector2)transform.position).normalized;
                    Shoot(lookDir);

                    SFX.Instance.Play("shoot");
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
