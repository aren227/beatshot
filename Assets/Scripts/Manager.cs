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

    public Enemy boss;

    public List<Player> players = new List<Player>();
    public List<Projectile> projectiles = new List<Projectile>();
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

    float bpm = 127.95f;
    float bps => bpm / 60;
    float spb => 1f / bps;
    float beamTime => time * bps;

    Coroutine bossPatternCoroutine;

    void Start() {
        BeginGame();
    }

    public void BeginGame() {
        // Enemy enemy = AddEnemy();
        // enemy.transform.position = Vector3.zero;

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

        bossPatternCoroutine = StartCoroutine(DoBossPattern());

        Music.Instance.audioSource.Play();
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

            // Boss beats
            {
                int prevBeat = Mathf.FloorToInt(time * bps);
                int currBeat = Mathf.FloorToInt((time + Time.deltaTime) * bps);
                if (prevBeat < currBeat) {
                    boss.shape.props.targetScale = 1.05f;
                    boss.shape.Scale(1, spb * 0.5f);
                }
            }

            time += Time.deltaTime;

            if (time - shapeRecorder.lastRecordTime > shapeRecordInterval) {
                shapeRecorder.TakeSnapshot();
            }

            if (currentPlayer && currentPlayerRecorder != null) {
                currentPlayerRecorder.TakeSnapshot(currentPlayer);
            }
        }
    }

    public IEnumerator DoBossPattern() {
        boss = AddEnemy();
        boss.transform.position = Vector3.zero;

        yield return new WaitForSeconds(4 * spb);

        // @Todo: All pattern coroutines must be force stopped when the game is restarting.
        // @Todo: If target is invalid, then target to current player, not oldest.
        // This implies pattern states should be shared.

        // #5
        // 상하좌우 영역
        // 8 beats
        {
            Debug.Log("#5");

            StartCoroutine(new ShootPattern(boss.entity, 16f * spb, 2f * spb).Play());

            StartCoroutine(new MoveToPattern(boss.entity, 4f * spb, new Vector2(0, -2.5f)).Play());

            StartCoroutine(new AreaPattern(4f * spb, 4f * spb, new Vector2(0, 5), new Vector2(20, 10), new Vector2(1, 0)).Play());
            yield return new WaitForSeconds(8f * spb);

            StartCoroutine(new MoveToPattern(boss.entity, 4f * spb, new Vector2(0, 2.5f)).Play());

            StartCoroutine(new AreaPattern(4f * spb, 4f * spb, new Vector2(0, -5), new Vector2(20, 10), new Vector2(1, 0)).Play());
            yield return new WaitForSeconds(8f * spb);
        }

        // #4
        // 타겟팅 점프 - 원
        // 32 beats
        {
            Debug.Log("#4");

            for (int i = 0; i < 4; i++) {
                StartCoroutine(new DashPattern(boss.entity, 2f * spb, 0.5f * spb).Play());
                yield return new WaitForSeconds(2 * spb);

                yield return new WaitForSeconds(2 * spb);

                StartCoroutine(new BulletCirclePattern(boss.entity, 15).Play());

                yield return new WaitForSeconds(2 * spb);
            }
        }

        yield return new WaitForSeconds(4 * spb);

        // #1
        // 왔다갔다 하면서 타겟팅
        // 16 beats
        {
            Debug.Log("#1");

            StartCoroutine(new MoveToPattern(boss.entity, 2 * spb, new Vector2(-5, 0)).Play());
            StartCoroutine(new ShootPattern(boss.entity, 2 * spb).Play());

            yield return new WaitForSeconds(4 * spb);

            StartCoroutine(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)).Play());
            StartCoroutine(new ShootPattern(boss.entity, 2 * spb).Play());

            yield return new WaitForSeconds(4 * spb);

            StartCoroutine(new MoveToPattern(boss.entity, 2 * spb, new Vector2(5, 0)).Play());
            StartCoroutine(new ShootPattern(boss.entity, 8 * spb).Play());

            yield return new WaitForSeconds(4 * spb);

            StartCoroutine(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)).Play());

            yield return new WaitForSeconds(4 * spb);
        }

        yield return new WaitForSeconds(4 * spb);

        // #2
        // 왼쪽에서 원, 오른쪽에서 원, 중앙에서 원x3
        // 16 beats
        {
            Debug.Log("#2");

            StartCoroutine(new MoveToPattern(boss.entity, 2 * spb, new Vector2(-5, 0)).Play());
            yield return new WaitForSeconds(2 * spb);

            StartCoroutine(new BulletCirclePattern(boss.entity, 15).Play());
            yield return new WaitForSeconds(2 * spb);

            StartCoroutine(new MoveToPattern(boss.entity, 2 * spb, new Vector2(5, 0)).Play());
            yield return new WaitForSeconds(2 * spb);

            StartCoroutine(new BulletCirclePattern(boss.entity, -15).Play());
            yield return new WaitForSeconds(2 * spb);

            StartCoroutine(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)).Play());
            yield return new WaitForSeconds(2 * spb);

            StartCoroutine(new BulletCirclePattern(boss.entity, 0).Play());
            yield return new WaitForSeconds(2 * spb);

            StartCoroutine(new BulletCirclePattern(boss.entity, 15).Play());
            yield return new WaitForSeconds(2 * spb);

            StartCoroutine(new BulletCirclePattern(boss.entity, 30).Play());
            yield return new WaitForSeconds(2 * spb);
        }

        yield return new WaitForSeconds(4 * spb);

        // #3
        // 레이저 시계방향, 반시계방향
        // 32 beats
        {
            Debug.Log("#3");

            StartCoroutine(new LaserPattern(boss.entity, 4 * spb, (32-4) * spb).Play());
            StartCoroutine(new RotatePattern(boss.entity, 16 * spb, 0, 360).Play());

            yield return new WaitForSeconds(16 * spb);

            StartCoroutine(new RotatePattern(boss.entity, 16 * spb, 360, 0).Play());

            yield return new WaitForSeconds(16 * spb);
        }

        yield return new WaitForSeconds(4 * spb);

        yield return new WaitForSeconds(2);
        {
            // ShootPattern pattern = new ShootPattern(boss.entity, 20f);

            // StartCoroutine(pattern.Play());
        }
        for (int i = 0; i < 10; i++) {
            Vector2 target;
            // @Hardcoded
            if (i % 2 == 0) target = new Vector2(-5, 0);
            else target = new Vector2(5, 0);

            MoveToPattern pattern = new MoveToPattern(boss.entity, 3, target);

            StartCoroutine(pattern.Play());

            yield return new WaitForSeconds(3);
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

            // Stop boss pattern.
            StopCoroutine(bossPatternCoroutine);

            // Flush player recorder.
            if (playerRecorders.Count >= maxClonedPlayers) {
                playerRecorders.RemoveAt(0);
            }
            playerRecorders.Add(currentPlayerRecorder);
            currentPlayerRecorder = null;

            Music.Instance.audioSource.Stop();

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
