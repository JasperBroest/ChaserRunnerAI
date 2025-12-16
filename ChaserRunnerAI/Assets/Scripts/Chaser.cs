using System.Collections.Generic;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Chaser : Agent
{
    [SerializeField] private LayerMask visionLayerMask;
    [SerializeField] private float maxDistance = 35f;
    [SerializeField] private int visionRayCount = 32;

    public float moveSpeed = 5f;

    private float maxDistanceSquared = 0f;
    private float distance;
    private float previousDistance = 0;
    private float anglePerVisionRay = 1f;

    private Vector3 desiredDirection;

    private Rigidbody rb;
    private Runner runner;

   private float[] rayObs;

    protected override void Awake()
    {
        runner = FindFirstObjectByType<Runner>();
        rb = GetComponent<Rigidbody>();

        base.Awake();

        maxDistanceSquared = 12f * 12f + 12 * 12; //Pythagoras
        anglePerVisionRay = 360f / (float)visionRayCount;
        rayObs = new float[visionRayCount * 3];
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        desiredDirection = Vector3.zero;
        previousDistance = Vector3.Distance(transform.position, runner.transform.position);
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        Vector3 movement = desiredDirection * moveSpeed;
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        ShootRaycasts();

        float currentDistance = Vector3.SqrMagnitude(runner.transform.position - transform.position); // SQUARED!
        sensor.AddObservation(currentDistance / maxDistanceSquared); // Always between 0 and 1.

        Vector3 dirToRunner = (runner.transform.position - transform.position).normalized;
        sensor.AddObservation(dirToRunner.x);
        sensor.AddObservation(dirToRunner.z);

        for (int i = 0; i < rayObs.Length; i++)
        {
            sensor.AddObservation(rayObs[i]);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int direction = actions.DiscreteActions[0];
        desiredDirection = DiscreteActionToDirection(direction);

        Vector3 optimalDirection = (runner.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(optimalDirection, desiredDirection); // always between 0 and 180
        angle /= 180.0f; // always between 0 and 1, where 0 is best and 1 is worst
        angle = 1.0f - angle; // Always between 0 and 1 where 1 is best and 0 is worst.
        
        distance = Vector3.Distance(transform.position, runner.transform.position);
        //float distanceDifference = previousDistance - distance;

        // Small step reward proportional to improvement
        //float stepReward = Mathf.Clamp(distanceDifference * 1.5f, -0.01f, 0.03f);
        float stepReward = Map(angle, 0f, 1f, -0.01f, 0.03f);

        AddReward(stepReward);

        // Big reward if it catches the runner
        if (distance < 3.0f)
        {
            AddReward(50f);
            EndEpisode();
        }

        previousDistance = distance;

        base.OnActionReceived(actions);
    }

    private Vector3 DiscreteActionToDirection(int action)
    {
        switch (action)
        {
            case 0: return transform.forward;
            case 1: return -transform.forward;
            case 2: return -transform.right;
            case 3: return transform.right;
            default: return Vector3.zero;
        }
    }

    private void ShootRaycasts()
    {
        for (int i = 0; i < visionRayCount; i++)
        {
            Vector3 direction = Quaternion.Euler(0, anglePerVisionRay * i, 0) * transform.forward;

            RaycastHit hit;
            float d = 1f;
            float w = 0f;
            float r = 0f;

            if (Physics.Raycast(transform.position, direction, out hit, maxDistance, visionLayerMask))
            {
                d = hit.distance / maxDistance;
                w = hit.collider.CompareTag("Wall") ? 1f : 0f;
                r = hit.collider.CompareTag("Runner") ? 1f : 0f;
            }

            int baseIndex = i * 3;      // index for this ray

            rayObs[baseIndex + 0] = d;
            rayObs[baseIndex + 1] = w;
            rayObs[baseIndex + 2] = r;
        }
    }

    private void OnDrawGizmos()
    {
        //if (!Application.isPlaying) return;

        float anglePerVisionRay = 360f / visionRayCount;

        for (int i = 0; i < visionRayCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, anglePerVisionRay * i, 0) * transform.forward;
            Vector3 start = transform.position;
            Vector3 end = start + dir * maxDistance;

            // Default ray = white
            Gizmos.color = Color.white;

            // If ray hits something, color based on type
            if (Physics.Raycast(start, dir, out RaycastHit hit, maxDistance, visionLayerMask))
            {
                end = hit.point;

                if (hit.collider.CompareTag("Wall"))
                    Gizmos.color = Color.red;      // wall
                else if (hit.collider.CompareTag("Runner"))
                    Gizmos.color = Color.green;    // runner
                else
                    Gizmos.color = Color.yellow;   // other
            }

            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.05f);
        }
    }

    public static float Map(float value, float fromLow, float fromHigh, float toLow, float toHigh)
    {
        return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
    }
}