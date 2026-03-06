using FSMs;
using UnityEngine;
using Steerings;

[CreateAssetMenu(fileName = "FSM_Roomba_Basic", menuName = "Finite State Machines/FSM_Roomba_Basic", order = 1)]
public class FSM_Roomba_Basic : FiniteStateMachine
{
private GoToTarget goToTarget;
    private SteeringContext steeringContext;
    private ROOMBA_Blackboard blackboard;

    private float baseMaxSpeed;
    private float baseMaxAccel;

    private GameObject currentPatrolPoint;
    private GameObject currentPoo;
    private GameObject currentDust;
    private GameObject currentChargingStation;

    private float pooCleanTimer;

    [Header("Fast multipliers (poo urgency)")]
    public float fastSpeedMult = 2f; // x2
    public float fastAccelMult = 4f; // x4

    public override void OnEnter()
    {
        goToTarget = GetComponent<GoToTarget>();
        steeringContext = GetComponent<SteeringContext>();
        blackboard = GetComponent<ROOMBA_Blackboard>();

        if (goToTarget != null) goToTarget.target = null;

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

        // per si de cas
        if (blackboard != null)
        {
            blackboard.StopSpinning();
            blackboard.stopRecharging();
        }

        base.DisableAllSteerings();
        base.OnExit();
    }

    public override void OnConstruction()
    {
        // ---------------- EMERGENCY: LOW ENERGY ----------------
        State goCharge = new State("EMERGENCY_GO_CHARGE",
            () => {
                ResetSpeed();
                currentChargingStation = GetNearestWithTag("ENERGY");
                if (goToTarget != null) goToTarget.target = currentChargingStation;
            },
            () => {
                // si no n'hi ha, reintenta
                if (currentChargingStation == null)
                {
                    currentChargingStation = GetNearestWithTag("ENERGY");
                    if (goToTarget != null) goToTarget.target = currentChargingStation;
                }
            },
            () => { }
        );

        State recharging = new State("EMERGENCY_RECHARGING",
            () => {
                if (goToTarget != null) goToTarget.target = null;
                ResetSpeed();
                if (blackboard != null) blackboard.startRecharging();
            },
            () => { },
            () => {
                if (blackboard != null) blackboard.stopRecharging();
            }
        );

        // ---------------- NORMAL: PATROL ----------------
        State patrol = new State("NORMAL_PATROL",
            () => {
                ResetSpeed();
                currentPatrolPoint = GetRandomWithTag("PATROLPOINT");
                if (goToTarget != null) goToTarget.target = currentPatrolPoint;
            },
            () => {
                if (Reached(currentPatrolPoint, (blackboard != null) ? blackboard.dustReachedRadius : 5f))
                {
                    currentPatrolPoint = GetRandomWithTag("PATROLPOINT");
                    if (goToTarget != null) goToTarget.target = currentPatrolPoint;
                }
            },
            () => { }
        );

        // ---------------- NORMAL: GO POO FAST ----------------
        State goPooFast = new State("NORMAL_GO_POO_FAST",
            () => {
                ApplyFastSpeed();
                currentPoo = GetNearestClose("POO", GetPooDetectionRadius());
                if (goToTarget != null) goToTarget.target = currentPoo;
            },
            () => {
                // si apareix una caca més propera mentre hi vas -> canvia
                GameObject nearer = GetNearestClose("POO", GetPooDetectionRadius());
                if (nearer != null && nearer != currentPoo)
                {
                    currentPoo = nearer;
                    if (goToTarget != null) goToTarget.target = currentPoo;
                }
            },
            () => { }
        );

        // ---------------- NORMAL: SPIN CLEAN POO ----------------
        State spinCleanPoo = new State("NORMAL_SPIN_CLEAN_POO",
            () => {
                if (goToTarget != null) goToTarget.target = null;
                ResetSpeed();

                pooCleanTimer = (blackboard != null) ? blackboard.pooCleaningTime : 2f;
                if (blackboard != null) blackboard.StartSpinning();
            },
            () => {
                pooCleanTimer -= Time.deltaTime;

                if (pooCleanTimer <= 0f)
                {
                    // neteja la caca (si encara existeix)
                    if (currentPoo != null)
                        Object.Destroy(currentPoo);

                    currentPoo = null;
                }
            },
            () => {
                if (blackboard != null) blackboard.StopSpinning();
            }
        );

        // ---------------- NORMAL: GO DUST ----------------
        State goDust = new State("NORMAL_GO_DUST",
            () => {
                ResetSpeed();
                currentDust = GetNearestClose("DUST", GetDustDetectionRadius());
                if (goToTarget != null) goToTarget.target = currentDust;
            },
            () => {
                // si arribo a la pols -> la destrueixo
                float rr = (blackboard != null) ? blackboard.dustReachedRadius : 5f;
                if (Reached(currentDust, rr))
                {
                    if (currentDust != null) Object.Destroy(currentDust);
                    currentDust = null;
                }
            },
            () => { }
        );

        // ---------------- TRANSITIONS ----------------
        // Emergency priority
        Transition lowEnergy = new Transition("LOW_ENERGY", () => (blackboard != null) && blackboard.EnergyIsLow());
        Transition energyFull = new Transition("ENERGY_FULL", () => (blackboard != null) && blackboard.EnergyIsFull());
        Transition reachedStation = new Transition("REACHED_STATION",
            () => Reached(currentChargingStation, (blackboard != null) ? blackboard.chargingStationReachedRadius : 4f)
        );

        // Normal priority: poo > dust > patrol
        Transition closePoo = new Transition("CLOSE_POO", () => GetNearestClose("POO", GetPooDetectionRadius()) != null);
        Transition closeDust = new Transition("CLOSE_DUST", () => GetNearestClose("DUST", GetDustDetectionRadius()) != null);

        Transition reachedPoo = new Transition("REACHED_POO",
            () => currentPoo != null && Reached(currentPoo, (blackboard != null) ? blackboard.pooReachedRadius : 5f)
        );

        Transition spinDone = new Transition("SPIN_DONE", () => currentPoo == null);

        Transition dustDone = new Transition("DUST_DONE", () => currentDust == null);

        // ---------------- WIRING ----------------
        AddStates(goCharge, recharging, patrol, goPooFast, spinCleanPoo, goDust);

        // Emergency from any normal state
        AddTransition(patrol, lowEnergy, goCharge);
        AddTransition(goDust, lowEnergy, goCharge);
        AddTransition(goPooFast, lowEnergy, goCharge);
        AddTransition(spinCleanPoo, lowEnergy, goCharge);

        // Emergency flow
        AddTransition(goCharge, reachedStation, recharging);
        AddTransition(recharging, energyFull, patrol);

        // Normal decisions
        AddTransition(patrol, closePoo, goPooFast);
        AddTransition(patrol, closeDust, goDust);

        // If going to dust and poo appears -> poo
        AddTransition(goDust, closePoo, goPooFast);

        // Poo flow
        AddTransition(goPooFast, reachedPoo, spinCleanPoo);
        AddTransition(spinCleanPoo, spinDone, patrol);

        // Dust flow
        AddTransition(goDust, dustDone, patrol);

        initialState = patrol;
    }

    // ---------- Helpers ----------
    private bool Reached(GameObject target, float radius)
    {
        if (target == null) return false;

        if (Vector3.Distance(transform.position, target.transform.position) <= radius)
            return true;

        if (goToTarget != null && goToTarget.routeTerminated())
            return true;

        return false;
    }

    private void ApplyFastSpeed()
    {
        if (steeringContext == null) return;
        steeringContext.maxSpeed = baseMaxSpeed * fastSpeedMult;
        steeringContext.maxAcceleration = baseMaxAccel * fastAccelMult;
    }

    private void ResetSpeed()
    {
        if (steeringContext == null) return;
        steeringContext.maxSpeed = baseMaxSpeed;
        steeringContext.maxAcceleration = baseMaxAccel;
    }

    private float GetDustDetectionRadius() => (blackboard != null) ? blackboard.dustDetectionRadius : 60f;
    private float GetPooDetectionRadius() => (blackboard != null) ? blackboard.pooDetectionRadius : 150f;

    private GameObject GetRandomWithTag(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        if (objs == null || objs.Length == 0) return null;
        return objs[Random.Range(0, objs.Length)];
    }

    private GameObject GetNearestWithTag(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        if (objs == null || objs.Length == 0) return null;

        GameObject best = null;
        float bestSq = float.PositiveInfinity;
        Vector3 p = transform.position;

        foreach (var o in objs)
        {
            if (o == null) continue;
            float sq = (o.transform.position - p).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = o; }
        }
        return best;
    }

    private GameObject GetNearestClose(string tag, float radius)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        if (objs == null || objs.Length == 0) return null;

        float rSq = radius * radius;

        GameObject best = null;
        float bestSq = float.PositiveInfinity;
        Vector3 p = transform.position;

        foreach (var o in objs)
        {
            if (o == null) continue;
            float sq = (o.transform.position - p).sqrMagnitude;
            if (sq <= rSq && sq < bestSq)
            {
                bestSq = sq;
                best = o;
            }
        }
        return best;
    }
}
