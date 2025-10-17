using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Chaser : Agent
{
    [SerializeField] private LayerMask visionLayerMask;
    [SerializeField] private TextMeshProUGUI directiontext1;
    [SerializeField] private TextMeshProUGUI directiontext2;
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private int visionRayCount = 32;

    public float moveSpeed = 5f;
    private float maxDistanceSquared = 0f;
    private float distance;
    private float previousDistance = 0;
    private float invertedMaxDistance = 1f;
    private float anglePerVisionRay = 1f;

    private Vector3 desiredDirection;

    private Rigidbody rb;
    private Runner runner;

    private float[] visionDistanceObs;

    protected override void Awake()
    {
        runner = FindFirstObjectByType<Runner>();
        rb = GetComponent<Rigidbody>();

        base.Awake();

        maxDistanceSquared = 12f * 12f + 12 * 12; //Pythagoras
        invertedMaxDistance = 1f / maxDistance;
        anglePerVisionRay = 360f / (float)visionRayCount;
        visionDistanceObs = new float[visionRayCount];
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
        ShootRaycasts();
    }

    private void Move()
    {
        Vector3 movement;
        //directiontext.text = desiredDirection.ToString();
        movement = desiredDirection * moveSpeed;
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);

        previousDistance = distance;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float currentDistance = Vector3.SqrMagnitude(runner.transform.position - transform.position); // SQUARED!
        sensor.AddObservation(currentDistance / maxDistanceSquared); // Always between 0 and 1.
        sensor.AddObservation(visionDistanceObs);

        base.CollectObservations(sensor);
    }

    // Komt uiteindelijk vanuit Python
    public override void OnActionReceived(ActionBuffers actions)
    {
        ActionSegment<int> discreteActions = actions.DiscreteActions;
        int direction = discreteActions[0];
        distance = Vector3.Distance(transform.position, runner.transform.position);
        float distanceDifference = distance - previousDistance;
        directiontext2.text = $"Dir: {direction}";
        directiontext1.text = $"Previous: {previousDistance.ToString()}";

        // We are getting further away
        if (distanceDifference > 0.0005f)
        {
            AddReward(-0.01f);
        }
        else if (distanceDifference < 0.0005f && distanceDifference > -0.0005f)
        {
            // We (almost) stood still
            AddReward(-0.001f);
        }
        else
        {
            // We came closer!
            AddReward(0.025f);
        }

        // Optionally: big reward if it catches runner
        if (distance < 3.0f)
        {
            AddReward(50f);
            EndEpisode();
        }
        base.OnActionReceived(actions);
    }

    private void ShootRaycasts()
    {
        for (int i = 0; i < visionRayCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, anglePerVisionRay * i, 0) * transform.forward;     // Calculate direction for each ray

            bool didHit = Physics.Raycast(transform.position, dir, out RaycastHit hit, maxDistance, visionLayerMask);

            if (didHit)
            {
                float visionObs = 1f - (hit.distance * invertedMaxDistance);
                if (hit.collider.tag == "Wall")
                {
                    visionObs *= -1f;
                }

                visionDistanceObs[i] = visionObs;
            }
            else
            {
                visionDistanceObs[i] = 0f;
            }
        }
    }
}