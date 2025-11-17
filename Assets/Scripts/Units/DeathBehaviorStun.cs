using UnityEngine;
using TMPro;
using System.Collections;

public class DeathBehaviorStun : UnitDeathBehavior
{
    [SerializeField] private GameObject stunGraphic;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float stunDuration = 10f;

    public override void OnDeath(Unit unit)
    {
        unit.aiScript.ForceTransition(AIState.Dead);

        if (stunGraphic != null)
        {
            stunGraphic.SetActive(true);
        }

        StartCoroutine(StunSequence(unit));
    }

    private IEnumerator StunSequence(Unit unit)
    {
        // Activate and setup timer
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.color = Color.gray;
        }
        unit.spriteManager.UseDamagePortrait(-1, true);

        float timeRemaining = stunDuration;

        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = seconds.ToString();
            }

            yield return null;
        }

        // Deactivate timer
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        // Reset unit
        if (stunGraphic != null)
        {
            stunGraphic.SetActive(false);
        }

        unit.SetHP(unit.GetMaxHP());
        unit.aiScript.ForceTransition(AIState.Target);
        unit.spriteManager.UseNormalPortrait();
    }
}