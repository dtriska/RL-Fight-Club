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

    [Header("HUMAN PLAYER")] 
    public GameObject PlayerGameObject;
    public float EliminationHitBonus = 0.1f;

    private int m_NumberOfBluePlayersRemaining = 3;
    private int m_NumberOfRedPlayersRemaining = 3;
    private SimpleMultiAgentGroup m_Team0AgentGroup;
    private SimpleMultiAgentGroup m_Team1AgentGroup;

    [Serializable]
    public class PlayerInfo
    {
        public ArenaAgent Agent;
        public int HitPointsRemaining;
        [HideInInspector] public Vector3 StartingPos;
        [HideInInspector] public Quaternion StartingRot;
        [HideInInspector] public Rigidbody Rb;
        [HideInInspector] public Collider Col;
        [HideInInspector] public int TeamID;
    }

    private bool m_Initialized;
    public List<PlayerInfo> Team0Players;
    public List<PlayerInfo> Team1Players;
    private int m_ResetTimer;

    public int MaxEnvironmentSteps = 5000;

    void Initialize()
    {
        m_Team0AgentGroup = new SimpleMultiAgentGroup();
        m_Team1AgentGroup = new SimpleMultiAgentGroup();

        foreach (var item in Team0Players)
        {
            item.Agent.Initialize();
            item.Agent.m_BehaviorParameters.TeamId = 0;
            item.TeamID = 0;
            m_Team0AgentGroup.RegisterAgent(item.Agent);
        }
        foreach (var item in Team1Players)
        {
            item.Agent.Initialize();
            item.Agent.m_BehaviorParameters.TeamId = 1;
            item.TeamID = 1;
            m_Team1AgentGroup.RegisterAgent(item.Agent);
        }

        m_Initialized = true;
        ResetScene();
    }

    void FixedUpdate()
    {
        if (!m_Initialized) return;

        m_ResetTimer++;
        if (m_ResetTimer >= MaxEnvironmentSteps)
        {
            EndEpisodes();
            ResetScene();
        }
    }

    public void PlayerWasHit(ArenaAgent hit, ArenaAgent thrower)
    {
        int hitTeamID = hit.teamID;

        if (hit.AgentHealth.IsOnFinalHit)
        {
            if (hitTeamID == 0) m_NumberOfBluePlayersRemaining--;
            else m_NumberOfRedPlayersRemaining--;

            if (m_NumberOfBluePlayersRemaining == 0 || m_NumberOfRedPlayersRemaining == 0)
            {
                EndEpisodes();
                ResetScene();
            }
        }
        else
        {
            thrower.AddReward(EliminationHitBonus);
        }
    }

    void EndEpisodes()
    {
        m_Team0AgentGroup.GroupEpisodeInterrupted();
        m_Team1AgentGroup.GroupEpisodeInterrupted();
    }

    void ResetScene()
    {
        Debug.Log("Resetting scene...");
        m_NumberOfBluePlayersRemaining = Team0Players.Count;
        m_NumberOfRedPlayersRemaining = Team1Players.Count;
        m_ResetTimer = 0;

        foreach (var item in Team0Players)
        {
            item.Agent.ResetAgent();
            item.Agent.gameObject.SetActive(true);
        }

        foreach (var item in Team1Players)
        {
            item.Agent.ResetAgent();
            item.Agent.gameObject.SetActive(true);
        }

        Debug.Log("Scene reset complete.");
    }

    void Update()
    {
        if (!m_Initialized)
        {
            Initialize();
        }
    }
}
