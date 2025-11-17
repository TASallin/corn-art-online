using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Update UnitStartingData in UnitPlacer.cs
public class UnitStartingData
{
    public Vector2 Position { get; private set; }
    public int TeamId { get; private set; }
    public UnitClass UnitClass { get; private set; } // Reference to ClassDataManager class
    public string UnitName { get; private set; }
    public float ScaleFactor { get; private set; }

    public UnitStartingData(Vector2 position, int teamId, UnitClass unitClass)
    {
        Position = position;
        TeamId = teamId;
        UnitClass = unitClass;
        UnitName = "Unit";
        ScaleFactor = 1;
    }

    public UnitStartingData(Vector2 position, int teamId, UnitClass unitClass, float scaleFactor)
    {
        Position = position;
        TeamId = teamId;
        UnitClass = unitClass;
        UnitName = "Unit";
        ScaleFactor = scaleFactor;
    }

    public UnitStartingData(Vector2 position, int teamId, UnitClass unitClass, string unitName, float scaleFactor)
    {
        Position = position;
        TeamId = teamId;
        UnitClass = unitClass;
        UnitName = unitName;
        ScaleFactor = scaleFactor;
    }
}
