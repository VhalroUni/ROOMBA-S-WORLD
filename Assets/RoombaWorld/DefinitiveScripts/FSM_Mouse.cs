using FSMs;
using UnityEngine;
using Steerings;

[CreateAssetMenu(fileName = "FSM_Mouse", menuName = "Finite State Machines/FSM_Mouse", order = 1)]
public class FSM_Mouse : FiniteStateMachine
{
    /* Declare here, as attributes, all the variables that need to be shared among
     * states and transitions and/or set in OnEnter or used in OnExit 
     * For instance: steering behaviours, blackboard, ...*/
    public GameObject mouse;
    private GoToTarget goToTarget;
    private SteeringContext steeringContext;
    private MOUSE_Blackboard blackboard;

    private GameObject currentPatrolPoint;
    private GameObject currentExit;


    public override void OnEnter()
    {
        /* Write here the FSM initialization code. This code is execute every time the FSM is entered.
         * It's equivalent to the on enter action of any state 
         * Usually this code includes .GetComponent<...> invocations */
        mouse = GetComponent<GameObject>();
        goToTarget = GetComponent<GoToTarget>();
        steeringContext = GetComponent<SteeringContext>();
        blackboard = GetComponent<MOUSE_Blackboard>();
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
         *-----------------------------------------------
         */

        State goRandom = new State("Mouse go random walkable location",
            () => { 
                currentPatrolPoint = LocationHelper.RandomPatrolPoint(); 
                goToTarget.target = currentPatrolPoint; 
                goToTarget.enabled = true; }, // write on enter logic inside {}
            () => { }, // write in state logic inside {}
            () => { goToTarget.target = null; goToTarget.enabled = false; }  // write on exit logic inisde {}  
        );

        State doPoo = new State("Mouse do poo",
           () => { Instantiate(blackboard.pooPrefab, transform.position, Quaternion.identity); }, // write on enter logic inside {}
           () => { }, // write in state logic inside {}
           () => { }  // write on exit logic inisde {}  
       );

        State entryAndExit = new State("Mouse exit the scene",
           () => { currentExit = LocationHelper.RandomEntryExitPoint(); goToTarget.target = currentExit; }, // write on enter logic inside {}
           () => { }, // write in state logic inside {}
           () => { Object.Destroy(gameObject); }  // write on exit logic inisde {}  
       );


        /* STAGE 2: create the transitions with their logic(s)
         * ---------------------------------------------------
        */
        Transition locationReached = new Transition("Location Reached",
            () => { return SensingUtils.DistanceToTarget(gameObject, currentPatrolPoint) < blackboard.locationReachedRadius; }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );

        Transition exitReached = new Transition("Location Reached",
            () => { return SensingUtils.DistanceToTarget(gameObject, currentExit) < blackboard.exitReachedRadius; }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );


        /* STAGE 3: add states and transitions to the FSM 
         * ----------------------------------------------
                 */

        AddStates(goRandom, doPoo, entryAndExit);

        AddTransition(goRandom, locationReached, doPoo);
        AddTransition(doPoo, exitReached, entryAndExit);


        /* STAGE 4: set the initial state
         */
        initialState = goRandom;
    }
}
