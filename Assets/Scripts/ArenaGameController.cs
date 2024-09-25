using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class AreaGameController : MonoBehaviour
{
    public enum SceneType
    {
        Game,
        Training,
    }

    public SceneType CurrentSceneType = SceneType.Training;

    [Header("HUMAN PLAYER")] public GameObject PlayerGameObject;
    public int PlayerMaxHitPoints = 5;
    public float EliminationHitBonus = 0.1f;
    private int m_NumberOfBluePlayersRemaining = 3;
    private int m_NumberOfRedPlayersRemaining = 3;
    private SimpleMultiAgentGroup m_Team0AgentGroup;
    private SimpleMultiAgentGroup m_Team1AgentGroup;
    // public List<ArenaAgent> teamBlue;
    // public List<ArenaAgent> teamRed;

    [Serializable]
    public class PlayerInfo
    {
        public ArenaAgent Agent;
        public int HitPointsRemaining;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
        [HideInInspector]
        public int TeamID;
    }

    private bool m_Initialized;
    public List<PlayerInfo> Team0Players;
    public List<PlayerInfo> Team1Players;
    private int m_ResetTimer;
    private float m_TimeBonus = 1.0f;
    private EnvironmentParameters m_EnvParameters;
    private StatsRecorder m_StatsRecorder;

    public int MaxEnvironmentSteps = 5000;

    void Initialize()
    {
        m_StatsRecorder = Academy.Instance.StatsRecorder;
        m_EnvParameters = Academy.Instance.EnvironmentParameters;
        m_Team0AgentGroup = new SimpleMultiAgentGroup();
        m_Team1AgentGroup = new SimpleMultiAgentGroup();

        //INITIALIZE AGENTS
        foreach (var item in Team0Players)
        {
            item.Agent.Initialize();
            item.Agent.HitPointsRemaining = PlayerMaxHitPoints;
            item.Agent.m_BehaviorParameters.TeamId = 0;
            item.TeamID = 0;
            item.Agent.NumberOfTimesPlayerCanBeHit = PlayerMaxHitPoints;
            m_Team0AgentGroup.RegisterAgent(item.Agent);
        }
        foreach (var item in Team1Players)
        {
            item.Agent.Initialize();
            item.Agent.HitPointsRemaining = PlayerMaxHitPoints;
            item.Agent.m_BehaviorParameters.TeamId = 1;
            item.TeamID = 1;
            item.Agent.NumberOfTimesPlayerCanBeHit = PlayerMaxHitPoints;
            m_Team1AgentGroup.RegisterAgent(item.Agent);
        }

        m_Initialized = true;
        ResetScene();
    }

    void FixedUpdate()
    {
        if (!m_Initialized) return;

        //RESET SCENE IF WE MaxEnvironmentSteps
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps)
        {
            m_Team0AgentGroup.GroupEpisodeInterrupted();
            m_Team1AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    public void PlayerWasHit(ArenaAgent hit, ArenaAgent thrower)
    {
        //SET AGENT/TEAM REWARDS HERE
        int hitTeamID = hit.teamID;
        int throwTeamID = thrower.teamID;
        var HitAgentGroup = hitTeamID == 1 ? m_Team1AgentGroup : m_Team0AgentGroup;
        var ThrowAgentGroup = hitTeamID == 1 ? m_Team0AgentGroup : m_Team1AgentGroup;
        float hitBonus = EliminationHitBonus;

        if (hit.HitPointsRemaining == 1) //FINAL HIT
        {
            m_NumberOfBluePlayersRemaining -= hitTeamID == 0 ? 1 : 0;
            m_NumberOfRedPlayersRemaining -= hitTeamID == 1 ? 1 : 0;
            // The current agent was just killed and is the final agent
            if (m_NumberOfBluePlayersRemaining == 0 || m_NumberOfRedPlayersRemaining == 0 || hit.gameObject == PlayerGameObject)
            {
                ThrowAgentGroup.AddGroupReward(2.0f - m_TimeBonus * (m_ResetTimer / MaxEnvironmentSteps));
                HitAgentGroup.AddGroupReward(-1.0f);
                ThrowAgentGroup.EndGroupEpisode();
                HitAgentGroup.EndGroupEpisode();
                print($"Team {throwTeamID} Won");
                hit.gameObject.SetActive(false);

                StartCoroutine(WaitAndResetScene());
            }
            else
            { // The current agent was just killed but there are still agents left
                hit.gameObject.SetActive(false);
            }
        }
        else
        {
            hit.HitPointsRemaining--;
            thrower.AddReward(hitBonus);
        }
    }

    private IEnumerator WaitAndResetScene()
    {
        // Wait for the specified duration
        float delayBeforeReset = 3f;
        yield return new WaitForSeconds(delayBeforeReset);

        // Reset the scene
        ResetScene();
    }

    private void GetAllParameters()
    {
        //Set time bonus to 1 for Elimination
        float defaultTimeBonus = 1.0f;
        m_TimeBonus = m_EnvParameters.GetWithDefault("time_bonus_scale", defaultTimeBonus);
        EliminationHitBonus = m_EnvParameters.GetWithDefault("elimination_hit_reward", EliminationHitBonus);
    }

    void ResetScene()
    {
        StopAllCoroutines();

        m_NumberOfBluePlayersRemaining = 3;
        m_NumberOfRedPlayersRemaining = 3;

        m_ResetTimer = 0;

        GetAllParameters();

        //Reset the agents
        foreach (var item in Team0Players)
        {
            item.Agent.HitPointsRemaining = PlayerMaxHitPoints;
            item.Agent.gameObject.SetActive(true);
            item.Agent.ResetAgent();
            m_Team0AgentGroup.RegisterAgent(item.Agent);
        }
        foreach (var item in Team1Players)
        {
            item.Agent.HitPointsRemaining = PlayerMaxHitPoints;
            item.Agent.gameObject.SetActive(true);
            item.Agent.ResetAgent();
            m_Team1AgentGroup.RegisterAgent(item.Agent);
        }
    }

    void Update()
    {
        if (!m_Initialized)
        {
            Initialize();
        }
    }
}