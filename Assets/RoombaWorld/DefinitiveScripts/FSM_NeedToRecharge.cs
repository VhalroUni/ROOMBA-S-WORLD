using FSMs;
using Steerings;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CreateAssetMenu(fileName = "FSM_NeedToRecharge", menuName = "Finite State Machines/FSM_NeedToRecharge", order = 1)]
public class FSM_NeedToRecharge : FiniteStateMachine
{
    /* Declare here, as attributes, all the variables that need to be shared among
     * states and transitions and/or set in OnEnter or used in OnExit 
     * For instance: steering behaviours, blackboard, ...*/
    SteeringContext steeringContext;
    ROOMBA_Blackboard blackboard;
    GoToTarget goToTarget;

    private GameObject stationTarget;

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

        FiniteStateMachine CLEAN_POO = ScriptableObject.CreateInstance<FSM_CleanPoo>();
        CLEAN_POO.Name = "Clean poo";

        State goToCharge = new State("Roomba is going to charge station",
            () => {
                stationTarget = SensingUtils.FindInstanceWithinRadius(gameObject, "ENERGY", blackboard.chargingStationDetectionRadius);
                goToTarget.target = stationTarget;
            }, // write on enter logic inside {}
            () => { }, // write in state logic inside {}
            () => { }  // write on exit logic inisde {}  
        );

        State charging = new State("Roomba is charging",
            () => { 
                goToTarget.target = null; 
                blackboard.startRecharging();
            }, // write on enter logic inside {}
            () => { blackboard.currentCharge += blackboard.energyRechargePerSecond * Time.deltaTime; }, // write in state logic inside {}
            () => { blackboard.stopRecharging(); }  // write on exit logic inisde {}  
        );




        /* STAGE 2: create the transitions with their logic(s)
         * ---------------------------------------------------*/

        Transition batteryLow = new Transition("Battery is low",
            () => { return blackboard.EnergyIsLow(); }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );

        Transition fullyCharged = new Transition("Battery is 100%",
            () => { return blackboard.EnergyIsFull(); }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );

        Transition stationReached = new Transition("Station is reached",
            () => { return SensingUtils.DistanceToTarget(gameObject, stationTarget) <= blackboard.chargingStationReachedRadius; }, // write the condition checkeing code in {}
            () => { }  // write the on trigger code in {} if any. Remove line if no on trigger action needed
        );




        /* STAGE 3: add states and transitions to the FSM 
         * ---------------------------------------------- */
            
        AddStates(CLEAN_POO, goToCharge, charging);

        AddTransition(CLEAN_POO, batteryLow, goToCharge);
        AddTransition(goToCharge, stationReached, charging);
        AddTransition(charging, fullyCharged, CLEAN_POO);




        /* STAGE 4: set the initial state*/

        initialState = CLEAN_POO;

         

    }
}
