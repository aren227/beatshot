using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    public Transform titleAnchor;
    public Transform pauseAnchor;

    public Text title, artist;

    public Button resumeButton;
    public Button backToTitleButton;

    Canvas canvas;

    void Awake() {
        canvas = GetComponent<Canvas>();

        titleAnchor.gameObject.SetActive(true);
        pauseAnchor.gameObject.SetActive(true);

        pauseAnchor.position = GetActualPos(Vector2.up);

        resumeButton.onClick.AddListener(() => {
            if (Manager.Instance.state == GameState.PLAYING) Manager.Instance.SetPause(false);
        });
        backToTitleButton.onClick.AddListener(() => {
            Time.timeScale = 1;
            SceneManager.LoadScene("Title");
        });
    }

    // @Copypasta: From Title.cs.

    public Vector2 GetActualPos(Vector2 pos) {
        pos += new Vector2(0.5f, 0.5f);
        pos *= canvas.GetComponent<RectTransform>().sizeDelta;
        pos *= canvas.GetComponent<RectTransform>().localScale;
        return pos;
    }

    public void DoAnimation() {
        Vector2 a = GetActualPos(Vector2.right);
        Vector2 b = GetActualPos(new Vector2(0.02f, 0f));
        Vector2 c = GetActualPos(new Vector2(-0.02f, 0f));
        Vector2 d = GetActualPos(Vector2.left);

        titleAnchor.position = a;

        titleAnchor.DOMove(b, 0.5f).SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => {
            titleAnchor.DOMove(c, 2f).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => {
                titleAnchor.DOMove(d, 0.5f).SetEase(Ease.InCubic).SetUpdate(true);
            });
        });
    }

    public void SetText(string title, string artist) {
        this.title.text = title;
        this.artist.text = $"by {artist}";
    }

    public void ShowPauseScreen() {
        DOTween.Kill("Pause");
        DOTween.Kill("Upmost");

        pauseAnchor.DOMove(GetActualPos(Vector2.zero), 0.2f).SetEase(Ease.OutCubic).SetId("Pause").SetUpdate(true);
        DOTween.To(() => Manager.Instance.upmostLayerOpacity, x => Manager.Instance.upmostLayerOpacity = x, 0.7f, 0.2f).SetEase(Ease.OutCubic).SetId("Upmost").SetUpdate(true);
    }

    public void HidePauseScreen() {
        DOTween.Kill("Pause");
        DOTween.Kill("Upmost");

        pauseAnchor.DOMove(GetActualPos(Vector2.up), 0.2f).SetEase(Ease.OutCubic).SetId("Pause").SetUpdate(true);
        DOTween.To(() => Manager.Instance.upmostLayerOpacity, x => Manager.Instance.upmostLayerOpacity = x, 0f, 0.2f).SetEase(Ease.OutCubic).SetId("Upmost").SetUpdate(true);
    }
}
