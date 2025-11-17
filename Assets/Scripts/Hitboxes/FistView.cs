using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistView : WeaponView
{
    public GameObject leftFist;
    public GameObject rightFist;
    public GameObject motionEffect2;
    public Vector3 rightRestingPosition;
    public Vector3 rightRestingRotation;
    public GameObject uppercutHitboxPrefab;

    public override IEnumerator TakeOut()
    {
        float countdown = 0f;
        float overshootTime = equipTime / 2;
        float overshootAngle = restingRotation.z + 15f;
        float rightOvershootAngle = rightRestingRotation.z + 15f;
        while (countdown < overshootTime)
        {
            countdown += Time.deltaTime;
            float multiplier = countdown / overshootTime;
            leftFist.transform.localScale = restingScale * multiplier;
            leftFist.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, overshootAngle * multiplier);
            leftFist.transform.localPosition = restingPosition * multiplier;
            rightFist.transform.localScale = restingScale * multiplier;
            rightFist.transform.localRotation = Quaternion.Euler(rightRestingRotation.x, rightRestingRotation.y, rightOvershootAngle * multiplier);
            rightFist.transform.localPosition = rightRestingPosition * multiplier;
            yield return null;
        }
        leftFist.transform.localScale = restingScale;
        leftFist.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, overshootAngle);
        leftFist.transform.localPosition = restingPosition;
        rightFist.transform.localScale = restingScale;
        rightFist.transform.localRotation = Quaternion.Euler(rightRestingRotation.x, rightRestingRotation.y, rightOvershootAngle);
        rightFist.transform.localPosition = rightRestingPosition;
        countdown -= overshootTime;
        float restingTime = equipTime * 0.3f;
        while (countdown < restingTime)
        {
            countdown += Time.deltaTime;
            float multiplier = (restingTime - countdown) / restingTime;
            leftFist.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingRotation.z + (overshootAngle - restingRotation.z) * multiplier);
            rightFist.transform.localRotation = Quaternion.Euler(rightRestingRotation.x, rightRestingRotation.y, rightRestingRotation.z + (rightOvershootAngle - rightRestingRotation.z) * multiplier);
        }
        leftFist.transform.localRotation = Quaternion.Euler(restingRotation);
        rightFist.transform.localRotation = Quaternion.Euler(rightRestingRotation);
        yield return new WaitForSeconds(equipTime / 2 - countdown);
    }

    public override IEnumerator PutAway()
    {
        float countdown = 0f;
        while (countdown < unequipTime)
        {
            countdown += Time.deltaTime;
            float multiplier = (unequipTime - countdown) / unequipTime;
            leftFist.transform.localScale = restingScale * multiplier;
            leftFist.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingRotation.z * multiplier);
            leftFist.transform.localPosition = restingPosition * multiplier;
            rightFist.transform.localScale = restingScale * multiplier;
            rightFist.transform.localRotation = Quaternion.Euler(rightRestingRotation.x, rightRestingRotation.y, rightRestingRotation.z * multiplier);
            rightFist.transform.localPosition = rightRestingPosition * multiplier;
            yield return null;
        }
        leftFist.transform.localScale = restingScale * 0;
        leftFist.transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, 0);
        leftFist.transform.localPosition = restingPosition * 0;
        rightFist.transform.localScale = restingScale * 0;
        rightFist.transform.localRotation = Quaternion.Euler(rightRestingRotation.x, rightRestingRotation.y, 0);
        rightFist.transform.localPosition = rightRestingPosition * 0;
    }

    public override List<SpriteRenderer> GetSprites()
    {
        List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        sprites.Add(leftFist.GetComponent<SpriteRenderer>());
        sprites.Add(rightFist.GetComponent<SpriteRenderer>());
        return sprites;
    }

    public override void ResetPosition()
    {
        leftFist.transform.localPosition = restingPosition;
        leftFist.transform.localRotation = Quaternion.Euler(restingRotation);
        leftFist.transform.localScale = restingScale;
        rightFist.transform.localPosition = rightRestingPosition;
        rightFist.transform.localRotation = Quaternion.Euler(rightRestingRotation);
        rightFist.transform.localScale = restingScale;
    }

}
