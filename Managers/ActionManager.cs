using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Anthology.Models
{
    /// <summary>
    /// Manages a container of actions throughout the lifetime of the simulation.
    /// </summary>
    public static class ActionManager
    {
        /// <summary>
        /// Actions available in the simulation.
        /// </summary>
		public static Dictionary<string, Action> Actions {get; set;} = new();
		// public static ActionContainer Actions { get; set; } = new();

        /// <summary>
        /// Compiled list of all actions of both scheduled and primary actions.
        /// </summary>
        // public static List<Action> AllActions { get; set; } = new();

        /// <summary>
        /// Initializes/resets all action manager variables.
        /// </summary>
        /// <param name="path">Path of actions JSON file.</param>
        public static void Init(string path)
        {
			Actions.Clear();
            World.ReadWrite.LoadActionsFromFile(path);
        }

        /// <summary>
        /// Clears all actions from the system.
        /// </summary>
        public static void Reset()
        {
            // Actions.ScheduleActions.Clear();
            // Actions.PrimaryActions.Clear();
            Actions.Clear();
        }

        /// <summary>
        /// Adds the given action to both action structures.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public static void AddAction(Action action)
        {
            Actions[action.Name] = action;
        }

        /// <summary>
        /// Retrieves an action with the specified name from the set of actions available in the simulation.
        /// </summary>
        /// <param name="actionName">The name of the action to find.</param>
        /// <returns>The action with specified name.</returns>
        /// <exception cref="Exception">Thrown when action cannot be found.</exception>
        public static Action GetActionByName(string actionName)
        {
			if(Actions.ContainsKey(actionName)){
				return Actions[actionName];
			}
            throw new Exception("Action with name: " + actionName + " cannot be found.");
        }

        /// <summary>
        /// Returns the net effect for an action for a specific agent.
        /// Takes into account the agent's current motivation statuses.
        /// </summary>
        /// <param name="agent">The agent relevant to retrieve motives from.</param>
        /// <param name="action">The action to calculate net effect.</param>
        /// <returns>How much to affect motive by.</returns>
        public static float GetEffectDeltaForAgentAction(Agent agent, Action action, LocationNode nearestLocation){
			float travelTime = LocationManager.DistanceMatrix[agent.CurrentLocation, nearestLocation];
			float denom = action.MinTime + travelTime;
			float deltaUtility = action.Effects.GetEffectDeltaForEffects(agent);
			
			if (denom > 0) deltaUtility /= denom;

			return deltaUtility;
        }

		public static void StartActionOnTargets(Agent actor, List<Agent>targets){

		}
		
		/// <summary>
        /// Starts an action (if the agent is at a location where the action can be performed).
        /// Else, makes the agent travel to a suitable location to perform the action.
        /// </summary>
        public static void StartAction(Agent agent)
        {
            Action action = agent.CurrentAction.First.Value;

			// if (action.Name == "wait_action" || action.Name == "travel_action"){
			// 	return;
			// }

			List<RMotive> rMotives = action.Requirements.Motives;
			List<RLocation> rLocations = action.Requirements.Locations;
			List<RPeople> rPeople = action.Requirements.People;

			bool perform = true; 
			if(rMotives!=null  && !AgentManager.AgentSatisfiesMotiveRequirement(agent, rMotives)){
				UnityEngine.Debug.Log(agent.Name + ": unable to perform action:" + action.Name + " because Motive requirements failed.");
				perform = false;
			}
			if (rLocations!=null && !LocationManager.LocationSatisfiesLocationRequirement(agent.CurrentLocation, rLocations[0])){
				UnityEngine.Debug.Log(agent.Name + ": unable to perform action:" + action.Name + " because Location requirements at " + agent.CurrentLocation.Name + "failed.");
				perform = false;
			}
			if (rPeople!=null && !LocationManager.LocationSatisfiesPeopleRequirement(agent, agent.CurrentLocation, rPeople[0])){
				UnityEngine.Debug.Log(agent.Name + ": unable to perform action:" + action.Name + " because Location requirements failed.");
				perform = false;
			}
			
			if(perform){
				agent.OccupiedCounter = action.MinTime;
			}
			else{
				agent.Destination = new();
				agent.OccupiedCounter = 0;
				agent.CurrentAction.RemoveFirst();
			}
            
            // if (action is ScheduleAction)
            // {
			// 	ScheduleAction schedAct = (ScheduleAction)action;
            //     schedAct.CurrentTargets.Clear();
            //     foreach (string name in agent.CurrentLocation.AgentsPresent)
            //     {
            //         schedAct.CurrentTargets.Add(AgentManager.GetAgentByName(name));
            //     }
            // }}
		}

		/// <summary>
        /// Starts travel to the agent's destination.
        /// </summary>
        /// <param name="destination">The agent's destination.</param>
        /// <param name="time">The time in which the agent started traveling.</param>
        
		public static void StartTravelToLocation(Agent agent, LocationNode destination)
        {
			agent.CurrentAction.AddFirst(ActionManager.GetActionByName("travel_action"));
			List<LocationNode> path = LocationManager.FindPathsBetween(agent.CurrentLocation, destination);
			agent.Destination = path;
			agent.OccupiedCounter = path.Count;
			UnityEngine.Debug.Log(agent.Name + " starting travel to:" + destination.Name);
			Action _currentAction = agent.CurrentAction.First.Value;
        }

        /// <summary>
        /// Moves closer to the agent's destination.
        /// Uses the manhattan distance to move the agent, so either moves along the x or y axis during any tick.
        /// </summary>
        public static void MoveCloserToDestination(Agent agent)
        {
            if (agent.Destination.Count == 0){
				agent.OccupiedCounter = 0;
				agent.CurrentAction.RemoveFirst();
				return;
			}
			agent.CurrentLocation = agent.Destination[0]; 
			agent.OccupiedCounter--;                
			agent.Destination.RemoveAt(0);
        }

		/// <summary>
        /// Applies the effect of an action to this agent.
        /// </summary>
        public static void ExecuteAction(Agent agent) {
			agent.Destination = new();
            agent.OccupiedCounter = 0;

			// Execute the first action queued up
			Action action = agent.CurrentAction.First.Value;
			UnityEngine.Debug.Log(agent.Name + " executing action:" + action.Name);
			action.Effects.ApplyActionEffects(agent);
			agent.CurrentAction.RemoveFirst();

			if (!agent.Motives.IsContent()){
				if (agent.CurrentAction.Count == 0){
					ActionManager.SelectNextAction(agent);
				}
				else {
					ActionManager.StartAction(agent);
				}
			}
        }

		/// <summary>
        /// Selects an action from a set of valid actions to be performed by this agent.
        /// Selects the action with the maximal utility of the agent (motive increase / time).
        /// </summary>
		public static void SelectNextAction(Agent agent){
			float maxDeltaUtility = -100f;
			// List<Tuple<Action, LocationNode>> choice = new();

			// SelectRandomAction(agent);
			// SelectRandomDestination(agent);
			List<Action> currentChoice = new();
            List<LocationNode> currentDest = new();
			List<string> actionSelectLog = new();

			foreach(Action action in ActionManager.Actions.Values)
            {
                if (action.Hidden) continue;
                actionSelectLog.Add("Action: " + action.Name);

                float travelTime;
                List<LocationNode> possibleLocations = new();
                List<RMotive> rMotives = action.Requirements.Motives;
                List<RLocation> rLocations = action.Requirements.Locations;
                List<RPeople> rPeople = action.Requirements.People;

                if (rMotives != null)
                {
                    if (!AgentManager.AgentSatisfiesMotiveRequirement(agent, rMotives))
                    {
                        continue;
                    }
                }
                if (rLocations != null)
                {
                    possibleLocations = LocationManager.LocationsSatisfyingLocationRequirement(rLocations[0]);
                }
                else
                {
                    possibleLocations.AddRange(LocationManager.LocationsByName.Values);
                }
                if (rPeople != null && possibleLocations.Count > 0)
                {
                    possibleLocations = LocationManager.LocationsSatisfyingPeopleRequirement(possibleLocations, rPeople[0]);
                }

                if (possibleLocations.Count > 0)
                {
                    LocationNode nearestLocation = LocationManager.FindNearestLocationFrom(agent.CurrentLocation, possibleLocations);
                    /*if (nearestLocation == null) continue;*/
                    travelTime = LocationManager.DistanceMatrix[agent.CurrentLocation, nearestLocation];
                    float deltaUtility = GetEffectDeltaForAgentAction(agent, action, nearestLocation);
                    float denom = action.MinTime + travelTime;
                    if (denom != 0)
                        deltaUtility /= denom;

                    if (deltaUtility == maxDeltaUtility)
                    {
                        currentChoice.Add(action);
                        currentDest.Add(nearestLocation);
                    }
                    else if (deltaUtility > maxDeltaUtility)
                    {
                        maxDeltaUtility = deltaUtility;
                        currentChoice.Clear();
                        currentDest.Clear();
                        currentChoice.Add(action);
                        currentDest.Add(nearestLocation);
                    }
                }
            }
            System.Random r = new();
            int idx = r.Next(0, currentChoice.Count);
            Action choice = currentChoice[idx];
            LocationNode dest = currentDest[idx];
            agent.CurrentAction.AddLast(choice);

			if (dest != null && dest != agent.CurrentLocation)
            {
				// Debug.LogFormat("Need to travel to: {0} from {1}", dest, agent.CurrentLocation);
                agent.CurrentAction.AddFirst(ActionManager.GetActionByName("travel_action"));
                StartTravelToLocation(agent, dest);
            }
            else if (dest == null || dest == agent.CurrentLocation)
            {
                StartAction(agent);
            }
        }

		private static void SelectRandomDestination(Agent agent)
		{
			LocationNode destination = LocationManager.GetRandomLocation();
			StartTravelToLocation(agent, destination);
		}

		public static void SelectRandomAction(Agent agent, List<Tuple<Action, LocationNode>> choices){
			System.Random r = new();
			int idx = r.Next(0, choices.Count);
			Action selectedAction = choices[idx].Item1;
			LocationNode destination = choices[idx].Item2;
			agent.CurrentAction.AddLast(selectedAction);

			if (destination != null && agent.CurrentLocation != destination){
				StartTravelToLocation(agent, destination);
			}
			else {
				StartAction(agent);
			}
		}
	}

		
    //     public static void SelectNextAction(Agent agent)
    //     {
    //         float maxDeltaUtility = 0f;
	// 		List<Tuple<Action, LocationNode>> choice = new();
    //         List<string> actionSelectLog = new();
    //         // LocationNode currentLoc = LocationManager.LocationsByName[CurrentLocation];

    //         foreach(Action action in ActionManager.Actions.Values)
    //         {
    //             if (action.Hidden) continue;
    //             actionSelectLog.Add("Action: " + action.Name);

    //             List<LocationNode> possibleLocations = new();
    //             List<RMotive> rMotives = action.Requirements.Motives;
    //             List<RLocation> rLocations = action.Requirements.Locations;
    //             List<RPeople> rPeople = action.Requirements.People;

    //             if (rMotives != null && AgentManager.AgentSatisfiesMotiveRequirement(agent, rMotives)){
	// 				if (rLocations != null){
	// 					possibleLocations = LocationManager.LocationsSatisfyingLocationRequirement(rLocations[0]);
	// 				}
	// 				else{
	// 					possibleLocations.AddRange(LocationManager.LocationsByName.Values);
	// 				}
					
	// 				if (rPeople != null && possibleLocations.Count > 0){
	// 					possibleLocations = LocationManager.LocationsSatisfyingPeopleRequirement(possibleLocations, rPeople[0], agent);
	// 				}

	// 				if (possibleLocations.Count > 0)
	// 				{
	// 					LocationNode nearestLocation = LocationManager.FindNearestLocationFrom(agent.CurrentLocation, possibleLocations);
	// 					float deltaUtility = ActionManager.GetEffectDeltaForAgentAction(agent, action, nearestLocation);
						
	// 					if (deltaUtility == maxDeltaUtility)
	// 					{
	// 						choice.Add(new(action,nearestLocation));
	// 					}
	// 					else if (deltaUtility > maxDeltaUtility)
	// 					{
	// 						maxDeltaUtility = deltaUtility;
	// 						choice.Clear();
	// 						choice.Add(new(action,nearestLocation));
	// 					}
	// 				}
	// 			}
    //         }
	// 		if(choice.Count > 0){
	// 			System.Random r = new();
	// 			int idx = r.Next(0, choice.Count);
	// 			UnityEngine.Debug.LogFormat("idx:{0} | choiceCount:{1}", idx, choice.Count);
	// 			Action selected  = choice[idx].Item1;
	// 			LocationNode selectedLoc  = choice[idx].Item2;
	// 			agent.CurrentAction.AddLast(selected);

				// if (selectedLoc != null && selectedLoc != agent.CurrentLocation){
				// 	UnityEngine.Debug.LogFormat("{2}: Need to travel to: {0} from {1} to perform {3}", selectedLoc, agent.CurrentLocation, agent.Name, selected.Name);
				// 	StartTravelToLocation(agent, selectedLoc, World.Time);
				// }

	// 			else if (selectedLoc == null || selectedLoc == agent.CurrentLocation){
	// 				StartAction(agent);
	// 			}
	// 		}
	// 		else{
	// 			agent.CurrentAction.AddLast(Actions["wait_action"]);
	// 			StartAction(agent);
	// 		}
    //     }
    // }
}
