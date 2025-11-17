using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Linalg
{
    public static Vector2 RotateVector2(Vector2 v, float delta) //In degrees
    {
        float radians = Mathf.Deg2Rad * delta;
        return new Vector2(
            v.x * Mathf.Cos(radians) - v.y * Mathf.Sin(radians),
            v.x * Mathf.Sin(radians) + v.y * Mathf.Cos(radians)
        );
    }

    public static Vector2 Vector3ToVector2(Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }

    public static Vector3 Vector2ToVector3(Vector2 v)
    {
        return new Vector3(v.x, v.y, 0);
    }

    public static Vector3 RotateVector2(Vector3 v, float delta)
    {
        return Vector2ToVector3(RotateVector2(Vector3ToVector2(v), delta));
    }
}
