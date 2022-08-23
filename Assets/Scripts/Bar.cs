using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bar : MonoBehaviour
{
    public float value = 0.5f;

    public Transform foreground;

    public bool upper;

    void Start() {
        float pixels = 2;

        // @Hack: Idk why.
        if (!upper) pixels += 1;

        float worldPixel = pixels / Camera.main.pixelHeight * (Camera.main.orthographicSize * 2);

        transform.localScale = new Vector3(
            Manager.Instance.worldMax.x - Manager.Instance.worldMin.x,
            worldPixel,
            1
        );

        if (upper) transform.position = new Vector2(0, Manager.Instance.worldMax.y - worldPixel * 0.5f);
        else transform.position = new Vector2(0, Manager.Instance.worldMin.y + worldPixel * 0.5f);
    }

    void Update() {
        foreground.localScale = new Vector3(value, 1, 1);
        foreground.localPosition = new Vector3(-0.5f + 0.5f * value, 0, 0);
    }
}
