using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class IngameUi : MonoBehaviour
{
    public static IngameUi Instance {
        get {
            if (!_instance) {
                _instance = FindObjectOfType<IngameUi>();
            }
            return _instance;
        }
    }

    static IngameUi _instance;

    public Transform anchor;
    public Text title, artist;

    Canvas canvas;

    void Awake() {
        canvas = GetComponent<Canvas>();
    }

    // @Copypasta: From Title.cs.

    public Vector2 GetActualPos(Vector2 pos) {
        pos += new Vector2(0.5f, 0.5f);
        pos *= canvas.GetComponent<RectTransform>().sizeDelta;
        pos *= canvas.GetComponent<RectTransform>().localScale;
        return pos;
    }

    public void AnimateAnchor(Transform transform, Vector2 from, Vector2 to) {
        from = GetActualPos(from);
        to = GetActualPos(to);

        transform.position = from;
        transform.DOMove(to, 0.3f).SetEase(Ease.OutCubic);
    }

    public void DoAnimation() {
        Vector2 a = GetActualPos(Vector2.right);
        Vector2 b = GetActualPos(new Vector2(0.02f, 0f));
        Vector2 c = GetActualPos(new Vector2(-0.02f, 0f));
        Vector2 d = GetActualPos(Vector2.left);

        anchor.position = a;

        anchor.DOMove(b, 0.5f).SetEase(Ease.OutCubic).OnComplete(() => {
            anchor.DOMove(c, 2f).SetEase(Ease.Linear).OnComplete(() => {
                anchor.DOMove(d, 0.5f).SetEase(Ease.InCubic);
            });
        });
    }

    public void SetText(string title, string artist) {
        this.title.text = title;
        this.artist.text = artist;
    }
}
