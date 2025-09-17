using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Chaser : Agent
{
    Runner runner;
    float maxDistanceSquared = 0f;
    Vector3 startPos;

    protected override void Awake()
    {
        runner = FindFirstObjectByType<Runner>();
        startPos = transform.position;
        base.Awake();

        // 
        maxDistanceSquared = 25f * 25f + 25f * 25f;
    }

    void Update()
    {
        Move(Random.Range(0, 2));
    }

    private void Move(int random)
    {
        if (random == 0)
        {
            transform.position = Vector3.Slerp(transform.position,
            new Vector3(
                transform.position.x + Random.Range(-0.1f, 0.1f),
                transform.position.y,
                transform.position.z
            ), 1);
        }
        else
        {
            transform.position = Vector3.Slerp(transform.position,
            new Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z + Random.Range(-0.1f, 0.1f)
                ), 1);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float currentDistance = Vector3.SqrMagnitude(runner.transform.position - transform.position); // SQUARED!
        sensor.AddObservation(currentDistance / maxDistanceSquared); // Always between 0 and 1.
        base.CollectObservations(sensor);
    }

    // Komt uiteindelijk vanuit Python
    public override void OnActionReceived(ActionBuffers actions)
    {
        ActionSegment<float> continuousActions = actions.ContinuousActions;
        if (!continuousActions.IsEmpty())
        {
            print(continuousActions[0]);
            Move((int)continuousActions[0]);
        }

        AddReward(0.1f);
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        transform.position = startPos;
    }
}
