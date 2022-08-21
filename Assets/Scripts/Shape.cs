using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ShapeProperties {
    public ShapeType type;
    public Color color;

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

    public float targetScale;
    public float scaleTime;
    public float scaleDuration;
}

public enum ShapeType {
    CIRCLE,
    BOX,
}

public class Shape : MonoBehaviour
{
    public ShapeProperties props;

    public SpriteRenderer spriteRenderer;

    public bool ignoreRecorder = false;

    void Awake() {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        props.targetScale = 1;

        SetColor(spriteRenderer.color);

        Manager.Instance.shapes.Add(this);
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
        else spriteRenderer.sprite = PrefabRegistry.Instance.boxSprite;
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
        props.targetScale = scale;
        props.scaleTime = props.scaleDuration = time;
    }

    public void DoNextFrame(float dt) {
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

        if (props.faded) col.a = 0;

        if (props.scaleTime > 0) {
            spriteRenderer.transform.localScale = Vector3.one * Mathf.Lerp(props.targetScale, 1, props.scaleTime / props.scaleDuration);

            props.scaleTime -= Mathf.Min(dt, props.scaleTime);
        }
        else {
            spriteRenderer.transform.localScale = Vector3.one * props.targetScale;
        }

        spriteRenderer.color = col;
    }

    public static Shape Create() {
        Shape shape = Instantiate(PrefabRegistry.Instance.shape).GetComponent<Shape>();
        return shape;
    }
}
