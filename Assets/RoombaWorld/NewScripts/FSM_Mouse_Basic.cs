using FSMs;
using UnityEngine;
using Steerings;
using Pathfinding;

[CreateAssetMenu(fileName = "FSM_Mouse_Basic", menuName = "Finite State Machines/FSM_Mouse_Basic", order = 1)]
public class FSM_Mouse_Basic : FiniteStateMachine
{
    private GoToTarget goToTarget;
    private SteeringContext steeringContext;
    private MOUSE_Blackboard blackboard;

    private GameObject roomba;
    private GameObject currentExit;

    // Target temporal per anar a un punt random (GoToTarget espera GameObject)
    private GameObject tempTarget;

    // Velocitats base
    private float baseMaxSpeed;
    private float baseMaxAccel;

    [Header("Random walkable sampling (world bounds)")]
    public Vector2 minBounds = new Vector2(-20, -10);
    public Vector2 maxBounds = new Vector2(20, 10);
    public int maxAttempts = 40;

    [Header("Reached radius (mouse)")]
    public float reachedRadius = 0.8f;

    [Header("Scared speed multipliers (optional)")]
    public float scaredSpeedMult = 2f;
    public float scaredAccelMult = 4f;

    public override void OnEnter()
    {
        goToTarget = GetComponent<GoToTarget>();
        steeringContext = GetComponent<SteeringContext>();
        blackboard = GetComponent<MOUSE_Blackboard>();

        if (goToTarget != null) goToTarget.target = null;

        // IMPORTANT: posa Tag "ROOMBA" a la roomba si vols; si no, busquem per nom
        roomba = GameObject.FindGameObjectWithTag("ROOMBA");
        if (roomba == null) roomba = GameObject.Find("roomba");

        if (steeringContext != null)
        {
            baseMaxSpeed = steeringContext.maxSpeed;
            baseMaxAccel = steeringContext.maxAcceleration;
        }

        base.DisableAllSteerings();
        base.OnEnter();
    }

    public override void OnExit()
    {
        if (goToTarget != null) goToTarget.target = null;
        ResetSpeed();
        base.DisableAllSteerings();
        base.OnExit();
    }

    public override void OnConstruction()
    {
        State goRandom = new State("MOUSE_GO_RANDOM_WALKABLE",
            () => {
                ResetColor();
                ResetSpeed();

                Vector3 p = GetRandomWalkablePosition();
                EnsureTempTarget(p);

                if (goToTarget != null) goToTarget.target = tempTarget;
            },
            () => { },
            () => { }
        );

        State doPoo = new State("MOUSE_DO_POO",
            () => {
                // Instancia poo on és el ratolí
                if (blackboard != null && blackboard.pooPrefab != null)
                {
                    GameObject poo = Object.Instantiate(blackboard.pooPrefab);
                    poo.transform.position = transform.position;
                }

                currentExit = GetRandomExit();
                if (goToTarget != null) goToTarget.target = currentExit;
            },
            () => { },
            () => { }
        );

        State goExitAndDie = new State("MOUSE_GO_EXIT_AND_DIE",
            () => {
                ResetColor();
                ResetSpeed();

                if (currentExit == null) currentExit = GetRandomExit();
                if (goToTarget != null) goToTarget.target = currentExit;
            },
            () => {
                if (Reached(currentExit))
                    Object.Destroy(gameObject);
            },
            () => { }
        );

        // “Emergency” (roomba detected): verd + corre al exit més proper + desapareix
        State scared = new State("MOUSE_SCARED_EMERGENCY",
            () => {
                SetGreen();
                ApplyScaredSpeed();

                currentExit = GetNearestExit();
                if (goToTarget != null) goToTarget.target = currentExit;
            },
            () => {
                // Manté exit més proper
                GameObject nearest = GetNearestExit();
                if (nearest != currentExit)
                {
                    currentExit = nearest;
                    if (goToTarget != null) goToTarget.target = currentExit;
                }

                if (Reached(currentExit))
                    Object.Destroy(gameObject);
            },
            () => {
                ResetSpeed();
                ResetColor();
            }
        );

        // Transicions
        Transition roombaDetected = new Transition("ROOMBA_DETECTED", () => RoombaIsClose());
        Transition reachedRandom = new Transition("REACHED_RANDOM", () => Reached(tempTarget));

        AddStates(goRandom, doPoo, goExitAndDie, scared);

        // Emergency des de qualsevol “normal”
        AddTransition(goRandom, roombaDetected, scared);
        AddTransition(doPoo, roombaDetected, scared);
        AddTransition(goExitAndDie, roombaDetected, scared);

        // Cicle normal
        AddTransition(goRandom, reachedRandom, doPoo);
        AddTransition(doPoo, new Transition("GO_EXIT", () => true), goExitAndDie);

        initialState = goRandom;
    }

    // ---------- Helpers ----------
    private bool RoombaIsClose()
    {
        if (roomba == null || blackboard == null) return false;
        return Vector3.Distance(transform.position, roomba.transform.position) <= blackboard.roombaDetectionRadius;
    }

    private bool Reached(GameObject target)
    {
        if (target == null) return false;

        if (Vector3.Distance(transform.position, target.transform.position) <= reachedRadius)
            return true;

        // Accepta “arribat” si GoToTarget ha acabat ruta
        if (goToTarget != null && goToTarget.routeTerminated())
            return true;

        return false;
    }

    private void EnsureTempTarget(Vector3 pos)
    {
        if (tempTarget == null)
        {
            tempTarget = new GameObject("TEMP_MOUSE_TARGET");
            tempTarget.hideFlags = HideFlags.HideInHierarchy;
        }
        tempTarget.transform.position = pos;
    }

    private Vector3 GetRandomWalkablePosition()
    {
        GridGraph gg = (AstarPath.active != null && AstarPath.active.data != null) ? AstarPath.active.data.gridGraph : null;
        if (gg == null) return transform.position;

        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(minBounds.x, maxBounds.x);
            float y = Random.Range(minBounds.y, maxBounds.y);
            Vector3 candidate = new Vector3(x, y, 0f);

            var nn = gg.GetNearest(candidate);
            if (nn.node != null && nn.node.Walkable)
                return candidate;
        }

        return transform.position;
    }

    private GameObject GetRandomExit()
    {
        GameObject[] exits = GameObject.FindGameObjectsWithTag("ENTRYEXITPOINT");
        if (exits == null || exits.Length == 0) return null;
        return exits[Random.Range(0, exits.Length)];
    }

    private GameObject GetNearestExit()
    {
        GameObject[] exits = GameObject.FindGameObjectsWithTag("ENTRYEXITPOINT");
        if (exits == null || exits.Length == 0) return null;

        GameObject best = null;
        float bestSq = float.PositiveInfinity;
        Vector3 p = transform.position;

        foreach (var e in exits)
        {
            if (e == null) continue;
            float sq = (e.transform.position - p).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = e; }
        }
        return best;
    }

    private void SetGreen()
    {
        var sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.green;
    }

    private void ResetColor()
    {
        var sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }

    private void ApplyScaredSpeed()
    {
        if (steeringContext == null) return;
        steeringContext.maxSpeed = baseMaxSpeed * scaredSpeedMult;
        steeringContext.maxAcceleration = baseMaxAccel * scaredAccelMult;
    }

    private void ResetSpeed()
    {
        if (steeringContext == null) return;
        steeringContext.maxSpeed = baseMaxSpeed;
        steeringContext.maxAcceleration = baseMaxAccel;
    }
}
