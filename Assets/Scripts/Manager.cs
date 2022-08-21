using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Manager : MonoBehaviour
{
    public static Manager Instance {
        get {
            if (!_instance) {
                _instance = FindObjectOfType<Manager>();
            }
            return _instance;
        }
    }

    static Manager _instance;

    public Player currentPlayer;

    List<Player> players = new List<Player>();
    List<Projectile> projectiles = new List<Projectile>();
    public List<Shape> shapes = new List<Shape>();

    int nextPlayerId = 1;
    int nextEntityId = 1;

    ShapeRecorder shapeRecorder;
    const float shapeRecordInterval = 0.05f;

    const int maxClonedPlayers = 2;
    List<PlayerRecorder> playerRecorders = new List<PlayerRecorder>();
    PlayerRecorder currentPlayerRecorder;

    public float time = 0;

    public bool isPlaying = false;

    void Start() {
        BeginGame();
    }

    public void BeginGame() {
        Enemy enemy = AddEnemy();
        enemy.transform.position = Vector3.zero;

        currentPlayer = AddPlayer();

        // @Todo: Find valid positions.
        currentPlayer.transform.position = Random.insideUnitCircle.normalized * 3;

        shapeRecorder = new ShapeRecorder();

        isPlaying = true;

        currentPlayerRecorder = new PlayerRecorder();
        currentPlayerRecorder.playerId = currentPlayer.entity.id;

        Vector3 scale = currentPlayer.transform.localScale;
        currentPlayer.transform.localScale = Vector3.zero;
        currentPlayer.transform.DOScale(scale, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);

        foreach (PlayerRecorder playerRecorder in playerRecorders) {
            playerRecorder.Reset();

            Player clonedPlayer = AddPlayer();
            clonedPlayer.entity.id = playerRecorder.playerId;
            clonedPlayer.playback = playerRecorder;

            clonedPlayer.transform.position = playerRecorder.snapshots[0].position;

            clonedPlayer.shape.SetColor(Color.gray);

            scale = clonedPlayer.transform.localScale;
            clonedPlayer.transform.localScale = Vector3.zero;
            clonedPlayer.transform.DOScale(scale, 0.5f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    public Player AddPlayer() {
        Player player = Instantiate(PrefabRegistry.Instance.player).GetComponent<Player>();
        // @Todo: Set spawn point.

        players.Add(player);

        player.entity.id = -nextPlayerId++;

        return player;
    }

    public Enemy AddEnemy() {
        Enemy enemy = Instantiate(PrefabRegistry.Instance.enemy).GetComponent<Enemy>();

        enemy.entity.id = nextEntityId++;

        return enemy;
    }

    public Projectile AddProjectile() {
        Projectile projectile = Instantiate(PrefabRegistry.Instance.projectile).GetComponent<Projectile>();

        projectiles.Add(projectile);

        return projectile;
    }

    void Update() {
        if (isPlaying) {
            foreach (Player player in players) {
                if (!player) continue;
                player.DoNextFrame(Time.deltaTime);
            }

            foreach (Projectile projectile in projectiles) {
                if (!projectile) continue;
                projectile.DoNextFrame(Time.deltaTime);
            }

            foreach (Shape shape in shapes) {
                if (!shape) continue;
                shape.DoNextFrame(Time.deltaTime);
            }

            // Remove invalid pointers.
            players.RemoveAll(x => !x);
            projectiles.RemoveAll(x => !x);
            shapes.RemoveAll(x => !x);

            time += Time.deltaTime;

            if (time - shapeRecorder.lastRecordTime > shapeRecordInterval) {
                shapeRecorder.TakeSnapshot();
            }

            if (currentPlayer && currentPlayerRecorder != null) {
                currentPlayerRecorder.TakeSnapshot(currentPlayer);
            }
        }
    }

    public void RewindGame() {
        // StartCoroutine(RewindGameCoroutine());

        DOTween.To(()=> Time.timeScale, x=> Time.timeScale = x, 0, 1f).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() => {

            foreach (Player player in players) if (player) DestroyImmediate(player.gameObject);
            foreach (Projectile projectile in projectiles) if (projectile) DestroyImmediate(projectile.gameObject);
            foreach (Shape shape in shapes) if (shape) DestroyImmediate(shape.gameObject);

            // @Inefficient
            foreach (Particle particle in FindObjectsOfType<Particle>()) {
                DestroyImmediate(particle.gameObject);
            }

            players.Clear();
            projectiles.Clear();
            shapes.Clear();

            float rewindDuration = time * 0.2f;

            isPlaying = false;

            Time.timeScale = 1;

            // Flush player recorder.
            if (playerRecorders.Count >= maxClonedPlayers) {
                playerRecorders.RemoveAt(0);
            }
            playerRecorders.Add(currentPlayerRecorder);
            currentPlayerRecorder = null;

            DOTween.To(() => time, x => {
                time = x;

                foreach (Shape shape in shapes) {
                    DestroyImmediate(shape.gameObject);
                }
                shapes.Clear();

                shapeRecorder.Show(time);
            }, 0, rewindDuration).SetEase(Ease.InOutQuad).OnComplete(() => {

                Time.timeScale = 0;

                foreach (Shape shape in shapes) {
                    DestroyImmediate(shape.gameObject);
                }
                shapes.Clear();

                BeginGame();

                DOTween.To(()=> Time.timeScale, x=> Time.timeScale = x, 1f, 1f).SetEase(Ease.InQuad).SetUpdate(true);
            });
        });
    }

    // IEnumerator RewindGameCoroutine() {
    //     yield return null;

    //     DOTween.To(()=> Time.timeScale, x=> Time.timeScale = x, 0, 1f).SetEase(Ease.OutCubic).SetUpdate(true);
    //     yield return new WaitForSecondsRealtime(1);

    //     foreach (Player player in players) Destroy(player.gameObject);
    //     foreach (Projectile projectile in projectiles) Destroy(projectile.gameObject);
    //     foreach (Shape shape in shapes) Destroy(shape.gameObject);

    //     // @Inefficient
    //     foreach (Particle particle in FindObjectsOfType<Particle>()) {
    //         Destroy(particle.gameObject);
    //     }

    //     players.Clear();
    //     projectiles.Clear();
    //     shapes.Clear();

    //     float rewindDuration = time * 1f;

    //     isPlaying = false;

    //     Time.timeScale = 1;

    //     DOTween.To(() => time, x => {
    //         time = x;

    //         foreach (Shape shape in shapes) {
    //             DestroyImmediate(shape.gameObject);
    //         }
    //         shapes.Clear();

    //         shapeRecorder.Show(time);
    //     }, 0, rewindDuration).SetEase(Ease.InOutCubic);
    //     yield return new WaitForSecondsRealtime(rewindDuration);

    //     Time.timeScale = 0;

    //     DOTween.To(()=> Time.timeScale, x=> Time.timeScale = x, 1f, 0.5f).SetEase(Ease.InCubic).SetUpdate(true);
    //     yield return new WaitForSecondsRealtime(0.5f);

    //     BeginGame();
    // }
}
