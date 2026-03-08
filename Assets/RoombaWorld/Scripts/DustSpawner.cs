using UnityEngine;

public class DustSpawner : MonoBehaviour
{
    private GameObject sample;
    private float timer;

    [Header("Timing")]
    public float spawnEverySeconds = 5f;

    void Start()
    {
        sample = Resources.Load<GameObject>("DUST");
        if (sample == null)
            Debug.LogError("No DUST prefab found as a resource");

        timer = spawnEverySeconds;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnEverySeconds;

        Vector3 spawnPosition = LocationHelper.RandomWalkableLocation();
        GameObject dust = Instantiate(sample, spawnPosition, Quaternion.identity);

        SpriteRenderer spriteRenderer = dust.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) 
            spriteRenderer.color = Random.ColorHSV();
    }
}