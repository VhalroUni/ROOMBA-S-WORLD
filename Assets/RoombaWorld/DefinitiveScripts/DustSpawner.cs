using UnityEngine;

public class DustSpawner : MonoBehaviour
{
    private GameObject sample;
    private Vector3 dustPrefab;
    private float timer;
    private SpriteRenderer spriteRenderer;

    [Header("Timing")]
    public float spawnEverySeconds = 5f;

    void Start()
    {
        sample = Resources.Load<GameObject>("DUST");
        if (sample == null)
            Debug.LogError("No DUST prefab found as a resource");

        dustPrefab = LocationHelper.RandomWalkableLocation();
        timer = spawnEverySeconds;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnEverySeconds;

        GameObject dust = Instantiate(sample, dustPrefab, Quaternion.identity);

        if (spriteRenderer != null) spriteRenderer.color = Random.ColorHSV();
    }
}
