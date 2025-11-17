using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateBowSpecial : UnitAIState
{
    static readonly float preDrawTime = 0.1f;
    static readonly float spinTime = 0.7f;
    static readonly float drawTime = 0.1f;
    static readonly float recoilTime = 0.2f;
    static readonly float cooldownTime = 1.4f;
    static readonly float bowDrawAngle = -70f;
    static readonly float arrowDrawAngle = 70f;
    static readonly float arrowSpinSpeed = 1000f;
    static readonly Vector3 arrowDrawPosition = new Vector3(0.25f, 0, 0);

    public override IEnumerator Run()
    {
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
        }
        parentAI.user.spriteManager.PlaySoundEffect(CharacterAssetLoader.Instance.LoadSoundEffect("Bow Special"));
        BowView view = (BowView)parentAI.user.spriteManager.weaponView;
        Transform bow = view.bow.transform;
        Transform arrow = view.arrow.transform;
        Transform spinArrow = view.spinArrow.transform;
        float countdown = 0f;
        while (countdown < preDrawTime)
        {
            countdown += Time.deltaTime;
            float bowAngle = bowDrawAngle * countdown / preDrawTime;
            bow.localRotation = Quaternion.Euler(0, 0, bowAngle);
            yield return null;
        }
        countdown = 0f;
        bow.localRotation = Quaternion.Euler(0, 0, bowDrawAngle);
        spinArrow.gameObject.SetActive(true);
        while (countdown < spinTime)
        {
            countdown += Time.deltaTime;
            spinArrow.Rotate(new Vector3(0, 0, Time.deltaTime * arrowSpinSpeed));
            if (countdown > spinTime / 2)
            {
                float bowAngle = 2 * bowDrawAngle * (spinTime - countdown) / spinTime;
                bow.localRotation = Quaternion.Euler(0, 0, bowAngle);
            }
            yield return null;
        }
        spinArrow.gameObject.SetActive(false);
        bow.localRotation = Quaternion.identity;
        arrow.gameObject.SetActive(true);
        for (int i = 0; i < 3; i++)
        {
            countdown = 0f;
            arrow.localRotation = Quaternion.Euler(0, 0, arrowDrawAngle);
            arrow.localPosition = arrowDrawPosition;
            while (countdown < drawTime)
            {
                countdown += Time.deltaTime;
                float arrowAngle = arrowDrawAngle * (drawTime - countdown) / drawTime;
                arrow.localRotation = Quaternion.Euler(0, 0, arrowAngle);
                yield return null;
            }
            arrow.localRotation = Quaternion.identity;
            GameObject hitbox = view.specialHitboxPrefab;
            hitbox = MonoBehaviour.Instantiate(hitbox, arrow.position, parentAI.user.transform.rotation);
            view.AttachHitbox(hitbox.GetComponent<AttackHitbox>(), true);
            if (i == 2)
            {
                break;
            }
            countdown = 0f;
            while (countdown < recoilTime)
            {
                countdown += Time.deltaTime;
                float arrowAngle = arrowDrawAngle * countdown / recoilTime;
                arrow.localRotation = Quaternion.Euler(0, 0, arrowAngle);
                yield return null;
            }
        }
        arrow.gameObject.SetActive(false);
        yield return new WaitForSeconds(cooldownTime);
    }

    public override void ForceExit()
    {
        BowView view = (BowView)parentAI.user.spriteManager.weaponView;
        view.arrow.SetActive(false);
        view.spinArrow.SetActive(false);
        view.transform.localRotation = Quaternion.Euler(view.restingRotation);
    }
}
