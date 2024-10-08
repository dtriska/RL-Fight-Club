using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    Googly eyes script
*/
public class Eyes : MonoBehaviour
{

    public Transform pupil;
    public float maxPupilDistance = 0.1f;
    public float gravity = 10f;
    public float bounceDamping = 0.8f;

    private Vector3 prevPosition;
    private Vector3 prevVelocity;
    private Vector3 velocity;

    void Start()
    {
        prevPosition = transform.localPosition;
        prevVelocity = Vector3.zero;
        velocity = Vector3.zero;
    }

    void Update()
    {
        Vector3 currentParentVelocity = (transform.parent.position - prevPosition) / Time.deltaTime;
        prevPosition = transform.parent.position;

        Vector3 acceleration = (currentParentVelocity - prevVelocity) / Time.deltaTime;
        prevVelocity = currentParentVelocity;

        velocity += acceleration * Time.deltaTime;
        velocity.y -= gravity * Time.deltaTime;

        Vector3 targetPosition = new Vector3(velocity.x * maxPupilDistance, 0, velocity.z * maxPupilDistance);

        targetPosition.x = Mathf.Clamp(targetPosition.x, -maxPupilDistance, maxPupilDistance);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -maxPupilDistance, maxPupilDistance);

        if (Mathf.Abs(targetPosition.x) >= maxPupilDistance)
        {
            velocity.x = -velocity.x * bounceDamping;
            targetPosition.x = Mathf.Sign(targetPosition.x) * maxPupilDistance;
        }

        if (Mathf.Abs(targetPosition.z) >= maxPupilDistance)
        {
            velocity.z = -velocity.z * bounceDamping;
            targetPosition.z = Mathf.Sign(targetPosition.z) * maxPupilDistance;
        }

        pupil.localPosition = targetPosition;
    }
}
