using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

    public ShootPattern(Entity entity, float duration, float shootRate = 0.15f) {
        this.entity = entity;
        this.duration = duration;
        this.shootRate = shootRate;
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

                projectile.shape.SetRadius(0.3f);
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

    public float from;
    public float to;

    public RotatePattern(Entity entity, float duration, float from, float to) {
        this.entity = entity;
        this.duration = duration;
        this.from = from;
        this.to = to;
    }

    public IEnumerator Play() {
        float begin = Manager.Instance.time;

        while (entity && begin + duration > Manager.Instance.time) {
            float time = Manager.Instance.time;

            float t = Mathf.Clamp01((time - begin) / duration);
            t = EasingFunctions.InOutQuad(t);

            entity.transform.eulerAngles = new Vector3(0, 0, Mathf.Lerp(from, to, t));

            yield return null;
        }

        if (entity) entity.transform.eulerAngles = new Vector3(0, 0, to);
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

        if (entity) {
            entity.transform.position = target;
            entity.transform.eulerAngles = new Vector3(0, 0, 0);
        }
    }
}

public class BulletCirclePattern {
    public Entity entity;
    public int count = 16;
    public float beginAngle = 0;
    public float bulletRadius = 0.3f;
    public float bulletSpeed = 6;

    public BulletCirclePattern(Entity entity, float beginAngle) {
        this.entity = entity;
        this.beginAngle = beginAngle;
    }

    public IEnumerator Play() {
        for (int i = 0; i < count; i++) {
            Projectile projectile = Manager.Instance.AddProjectile();
            projectile.transform.position = entity.transform.position;

            float angle = beginAngle * Mathf.Deg2Rad + (float)i / 16f * Mathf.PI * 2;

            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            projectile.shape.SetRadius(bulletRadius);
            projectile.velocity = direction * bulletSpeed;

            projectile.IgnoreEntity(entity);

            // @Hardcoded
            projectile.GetComponentInChildren<Shape>().SetColor(Color.Lerp(Color.red, Color.white, 0.5f));
        }
        yield return null;
    }
}

public class LaserPattern {
    public Entity entity;
    public float warningDuration;
    public float laserDuration;
    public int count = 4;

    public LaserPattern(Entity entity, float warningDuration, float laserDuration) {
        this.entity = entity;
        this.warningDuration = warningDuration;
        this.laserDuration = laserDuration;
    }

    public IEnumerator Play() {
        List<Shape> shapes = new List<Shape>();

        GameObject anchor = new GameObject("Laser Anchor");
        anchor.transform.SetParent(entity.transform, false);

        for (int i = 0; i < count; i++) {
            Shape shape = Shape.Create();
            shape.SetType(ShapeType.BOX);
            shape.SetColor(new Color(0.5f, 0.5f, 0.5f, 0.5f));

            shape.transform.SetParent(anchor.transform, false);

            const float laserLength = 20f;
            const float laserThickness = 0.7f;

            float angle = (float)i / count * Mathf.PI * 2;

            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            shape.transform.localPosition = new Vector3(dir.x, dir.y) * laserLength * 0.5f;
            shape.transform.localEulerAngles = new Vector3(0, 0, angle * Mathf.Rad2Deg + 90);
            shape.transform.localScale = new Vector3(laserThickness, laserLength, 1);

            shapes.Add(shape);
        }

        yield return new WaitForSeconds(warningDuration);

        foreach (Shape shape in shapes) {
            shape.SetColor(Color.red);
            shape.SetToEnemy();
        }

        yield return new WaitForSeconds(laserDuration);

        GameObject.DestroyImmediate(anchor);
    }
}

public class DashPattern {
    public Entity entity;
    public float warningDuration;
    public float dashDuration;
    public Vector2 fromPos;
    public Vector2 targetPos;

    public DashPattern(Entity entity, float warningDuration, float dashDuration) {
        this.entity = entity;
        this.warningDuration = warningDuration;
        this.dashDuration = dashDuration;

        fromPos = entity.transform.position;

        Player player = PatternUtil.GetOldestPlayer();
        if (player) {
            targetPos = player.transform.position;
        }
        else {
            targetPos = entity.transform.position;
        }
    }

    public IEnumerator Play() {
        Shape shape = Shape.Create();

        shape.transform.position = targetPos;
        shape.SetColor(new Color(0.5f, 0.5f, 0.5f, 0.5f));
        shape.SetType(ShapeType.BOX);
        shape.SetScale(entity.GetComponentInChildren<Shape>().props.scale);

        yield return new WaitForSeconds(warningDuration);

        GameObject.Destroy(shape.gameObject);

        float begin = Manager.Instance.time;

        while (entity && begin + dashDuration > Manager.Instance.time) {
            float time = Manager.Instance.time;

            float t = Mathf.Clamp01((time - begin) / dashDuration);
            t = EasingFunctions.OutQuad(t);

            entity.transform.position = Vector2.Lerp(fromPos, targetPos, t);

            yield return null;
        }

        if (entity) entity.transform.position = targetPos;
    }
}

public class AreaPattern {
    public float warningDuration;
    public float transitionDuration = 0.5f;
    public float duration;
    public Vector2 moveDirection;
    public Vector2 center;
    public Vector2 size;

    public AreaPattern(float warningDuration, float duration, Vector2 center, Vector2 size, Vector2 moveDirection) {
        this.warningDuration = warningDuration;
        this.duration = duration;
        this.center = center;
        this.size = size;
        this.moveDirection = moveDirection;
    }

    public IEnumerator Play() {
        Shape shape = Shape.Create();

        shape.SetColor(new Color(0.5f, 0.5f, 0.5f, 0.5f));
        shape.SetType(ShapeType.BOX);

        shape.SetScale(size);
        shape.transform.position = center;

        yield return new WaitForSeconds(warningDuration);

        shape.SetColor(Color.red);
        shape.SetToEnemy();

        float begin = Manager.Instance.time;

        shape.transform.position = center - moveDirection * 20;
        shape.transform.DOMove(center, transitionDuration);

        yield return new WaitForSeconds(duration);

        shape.transform.DOMove(center + moveDirection * 20, transitionDuration).OnComplete(() => {
            if (shape) GameObject.DestroyImmediate(shape.gameObject);
        });
    }
}