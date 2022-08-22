using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Targeting
{
    public List<int[]> targets = new List<int[]>();

    public int index = 0;

    public void Reset() {
        index = 0;
    }

    Player FindPlayerById(int id) {
        foreach (Player player in Manager.Instance.players) {
            if (player.entity.id == id) return player;
        }
        return null;
    }

    public Player GetTarget() {
        int id = 0;
        if (index < targets.Count) {
            id = targets[index][0];
        }
        else {
            targets.Add(new int[1]);
        }

        Player player = FindPlayerById(id);

        if (!player) {
            player = Manager.Instance.currentPlayer;
        }

        targets[index][0] = player ? player.entity.id : 0;

        index++;

        return player;
    }

    public void GetTwoTargets(ref Player a, ref Player b) {
        int idA = 0, idB = 0;
        if (index < targets.Count) {
            idA = targets[index][0];
            idB = targets[index][1];
        }
        else {
            targets.Add(new int[2]);
        }

        a = FindPlayerById(idA);
        b = FindPlayerById(idB);

        if (!a) {
            a = Manager.Instance.currentPlayer;
        }
        else if (!b) {
            b = Manager.Instance.currentPlayer;
        }

        targets[index][0] = a ? a.entity.id : 0;
        targets[index][1] = b ? b.entity.id : 0;

        index++;
    }
}
