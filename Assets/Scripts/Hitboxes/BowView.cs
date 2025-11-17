using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowView : WeaponView
{
    public GameObject arrow;
    public GameObject bow;
    public GameObject spinArrow;

    public override IEnumerator TakeOut()
    {
        float countdown = 0f;
        float overshootTime = equipTime / 2;
        float overshootAngle = restingRotation.z + 15f;
        while (countdown < overshootTime)
        {
            countdown += Time.deltaTime;
            float multiplier = countdown / overshootTime;
            bow.transform.localScale = restingScale * multiplier;
            bow.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, overshootAngle * multiplier);
            bow.transform.localPosition = restingPosition * multiplier;
            yield return null;
        }
        bow.transform.localScale = restingScale;
        bow.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, overshootAngle);
        bow.transform.localPosition = restingPosition;
        countdown -= overshootTime;
        float restingTime = equipTime * 0.3f;
        while (countdown < restingTime)
        {
            countdown += Time.deltaTime;
            float multiplier = (restingTime - countdown) / restingTime;
            bow.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingRotation.z + (overshootAngle - restingRotation.z) * multiplier);
        }
        bow.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingPosition.z);
        yield return new WaitForSeconds(equipTime / 2 - countdown);
    }

    public override IEnumerator PutAway()
    {
        float countdown = 0f;
        while (countdown < unequipTime)
        {
            countdown += Time.deltaTime;
            float multiplier = (unequipTime - countdown) / unequipTime;
            bow.transform.localScale = restingScale * multiplier;
            bow.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingRotation.z * multiplier);
            bow.transform.localPosition = restingPosition * multiplier;
            yield return null;
        }
        bow.transform.localScale = restingScale * 0;
        bow.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingRotation.z * 0);
        bow.transform.localPosition = restingPosition * 0;
    }

    public override List<SpriteRenderer> GetSprites()
    {
        List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        sprites.Add(bow.GetComponent<SpriteRenderer>());
        sprites.Add(arrow.GetComponent<SpriteRenderer>());
        return sprites;
    }

    public override void ResetPosition()
    {
        bow.transform.localPosition = restingPosition;
        bow.transform.localRotation = Quaternion.Euler(restingRotation);
        bow.transform.localScale = restingScale;
    }

}
