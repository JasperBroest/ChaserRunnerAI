using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Chaser : Agent
{
    private Runner runner;
    private float maxDistanceSquared = 0f;
    private Vector3 startPos;

    Transform runnerTransform;


    private Rigidbody rb;
    public float moveSpeed = 5f;
    
    protected override void Awake()
    {
        runner = FindFirstObjectByType<Runner>();
        rb = GetComponent<Rigidbody>();

        startPos = transform.position;
        base.Awake();

        maxDistanceSquared = 12f * 12f + 12 * 12; //Pythagoras
    }
    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
    }

    private void Update()
    {
        //var rayOutputs = GetComponent<RayPerceptionSensorComponent3D>().m_RaySensor.RayPerceptionOutput.RayOutputs;

        //foreach (RayPerceptionOutput.RayOutput output in rayOutputs)
        //{
        //    if (output.HitGameObject.CompareTag("Runner"))
        //    {
        //        Debug.Log($"Object = {output.HitGameObject.transform.position} END");
        //        runnerTransform = output.HitGameObject.transform;
        //    }
        //    else
        //    {
        //        Debug.Log("No runner found");
        //    }
        //}
    }

    private void Move(Vector2 direction)
    {
        Vector3 movement;
        if (runnerTransform != null)
        {
            movement = (transform.position - runnerTransform.position).normalized;

        }
        else
        {
            movement = new Vector3(direction.x, 0f, direction.y) * moveSpeed;
        }

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float currentDistance = Vector3.SqrMagnitude(runner.transform.position - transform.position); // SQUARED!
        sensor.AddObservation(currentDistance / maxDistanceSquared); // Always between 0 and 1.
        base.CollectObservations(sensor);

        // Observations:
        // 1. Distance to runner
        // 2. Raycast to see if runner is in line of sight

    }

    // Komt uiteindelijk vanuit Python
    public override void OnActionReceived(ActionBuffers actions)
    {
        ActionSegment<float> continuousActions = actions.ContinuousActions;
        if (!continuousActions.IsEmpty())
        {
            Move(new Vector2(continuousActions[0], continuousActions[1]));
        }

        AddReward(0.1f);
    }

}