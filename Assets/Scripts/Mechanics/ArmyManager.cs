using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmyManager : MonoBehaviour
{
    public Dictionary<int, List<Unit>> armies;
    public GameObject gameRulesObject;
    private GameRules gameRules;
    public SpriteRenderer chapterBackground;
    [SerializeField] private GameObject seizeTargetPrefab;
    [SerializeField] private GameObject hotPotatoPrefab;

    public List<Unit> GetLivingEnemies(Unit unit)
    {
        List<Unit> targets = new List<Unit>();
        foreach (int key in armies.Keys)
        {
            if (key == 0 || key != unit.teamID)
            {
                foreach (Unit u in armies[key])
                {
                    if (u.GetAlive() && u != unit)
                    {
                        targets.Add(u);
                    }
                }
            }
        }
        return targets;
    }

    public void AddUnit(Unit unit)
    {
        if (armies == null)
        {
            armies = new Dictionary<int, List<Unit>>();
        }
        if (!armies.ContainsKey(unit.teamID))
        {
            armies.Add(unit.teamID, new List<Unit>());
        }
        armies[unit.teamID].Add(unit);
        unit.armyManager = this;
        gameRules.RegisterUnit(unit);
    }

    public static bool IsEnemy(Unit unit1, Unit unit2)
    {
        if (unit1 == null || unit2 == null || unit1 == unit2)
        {
            return false;
        }
        return (unit1.teamID == 0 || unit1.teamID != unit2.teamID);
    }

    public void CreateRuleset()
    {
        string ruleString = MenuSettings.Instance.selectedGameMode;
        switch (ruleString)
        {
            case "Route":
                gameRulesObject.AddComponent<RouteGameRules>();
                break;
            case "Defeat Boss":
            case "Defeat Bosses":
                gameRulesObject.AddComponent<DefeatBossesGameRules>();
                break;
            case "Seize":
                gameRulesObject.AddComponent<SeizeGameRules>();
                gameRulesObject.GetComponent<SeizeGameOverCondition>().Initialize(seizeTargetPrefab);
                break;
            case "Survive":
                gameRulesObject.AddComponent<SurviveGameRules>();
                break;
            case "Battle Royale":
                gameRulesObject.AddComponent<BattleRoyaleRules>();
                break;
            case "Hot Potato":
                gameRulesObject.AddComponent<HotPotatoRules>();
                gameRulesObject.GetComponent<HotPotatoRules>().SetHotPotatoPrefab(hotPotatoPrefab);
                break;
            case "Team Battle":
                gameRulesObject.AddComponent<TeamBattleRules>();
                break;
            default:
                Debug.LogWarning(ruleString + " is not properly formatted game mode string");
                gameRulesObject.AddComponent<DefeatBossesGameRules>();
                break;
        }
        gameRules = gameRulesObject.GetComponent<GameRules>();
        string chapterName = MenuSettings.Instance.selectedLevel;
        Sprite bgSprite = CharacterAssetLoader.Instance.LoadChapterBackground(chapterName);
        if (bgSprite != null)
        {
            chapterBackground.sprite = bgSprite;
        }
    }

    public void ActivateAI()
    {
        foreach (List<Unit> army in armies.Values)
        {
            foreach (Unit unit in army)
            {
                unit.aiScript.ForceTransition(AIState.Target);
            }
        }
    }
}
