using FSMs;
using UnityEngine;
using Steerings;

[CreateAssetMenu(fileName = "FSM_CleanDust", menuName = "Finite State Machines/FSM_CleanDust", order = 1)]
public class FSM_CleanDust : FiniteStateMachine
{
    /* Declare here, as attributes, all the variables that need to be shared among
     * states and transitions and/or set in OnEnter or used in OnExit 
     * For instance: steering behaviours, blackboard, ...*/

    ROOMBA_Blackboard blackboard;
    GoToTarget goToTarget;
    SteeringContext steeringContext;
    private GameObject currentDust;

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
        goToTarget.target = null;
        base.OnExit();
    }

    public override void OnConstruction()
    {
        /* STAGE 1: create the states with their logic(s)
         *-----------------------------------------------*/
         
        FiniteStateMachine PATROL = ScriptableObject.CreateInstance<FSM_RoombaPatrol>();
        PATROL.Name = "PATROL";

        State goToCleanDust = new State("Go To Dust",
            () => { 
                ResetSpeed();
                goToTarget.target = currentDust;
            }, // write on enter logic inside {}
            () => {
                float distance = SensingUtils.DistanceToTarget(gameObject, currentDust);
                Debug.Log("Distance to dust: " + distance);
            }, // write in state logic inside {}
            () => { GameObject.Destroy(currentDust); }  // write on exit logic inisde {}
        );

        /* STAGE 2: create the transitions with their logic(s)
         * ---------------------------------------------------*/

        Transition dustDetected = new Transition("Roomba detected dust",
            () => { return currentDust = SensingUtils.FindInstanceWithinRadius(gameObject, "DUST", blackboard.dustDetectionRadius); }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );

        Transition dustReached = new Transition("Roomba reached the dust",
            () => { return SensingUtils.DistanceToTarget(gameObject, currentDust) <= blackboard.dustReachedRadius; }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );


        /* STAGE 3: add states and transitions to the FSM 
         * ----------------------------------------------*/

        AddStates(PATROL, goToCleanDust);

        AddTransition(PATROL, dustDetected, goToCleanDust);
        AddTransition(goToCleanDust, dustReached, PATROL);

        /* STAGE 4: set the initial state*/

        initialState = PATROL;

    }

    private void ResetSpeed()
    {
        if (steeringContext == null) return;
        steeringContext.maxSpeed = blackboard.baseMaxSpeed;
        steeringContext.maxAcceleration = blackboard.baseMaxAccel;
    }
}
