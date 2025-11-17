using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WingSfx : MonoBehaviour
{
    public UnitSprite sprite;
    public AudioClip wingFlap;
    public float flapCooldown;
    public float speedThreshold;

    private Vector3 storedPosition;
    private float countdown;

    // Start is called before the first frame update
    void OnEnable()
    {
        countdown = (float)GameManager.GetInstance().rng.NextDouble() * flapCooldown;
        storedPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        countdown += Time.deltaTime;
        if (countdown > flapCooldown)
        {
            countdown -= flapCooldown;
            if (Vector3.Distance(transform.position, storedPosition) > speedThreshold)
            {
                sprite.PlaySoundEffect(wingFlap);
            }
            storedPosition = transform.position;
        }
    }
}
