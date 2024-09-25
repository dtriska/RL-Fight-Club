using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private float m_damage = 10f;
    [SerializeField] private float m_knockback = 10f;
    [SerializeField] private Animation m_swordSwing;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("redArenaAgent"))
        {
        }
    }

    public void SwingSword()
    {
        m_swordSwing.Play();
    }

    public void Attack()
    {
        SwingSword();
    }
}
