using FSMs;
using UnityEngine;
using Steerings;

[CreateAssetMenu(fileName = "FSM_RoombaPatrol", menuName = "Finite State Machines/FSM_RoombaPatrol", order = 1)]
public class FSM_RoombaPatrol : FiniteStateMachine
{
    /* Declare here, as attributes, all the variables that need to be shared among
     * states and transitions and/or set in OnEnter or used in OnExit 
     * For instance: steering behaviours, blackboard, ...*/

    private GoToTarget goToTarget;
    private SteeringContext steeringContext;
    private ROOMBA_Blackboard blackboard;

    private GameObject currentPatrolPoint;

    public override void OnEnter()
    {
        /* Write here the FSM initialization code. This code is execute every time the FSM is entered.
         * It's equivalent to the on enter action of any state 
         * Usually this code includes .GetComponent<...> invocations */
        goToTarget = GetComponent<GoToTarget>();
        steeringContext = GetComponent<SteeringContext>();
        blackboard = GetComponent<ROOMBA_Blackboard>();

        blackboard.baseMaxSpeed = steeringContext.maxSpeed;
        blackboard.baseMaxAccel = steeringContext.maxAcceleration;
        
        base.DisableAllSteerings();
        base.OnEnter(); // do not remove
    }

    public override void OnExit()
    {
        /* Write here the FSM exiting code. This code is execute every time the FSM is exited.
         * It's equivalent to the on exit action of any state 
         * Usually this code turns off behaviours that shouldn't be on when one the FSM has
         * been exited. */

        ResetSpeed();
        base.DisableAllSteerings();
        base.OnExit();
    }

    public override void OnConstruction()
    {
        /* STAGE 1: create the states with their logic(s)
         *-----------------------------------------------*/
        
        State patrolling = new State("Patrolling",
            () => {
                ResetSpeed();
                currentPatrolPoint = LocationHelper.RandomPatrolPoint();
                goToTarget.target = currentPatrolPoint;
            }, // write on enter logic inside {}
            () => { }, // write in state logic inside {}
            () => { }  // write on exit logic inisde {}
        );

         


        /* STAGE 2: create the transitions with their logic(s)
         * ---------------------------------------------------*/

        Transition patrolReached = new Transition("Patrol point reached",
            () => { return goToTarget.routeTerminated();}, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );

       


        /* STAGE 3: add states and transitions to the FSM 
         * ----------------------------------------------*/
            
        AddStates(patrolling);

        AddTransition(patrolling, patrolReached, patrolling);




        /* STAGE 4: set the initial state */

        initialState = patrolling;

        

    }

    /* STAGE 5: Add more functions to help the fsm */
    private void ResetSpeed()
    {
        if (steeringContext == null) return;
        steeringContext.maxSpeed = blackboard.baseMaxSpeed;
        steeringContext.maxAcceleration = blackboard.baseMaxAccel;
    }
}
