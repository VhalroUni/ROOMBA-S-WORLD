using UnityEngine;
using Steerings;
using Pathfinding;

public class MOUSE_Blackboard : MonoBehaviour
{
    public GameObject pooPrefab;
    public float roombaDetectionRadius = 20f;
    public float locationReachedRadius = 8f;
    public float exitReachedRadius = 6f;

    public float baseMaxSpeed;
    public float baseMaxAccel;

    void Awake()
    {
        pooPrefab = Resources.Load<GameObject>("POO");
    }
}
