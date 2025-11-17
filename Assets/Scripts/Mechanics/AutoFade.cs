using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoFade : MonoBehaviour
{
    public float fadeDuration;
    float countdown;

    // Start is called before the first frame update
    void Start()
    {
        countdown = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (countdown < fadeDuration)
        {
            countdown += Time.deltaTime;
            float alpha = System.Math.Max(0, 1 - countdown / fadeDuration);
            SpriteRenderer ren = gameObject.GetComponent<SpriteRenderer>();
            ren.color = new Color(ren.color.r, ren.color.g, ren.color.b, alpha);
        }
    }
}
