using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AgentHealth : MonoBehaviour
{
    public float CurrentPercentage = 100;
    public Slider UISlider;

    public MeshRenderer bodyMesh;
    public Color damageColor;
    public Color startingColor;
    public float damageFlashDuration = .02f;

    public GameObject CubeBody;
    public GameObject DeathCube;
    public GameObject ExplosionParticles;
    public bool ResetSceneAfterDeath = false;
    public bool Dead;

    [Header("PLAYER DAMAGE")] public bool UseGlobalDamageSettings;
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

    private void OnCollisionEnter(Collision col)
    {
        if (Dead)
        {
            return;
        }
        if (col.transform.CompareTag("projectile"))
        {

            var damage = DamagePerHit;

            CurrentPercentage = Mathf.Clamp(CurrentPercentage - damage, 0, 100);
            if (CurrentPercentage == 0)
            {
                Dead = true;
                rb.isKinematic = true;
                CubeBody.SetActive(false);
                DeathCube.transform.position = CubeBody.transform.position;
                DeathCube.SetActive(true);
                ExplosionParticles.transform.position = CubeBody.transform.position;
                ExplosionParticles.SetActive(true);


                if (ResetSceneAfterDeath)
                {
                    StartCoroutine(RestartScene());
                }
            }
            StartCoroutine(BodyDamageFlash());
        }
    }

    IEnumerator RestartScene()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        float timer = 0;
        while (timer < 1)
        {
            timer += Time.fixedDeltaTime;
            yield return wait;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    IEnumerator BodyDamageFlash()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        if (bodyMesh)
        {
            bodyMesh.material.color = damageColor;
        }
        float timer = 0;
        while (timer < damageFlashDuration)
        {
            timer += Time.fixedDeltaTime;
            yield return wait;
        }
        if (bodyMesh)
        {
            bodyMesh.material.color = startingColor;
        }
    }
}
