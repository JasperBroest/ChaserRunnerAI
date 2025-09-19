using Unity.MLAgents;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Runner runner;
    [SerializeField] private Chaser chaser;

    float minSpawnDistance = 5f;


    private void Awake()
    {
        Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
    }

    public void FixedUpdate()
    {
        Academy.Instance.EnvironmentStep();
    }

    public void OnEnvironmentReset()
    {
        

        chaser.transform.position = GenerateRandomPosition();
        runner.transform.position = GenerateRunnerPosition();
    }

    private Vector3 GenerateRandomPosition()
    {
        float x = Random.Range(-12, 12);
        float z = Random.Range(-12, 12);

        return new Vector3(x, 0, z);
    }

    private Vector3 GenerateRunnerPosition()
    {
        Vector3 position;
        do
        {
            position = GenerateRandomPosition();

        } while (Vector3.Distance(position, chaser.transform.position) < minSpawnDistance);

        return position;
    }
}