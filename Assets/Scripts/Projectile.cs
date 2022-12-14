using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector2 velocity;
    public float radius { get; private set; } = 0.5f;

    List<int> collidedEntities = new List<int>();

    public Shape shape { get; private set; }

    static RaycastHit2D[] hits = new RaycastHit2D[256];

    void Awake() {
        shape = GetComponentInChildren<Shape>();
    }

    void Start() {
        shape.SetShadow(PrefabRegistry.Instance.shadowOffset * 0.7f, PrefabRegistry.Instance.shadowColor);
    }

    public void IgnoreEntity(Entity entity) {
        collidedEntities.Add(entity.id);
    }

    public void IgnoreClonedPlayers() {
        Manager.Instance.MakeClonedPlayersIgnoreProjectile(this);
    }

    public void DoNextFrame(float dt) {
        Vector2 curPos = (Vector2)transform.position;
        Vector2 nextPos = curPos + velocity * dt;

        int ignoreSelfLayer = ~(1 << gameObject.layer);
        int count = Physics2D.CircleCastNonAlloc(curPos, shape.GetRadius(), (nextPos-curPos).normalized, hits, (nextPos-curPos).magnitude, ignoreSelfLayer);

        bool destroy = false;

        // @Todo: Use layers to speed it up.
        for (int i = 0; i < count; i++) {
            Entity entity = hits[i].collider.GetComponent<Entity>();
            if (!entity) entity = hits[i].collider.transform.parent?.GetComponent<Entity>();
            if (!entity) {
                // If collided with something, destory anyway.
                destroy = true;
                continue;
            }

            if (collidedEntities.Contains(entity.id)) continue;

            destroy = true;

            // Collide only once.
            collidedEntities.Add(entity.id);

            Health health = entity.GetComponent<Health>();
            if (health) {
                health.Damage(1);
            }
        }

        if (destroy) {
            // Particle
            {
                Particle particle = Particle.Create();

                particle.transform.position = transform.position;

                particle.amount = 16;
                particle.color = GetComponentInChildren<Shape>().props.color;
                particle.duration = 0.4f;
                particle.scale = 0.25f;
                particle.speed = 3f;
            }

            // Drop fx
            {
                Shape drop = Shape.Create(ShapeType.CIRCLE);

                // @Hardcoded: Low order (like shadow)
                drop.spriteRenderer.sortingOrder = -10;

                drop.transform.position = transform.position;

                drop.SetColor(new Color(shape.props.color.r, shape.props.color.g, shape.props.color.b, 0.06f));
                drop.Fade(0.5f);
                drop.Scale(2f, 0.5f);
            }

            Destroy(gameObject);
        }
        else {
            transform.position = nextPos;
        }
    }
}
