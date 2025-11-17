using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChapterBackgroundMotion : MonoBehaviour
{
    public int xMax = 1600;
    public int yMax = 1000;
    public float moveTime = 5f;
    public int moveDistance = 100;
    public Vector2 directionVector;
    private float timer;

    // Start is called before the first frame update
    void Start()
    {
        timer = 0f;
        SetMotion();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= moveTime)
        {
            timer = 0;
            SetMotion();
        } else
        {
            //float xCurrent = xStart + directionVector.x * (timer / moveTime) * moveDistance;
            //float yCurrent = yStart + directionVector.y * (timer / moveTime) * moveDistance;
            //transform.position = new Vector3(xCurrent, yCurrent, 0);
            transform.Translate(directionVector.x * (Time.deltaTime / moveTime) * moveDistance, directionVector.y * (Time.deltaTime / moveTime) * moveDistance, 0);
        }
    }

    public void SetMotion()
    {
        System.Random rng = GameManager.GetInstance().rng;
        float xStart = (float)(rng.Next((xMax - moveDistance) * 2) - xMax + moveDistance);
        float yStart = (float)(rng.Next((yMax - moveDistance) * 2) - yMax + moveDistance);
        transform.position = new Vector3(xStart, yStart, 0);
        float directionAngle = (float)rng.Next(360);
        directionVector = Linalg.RotateVector2(new Vector2(1, 0), directionAngle);
    }
}
