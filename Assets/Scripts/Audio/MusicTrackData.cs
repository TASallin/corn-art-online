using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MusicTrackData
{
    public string songName;
    public float loopStart;
    public float loopEnd;
    public bool isBattleTrack;
    public bool isMenuTrack;

    public MusicTrackData(string name, float start, float end, bool battle, bool menu)
    {
        songName = name;
        loopStart = start;
        loopEnd = end;
        isBattleTrack = battle;
        isMenuTrack = menu;
    }
}