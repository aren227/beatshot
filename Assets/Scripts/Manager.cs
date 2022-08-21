using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    List<Player> players = new List<Player>();
    List<Projectile> projectiles = new List<Projectile>();

    List<Projectile> deleteProjectiles = new List<Projectile>();

    int nextEntityId = 0;

    void Start() {
        Enemy enemy = AddEnemy();
        enemy.transform.position = Vector3.zero;

        Player player = AddPlayer();
        player.transform.position = new Vector2(0, -3);
    }

    public Player AddPlayer() {
        Player player = Instantiate(PrefabRegistry.Instance.player).GetComponent<Player>();
        // @Todo: Set spawn point.

        players.Add(player);

        player.entity.id = -players.Count;

        return player;
    }

    public void RemovePlayer(Player player) {
        // @Todo
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

    public void RemoveProjectile(Projectile projectile) {
        deleteProjectiles.Add(projectile);

        Destroy(projectile.gameObject);
    }

    void Update() {
        foreach (Player player in players) {
            player.DoNextFrame(Time.deltaTime);
        }

        foreach (Projectile projectile in projectiles) {
            projectile.DoNextFrame(Time.deltaTime);
        }

        foreach (Projectile projectile in deleteProjectiles) {
            projectiles.Remove(projectile);
        }
        deleteProjectiles.Clear();
    }
}
