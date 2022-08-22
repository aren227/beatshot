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
    public List<Enemy> enemies = new List<Enemy>();
    public List<Projectile> projectiles = new List<Projectile>();
    public List<Shape> shapes = new List<Shape>();
    public List<Particle> particles = new List<Particle>();

    int nextPlayerId = 1;
    int nextEntityId = 1;

    ShapeRecorder shapeRecorder;
    const float shapeRecordInterval = 0.05f;

    const int maxClonedPlayers = 1;
    List<PlayerRecorder> playerRecorders = new List<PlayerRecorder>();
    PlayerRecorder currentPlayerRecorder;

    public float time = 0;

    public GameState state { get; private set; } = GameState.READY;

    float bpm = 100f;
    float totalBeats = 32;

    float bps => bpm / 60;
    float spb => 1f / bps;
    float beamTime => time * bps;

    Coroutine bossPatternCoroutine;

    List<Coroutine> patternCoroutines = new List<Coroutine>();

    Targeting targeting = new Targeting();

    public bool invincibleFlag { get; private set; } = false;

    public GlobalData globalData;

    public SpriteRenderer playerBelowLayer;
    float playerBelowLayerOpacity;

    void Start() {
        playerBelowLayer.enabled = true;
        playerBelowLayer.color = Color.black;
        playerBelowLayerOpacity = 1;

        StartCoroutine(InitCoroutine());
    }

    IEnumerator InitCoroutine() {
        IngameUi.Instance.SetText(globalData.current.title, globalData.current.artist);
        IngameUi.Instance.DoAnimation();

        // @Hardcoded: Can be different from the actual duration!
        yield return new WaitForSeconds(3f);

        BeginGame();
    }

    public void BeginGame() {
        // Enemy enemy = AddEnemy();
        // enemy.transform.position = Vector3.zero;

        const float prepareDuration = 2f;

        // Restore timescale.
        Time.timeScale = 0;
        DOTween.To(()=> Time.timeScale, x=> Time.timeScale = x, 1f, prepareDuration).SetEase(Ease.InQuad).SetUpdate(true);

        // Clear black layer.
        playerBelowLayerOpacity = 1;
        DOTween.To(() => playerBelowLayerOpacity, x => playerBelowLayerOpacity = x, 0, prepareDuration);

        currentPlayer = AddPlayer();

        // @Todo: Find valid positions.
        currentPlayer.transform.position = Random.insideUnitCircle.normalized * 3;

        shapeRecorder = new ShapeRecorder();

        state = GameState.PLAYING;

        invincibleFlag = false;

        currentPlayerRecorder = new PlayerRecorder();
        currentPlayerRecorder.playerId = currentPlayer.entity.id;

        Vector3 scale = currentPlayer.transform.localScale;
        currentPlayer.transform.localScale = Vector3.zero;
        currentPlayer.transform.DOScale(scale, 1f).SetEase(Ease.OutBounce).SetUpdate(true);

        foreach (PlayerRecorder playerRecorder in playerRecorders) {
            playerRecorder.Reset();

            Player clonedPlayer = AddPlayer();
            clonedPlayer.entity.id = playerRecorder.playerId;
            clonedPlayer.playback = playerRecorder;

            clonedPlayer.transform.position = playerRecorder.snapshots[0].position;

            clonedPlayer.shape.SetColor(Color.gray);

            scale = clonedPlayer.transform.localScale;
            clonedPlayer.transform.localScale = Vector3.zero;
            clonedPlayer.transform.DOScale(scale, 1f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        foreach (Player player in players) {
            player.MakeInvincible(4);
        }

        targeting.Reset();

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

        enemies.Add(enemy);

        // enemy.entity.id = nextEntityId++;

        // @Hack: Enemies ignore their bullets.
        enemy.entity.id = 0;

        return enemy;
    }

    public Projectile AddProjectile() {
        Projectile projectile = Instantiate(PrefabRegistry.Instance.projectile).GetComponent<Projectile>();

        projectiles.Add(projectile);

        return projectile;
    }

    void Update() {
        if (state == GameState.PLAYING) {
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

            foreach (Particle particle in particles) {
                if (!particle) continue;
                particle.DoNextFrame(Time.deltaTime);
            }

            // Remove invalid pointers.
            players.RemoveAll(x => !x);
            projectiles.RemoveAll(x => !x);
            shapes.RemoveAll(x => !x);
            particles.RemoveAll(x => !x);

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

            // @Todo: Usage of invincibleFlag is pretty strange.
            if (beamTime > totalBeats && !invincibleFlag) {
                Debug.Log("Music ends.");

                Manager.Instance.RewindGame();
            }
        }

        playerBelowLayer.color = new Color(0, 0, 0, playerBelowLayerOpacity);
    }

    void PlayPattern(Pattern pattern) {
        patternCoroutines.Add(StartCoroutine(pattern.Play()));
    }

    public IEnumerator DoBossPattern() {
        boss = AddEnemy();
        boss.transform.position = Vector3.zero;

        boss.shape.SetScale(new Vector2(3, 3));

        // @Todo: All pattern coroutines must be force stopped when the game is restarting.
        // @Todo: If target is invalid, then target to current player, not oldest.
        // This implies pattern states should be shared.

        // #0
        // IDLE
        // 8 beats
        {
            Debug.Log("#0");

            yield return new WaitForSeconds(8f * spb);
        }

        // #6
        // 추적
        // 16 beats
        {
            Debug.Log("#6");

            PlayPattern(new FollowPattern(boss.entity, 16f * spb, 360 * 2, targeting.GetTarget()));

            yield return new WaitForSeconds(16f * spb);
        }

        yield return new WaitForSeconds(4 * spb);

        // #1
        // 왔다갔다 하면서 타겟팅
        // 16 beats
        {
            Debug.Log("#1");

            PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(-5, 0)));
            PlayPattern(new ShootPattern(boss.entity, 2 * spb, targeting.GetTarget()));

            yield return new WaitForSeconds(4 * spb);

            PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)));
            PlayPattern(new ShootPattern(boss.entity, 2 * spb, targeting.GetTarget()));

            yield return new WaitForSeconds(4 * spb);

            PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(5, 0)));
            PlayPattern(new ShootPattern(boss.entity, 8 * spb, targeting.GetTarget()));

            yield return new WaitForSeconds(4 * spb);

            PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)));

            yield return new WaitForSeconds(4 * spb);
        }

        yield return new WaitForSeconds(4 * spb);

        PlayPattern(new BulletCirclePattern(boss.entity, 15));

        // #2
        // 왼쪽에서 원, 오른쪽에서 원, 중앙에서 원x3
        // 16 beats
        {
            Debug.Log("#2");

            PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(-5, 0)));
            yield return new WaitForSeconds(2 * spb);

            PlayPattern(new BulletCirclePattern(boss.entity, 15));
            yield return new WaitForSeconds(2 * spb);

            PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(5, 0)));
            yield return new WaitForSeconds(2 * spb);

            PlayPattern(new BulletCirclePattern(boss.entity, -15));
            yield return new WaitForSeconds(2 * spb);

            PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)));
            yield return new WaitForSeconds(2 * spb);

            PlayPattern(new BulletCirclePattern(boss.entity, 0));
            yield return new WaitForSeconds(2 * spb);

            PlayPattern(new BulletCirclePattern(boss.entity, 15));
            yield return new WaitForSeconds(2 * spb);

            PlayPattern(new BulletCirclePattern(boss.entity, 30));
            yield return new WaitForSeconds(2 * spb);
        }

        // #7
        // 최대 두명 타겟팅해서 따라다니면서 발사
        // 16 beats
        {
            Debug.Log("#7");

            Player playerA = null, playerB = null;

            targeting.GetTwoTargets(ref playerA, ref playerB);

            Player[] players = new Player[] { playerA, playerB };

            foreach (Player player in players) {
                if (!player) continue;

                Enemy enemy = AddEnemy();
                enemy.shape.SetScale(new Vector2(1, 1));
                enemy.health.health = 40;

                PlayPattern(new FollowPattern(enemy.entity, 16f * spb, 360 * 2, player));

                PlayPattern(new ShootPattern(enemy.entity, 16f * spb, player, 1f * spb));
            }

            yield return new WaitForSeconds(16f * spb);
        }

        // #5
        // 상하좌우 영역
        // 16 beats
        {
            Debug.Log("#5");

            // Upper

            PlayPattern(new ShootPattern(boss.entity, 4f * spb, targeting.GetTarget(), 2f * spb));

            PlayPattern(new MoveToPattern(boss.entity, 4f * spb, new Vector2(0, -2.5f)));

            PlayPattern(new AreaPattern(4f * spb, 4f * spb, new Vector2(0, 5), new Vector2(20, 10), new Vector2(1, 0)));

            yield return new WaitForSeconds(4f * spb);

            PlayPattern(new ShootPattern(boss.entity, 4f * spb, targeting.GetTarget(), 2f * spb));

            yield return new WaitForSeconds(4f * spb);

            // Lower

            PlayPattern(new ShootPattern(boss.entity, 4f * spb, targeting.GetTarget(), 2f * spb));

            PlayPattern(new MoveToPattern(boss.entity, 4f * spb, new Vector2(0, 2.5f)));

            PlayPattern(new AreaPattern(4f * spb, 4f * spb, new Vector2(0, -5), new Vector2(20, 10), new Vector2(1, 0)));

            yield return new WaitForSeconds(4f * spb);

            PlayPattern(new ShootPattern(boss.entity, 4f * spb, targeting.GetTarget(), 2f * spb));

            yield return new WaitForSeconds(4f * spb);
        }

        // #7
        // 최대 두명 타겟팅해서 따라다니면서 발사
        // 16 beats
        {
            Debug.Log("#7");

            Player playerA = null, playerB = null;

            targeting.GetTwoTargets(ref playerA, ref playerB);

            Player[] players = new Player[] { playerA, playerB };

            foreach (Player player in players) {
                if (!player) continue;

                Enemy enemy = AddEnemy();
                enemy.shape.SetScale(new Vector2(1, 1));
                enemy.health.health = 40;

                PlayPattern(new FollowPattern(enemy.entity, 16f * spb, 360 * 2, player));

                PlayPattern(new ShootPattern(enemy.entity, 16f * spb, player, 1f * spb));
            }

            yield return new WaitForSeconds(16f * spb);
        }



        // // #4
        // // 타겟팅 점프 - 원
        // // 32 beats
        // {
        //     Debug.Log("#4");

        //     for (int i = 0; i < 4; i++) {
        //         PlayPattern(new DashPattern(boss.entity, 2f * spb, 0.5f * spb));
        //         yield return new WaitForSeconds(2 * spb);

        //         yield return new WaitForSeconds(2 * spb);

        //         PlayPattern(new BulletCirclePattern(boss.entity, 15));

        //         yield return new WaitForSeconds(2 * spb);
        //     }
        // }

        // yield return new WaitForSeconds(4 * spb);

        // // #1
        // // 왔다갔다 하면서 타겟팅
        // // 16 beats
        // {
        //     Debug.Log("#1");

        //     PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(-5, 0)));
        //     PlayPattern(new ShootPattern(boss.entity, 2 * spb));

        //     yield return new WaitForSeconds(4 * spb);

        //     PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)));
        //     PlayPattern(new ShootPattern(boss.entity, 2 * spb));

        //     yield return new WaitForSeconds(4 * spb);

        //     PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(5, 0)));
        //     PlayPattern(new ShootPattern(boss.entity, 8 * spb));

        //     yield return new WaitForSeconds(4 * spb);

        //     PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)));

        //     yield return new WaitForSeconds(4 * spb);
        // }

        // yield return new WaitForSeconds(4 * spb);

        // // #2
        // // 왼쪽에서 원, 오른쪽에서 원, 중앙에서 원x3
        // // 16 beats
        // {
        //     Debug.Log("#2");

        //     PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(-5, 0)));
        //     yield return new WaitForSeconds(2 * spb);

        //     PlayPattern(new BulletCirclePattern(boss.entity, 15));
        //     yield return new WaitForSeconds(2 * spb);

        //     PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(5, 0)));
        //     yield return new WaitForSeconds(2 * spb);

        //     PlayPattern(new BulletCirclePattern(boss.entity, -15));
        //     yield return new WaitForSeconds(2 * spb);

        //     PlayPattern(new MoveToPattern(boss.entity, 2 * spb, new Vector2(0, 0)));
        //     yield return new WaitForSeconds(2 * spb);

        //     PlayPattern(new BulletCirclePattern(boss.entity, 0));
        //     yield return new WaitForSeconds(2 * spb);

        //     PlayPattern(new BulletCirclePattern(boss.entity, 15));
        //     yield return new WaitForSeconds(2 * spb);

        //     PlayPattern(new BulletCirclePattern(boss.entity, 30));
        //     yield return new WaitForSeconds(2 * spb);
        // }

        // yield return new WaitForSeconds(4 * spb);

        // // #3
        // // 레이저 시계방향, 반시계방향
        // // 32 beats
        // {
        //     Debug.Log("#3");

        //     PlayPattern(new LaserPattern(boss.entity, 4 * spb, (32-4) * spb));
        //     PlayPattern(new RotatePattern(boss.entity, 16 * spb, 0, 360));

        //     yield return new WaitForSeconds(16 * spb);

        //     PlayPattern(new RotatePattern(boss.entity, 16 * spb, 360, 0));

        //     yield return new WaitForSeconds(16 * spb);
        // }

        // yield return new WaitForSeconds(4 * spb);

        // yield return new WaitForSeconds(2);
        // {
        //     // ShootPattern pattern = new ShootPattern(boss.entity, 20f);

        //     // PlayPattern(pattern.Play());
        // }
        // for (int i = 0; i < 10; i++) {
        //     Vector2 target;
        //     // @Hardcoded
        //     if (i % 2 == 0) target = new Vector2(-5, 0);
        //     else target = new Vector2(5, 0);

        //     MoveToPattern pattern = new MoveToPattern(boss.entity, 3, target);

        //     PlayPattern(pattern);

        //     yield return new WaitForSeconds(3);
        // }
    }

    void StopBossPattern() {
        StopCoroutine(bossPatternCoroutine);
        foreach (Coroutine coroutine in patternCoroutines) {
            StopCoroutine(coroutine);
        }
        patternCoroutines.Clear();
    }

    public void RewindGame() {
        // StartCoroutine(RewindGameCoroutine());
        invincibleFlag = true;

        DOTween.To(()=> Time.timeScale, x=> Time.timeScale = x, 0, 1f).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() => {

            foreach (Player player in players) if (player) DestroyImmediate(player.gameObject);
            foreach (Enemy enemy in enemies) if (enemy) DestroyImmediate(enemy.gameObject);
            foreach (Projectile projectile in projectiles) if (projectile) DestroyImmediate(projectile.gameObject);
            foreach (Shape shape in shapes) if (shape) DestroyImmediate(shape.gameObject);

            // @Inefficient
            foreach (Particle particle in FindObjectsOfType<Particle>()) {
                DestroyImmediate(particle.gameObject);
            }

            players.Clear();
            enemies.Clear();
            projectiles.Clear();
            shapes.Clear();

            float rewindDuration = time * 0.2f;

            state = GameState.REWINDING;

            Time.timeScale = 1;

            StopBossPattern();

            // Flush player recorder.
            if (playerRecorders.Count >= maxClonedPlayers) {
                playerRecorders.RemoveAt(0);
            }
            playerRecorders.Add(currentPlayerRecorder);
            currentPlayerRecorder = null;

            Music.Instance.audioSource.Stop();

            DOTween.To(() => playerBelowLayerOpacity, x => playerBelowLayerOpacity = x, 1, rewindDuration).SetEase(Ease.InCubic);

            DOTween.To(() => time, x => {
                time = x;

                foreach (Shape shape in shapes) {
                    DestroyImmediate(shape.gameObject);
                }
                shapes.Clear();

                shapeRecorder.Show(time);
            }, 0, rewindDuration).SetEase(Ease.InOutQuad).OnComplete(() => {

                time = 0;

                Time.timeScale = 0;

                foreach (Shape shape in shapes) {
                    DestroyImmediate(shape.gameObject);
                }
                shapes.Clear();

                BeginGame();
            });
        });
    }

    public void WinGame() {
        invincibleFlag = true;

        DOTween.To(()=> Time.timeScale, x=> Time.timeScale = x, 0, 1f).SetEase(Ease.OutQuad).SetUpdate(true);

        StartCoroutine(WinGameCoroutine());
    }

    IEnumerator WinGameCoroutine() {
        float begin = Time.realtimeSinceStartup;

        const float duration = 4;

        boss.shape.ignoreUpdate = true;

        const float shakeIntensity = 1f;

        Color tintColor = Color.white;
        Color originalColor = boss.shape.props.color;

        while (Time.realtimeSinceStartup - begin < duration) {
            float t = Time.realtimeSinceStartup;
            float intensity = Mathf.Clamp01((t - begin) / duration);

            // @Copypasta: From Shape.cs.
            const float minShakeSpeed = 1;
            const float maxShakeSpeed = 10;

            float shakeSpeed = Mathf.Lerp(minShakeSpeed, maxShakeSpeed, Mathf.Pow(intensity, 2));

            Vector2 randomVector = new Vector2(Mathf.PerlinNoise(t * shakeSpeed, 0) - 0.5f, Mathf.PerlinNoise(0, t * shakeSpeed) - 0.5f);

            boss.shape.spriteRenderer.transform.localPosition = randomVector * intensity * shakeIntensity;

            // @Hardcoded: Magic numbers.
            float sin = Mathf.Sin(20 * Mathf.PI / Mathf.Pow(1 - intensity, 0.5f));

            boss.shape.spriteRenderer.color = sin > 0 ? tintColor : originalColor;

            yield return null;
        }

        state = GameState.FINISH;

        // Time.timeScale = 1;

        StopBossPattern();

        Music.Instance.audioSource.Stop();

        const float particleTime = 2f;

        Particle particle;

        // Particle
        {
            particle = Particle.Create();

            particle.transform.position = boss.transform.position;

            particle.amount = 64;
            particle.color = originalColor;
            particle.duration = particleTime;
            particle.scale = 0.5f;
            particle.speed = 7f;
        }

        // Wait for particle to initialize.
        yield return null;

        // Delete boss gameobject here.
        DestroyImmediate(boss.gameObject);

        begin = Time.realtimeSinceStartup;

        float lastTime = begin;

        while (Time.realtimeSinceStartup - begin < particleTime) {
            float rt = Time.realtimeSinceStartup;
            float dt = rt - lastTime;

            particle.DoNextFrame(dt);

            foreach (Shape shape in particle.shapes) {
                shape.DoNextFrame(dt);
            }

            lastTime = rt;

            yield return null;
        }

        // @Todo: Implement scene transition logic here.
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

public enum GameState {
    READY,
    PLAYING,
    REWINDING,
    FINISH,
}