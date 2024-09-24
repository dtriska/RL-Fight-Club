using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class TeamManager : MonoBehaviour
{
    public List<ArenaAgent> hiders;
    public List<ArenaAgent> seekers;


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
        bool allHidersEliminated = hiders.TrueForAll(h => h.isEliminated);
        bool allSeekersEliminated = seekers.TrueForAll(s => s.isEliminated);

        if (allHidersEliminated || allSeekersEliminated)
        {
            EndEpisode();
        }
    }

    public void EndEpisode()
    {
        if (hiders.TrueForAll(h => h.isEliminated))
        {
            // Seekers win
            foreach (var seeker in seekers)
            {
                seeker.Win();
            }
            foreach (var hider in hiders)
            {
                hider.Lose();
            }
        }
        else if (seekers.TrueForAll(s => s.isEliminated))
        {
            // Hiders win
            foreach (var hider in hiders)
            {
                hider.Win();
            }
            foreach (var seeker in seekers)
            {
                seeker.Lose();
            }
        }

        foreach (var hider in hiders)
        {
            hider.EndEpisode();
        }

        foreach (var seeker in seekers)
        {
            seeker.EndEpisode();
        }
    }


}
