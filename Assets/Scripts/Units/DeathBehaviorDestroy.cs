using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBehaviorDestroy : UnitDeathBehavior
{
    public SpriteRenderer deadSprite;
    public float fadeDelay;
    public float fadeDuration;

    public override void OnDeath(Unit unit)
    {
        unit.aiScript.ForceTransition(AIState.Dead);
        deadSprite.gameObject.SetActive(true);
        StartCoroutine(FadeAndDestroy(unit));
    }

    public IEnumerator FadeAndDestroy(Unit unit)
    {
        yield return new WaitForSeconds(fadeDelay);
        List<SpriteRenderer> fadeSprites = unit.spriteManager.GetAllSprites();
        fadeSprites.Add(deadSprite);
        unit.spriteManager.UseDamagePortrait(-1, true);
        float fadeTimer = 0;
        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float alpha = System.Math.Max(0, 1 - fadeTimer / fadeDuration);
            foreach (SpriteRenderer ren in fadeSprites)
            {
                if (ren != null && ren.gameObject.activeInHierarchy)
                {
                    ren.color = new Color(ren.color.r, ren.color.g, ren.color.b, alpha);
                }
            }
            yield return null;
        }
        Destroy(unit.gameObject);
    }
}
