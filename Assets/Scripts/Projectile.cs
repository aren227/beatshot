using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector2 velocity;
    public float radius { get; private set; } = 0.5f;

    List<int> collidedEntities = new List<int>();

    static RaycastHit2D[] hits = new RaycastHit2D[256];

    void Start() {
    }

    public void SetRadius(float radius) {
        this.radius = radius;
        transform.localScale = Vector3.one * radius * 2;
    }

    public void IgnoreEntity(Entity entity) {
        collidedEntities.Add(entity.id);
    }

    public void DoNextFrame(float dt) {
        Vector2 curPos = (Vector2)transform.position;
        Vector2 nextPos = curPos + velocity * dt;

        int ignoreSelfLayer = ~(1 << gameObject.layer);
        int count = Physics2D.CircleCastNonAlloc(curPos, radius, (nextPos-curPos).normalized, hits, (nextPos-curPos).magnitude, ignoreSelfLayer);

        bool destroy = false;

        // @Todo: Use layers to speed it up.
        for (int i = 0; i < count; i++) {
            Entity entity = hits[i].collider.GetComponent<Entity>();
            if (!entity) continue;

            if (collidedEntities.Contains(entity.id)) continue;

            // Collide only once.
            collidedEntities.Add(entity.id);

            Health health = hits[i].collider.GetComponent<Health>();
            if (health) {
                health.Damage(1);
            }
        }

        if (destroy) {
            Manager.Instance.RemoveProjectile(this);
        }
        else {
            transform.position = nextPos;
        }
    }
}
