using Pathfinding;
using UnityEngine;

public class DustSpawnerBasic : MonoBehaviour
{
    [Header("Timing")]
    public float spawnEverySeconds = 5f;

    [Header("Spawn bounds")]
    public Vector2 minBounds = new Vector2(-20, -10);
    public Vector2 maxBounds = new Vector2(20, 10);

    public int maxAttempts = 40;

    private GameObject dustPrefab;
    private ROOMBA_Blackboard blackboard;
    private float timer;

    void Start()
    {
        dustPrefab = Resources.Load<GameObject>("DUST");
        timer = spawnEverySeconds;
        blackboard = GameObject.Find("Roomba").GetComponent<ROOMBA_Blackboard>();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnEverySeconds;

        if (!TryGetRandomWalkablePosition(out Vector3 pos)) return;

        GameObject dust = Instantiate(dustPrefab, pos, Quaternion.identity);

        var sr = dust.GetComponent<SpriteRenderer>();
        if (sr == null) sr = dust.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Random.ColorHSV();
    }

    private bool TryGetRandomWalkablePosition(out Vector3 position)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(minBounds.x, maxBounds.x);
            float y = Random.Range(minBounds.y, maxBounds.y);
            Vector3 p = new Vector3(x, y, 0f);

            if (Walkable(p))
            {
                position = p;
                return true;
            }
        }

        position = default;
        return false;
    }

    private static bool Walkable(Vector3 position)
    {
        var active = AstarPath.active;
        if (active == null || active.data == null) return false;

        GridGraph gg = active.data.gridGraph;
        if (gg == null) return false;

        NNInfoInternal nn = gg.GetNearest(position);
        return nn.node != null && nn.node.Walkable;
    }
}