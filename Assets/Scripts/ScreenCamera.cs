using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenCamera : MonoBehaviour
{
    public static ScreenCamera Instance {
        get {
            if (!_instance) {
                _instance = FindObjectOfType<ScreenCamera>();
            }
            return _instance;
        }
    }

    static ScreenCamera _instance;

    const float downscale = 3;

    public Camera gameCamera;

    Camera cam;

    public RenderTexture renderTexture { get; private set; }

    public Material blitMaterial;

    // @Todo: Update if screen resolution is changed.
    public static RenderTexture CreateRenderTexture(Camera camera, float downscale, RenderTextureFormat format = RenderTextureFormat.ARGBFloat)
    {
        RenderTexture renderTexture = new RenderTexture(Mathf.RoundToInt(camera.pixelWidth / downscale), Mathf.RoundToInt(camera.pixelHeight / downscale), 16, format);
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();

        return renderTexture;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();

        renderTexture = CreateRenderTexture(cam, downscale);

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

        quad.transform.parent = transform;
        quad.transform.localPosition = new Vector3(0, 0, 1);
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(cam.orthographicSize * cam.aspect * 2, cam.orthographicSize * 2, 1);

        Material mat = Instantiate(blitMaterial);
        mat.SetTexture("_MainTex", renderTexture);

        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;

        gameCamera.targetTexture = renderTexture;
        // gameCamera.depthTextureMode = DepthTextureMode.DepthNormals;
    }
}
