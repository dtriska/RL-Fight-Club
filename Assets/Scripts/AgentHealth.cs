using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AgentHealth : MonoBehaviour
{
    private int teamID;
    public float m_knockback = 10f;
    public bool IsOnFinalHit = false;
    public AreaGameController m_GameController;
    public float CurrentPercentage = 100;
    public Slider UISlider;

    public MeshRenderer bodyMesh;
    public Color damageColor;
    public Color startingColor;
    public float damageFlashDuration = .02f;

    public GameObject CubeBody;
    public GameObject DeathCube;
    public GameObject ExplosionParticles;
    public bool Dead;
    public float DamagePerHit = 15f;

    private Rigidbody rb;

    void OnEnable()
    {
        CurrentPercentage = 100;

        if (UISlider)
        {
            UISlider.value = CurrentPercentage;
        }

        if (bodyMesh)
        {
            startingColor = bodyMesh.sharedMaterial.color;
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

    private void OnTriggerEnter(Collider other)
    {
        if (Dead)
        {
            return;
        }


        if (!other.CompareTag("Sword"))
        {
            return;
        }
        else
        {
            var swordParentAgent = other.gameObject.transform.parent.parent.GetComponent<ArenaAgent>().teamID;

            if (swordParentAgent == teamID)
            {
                return;
            }
            print("Player: " + teamID + " was hit by Player: " + swordParentAgent);

            var dir = transform.position - other.transform.position;
            dir.y = 0;
            dir.Normalize();
            rb.AddForce(dir * m_knockback, ForceMode.Impulse);
            m_GameController.PlayerWasHit(this.GetComponentInParent<ArenaAgent>(), other.gameObject.transform.parent.parent.GetComponent<ArenaAgent>());

            var damage = DamagePerHit;

            CurrentPercentage = Mathf.Clamp(CurrentPercentage - damage, 0, 100);
            IsOnFinalHit = (CurrentPercentage - DamagePerHit) <= 0;

            if (CurrentPercentage == 0)
            {
                Dead = true;
                rb.isKinematic = true;
                CubeBody.SetActive(false);
                DeathCube.transform.position = CubeBody.transform.position;
                DeathCube.SetActive(true);
                ExplosionParticles.transform.position = CubeBody.transform.position;
                ExplosionParticles.SetActive(true);
            }

            if (!Dead && m_GameController.CurrentSceneType == AreaGameController.SceneType.Game && m_GameController.ShouldPlayEffects)
                StartCoroutine(BodyDamageFlash());

        }
    }

    public void ResetHealth()
    {
        CurrentPercentage = 100;
        rb.isKinematic = false;
        Dead = false;
        IsOnFinalHit = false;
        CubeBody.SetActive(true);
        DeathCube.SetActive(false);
        ExplosionParticles.SetActive(false);
    }

    private IEnumerator BodyDamageFlash()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        try
        {
            if (bodyMesh)
            {
                bodyMesh.material.color = damageColor;
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }

        float timer = 0;
        while (timer < damageFlashDuration)
        {
            timer += Time.fixedDeltaTime;
            yield return wait;
        }

        try
        {
            if (bodyMesh)
            {
                bodyMesh.material.color = startingColor;
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }
    }


}