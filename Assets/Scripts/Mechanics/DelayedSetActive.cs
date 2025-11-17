using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedSetActive : MonoBehaviour
{
    public List<GameObject> targets;
    public float delay;
    float countdown;
    // Start is called before the first frame update
    void Start()
    {
        countdown = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (countdown < delay)
        {
            countdown += Time.deltaTime;
            if (countdown >= delay)
            {
                foreach (GameObject target in targets)
                {
                    target.SetActive(true);
                }
            }
        }
    }
}
