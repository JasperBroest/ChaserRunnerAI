using Unity.MLAgents;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Runner runner;
    [SerializeField] private Chaser chaser;

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
        print("Reset");
        chaser.transform.position = new Vector3(8, 0, 8);
        runner.transform.position = new Vector3(-8, 0, -8);
    }

}
