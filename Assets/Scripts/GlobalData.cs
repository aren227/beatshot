using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/GlobalData")]
public class GlobalData : ScriptableObject
{
    public List<LevelSO> levels;
    public LevelSO current;
}