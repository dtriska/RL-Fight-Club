using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MLAgents;

public class AgentHealth : MonoBehaviour
{
    private int teamID;
    public float m_knockback = 10f;
    public bool IsOnFinalHit = false;
    public AreaGameController m_GameController;
    public float CurrentPercentage = 100;
    public Slider UISlider;

    public Color damageColor;
    public Color startingColor;
    public float damageFlashDuration = .02f;

    public GameObject CubeBody;
    public GameObject ExplosionParticles;
    public bool Dead;
    public float DamagePerHit = 15f;

    public float DamagePerHitHEAVY = 40f;

    private Rigidbody rb;
    private static Dictionary<Collider, ArenaAgent> swordParentTeamIDCache = new Dictionary<Collider, ArenaAgent>();

    void OnEnable()
    {
        CurrentPercentage = 100;

        if (UISlider)
        {
            UISlider.value = CurrentPercentage;
        }

        rb = GetComponent<Rigidbody>();
        m_GameController = GetComponentInParent<AreaGameController>();
        teamID = GetComponentInParent<ArenaAgent>().teamID;
    }

    void Update()
    {
        if (Dead)
        {
            return;
        }

        if (UISlider)
        {
            UISlider.value = CurrentPercentage;
        }
    }

//////////////////
private void OnTriggerEnter(Collider other)
{
    if (Dead)
    {
        return;
    }

    // Check if the agent is blocking; if so, ignore the damage and exit
    if (GetComponentInParent<AgentCubeMovement>().IsAnimationPlaying("Block"))
    {
        return;
    }

    // Check if the collision object is tagged as a Sword
    if (!other.CompareTag("Sword"))
    {
        return;
    }

    // Get the sword's parent agent (the attacker) from cache or hierarchy
    ArenaAgent swordParentAgent;
    if (!swordParentTeamIDCache.TryGetValue(other, out swordParentAgent))
    {
        Transform parentAgent = GetAncestorAtLevel(other.transform, 9);
        swordParentAgent = parentAgent.GetComponent<ArenaAgent>();
        swordParentTeamIDCache[other] = swordParentAgent;
    }

    // Ignore if the attacker is on the same team
    if (swordParentAgent.teamID == teamID)
    {
        return;
    }

    print("Player: " + teamID + " was hit by Player: " + swordParentAgent.teamID);

    // Apply knockback force
    var dir = transform.position - other.transform.position;
    dir.y = 0;
    dir.Normalize();
    rb.AddForce(dir * m_knockback, ForceMode.Impulse);

    // Notify the game controller that this agent was hit
    m_GameController.PlayerWasHit(this.GetComponentInParent<ArenaAgent>(), swordParentAgent.GetComponent<ArenaAgent>());

    // Check if the attacker is performing a heavy or light attack and set damage accordingly
    float damage = DamagePerHit; // Default to light attack damage
    AgentCubeMovement attackerMovement = swordParentAgent.GetComponent<AgentCubeMovement>();

    if (attackerMovement.IsAnimationPlaying("HeavyAttack"))
    {
        damage = DamagePerHitHEAVY;
    }
    else if (attackerMovement.IsAnimationPlaying("LightAttack"))
    {
        damage = DamagePerHit;
    }

    // Apply the calculated damage
    CurrentPercentage = Mathf.Clamp(CurrentPercentage - damage, 0, 100);
    IsOnFinalHit = (CurrentPercentage - damage) <= 0;

    if (CurrentPercentage == 0)
    {
        Dead = true;
        rb.isKinematic = true;
        CubeBody.SetActive(false);
        
        ExplosionParticles.transform.position = CubeBody.transform.position;
        ExplosionParticles.SetActive(true);
    }

    if (!Dead && m_GameController.CurrentSceneType == AreaGameController.SceneType.Game && m_GameController.ShouldPlayEffects)
    {
        StartCoroutine(BodyDamageFlash());
    }
}
////////////////

    public static Transform GetAncestorAtLevel(Transform child, int level)
    {
        Transform currentParent = child;
        for (int i = 0; i < level; i++)
        {
            if (currentParent.parent != null)
            {
                currentParent = currentParent.parent;
            }
            else
            {
                return currentParent;
            }
        }
        return currentParent;
    }

    public void ResetHealth()
    {
        CurrentPercentage = 100;
        rb.isKinematic = false;
        Dead = false;
        IsOnFinalHit = false;
        CubeBody.SetActive(true);
        ExplosionParticles.SetActive(false);
    }

    private IEnumerator BodyDamageFlash()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        float timer = 0;
        while (timer < damageFlashDuration)
        {
            timer += Time.fixedDeltaTime;
            yield return wait;
        }


    }


}