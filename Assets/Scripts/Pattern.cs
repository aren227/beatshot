using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public class DashPattern {
//     public Vector2 targetPos;

//     public float duration = 2;

//     public void Init() {

//     }

//     public void Update(float t, float dt) {
//         if (t < 1) {

//         }
//     }
// }

public static class PatternUtil {
    public static Player GetOldestPlayer() {
        Player player = null;
        int id = int.MinValue;
        foreach (Player p in Manager.Instance.players) {
            if (id < p.entity.id) {
                id = p.entity.id;
                player = p;
            }
        }
        return player;
    }
}

public class ShootPattern {
    public Entity entity;
    public float duration;
    public float shootRate = 0.15f;

    float lastShoot;

    public ShootPattern(Entity entity, float duration) {
        this.entity = entity;
        this.duration = duration;
    }

    public IEnumerator Play() {
        float begin = Manager.Instance.time;

        while (entity && begin + duration > Manager.Instance.time) {
            Player target = PatternUtil.GetOldestPlayer();

            if (target) {
                Projectile projectile = Manager.Instance.AddProjectile();
                projectile.transform.position = entity.transform.position;

                const float bulletSpeed = 13f;

                Vector2 direction = (target.transform.position - entity.transform.position).normalized;

                projectile.SetRadius(0.3f);
                projectile.velocity = direction * bulletSpeed;

                projectile.IgnoreEntity(entity);

                // @Hardcoded
                projectile.GetComponentInChildren<Shape>().SetColor(Color.Lerp(Color.red, Color.white, 0.5f));
            }

            yield return new WaitForSeconds(shootRate);
        }
    }
}

public class RotatePattern {
    public Entity entity;
    public float duration;

    public float warmup = 1f;
    public float speed = 45f;

    public RotatePattern(Entity entity, float duration) {
        this.entity = entity;
        this.duration = duration;
    }

    public IEnumerator Play() {
        float begin = Manager.Instance.time;

        while (entity && begin + duration > Manager.Instance.time) {
            float time = Manager.Instance.time;

            float t = 1;
            if (begin + duration - time < warmup) {
                t = (begin + duration - time) / warmup;
            }
            else if (time - begin < warmup) {
                t = (time - begin) / warmup;
            }

            entity.transform.Rotate(new Vector3(0, 0, t * speed * Time.deltaTime), Space.Self);

            yield return null;
        }
    }
}

public class MoveToPattern {
    public Entity entity;
    public float duration;

    public Vector2 from;
    public Vector2 target;
    public float rotationTarget;

    public MoveToPattern(Entity entity, float duration, Vector2 target) {
        this.entity = entity;
        this.duration = duration;

        from = entity.transform.position;
        this.target = target;

        if (target.x > from.x) rotationTarget = 90;
        else rotationTarget = -90;
    }

    public IEnumerator Play() {
        float begin = Manager.Instance.time;

        while (entity && begin + duration > Manager.Instance.time) {
            float time = Manager.Instance.time;

            float t = Mathf.Clamp01((time - begin) / duration);
            t = EasingFunctions.InOutQuad(t);

            entity.transform.position = Vector2.Lerp(from, target, t);
            entity.transform.eulerAngles = new Vector3(0, 0, t * rotationTarget);

            yield return null;
        }
    }
}