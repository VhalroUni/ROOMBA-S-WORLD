using UnityEngine;

public class MouseSpawner : MonoBehaviour
{
    [Header("Times")]
    public float baseSeconds = 30f;
    public float plusMinusSeconds = 20f;

    [Header("EntryExit")]
    public Transform entryExitRoot;

    private GameObject mousePrefab;
    private float timer;

    void Start()
    {
        mousePrefab = Resources.Load<GameObject>("MOUSE");
        ResetTimer();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        SpawnMouse();
        ResetTimer();
    }

    private void ResetTimer()
    {
        timer = Random.Range(baseSeconds - plusMinusSeconds, baseSeconds + plusMinusSeconds);
        if (timer < 0.1f) timer = 0.1f;
    }

    private void SpawnMouse()
    {
        int idx = Random.Range(0, entryExitRoot.childCount);
        Transform spawnPoint = entryExitRoot.GetChild(idx);

        Instantiate(mousePrefab, spawnPoint.position, Quaternion.identity);
    }
}