using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Random = UnityEngine.Random;

public class ArenaAgent : Agent
{
    [Header("TEAM")]

    public int teamID;
    private AgentCubeMovement m_CubeMovement;
    public bool UseVectorObs;
    private AreaGameController m_GameController;
    private Vector3 m_StartingPos;
    private Quaternion m_StartingRot;
    [SerializeField] private Sword sword;
    [Header("INPUT")]
    public ArenaAgentInput input;

    [HideInInspector]
    public Rigidbody AgentRb;
    [HideInInspector]
    public int NumberOfTimesPlayerCanBeHit = 5;
    [HideInInspector]
    public int HitPointsRemaining;
    [HideInInspector]
    public BehaviorParameters m_BehaviorParameters;
    [Header("OTHER")]
    public float m_InputH;
    private float m_InputV;
    private float m_Rotate;
    public float m_AttackInput;
    public float m_DashInput;
    private bool m_FirstInitialize = true;
    private bool m_DashCoolDownReady;
    private float m_LocationNormalizationFactor = 80.0f; // About the size of a reasonable stage
    public BufferSensorComponent m_OtherAgentsBuffer;

    private bool m_IsDecisionStep;
    [HideInInspector]
    //because heuristic only runs every 5 fixed update steps, the input for a human feels really bad
    //set this to true on an agent that you want to be human playable and it will collect input every
    //FixedUpdate tick instead of ever decision step
    public bool disableInputCollectionInHeuristicCallback;


    public override void Initialize()
    {

        var bufferSensors = GetComponentsInChildren<BufferSensorComponent>();
        m_OtherAgentsBuffer = bufferSensors[0];

        m_CubeMovement = GetComponent<AgentCubeMovement>();
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();

        AgentRb = GetComponent<Rigidbody>();
        input = GetComponent<ArenaAgentInput>();
        m_GameController = GetComponentInParent<AreaGameController>();

        if (m_FirstInitialize)
        {
            m_StartingPos = transform.position;
            m_StartingRot = transform.rotation;
            m_FirstInitialize = false;
        }
    }

    public void ResetAgent()
    {
        transform.position = m_StartingPos;
        AgentRb.constraints = RigidbodyConstraints.FreezeRotation;

        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

        AgentRb.velocity = Vector3.zero;
        AgentRb.angularVelocity = Vector3.zero;
        AgentRb.drag = 4;
        AgentRb.angularDrag = 1;
    }

    private int m_AgentStepCount; //current agent step
    void FixedUpdate()
    {
        m_DashCoolDownReady = m_CubeMovement.dashCoolDownTimer > m_CubeMovement.dashCoolDownDuration;
        if (StepCount % 5 == 0)
        {
            m_IsDecisionStep = true;
            m_AgentStepCount++;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);

        if (UseVectorObs)
        {
            sensor.AddObservation((float)HitPointsRemaining / (float)NumberOfTimesPlayerCanBeHit);

            sensor.AddObservation(Vector3.Dot(AgentRb.velocity, AgentRb.transform.forward));
            sensor.AddObservation(Vector3.Dot(AgentRb.velocity, AgentRb.transform.right));
            sensor.AddObservation(m_DashCoolDownReady);
        }


        List<AreaGameController.PlayerInfo> teamList;
        List<AreaGameController.PlayerInfo> opponentsList;

        if (m_BehaviorParameters.TeamId == 0)
        {
            teamList = m_GameController.Team0Players;
            opponentsList = m_GameController.Team1Players;
        }
        else
        {
            teamList = m_GameController.Team1Players;
            opponentsList = m_GameController.Team0Players;
        }

        foreach (var info in teamList)
        {
            if (info.Agent != this && info.Agent.gameObject.activeInHierarchy)
            {
                m_OtherAgentsBuffer.AppendObservation(GetOtherAgentData(info));
            }
        }
        
        int numOpponents = 0;
        foreach (var info in opponentsList)
        {
            if (info.Agent.gameObject.activeInHierarchy)
            {
                numOpponents++;
            }
        }

        sensor.AddObservation(numOpponents);
    }

    private float[] GetOtherAgentData(AreaGameController.PlayerInfo info)
    {
        var otherAgentdata = new float[6];
        otherAgentdata[0] = (float)info.Agent.HitPointsRemaining / (float)NumberOfTimesPlayerCanBeHit;
        var relativePosition = transform.InverseTransformPoint(info.Agent.transform.position);
        otherAgentdata[1] = relativePosition.x / m_LocationNormalizationFactor;
        otherAgentdata[2] = relativePosition.z / m_LocationNormalizationFactor;
        otherAgentdata[3] = info.TeamID == teamID ? 0.0f : 1.0f;
        var relativeVelocity = transform.InverseTransformDirection(info.Agent.AgentRb.velocity);
        otherAgentdata[4] = relativeVelocity.x / 30.0f;
        otherAgentdata[5] = relativeVelocity.z / 30.0f;
        return otherAgentdata;
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        m_InputV = continuousActions[0];
        m_InputH = continuousActions[1];
        m_Rotate = continuousActions[2];
        m_AttackInput = (int)discreteActions[0];
        m_DashInput = (int)discreteActions[1];

        //HANDLE ROTATION
        m_CubeMovement.Look(m_Rotate);

        //HANDLE XZ MOVEMENT
        var moveDir = transform.TransformDirection(new Vector3(m_InputH, 0, m_InputV));
        m_CubeMovement.RunOnGround(moveDir);

        //perform discrete actions only once between decisions
        if (m_IsDecisionStep)
        {
            m_IsDecisionStep = false;
            //HANDLE THROWING
            if (m_AttackInput > 0)
            {
                sword.Attack();
            }
            //HANDLE DASH MOVEMENT
            if (m_DashInput > 0 && m_DashCoolDownReady)
            {
                m_CubeMovement.Dash(moveDir);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
    }


    private void OnTriggerEnter(Collider col)
    {
        ArenaAgent fromSword = col.gameObject.GetComponent<ArenaAgent>();
        m_GameController.PlayerWasHit(this, fromSword);
    }

    public void Attack()
    {
        sword.Attack();
    }

    // HUMAN INPUT
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (disableInputCollectionInHeuristicCallback)
        {
            return;
        }
        var contActionsOut = actionsOut.ContinuousActions;
        contActionsOut[0] = input.moveInput.y;
        contActionsOut[1] = input.moveInput.x;
        contActionsOut[2] = input.rotateInput * 3; //rotate
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = input.CheckIfInputSinceLastFrame(ref input.m_attackPressed) ? 1 : 0; //dash
        discreteActionsOut[1] = input.CheckIfInputSinceLastFrame(ref input.m_dashPressed) ? 1 : 0; //dash
    }
}