using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Chaser : Agent
{
    private Runner runner;
    private float maxDistanceSquared = 0f;
    private Vector3 startPos;

    protected override void Awake()
    {
        runner = FindFirstObjectByType<Runner>();
        startPos = transform.position;
        base.Awake();

        maxDistanceSquared = 25f * 25f + 25f * 25f; //Pythagoras
    }

    private void Move(int actionOutput)
    {
        switch (actionOutput)
        {
            case 0:
                // Forward
                transform.position = Vector3.Slerp(transform.position, transform.position + Vector3.forward, 1);
                break;

            case 1:
                // Right
                transform.position = Vector3.Slerp(transform.position, transform.position + Vector3.right, 1);
                break;
            case 2:
                // Left
                transform.position = Vector3.Slerp(transform.position, transform.position + Vector3.left, 1);
                break;
            case 3:
                // Back
                transform.position = Vector3.Slerp(transform.position, transform.position + Vector3.back, 1);
                break;
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
            UIMenu.Instance.Output(continuousActions[0].ToString());
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