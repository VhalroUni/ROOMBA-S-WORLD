using FSMs;
using Steerings;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CreateAssetMenu(fileName = "FSM_CleanPoo", menuName = "Finite State Machines/FSM_CleanPoo", order = 1)]
public class FSM_CleanPoo : FiniteStateMachine
{
    /* Declare here, as attributes, all the variables that need to be shared among
     * states and transitions and/or set in OnEnter or used in OnExit 
     * For instance: steering behaviours, blackboard, ...*/

    SteeringContext steeringContext;
    ROOMBA_Blackboard blackboard;
    GoToTarget goToTarget;

    private float timer;
    private GameObject targetPoo;
    private GameObject otherPoo;

    [Header("Fast multipliers (poo urgency)")]
    public float fastSpeedMult = 2f; // x2
    public float fastAccelMult = 4f; // x4

    public override void OnEnter()
    {
        /* Write here the FSM initialization code. This code is execute every time the FSM is entered.
         * It's equivalent to the on enter action of any state 
         * Usually this code includes .GetComponent<...> invocations */

        blackboard = GetComponent<ROOMBA_Blackboard>();
        goToTarget = GetComponent<GoToTarget>();
        steeringContext = GetComponent<SteeringContext>();
        base.OnEnter(); // do not remove
    }

    public override void OnExit()
    {
        /* Write here the FSM exiting code. This code is execute every time the FSM is exited.
         * It's equivalent to the on exit action of any state 
         * Usually this code turns off behaviours that shouldn't be on when one the FSM has
         * been exited. */
        base.OnExit();
    }

    public override void OnConstruction()
    {
        /* STAGE 1: create the states with their logic(s)
         *-----------------------------------------------*/

        FiniteStateMachine CLEAN_DUST = ScriptableObject.CreateInstance<FSM_CleanDust>();
        CLEAN_DUST.Name = "Clean Dust";

        State goToCleanPoo = new State("romba goes to clean poo",
            () => {
                ApplyFastSpeed();
            }, // write on enter logic inside {}
            () => {
                goToTarget.target = targetPoo;
                float distanceToFirstTarget = SensingUtils.DistanceToTarget(gameObject, targetPoo);
                otherPoo = SensingUtils.FindInstanceWithinRadius(gameObject, "POO", blackboard.dustDetectionRadius);
                float distanceToOtherTarget = SensingUtils.DistanceToTarget(gameObject, otherPoo);
                if (distanceToFirstTarget > distanceToOtherTarget) targetPoo = otherPoo;
            }, // write in state logic inside {}
            () => { }  // write on exit logic inisde {}  
        );

        State cleaningPoo = new State("romba is cleaning poo",
            () => {
                goToTarget.target = null;
                blackboard.StartSpinning();
                timer = 0;
            }, // write on enter logic inside {}
            () => { timer += Time.deltaTime; }, // write in state logic inside {}
            () => { 
                GameObject.Destroy(targetPoo);
                blackboard.StopSpinning();
                ResetSpeed();
            }  // write on exit logic inisde {}  
        );


        /* STAGE 2: create the transitions with their logic(s)
         * ---------------------------------------------------*/

        Transition pooDeteceted = new Transition("Roomba detected a poo",
            () => { return targetPoo = SensingUtils.FindRandomInstanceWithinRadius(gameObject, "POO", blackboard.pooDetectionRadius); }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );

        Transition pooReached = new Transition("Roomba reached the poo",
            () => { return SensingUtils.DistanceToTarget(gameObject, targetPoo) <= blackboard.pooReachedRadius; }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );

        Transition pooDesapeared = new Transition("Roomba cleaned the poo",
            () => { return timer >= blackboard.pooCleaningTime; }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );


        /* STAGE 3: add states and transitions to the FSM 
         * ----------------------------------------------*/

        AddStates(CLEAN_DUST, goToCleanPoo, cleaningPoo);

        AddTransition(CLEAN_DUST, pooDeteceted, goToCleanPoo);
        AddTransition(goToCleanPoo, pooReached, cleaningPoo);
        AddTransition(cleaningPoo, pooDesapeared, CLEAN_DUST);




        /* STAGE 4: set the initial state*/

        initialState = CLEAN_DUST;

         

    }

    private void ResetSpeed()
    {
        if (steeringContext == null) return;
        steeringContext.maxSpeed = blackboard.baseMaxSpeed;
        steeringContext.maxAcceleration = blackboard.baseMaxAccel;
    }

    private void ApplyFastSpeed()
    {
        if (steeringContext == null) return;
        steeringContext.maxSpeed = blackboard.baseMaxSpeed * fastSpeedMult;
        steeringContext.maxAcceleration = blackboard.baseMaxAccel * fastAccelMult;
    }
}
