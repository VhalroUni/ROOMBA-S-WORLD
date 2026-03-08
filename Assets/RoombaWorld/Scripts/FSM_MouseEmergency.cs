using FSMs;
using UnityEngine;
using Steerings;

[CreateAssetMenu(fileName = "FSM_MouseEmergency", menuName = "Finite State Machines/FSM_MouseEmergency", order = 1)]
public class FSM_MouseEmergency : FiniteStateMachine
{
    /* Declare here, as attributes, all the variables that need to be shared among
     * states and transitions and/or set in OnEnter or used in OnExit 
     * For instance: steering behaviours, blackboard, ...*/
    private SteeringContext steeringContext;
    private MOUSE_Blackboard blackboard;
    private GoToTarget goToTarget;
    private GameObject currentExit;
    private SpriteRenderer mouse;
    private GameObject roomba;

    public override void OnEnter()
    {
        /* Write here the FSM initialization code. This code is execute every time the FSM is entered.
         * It's equivalent to the on enter action of any state 
         * Usually this code includes .GetComponent<...> invocations */
        steeringContext = GetComponent<SteeringContext>();
        blackboard = GetComponent<MOUSE_Blackboard>();
        goToTarget = GetComponent<GoToTarget>();
        mouse = GetComponent<SpriteRenderer>();
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
        //STAGE 1: create the states with their logic(s)
         FiniteStateMachine DEFAULT = ScriptableObject.CreateInstance<FSM_Mouse>();
         DEFAULT.Name = "DEFAULT";

        State scared = new State("Mouse get scared",
            () => { 
                mouse.color = Color.green;
                steeringContext.maxAcceleration = 480f;
                steeringContext.maxSpeed = 40f;
                goToTarget.target = currentExit;
            }, // write on enter logic inside {}
            () => { }, // write in state logic inside {}
            () => {
                steeringContext.maxAcceleration = 120f;
                steeringContext.maxSpeed = 20f;
                mouse.color = Color.white; 
            }  // write on exit logic inisde {}  
        );

        State die = new State("Mouse died",
            () => { GameObject.Destroy(gameObject); }, // write on enter logic inside {}
            () => { }, // write in state logic inside {}
            () => {  }  // write on exit logic inisde {}  
        );

        /* STAGE 2: create the transitions with their logic(s)
         * ---------------------------------------------------
        */
        Transition roombaDetect = new Transition("Roomba detect",
            () => {return roomba = SensingUtils.FindInstanceWithinRadius(gameObject, "ROOMBA", blackboard.roombaDetectionRadius);
            }, // write the condition checkeing code in {}
            () => { currentExit = LocationHelper.NearestExitPoint(gameObject); }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );

        Transition exitReached = new Transition("Location Reached",
            () => { return SensingUtils.DistanceToTarget(gameObject, currentExit) < blackboard.exitReachedRadius; } // write the condition checkeing code in {}
        );


        /* STAGE 3: add states and transitions to the FSM 
         * ----------------------------------------------
         */
        AddStates(DEFAULT, scared, die);

        AddTransition(DEFAULT, roombaDetect, scared);
        AddTransition(scared, exitReached, die);


        /* STAGE 4: set the initial state
        */
        initialState = DEFAULT;
    }
}