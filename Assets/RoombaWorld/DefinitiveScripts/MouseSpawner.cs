using UnityEngine;

public class MouseSpawner : MonoBehaviour
{
    private GameObject mousePrefab;
    private float timer;

    [Header("Times")]
    public float baseSeconds = 30f;
    public float plusMinusSeconds = 20f;

    void Start()
    {
        mousePrefab = Resources.Load<GameObject>("MOUSE");
        if (mousePrefab == null)
            Debug.LogError("No MOUSE prefab found as a resource");

        ResetTimer();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        GameObject randomPoint = LocationHelper.RandomEntryExitPoint();

        Instantiate(mousePrefab, randomPoint.transform.position, Quaternion.identity);

        ResetTimer();
    }

    private void ResetTimer()
    {
        timer = Random.Range(baseSeconds - plusMinusSeconds, baseSeconds + plusMinusSeconds);

        if (timer < 0.1f) 
            timer = 0.1f;
    }
}