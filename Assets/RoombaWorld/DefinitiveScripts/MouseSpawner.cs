using UnityEngine;

public class MouseSpawner : MonoBehaviour
{
    private GameObject sample;
    private GameObject mousePrefab;
    private float timer;

    [Header("Times")]
    public float baseSeconds = 30f;
    public float plusMinusSeconds = 20f;

    void Start()
    {
        sample = Resources.Load<GameObject>("MOUSE");
        if (sample == null)
            Debug.LogError("No MOUSE prefab found as a resource");

        mousePrefab = LocationHelper.RandomEntryExitPoint();
        ResetTimer();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        mousePrefab = LocationHelper.RandomEntryExitPoint();
        ResetTimer();
    }

    private void ResetTimer()
    {
        timer = Random.Range(baseSeconds - plusMinusSeconds, baseSeconds + plusMinusSeconds);

        if (timer < 0.1f) timer = 0.1f;
    }
}
