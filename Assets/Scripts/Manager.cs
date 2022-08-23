using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

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
    public HashSet<Shape> shapes = new HashSet<Shape>();
    public List<Particle> particles = new List<Particle>();

    int nextPlayerId = 1;
    int nextEntityId = 1;

    ShapeRecorder shapeRecorder;
    const float shapeRecordInterval = 0.1f;

    const int maxClonedPlayers = 1;
    List<PlayerRecorder> playerRecorders = new List<PlayerRecorder>();
    PlayerRecorder currentPlayerRecorder;

    public float time = 0;
    public float deltaTime = 0;

    public GameState state { get; private set; } = GameState.READY;

    float bpm = 100f;
    float totalBeats = 48 + 32 + 32;

    public float bps => bpm / 60;
    public float spb => 1f / bps;
    public float beamTime => time * bps;

    Coroutine bossPatternCoroutine;

    List<Coroutine> patternCoroutines = new List<Coroutine>();

    Targeting targeting = new Targeting();

    public bool invincibleFlag { get; private set; } = false;

    public GlobalData globalData;

    public SpriteRenderer playerBelowLayer;
    public float playerBelowLayerOpacity;

    public SpriteRenderer upmostLayer;
    public float upmostLayerOpacity;

    public bool paused = false;

    public Vector2 worldMin, worldMax;

    public Bar bossBar;
    public Bar timeBar;

    void Awake() {
        worldMin = new Vector2(-5 * (16f / 9f), -5);
        worldMax = new Vector2(5 * (16f / 9f), 5);
    }

    void Start() {
        playerBelowLayer.enabled = true;
        playerBelowLayer.color = Color.black;
        playerBelowLayerOpacity = 1;

        upmostLayer.enabled = true;
        upmostLayer.color = Color.black;
        upmostLayerOpacity = 0;

        // Set music volume
        Music.Instance.audioSource.volume = globalData.musicVolume;

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

        Music.Instance.audioSource.loop = true;
        Music.Instance.audioSource.time = 0;
        if (!Music.Instance.audioSource.isPlaying) Music.Instance.audioSource.Play();
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

    public void MakeClonedPlayersIgnoreProjectile(Projectile projectile) {
        foreach (Player player in players) {
            if (player == currentPlayer) continue;
            projectile.IgnoreEntity(player.entity);
        }
    }

    void Update() {
        deltaTime = 0;

        if (state == GameState.PLAYING) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                SetPause(!paused);
            }

            if (!paused) {
                float dt = Time.deltaTime;
                deltaTime = dt;

                Music.Instance.audioSource.pitch = Time.timeScale;

                foreach (Player player in players) {
                    if (!player) continue;
                    player.DoNextFrame(dt);
                }

                foreach (Projectile projectile in projectiles) {
                    if (!projectile) continue;
                    projectile.DoNextFrame(dt);
                }

                Background.Instance.DoNextFrame(dt);

                foreach (Shape shape in shapes) {
                    if (!shape || !shape.gameObject.activeInHierarchy) continue;
                    shape.DoNextFrame(dt);
                }

                foreach (Particle particle in particles) {
                    if (!particle) continue;
                    particle.DoNextFrame(dt);
                }

                // Remove invalid pointers.
                players.RemoveAll(x => !x);
                projectiles.RemoveAll(x => !x);
                shapes.RemoveWhere(x => !x);
                particles.RemoveAll(x => !x);

                // Boss beats
                {
                    int prevBeat = Mathf.FloorToInt(time * bps);
                    int currBeat = Mathf.FloorToInt((time + dt) * bps);
                    if (prevBeat < currBeat) {
                        boss.shape.props.targetScale = 1.05f;
                        boss.shape.Scale(1, spb * 0.5f);
                    }
                }

                // Debug.Log(time + " vs " + Music.Instance.audioSource.time);

                time += dt;

                if (time - shapeRecorder.lastRecordTime > shapeRecordInterval) {
                    shapeRecorder.TakeSnapshot();
                }

                if (currentPlayer && currentPlayerRecorder != null) {
                    currentPlayerRecorder.TakeSnapshot(currentPlayer);
                }

                // @Todo: Usage of invincibleFlag is pretty strange.
                if (beamTime > totalBeats && !invincibleFlag) {
                    Debug.Log("Music ends.");

                    Manager.Instance.RewindGame(musicEnded: true);
                }
            }
            else {
                Music.Instance.audioSource.pitch = 0;
            }
        }

        // @Hardcoded: Background color
        playerBelowLayer.color = new Color(0.05f, 0.05f, 0.05f, playerBelowLayerOpacity);
        upmostLayer.color = new Color(0.05f, 0.05f, 0.05f, upmostLayerOpacity);

        timeBar.value = Mathf.Clamp01(beamTime / totalBeats);

        if (boss) bossBar.value = Mathf.Clamp01((float)boss.health.health / boss.maxHealth);
        else bossBar.value = 0;
    }

    void PlayPattern(Pattern pattern) {
        patternCoroutines.Add(StartCoroutine(pattern.Play()));
    }

    public IEnumerator DoBossPattern() {
        boss = AddEnemy();
        boss.transform.position = Vector3.zero;

        boss.shape.SetScale(new Vector2(3, 3));

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
        // 최대 두명 타겟팅해서 따라다니기 (발사까지 하면 난이도가 너무 높아짐)
        // 16 beats
        {
            Debug.Log("#7");

            Player playerA = null, playerB = null;

            targeting.GetTwoTargets(ref playerA, ref playerB);

            Player[] players = new Player[] { playerA, playerB };
            Enemy[] enemies = new Enemy[] { null, null };

            for (int i = 0; i < 2; i++) {
                Player player = players[i];

                if (!player) continue;

                Enemy enemy = AddEnemy();
                enemy.shape.SetScale(new Vector2(1, 1));
                enemy.health.health = 20;

                PlayPattern(new FollowPattern(enemy.entity, 16f * spb, 360 * 2, player));

                enemies[i] = enemy;

                // PlayPattern(new ShootPattern(enemy.entity, 16f * spb, player, 1f * spb));
            }

            LockDamagePattern lockDamagePattern = new LockDamagePattern(boss, 16f * spb);

            for (int i = 0; i < 2; i++) {
                if (enemies[i]) lockDamagePattern.targetEntities.Add(enemies[i].entity);
            }

            PlayPattern(lockDamagePattern);

            yield return new WaitForSeconds(16f * spb);

            // Delete enemies.
            for (int i = 0; i < 2; i++) {
                if (enemies[i]) {
                    enemies[i].health.Damage(enemies[i].health.health);
                }
            }
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
        // 최대 두명 타겟팅해서 따라다니기 + 약한 원
        // 16 beats
        {
            Debug.Log("#7");

            Player playerA = null, playerB = null;

            targeting.GetTwoTargets(ref playerA, ref playerB);

            Player[] players = new Player[] { playerA, playerB };
            Enemy[] enemies = new Enemy[] { null, null };

            for (int i = 0; i < 2; i++) {
                Player player = players[i];

                if (!player) continue;

                Enemy enemy = AddEnemy();
                enemy.shape.SetScale(new Vector2(1, 1));
                enemy.health.health = 20;

                PlayPattern(new FollowPattern(enemy.entity, 16f * spb, 360 * 2, player));

                enemies[i] = enemy;

                // PlayPattern(new ShootPattern(enemy.entity, 16f * spb, player, 1f * spb));
            }

            LockDamagePattern lockDamagePattern = new LockDamagePattern(boss, 16f * spb);

            for (int i = 0; i < 2; i++) {
                if (enemies[i]) lockDamagePattern.targetEntities.Add(enemies[i].entity);
            }

            PlayPattern(lockDamagePattern);

            for (int i = 0; i < 4; i++) {
                BulletCirclePattern pattern = new BulletCirclePattern(boss.entity, i * 45);
                pattern.count = 4;
                PlayPattern(pattern);

                yield return new WaitForSeconds(4f * spb);
            }

            // Delete enemies.
            for (int i = 0; i < 2; i++) {
                if (enemies[i]) {
                    enemies[i].health.Damage(enemies[i].health.health);
                }
            }
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

    public void SetPause(bool pause) {
        if (paused == pause) return;

        paused = pause;

        if (paused) {
            Time.timeScale = 0;
            IngameUi.Instance.ShowPauseScreen();
        }
        else {
            Time.timeScale = 1;
            IngameUi.Instance.HidePauseScreen();
        }
    }

    public void RewindGame(bool musicEnded) {
        // StartCoroutine(RewindGameCoroutine());
        invincibleFlag = true;

        Camera cam = Camera.main;

        DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, 5 * 1.1f, 0.7f).SetEase(Ease.OutCubic).SetUpdate(true);

        DOTween.To(()=> Time.timeScale, x=> Time.timeScale = x, 0, 1f).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() => {

            foreach (Player player in players) if (player) Destroy(player.gameObject);
            foreach (Enemy enemy in enemies) if (enemy) Destroy(enemy.gameObject);
            foreach (Projectile projectile in projectiles) if (projectile) Destroy(projectile.gameObject);
            // foreach (Shape shape in shapes) if (shape) Destroy(shape.gameObject);

            // @Inefficient
            foreach (Particle particle in FindObjectsOfType<Particle>()) {
                particle.Remove();
            }

            players.Clear();
            enemies.Clear();
            projectiles.Clear();
            // shapes.Clear();

            PoolManager.Instance.DespawnAll("shape");

            float rewindDuration = Mathf.Log(time + 1);

            state = GameState.REWINDING;

            Time.timeScale = 1;

            StopBossPattern();

            // Flush player recorder.
            if (playerRecorders.Count >= maxClonedPlayers) {
                playerRecorders.RemoveAt(0);
            }
            playerRecorders.Add(currentPlayerRecorder);
            currentPlayerRecorder = null;

            // Music.Instance.audioSource.Stop();

            DOTween.To(() => playerBelowLayerOpacity, x => playerBelowLayerOpacity = x, 1, rewindDuration).SetEase(Ease.InCubic);

            float prevTime = time;

            DOTween.To(() => time, x => {
                time = x;

                Music.Instance.audioSource.pitch = (time - prevTime) / Time.deltaTime;

                prevTime = time;

                PoolManager.Instance.DespawnAll("shape");
                // shapes.Clear();

                shapeRecorder.Show(time);
            }, 0, rewindDuration).SetEase(Ease.InOutQuad).OnComplete(() => {

                time = 0;
                deltaTime = 0;

                Time.timeScale = 0;

                PoolManager.Instance.DespawnAll("shape");
                // shapes.Clear();

                DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, 5, 0.7f).SetEase(Ease.OutCubic).SetUpdate(true);

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

        // @Hardcoded: Tint color. Currently copied from Enemy.cs.
        Color tintColor = Color.Lerp(boss.shape.props.color, Color.white, 0.7f);
        Color originalColor = boss.shape.props.color;

        SFX.Instance.Play("preBigExplosion");

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

        SFX.Instance.Play("postBigExplosion");

        const float particleTime = 4f;

        Particle particle;

        // Particle
        {
            particle = Particle.Create();

            particle.transform.position = boss.transform.position;

            particle.amount = 64;
            particle.color = originalColor;
            particle.duration = particleTime;
            particle.scale = 0.5f;
            particle.speed = 4f;

            // @Hardcoded: Highest order like players.
            particle.SetOrder(20);
        }

        // Wait for particle to initialize.
        yield return null;

        DOTween.To(() => playerBelowLayerOpacity, x => playerBelowLayerOpacity = x, 1, particleTime).SetEase(Ease.OutQuad).SetUpdate(true);

        // Delete boss gameobject here.
        Destroy(boss.gameObject);

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
        const float fadeOutTime = 2;

        DOTween.To(() => upmostLayerOpacity, x => upmostLayerOpacity = x, 1, fadeOutTime).SetEase(Ease.OutQuad).SetUpdate(true);

        yield return new WaitForSecondsRealtime(fadeOutTime);

        globalData.ending = true;

        Time.timeScale = 1;
        SceneManager.LoadScene("Title");
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