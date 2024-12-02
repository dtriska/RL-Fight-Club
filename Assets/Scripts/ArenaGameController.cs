using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

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

    public bool ShouldPlayEffects
    {
        get
        {
            return CurrentSceneType != SceneType.Training;
        }
    }

    [Serializable]
    public class PlayerInfo
    {
        public ArenaAgent Agent;
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

    [Header("Visual Effects")]
    public bool usePoofParticlesOnElimination;
    public List<GameObject> poofParticlesList;

    [Header("UI Audio")]
    public AudioClip HurtVoiceAudioClip;
    public AudioClip CountdownClip;
    public AudioClip WinSoundFX1;
    public AudioClip WinSoundFX2;
    public AudioClip LoseSoundFX1;
    public AudioClip LoseSoundFX2;
    private AudioSource m_audioSource;

    [Header("UI")]
    public GameObject BlueTeamWonUI;
    public GameObject RedTeamWonUI;
    public TMP_Text CountDownText;

    private bool m_Initialized;
    public List<PlayerInfo> Team0Players;
    public List<PlayerInfo> Team1Players;
    private int m_ResetTimer;
    private float m_TimeBonus = 1.0f;
    private EnvironmentParameters m_EnvParameters;
    public int MaxEnvironmentSteps = 5000;

    void Initialize()
    {
        m_EnvParameters = Academy.Instance.EnvironmentParameters;
        m_Team0AgentGroup = new SimpleMultiAgentGroup();
        m_Team1AgentGroup = new SimpleMultiAgentGroup();

        foreach (var item in Team0Players)
        {
            item.Agent.Initialize();
            item.Agent.AgentHealth.CurrentPercentage = 100;
            item.Agent.m_BehaviorParameters.TeamId = 0;
            item.TeamID = 0;
            m_Team0AgentGroup.RegisterAgent(item.Agent);
        }
        foreach (var item in Team1Players)
        {
            item.Agent.Initialize();
            item.Agent.AgentHealth.CurrentPercentage = 100;
            item.Agent.m_BehaviorParameters.TeamId = 1;
            item.TeamID = 1;
            m_Team1AgentGroup.RegisterAgent(item.Agent);
        }

        if (usePoofParticlesOnElimination)
        {
            foreach (var item in poofParticlesList)
            {
                item.SetActive(false);
            }
        }

        if (CurrentSceneType == SceneType.Game)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
            m_Team0AgentGroup.GroupEpisodeInterrupted();
            m_Team1AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    public void PlayerWasHit(ArenaAgent hit, ArenaAgent thrower)
    {
        int hitTeamID = hit.teamID;
        int throwTeamID = thrower.teamID;
        var HitAgentGroup = hitTeamID == 1 ? m_Team1AgentGroup : m_Team0AgentGroup;
        var ThrowAgentGroup = hitTeamID == 1 ? m_Team0AgentGroup : m_Team1AgentGroup;
        float hitBonus = EliminationHitBonus;

        if (hit.AgentHealth.IsOnFinalHit) //FINAL HIT
        {
            m_NumberOfBluePlayersRemaining -= hitTeamID == 0 ? 1 : 0;
            m_NumberOfRedPlayersRemaining -= hitTeamID == 1 ? 1 : 0;
            print("Blue Remaining: " + m_NumberOfBluePlayersRemaining + " Red Remaining: " + m_NumberOfRedPlayersRemaining);
            // The current agent was just killed and is the final agent
            if (m_NumberOfBluePlayersRemaining == 0 || m_NumberOfRedPlayersRemaining == 0 || hit.gameObject == PlayerGameObject)
            {
                ThrowAgentGroup.AddGroupReward(2.0f - m_TimeBonus * (m_ResetTimer / MaxEnvironmentSteps));
                HitAgentGroup.AddGroupReward(-1.0f);
                ThrowAgentGroup.EndGroupEpisode();
                HitAgentGroup.EndGroupEpisode();
                print($"Team {throwTeamID} Won");

                if (ShouldPlayEffects)
                {
                    // Don't poof the last agent
                    StartCoroutine(TumbleThenPoof(hit, false));
                }
                EndGame(throwTeamID);
            }
            else
            {
                // Additional effects for game mode
                if (ShouldPlayEffects)
                {
                    StartCoroutine(TumbleThenPoof(hit));
                }
                else
                {
                    hit.gameObject.SetActive(false);
                }
            }
        }
        else
        {
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

    public IEnumerator TumbleThenPoof(ArenaAgent agent, bool shouldPoof = true)
    {
        // Add force to make the agent tumble
        agent.AgentRb.AddForce(Vector3.right * 2f, ForceMode.Impulse);
        agent.AgentRb.constraints = RigidbodyConstraints.None;
        agent.AgentRb.drag = .5f;
        agent.AgentRb.angularDrag = 0;
        yield return new WaitForSeconds(0.5f);
        if (shouldPoof)
        {
            agent.gameObject.SetActive(false);
            //Poof Particles
            if (usePoofParticlesOnElimination)
            {
                PlayParticleAtPosition(agent.transform.position);
            }
        }
    }

    public void PlayParticleAtPosition(Vector3 pos)
    {
        foreach (var item in poofParticlesList)
        {
            if (!item.activeInHierarchy)
            {
                item.transform.position = pos;
                item.SetActive(true);
                break;
            }
        }
    }

    private bool m_GameEnded = false;
    public void ShowWinScreen(int winningTeam, float delaySeconds)
    {
        if (m_GameEnded) return;
        m_GameEnded = true;
        StartCoroutine(ShowWinScreenThenReset(winningTeam, delaySeconds));
    }

    // End the game, resetting if in training mode and showing a win screen if in game mode.
    public void EndGame(int winningTeam, float delaySeconds = 1.0f)
    {
        //GAME MODE
        if (ShouldPlayEffects)
        {
            ShowWinScreen(winningTeam, delaySeconds);
        }
        //TRAINING MODE
        else
        {
            ResetScene();
        }
    }

    public IEnumerator ShowWinScreenThenReset(int winningTeam, float delaySeconds)
    {
        GameObject winTextGO = winningTeam == 0 ? BlueTeamWonUI : RedTeamWonUI;
        AudioClip clipToUse1 = winningTeam == 0 ? WinSoundFX1 : LoseSoundFX1;
        AudioClip clipToUse2 = winningTeam == 0 ? WinSoundFX2 : LoseSoundFX2;
        yield return new WaitForSeconds(delaySeconds);
        winTextGO.SetActive(true);
        if (ShouldPlayEffects)
        {
            m_audioSource.PlayOneShot(clipToUse1, .05f);
            m_audioSource.PlayOneShot(clipToUse2, .05f);
        }

        winTextGO.SetActive(false);
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

        m_NumberOfBluePlayersRemaining = Team0Players.Count;
        m_NumberOfRedPlayersRemaining = Team1Players.Count;
        m_ResetTimer = 0;

        GetAllParameters();

        //Reset the agents
        foreach (var item in Team0Players)
        {
            item.Agent.gameObject.SetActive(true);
            item.Agent.ResetAgent();
            m_Team0AgentGroup.RegisterAgent(item.Agent);
        }
        foreach (var item in Team1Players)
        {
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