using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Vector3 moveDir;
    Vector3 moveDirVel;

    float lastShoot;

    public Entity entity;

    void Awake() {
        entity = GetComponent<Entity>();
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
    }
}
