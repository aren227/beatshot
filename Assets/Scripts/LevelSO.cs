using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/LevelSO")]
public class LevelSO : ScriptableObject
{
    public string title;
    public string artist;
    public int maxPlayers;
}
