using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ArenaAgent : Agent
{
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private MeshRenderer floorMeshRenderer;
    public TeamManager teamManager;

    public bool isEliminated = false;


    public override void OnEpisodeBegin()
    {
        isEliminated = false;
        transform.localPosition = new Vector3(Random.Range(-4f, 4f), 4f, Random.Range(-4f, 4f));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        float moveSpeed = 3f;

        transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;

        // Reward for staying alive
        if (!isEliminated)
        {
            AddReward(0.01f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Opponent")
        {
            // Reward for eliminating an opponent
            AddReward(1.0f);
            other.GetComponent<ArenaAgent>().Eliminate();
        }
        else if (other.tag == "Teammate")
        {
            // Punishment for hitting a teammate
            AddReward(-0.5f);
        }
    }

    public void Eliminate()
    {
        if (!isEliminated)
        {
            isEliminated = true;
            AddReward(-1.0f);
            floorMeshRenderer.material = loseMaterial;
            // teamManager.CheckEndEpisode();
        }
    }

    public void Win()
    {
        AddReward(2.0f);
        floorMeshRenderer.material = winMaterial;
    }

    public void Lose()
    {
        AddReward(-2.0f);
        floorMeshRenderer.material = loseMaterial;
    }
}