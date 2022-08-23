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

public interface Pattern {
    IEnumerator Play();
}

public class ShootPattern : Pattern {
    public Entity entity;
    public float duration;
    public float shootRate = 0.15f;
    public Player target;

    float lastShoot;

    public ShootPattern(Entity entity, float duration, Player target, float shootRate = 0.15f) {
        this.entity = entity;
        this.duration = duration;
        this.target = target;
        this.shootRate = shootRate;
    }

    public IEnumerator Play() {
        float begin = Manager.Instance.time;

        while (entity && begin + duration > Manager.Instance.time) {
            if (target) {
                Projectile projectile = Manager.Instance.AddProjectile();
                projectile.transform.position = entity.transform.position;

                const float bulletSpeed = 7f;

                Vector2 direction = (target.transform.position - entity.transform.position).normalized;

                projectile.shape.SetRadius(0.3f);
                projectile.velocity = direction * bulletSpeed;

                projectile.IgnoreEntity(entity);
                projectile.IgnoreClonedPlayers();

                // @Hardcoded
                Shape shape = projectile.GetComponentInChildren<Shape>();
                shape.SetColor(Color.Lerp(Color.red, Color.white, 0.5f));
                shape.DoNextFrame(0);
            }

            yield return new WaitForSeconds(shootRate);
        }
    }
}

public class RotatePattern : Pattern {
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

public class MoveToPattern : Pattern {
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

public class BulletCirclePattern : Pattern {
    public Entity entity;
    public int count = 16;
    public float beginAngle = 0;
    public float bulletRadius = 0.3f;
    public float bulletSpeed = 5;

    public BulletCirclePattern(Entity entity, float beginAngle) {
        this.entity = entity;
        this.beginAngle = beginAngle;
    }

    public IEnumerator Play() {
        for (int i = 0; i < count; i++) {
            Projectile projectile = Manager.Instance.AddProjectile();
            projectile.transform.position = entity.transform.position;

            float angle = beginAngle * Mathf.Deg2Rad + (float)i / count * Mathf.PI * 2;

            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            projectile.shape.SetRadius(bulletRadius);
            projectile.velocity = direction * bulletSpeed;

            projectile.IgnoreEntity(entity);
            projectile.IgnoreClonedPlayers();

            // @Hardcoded
            projectile.GetComponentInChildren<Shape>().SetColor(Color.Lerp(Color.red, Color.white, 0.5f));
        }
        yield return null;
    }
}

public class LaserPattern : Pattern {
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
            // @Todo: RELEASE SHAPE.
            Shape shape = Shape.Create(ShapeType.BOX);
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

        GameObject.Destroy(anchor);
    }
}

public class DashPattern : Pattern {
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
        Shape shape = Shape.Create(ShapeType.BOX);

        shape.transform.position = targetPos;
        shape.SetColor(new Color(0.5f, 0.5f, 0.5f, 0.5f));
        shape.SetScale(entity.GetComponentInChildren<Shape>().props.scale);

        yield return new WaitForSeconds(warningDuration);

        shape.Release();

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

public class AreaPattern : Pattern {
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
        Shape shape = Shape.Create(ShapeType.BOX);

        Color color = PrefabRegistry.Instance.warningColor;
        color.a = 0.3f;

        shape.SetColor(color);

        shape.SetScale(size);
        shape.transform.position = center;

        shape.DoNextFrame(0);

        yield return new WaitForSeconds(warningDuration);

        color = Color.red;
        if (Manager.Instance.boss) color = Manager.Instance.boss.shape.props.color;

        shape.SetColor(color);
        shape.SetToEnemy();

        float begin = Manager.Instance.time;

        shape.transform.position = center - moveDirection * 20;
        shape.transform.DOMove(center, transitionDuration);
        shape.Shake(0.5f, 0.2f);

        shape.DoNextFrame(0);

        yield return new WaitForSeconds(duration);

        shape.transform.DOMove(center + moveDirection * 20, transitionDuration).OnComplete(() => {
            shape.Release();
        });
    }
}

public class FollowPattern : Pattern {
    public Entity entity;
    public float duration;
    public float rotation;
    public Player target;

    public Vector2 positionVel;

    public const float smoothTime = 0.5f;
    public const float maxSpeed = 3;

    public FollowPattern(Entity entity, float duration, float rotation, Player target) {
        this.entity = entity;
        this.duration = duration;
        this.rotation = rotation;
        this.target = target;
    }

    public IEnumerator Play() {
        float begin = Manager.Instance.time;

        entity.transform.localEulerAngles = new Vector3(0, 0, 0);

        positionVel = Vector2.zero;

        while (entity && begin + duration > Manager.Instance.time) {
            float time = Manager.Instance.time;

            float t = Mathf.Clamp01((time - begin) / duration);
            t = EasingFunctions.InOutQuad(t);

            Vector2 targetPos = entity.transform.position;
            if (target) targetPos = target.transform.position;

            entity.transform.position = Vector2.SmoothDamp(
                entity.transform.position, targetPos, ref positionVel, smoothTime, maxSpeed, Manager.Instance.deltaTime
            );
            entity.transform.eulerAngles = new Vector3(0, 0, t * rotation);

            yield return null;
        }
    }
}

public class LockDamagePattern : Pattern {
    public Enemy enemy;
    public float duration;
    public List<Entity> targetEntities = new List<Entity>();

    Color originalColor;

    public LockDamagePattern(Enemy enemy, float duration) {
        this.enemy = enemy;
        this.duration = duration;
        originalColor = enemy.shape.props.color;
    }

    public IEnumerator Play() {
        float begin = Manager.Instance.time;

        while (enemy && begin + duration > Manager.Instance.time) {
            float time = Manager.Instance.time;

            bool canBeDamaged = true;
            foreach (Entity entity in targetEntities) {
                if (entity) {
                    canBeDamaged = false;
                    break;
                }
            }

            // @Hardcoded
            if (canBeDamaged) {
                enemy.shape.SetColor(originalColor);
                // @Todo: This will overwrite but for now, this value is not used by enemy.
                enemy.health.ignoreDamageUntil = 0;
            }
            else {
                enemy.shape.SetColor(Color.gray);
                enemy.health.ignoreDamageUntil = float.PositiveInfinity;
            }

            yield return null;
        }

        enemy.shape.SetColor(originalColor);
        enemy.health.ignoreDamageUntil = 0;
    }
}