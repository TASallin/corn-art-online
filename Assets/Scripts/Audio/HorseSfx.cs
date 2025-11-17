using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HorseSfx : MonoBehaviour
{
    public UnitSprite sprite;
    public AudioClip neigh;
    public float minWaitTime;
    public float maxWaitTime;
    public float speedThreshold;

    private Vector3 storedPosition;
    private float countdown;
    private float currentWaitTime;

    // Start is called before the first frame update
    void OnEnable()
    {
        countdown = 0;
        storedPosition = transform.position;
        currentWaitTime = (float)GameManager.GetInstance().rng.NextDouble() * (maxWaitTime - minWaitTime) + minWaitTime;
    }

    // Update is called once per frame
    void Update()
    {
        countdown += Time.deltaTime;
        if (countdown > currentWaitTime)
        {
            countdown -= currentWaitTime;
            if (Vector3.Distance(transform.position, storedPosition) > speedThreshold)
            {
                sprite.PlaySoundEffect(neigh);
            }
            storedPosition = transform.position;
            currentWaitTime = (float)GameManager.GetInstance().rng.NextDouble() * (maxWaitTime - minWaitTime) + minWaitTime;
        }
    }
}
