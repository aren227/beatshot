using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ShapeProperties {
    public ShapeType type;
    public Color color;

    public Vector3 scale;

    public float blinkTime;
    public const float blinkPeriod = 0.05f;

    public float tintTime;
    public float tintDuration;
    public Color tintColor;

    public float shakeTime;
    public float shakeDuration;
    public float shakeIntensity;

    public float fadeTime;
    public float fadeDuration;
    public bool faded;

    public float prevScale;
    public float targetScale;
    public float scaleTime;
    public float scaleDuration;

    public bool hasShadow;
    public Vector2 shadowOffset;
    public Color shadowColor;
}

public enum ShapeType {
    CIRCLE,
    BOX,
    CIRCLE_BORDER,
    CIRCLE_BORDER_2,
}

public class Shape : MonoBehaviour
{
    public ShapeProperties props;

    public SpriteRenderer spriteRenderer;

    public bool ignoreRecorder = false;

    public bool ignoreUpdate = false;

    public SpriteRenderer shadowSpriteRenderer;

    public Collider2D tempCollider;

    void Awake() {
        Reset();

        Manager.Instance.shapes.Add(this);
    }

    public void Reset() {
        props = new ShapeProperties();

        props.scale = Vector3.one;
        props.targetScale = 1;

        SetColor(spriteRenderer.color);

        shadowSpriteRenderer.gameObject.SetActive(false);

        spriteRenderer.transform.localPosition = Vector3.zero;

        // spriteRenderer.sortingOrder = 0;

        ignoreRecorder = false;
        ignoreUpdate = false;

        if (tempCollider) {
            tempCollider.enabled = false;
            Destroy(tempCollider);
            tempCollider = null;
        }
    }

    void OnDestroy() {
        // Manager.Instance.shapes.Remove(this);
    }

    public void SetColor(Color color) {
        props.color = color;
    }

    public void SetType(ShapeType type) {
        props.type = type;

        if (type == ShapeType.CIRCLE) spriteRenderer.sprite = PrefabRegistry.Instance.circleSprite;
        else if (type == ShapeType.CIRCLE_BORDER) {
            spriteRenderer.sprite = PrefabRegistry.Instance.circleBorderSprite;
            // spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        }
        else if (type == ShapeType.CIRCLE_BORDER_2) {
            spriteRenderer.sprite = PrefabRegistry.Instance.circleBorder2Sprite;
        }
        else {
            spriteRenderer.sprite = PrefabRegistry.Instance.boxSprite;
        }

        shadowSpriteRenderer.sprite = spriteRenderer.sprite;
    }

    public void SetRadius(float radius) {
        props.scale = Vector3.one * radius * 2;
    }

    public float GetRadius() {
        // @Todo: Maybe Max(x, y) like Unity does?
        return props.scale.x / 2;
    }

    public void SetScale(Vector2 scale) {
        props.scale = new Vector3(scale.x, scale.y, 1);
    }

    public void SetShadow(Vector2 offset, Color color) {
        shadowSpriteRenderer.gameObject.SetActive(true);

        shadowSpriteRenderer.sprite = spriteRenderer.sprite;
        shadowSpriteRenderer.color = color;

        // @Hardcoded: Very low order.
        shadowSpriteRenderer.sortingOrder = -10;

        props.shadowOffset = offset;
        props.shadowColor = color;
    }

    public void Blink(float time) {
        props.blinkTime = time;
    }

    public void Tint(float time, Color color) {
        props.tintTime = props.tintDuration = time;
        props.tintColor = color;
    }

    public void Shake(float time, float intensity) {
        props.shakeTime = props.shakeDuration = time;
        props.shakeIntensity = intensity;
    }

    public void Fade(float time) {
        props.fadeDuration = props.fadeTime = time;
    }

    public void Scale(float scale, float time) {
        props.prevScale = props.targetScale;
        props.targetScale = scale;
        props.scaleTime = props.scaleDuration = time;
    }

    public void DoNextFrame(float dt) {
        if (ignoreUpdate) return;

        Color col = props.color;

        if (props.tintTime > 0) {
            col = Color.Lerp(col, props.tintColor, props.tintTime / props.tintDuration);

            props.tintTime -= Mathf.Min(dt, props.tintTime);
        }

        if (props.blinkTime > 0) {
            if (Mathf.FloorToInt(props.blinkTime / ShapeProperties.blinkPeriod) % 2 == 0) {
                // Set custom color?
                col = col * 0.2f;
            }

            props.blinkTime -= Mathf.Min(dt, props.blinkTime);
        }

        if (props.shakeTime > 0) {
            const float shakeSpeed = 20;
            Vector2 randomVector = new Vector2(Mathf.PerlinNoise(Time.time * shakeSpeed, 0) - 0.5f, Mathf.PerlinNoise(0, Time.time * shakeSpeed) - 0.5f);
            spriteRenderer.transform.localPosition = randomVector * (props.shakeTime / props.shakeDuration) * props.shakeIntensity;

            props.shakeTime -= Mathf.Min(dt, props.shakeTime);
        }
        else {
            spriteRenderer.transform.localPosition = Vector3.zero;
        }

        if (props.fadeTime > 0) {
            col.a *= props.fadeTime / props.fadeDuration;

            props.fadeTime -= Mathf.Min(dt, props.fadeTime);
            if (props.fadeTime <= 0) props.faded = true;
        }

        if (props.faded) {
            col.a = 0;
        }

        if (props.scaleTime > 0) {
            // if (spriteRenderer.drawMode == SpriteDrawMode.Sliced) {
            //     spriteRenderer.transform.localScale = Vector3.one * Mathf.Lerp(props.targetScale, props.prevScale, props.scaleTime / props.scaleDuration);
            //     spriteRenderer.size = props.scale;
            // }
            // else {
                spriteRenderer.transform.localScale = Vector2.one * Mathf.Lerp(props.targetScale, props.prevScale, props.scaleTime / props.scaleDuration);
            // }

            props.scaleTime -= Mathf.Min(dt, props.scaleTime);
        }
        else {
            // if (spriteRenderer.drawMode == SpriteDrawMode.Sliced) {
            //     spriteRenderer.transform.localScale = Vector3.one * props.targetScale;
            //     spriteRenderer.size = props.scale;
            // }
            // else {
                spriteRenderer.transform.localScale = Vector2.one * props.targetScale;
            // }
        }

        transform.localScale = props.scale;

        spriteRenderer.color = col;

        if (props.faded) {
            PoolManager.Instance?.Despawn(gameObject);
        }
    }

    void LateUpdate() {
        if (shadowSpriteRenderer.gameObject.activeInHierarchy) {
            shadowSpriteRenderer.transform.localScale = spriteRenderer.transform.localScale;
            shadowSpriteRenderer.transform.position = (Vector2)spriteRenderer.transform.position + props.shadowOffset;
        }
    }

    public void SetToEnemy() {
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (tempCollider) {
            Destroy(tempCollider);
        }

        if (props.type == ShapeType.CIRCLE) {
            tempCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        else {
            tempCollider = gameObject.AddComponent<BoxCollider2D>();
        }
    }

    public void Release() {
        PoolManager.Instance?.Despawn(gameObject);
    }

    public static Shape Create(ShapeType type) {
        Shape shape = PoolManager.Instance.Spawn("shape").GetComponent<Shape>();
        shape.Reset();
        shape.SetType(type);

        // @Todo: Very dirty hack.
        // This works because player, enemy, projectiles they are not using Shape.Create().
        // Their orders are defined in their prefabs.
        // I think they also use Shape.Create for consistency.
        shape.spriteRenderer.sortingOrder = 0;

        return shape;
    }
}
