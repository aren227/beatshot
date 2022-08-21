using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRecorder
{
    public int playerId;
    public List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();

    public int lastProcessedFrame = -1;

    public void TakeSnapshot(Player player) {
        PlayerSnapshot snapshot = new PlayerSnapshot();

        snapshot.time = Manager.Instance.time;

        snapshot.position = player.transform.position;

        snapshot.hasShoot = player.shootFlag;
        snapshot.shootDirection = player.shootDirection;

        snapshots.Add(snapshot);
    }

    public void Reset() {
        lastProcessedFrame = -1;
    }
}

public struct PlayerSnapshot {
    public float time;
    public Vector2 position;
    public bool hasShoot;
    public Vector2 shootDirection;
}
