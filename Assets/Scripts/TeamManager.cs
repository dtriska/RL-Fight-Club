using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class TeamManager : MonoBehaviour
{
    public List<ArenaAgent> teamBlue;
    public List<ArenaAgent> teamRed;


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

    public void CheckEndEpisode()
    {
        bool allBlueEliminated = teamBlue.TrueForAll(b => b.isEliminated);
        bool allRedEliminated = teamRed.TrueForAll(r => r.isEliminated);

        if (allBlueEliminated || allRedEliminated)
        {
            EndEpisode();
        }
    }

    public void EndEpisode()
    {
        if (teamBlue.TrueForAll(b => b.isEliminated))
        {
            // Red win
            foreach (var red in teamRed)
            {
                red.Win();
            }
            foreach (var blue in teamBlue)
            {
                blue.Lose();
            }
        }
        else if (teamRed.TrueForAll(r => r.isEliminated))
        {
            // Blue win
            foreach (var blue in teamBlue)
            {
                blue.Win();
            }
            foreach (var red in teamRed)
            {
                red.Lose();
            }
        }

        foreach (var blue in teamBlue)
        {
            blue.EndEpisode();
        }

        foreach (var red in teamRed)
        {
            red.EndEpisode();
        }
    }


}
