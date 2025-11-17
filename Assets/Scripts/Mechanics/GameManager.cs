using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager
{
    private static GameManager instance;
    public System.Random rng;
    public float xBound;
    public float yBound;

    public GameManager()
    {
        rng = new System.Random();
        xBound = 31f;
        yBound = 19f;
    }

    public static GameManager GetInstance()
    {
        if (instance == null)
        {
            instance = new GameManager();
        }
        return instance;
    }

    public bool InBounds(Vector2 point)
    {
        if (System.Math.Abs(point.x) > xBound)
        {
            return false;
        }
        if (System.Math.Abs(point.y) > yBound)
        {
            return false;
        }
        return true;
    }
}
