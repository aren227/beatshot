using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shape : MonoBehaviour
{
    public Color color;

    public float blinkTime;
    const float blinkPeriod = 0.05f;

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

    SpriteRenderer spriteRenderer;

    void Awake() {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Blink(float time) {
        blinkTime = time;
    }

    public void Tint(float time, Color color) {
        tintTime = tintDuration = time;
        tintColor = color;
    }

    public void Shake(float time, float intensity) {
        shakeTime = shakeDuration = time;
        shakeIntensity = intensity;
    }

    public void Fade(float time) {
        fadeDuration = fadeTime = time;
    }

    public void Scale(float scale, float time) {
        targetScale = scale;
        scaleTime = scaleDuration = time;
    }

    void Update() {
        Color col = color;

        if (tintTime > 0) {
            col = Color.Lerp(col, tintColor, tintTime / tintDuration);

            tintTime -= Mathf.Min(Time.deltaTime, tintTime);
        }

        if (blinkTime > 0) {
            if (Mathf.FloorToInt(blinkTime / blinkPeriod) % 2 == 0) {
                // Set custom color?
                col = col * 0.2f;
            }

            blinkTime -= Mathf.Min(Time.deltaTime, blinkTime);
        }

        if (shakeTime > 0) {
            const float shakeSpeed = 20;
            Vector2 randomVector = new Vector2(Mathf.PerlinNoise(Time.time * shakeSpeed, 0) - 0.5f, Mathf.PerlinNoise(0, Time.time * shakeSpeed) - 0.5f);
            spriteRenderer.transform.localPosition = randomVector * (shakeTime / shakeDuration) * shakeIntensity;

            shakeTime -= Mathf.Min(Time.deltaTime, shakeTime);
        }
        else {
            spriteRenderer.transform.localPosition = Vector3.zero;
        }

        if (fadeTime > 0) {
            col.a *= fadeTime / fadeDuration;

            fadeTime -= Mathf.Min(Time.deltaTime, fadeTime);
            if (fadeTime <= 0) faded = true;
        }

        if (faded) col.a = 0;

        if (scaleTime > 0) {
            spriteRenderer.transform.localScale = Vector3.one * Mathf.Lerp(targetScale, 1, scaleTime / scaleDuration);

            scaleTime -= Mathf.Min(Time.deltaTime, scaleTime);
        }

        spriteRenderer.color = col;
    }

    public static Shape Create() {
        Shape shape = Instantiate(PrefabRegistry.Instance.shape).GetComponent<Shape>();
        return shape;
    }
}
