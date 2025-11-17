using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateBowAttack : UnitAIState
{
    static readonly float preDrawTime = 0.2f;
    static readonly float drawTime = 0.8f;
    static readonly float cooldownTime = 1f;
    static readonly float drawAngle = 30f;
    static readonly float arrowDrawLength = 0.2f;
    static readonly float initialArrowLength = 0.4f;

    public override IEnumerator Run()
    {
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
        }
        //parentAI.user.velocityManager.rb.velocity = Vector2.zero;
        parentAI.user.spriteManager.PlaySoundEffect(CharacterAssetLoader.Instance.LoadSoundEffect("Bow Draw"));
        BowView view = (BowView)parentAI.user.spriteManager.weaponView;
        Transform bow = view.bow.transform;
        Transform arrow = view.arrow.transform;
        float countdown = 0f;
        while (countdown < preDrawTime)
        {
            countdown += Time.deltaTime;
            float bowAngle = -1 * drawAngle * countdown / preDrawTime;
            bow.localRotation = Quaternion.Euler(0, 0, bowAngle);
            yield return null;
        }
        countdown = 0f;
        bow.localRotation = Quaternion.Euler(0, 0, drawAngle * -1);
        arrow.localRotation = Quaternion.Euler(0, 0, drawAngle);
        arrow.localPosition = new Vector3(initialArrowLength, 0, 0);
        arrow.gameObject.SetActive(true);
        float readyTime = drawTime / 2;
        while (countdown < readyTime)
        {
            countdown += Time.deltaTime;
            float angle = drawAngle * (readyTime - countdown) / readyTime;
            bow.localRotation = Quaternion.Euler(0, 0, angle * -1);
            arrow.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        bow.localRotation = Quaternion.identity;
        arrow.localRotation = Quaternion.identity;
        countdown = 0f;
        while (countdown < readyTime)
        {
            countdown += Time.deltaTime;
            float arrowX = (initialArrowLength - arrowDrawLength) * (readyTime - countdown) / readyTime + arrowDrawLength;
            arrow.localPosition = new Vector3(arrowX, 0, 0);
            yield return null;
        }
        arrow.localPosition = new Vector3(arrowDrawLength, 0, 0);
        GameObject hitbox = view.normalHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, arrow.position, parentAI.user.transform.rotation);
        view.AttachHitbox(hitbox.GetComponent<AttackHitbox>(), true);
        arrow.gameObject.SetActive(false);
        yield return new WaitForSeconds(cooldownTime);
    }

    public override void ForceExit()
    {
        BowView view = (BowView)parentAI.user.spriteManager.weaponView;
        view.arrow.SetActive(false);
        view.transform.localRotation = Quaternion.Euler(view.restingRotation);
    }
}
