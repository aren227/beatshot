using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Title : MonoBehaviour
{
    public Transform mainAnchor;
    public Transform optionsAnchor;
    public Transform creditsAnchor;

    public Button playButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button exitButton;

    Transform currentAnchor;

    public List<Button> returnToMainButtons;

    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    Canvas canvas;
    CanvasScaler canvasScaler;

    bool animating = false;

    public GlobalData globalData;

    void Start() {
        canvas = mainAnchor.GetComponentInParent<Canvas>();
        canvasScaler = mainAnchor.GetComponentInParent<CanvasScaler>();

        mainAnchor.gameObject.SetActive(true);
        optionsAnchor.gameObject.SetActive(true);
        creditsAnchor.gameObject.SetActive(true);

        MoveAnchor(optionsAnchor, Vector2.left);
        MoveAnchor(creditsAnchor, Vector2.left);

        AnimateAnchor(mainAnchor, Vector2.down, Vector2.zero);

        currentAnchor = mainAnchor;

        playButton.onClick.AddListener(() => {
            if (animating) return;

            AnimateAnchor(mainAnchor, Vector2.zero, Vector2.left, () => {
                globalData.current = globalData.levels[0];

                SceneManager.LoadScene("SampleScene");
            });
        });

        optionsButton.onClick.AddListener(() => {
            if (animating) return;

            AnimateAnchor(mainAnchor, Vector2.zero, Vector2.right);
            AnimateAnchor(optionsAnchor, Vector2.left, Vector2.zero);

            currentAnchor = optionsAnchor;
        });

        creditsButton.onClick.AddListener(() => {
            if (animating) return;

            AnimateAnchor(mainAnchor, Vector2.zero, Vector2.right);
            AnimateAnchor(creditsAnchor, Vector2.left, Vector2.zero);

            currentAnchor = creditsAnchor;
        });

        exitButton.onClick.AddListener(() => {
            if (animating) return;
            Application.Quit();
        });

        foreach (Button button in returnToMainButtons) {
            button.onClick.AddListener(() => {
                if (animating) return;

                AnimateAnchor(currentAnchor, Vector2.zero, Vector2.left);
                AnimateAnchor(mainAnchor, Vector2.right, Vector2.zero);

                currentAnchor = mainAnchor;
            });
        }

        musicVolumeSlider.onValueChanged.AddListener(x => {
            globalData.musicVolume = x;
        });
        sfxVolumeSlider.onValueChanged.AddListener(x => {
            globalData.sfxVolume = x;
        });
    }

    Vector2 GetActualPos(Vector2 pos) {
        pos += new Vector2(0.5f, 0.5f);
        pos *= canvas.GetComponent<RectTransform>().sizeDelta;
        pos *= canvas.GetComponent<RectTransform>().localScale;
        return pos;
    }

    void MoveAnchor(Transform transform, Vector2 to) {
        to = GetActualPos(to);

        transform.position = to;
    }

    void AnimateAnchor(Transform transform, Vector2 from, Vector2 to, UnityAction action = null) {
        animating = true;

        from = GetActualPos(from);
        to = GetActualPos(to);

        transform.position = from;
        transform.DOMove(to, 0.3f).SetEase(Ease.OutCubic).OnComplete(() => {
            animating = false;

            if (action != null) action.Invoke();
        });
    }
}
