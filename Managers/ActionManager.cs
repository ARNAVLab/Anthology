using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson.Serialization.Serializers;
using Unity.VisualScripting;
using UnityEditor.Playables;

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
		

        /// <summary>
        /// Initializes/resets all action manager variables.
        /// </summary>
        /// <param name="path">Path of actions JSON file.</param>
        public static void Init(string path)
        {
			Actions.Clear();
            World.ReadWrite.LoadActionsFromFile(path);
			InitDefaultActions();
        }

		public static void InitDefaultActions(){
			Action wait_action = new Action();
			wait_action.Name = "wait_action"; 
			wait_action.MinTime = 0;
			wait_action.Hidden = true; 
			Actions.Add(wait_action.Name, wait_action);

			Action travel = new Action();
			travel.Name = "travel_action"; 
			travel.MinTime = 0;
			travel.Hidden = true; 
			Actions.Add(travel.Name, travel);
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
        /// Interrupts the agent from the current action they are performing.
        /// Potential future implementation: Optionally add the interrupted action (with the remaining occupied counter) to the end of the action queue.
        /// </summary>
        /// <param name="agent">The agent to interrupt.</param>
        public static void InterruptAgent(Agent agent, Action interruptingAction, Agent interruptingAgent)
        {
			if (agent.CurrentAction.First.Value.Name == interruptingAction.Name) return; 

			string oldAct = agent.CurrentAction.First.Value.Name;
            ExecuteAction(agent); 

			agent.Targets.Add(interruptingAgent);
			
			List<string> targetActions = interruptingAction.Effects.TargetActions;
			targetActions.Reverse();

			if(targetActions.Any()){
				foreach (string action in targetActions){
					agent.CurrentAction.AddFirst(GetActionByName(action));
				}
				// UnityEngine.Debug.LogFormat("Agent:{0} is interrupted from:{1} to:{2}", agent.Name, oldAct, agent.getCurrentAction().Name);
			}
			else {
				agent.CurrentAction.AddFirst(interruptingAction);
				// UnityEngine.Debug.LogFormat("Agent:{0} is interrupted from:{1} to:{2}", agent.Name, oldAct, agent.CurrentAction.First.Value.Name);
			}

			// agent.OccupiedCounter = interruptingAction.MinTime;
			StartAction(agent);

			// Future: Add a check to see whether the other agent wants to perform the action/can perform it 
			// For instance, if you're eating with a friend, make sure the other agent thinks of you as a friend.
			// ChainAction(agent, interruptingAction, location);
        }

		public static void StopAction(Agent agent){
			agent.Destination = new();
			agent.OccupiedCounter = 0;
			agent.CurrentAction.RemoveFirst();

			if (agent.CurrentAction.Count == 0) SelectNextAction(agent);
		}

		/// <summary>
        /// Starts an action (if the agent is at a location where the action can be performed).
        /// Else, makes the agent travel to a suitable location to perform the action.
        /// </summary>
        public static void StartAction(Agent agent)
        {
            Action action = agent.getCurrentAction(); // agent.CurrentAction.First.Value;
			if(action == null) return; 

			// // If this action was performed on the agent, there's no choice. Just start it. 
			// if(action.Hidden == true){
			// 	agent.OccupiedCounter = action.MinTime;
			// 	return;
			// }

			List<RPeople> rPeople = action.Requirements.People;
			List<RLocation> rLocations = action.Requirements.Locations; 

			if(rLocations == null && rPeople == null) {
				agent.OccupiedCounter = action.MinTime;
				return;
			}

			if(rLocations != null && !agent.CurrentLocation.SatisfiesLocationRequirements(rLocations[0])){
				LocationNode nearestLocation = LocationManager.GetNearestLocationSatisfyingRequirements(action, agent);
				
				if(nearestLocation != null){
					StartTravelToLocation(agent,nearestLocation);
					return; 
				}
				StopAction(agent);
				return;
			}

			if(rPeople != null){
				List<Agent> targets = agent.CurrentLocation.FindTargetsForAction(rPeople[0], agent);
				
				if (targets == null){
					// People requirement failed 
					UnityEngine.Debug.LogFormat("{0}: Foiled from performing:{1} by others! PeopleRequirement has failed.", agent.Name, action.Name);
					StopAction(agent);
					return;
				}
				else if (targets.Count == 0){
					if (rPeople[0].MaxNumPeople == 0){
						agent.OccupiedCounter = action.MinTime;
						return;
					}
					// there's no one to do this social action to/with 
					StopAction(agent);
					return;
				}
				else {
					// found people to perform the social action to/with
					foreach(Agent target in targets){
						InterruptAgent(target, action, agent);
					}
					agent.OccupiedCounter = action.MinTime;
					agent.Targets = targets;
					return;
				}
        	}
			agent.OccupiedCounter = action.MinTime;
		}

		/// <summary>
        /// Starts travel to the agent's destination.
        /// </summary>
        /// <param name="destination">The agent's destination.</param>
        /// <param name="time">The time in which the agent started traveling.</param>
		public static void StartTravelToLocation(Agent agent, LocationNode destination)
        {
			agent.CurrentAction.AddFirst(ActionManager.GetActionByName("travel_action"));
			List<string> path = LocationManager.FindPathsBetween(agent.CurrentLocation, destination);
			agent.Destination = path;
			agent.OccupiedCounter = path.Count;
			// var something = LocationManager.DiscoveredPaths;
			MoveCloserToDestination(agent);
			// Action _currentAction = agent.CurrentAction.First.Value;
        }

        /// <summary>
        /// Moves closer to the agent's destination.
        /// Uses the manhattan distance to move the agent, so either moves along the x or y axis during any tick.
        /// </summary>
        public static void MoveCloserToDestination(Agent agent)
        {
            if (agent.Destination.Count == 0){
				return;
			}
			agent.CurrentLocation = LocationManager.LocationsByName[agent.Destination[0]]; 
			agent.Destination.RemoveAt(0);
        }

		/// <summary>
        /// Applies the effect of an action to this agent.
        /// </summary>
        public static void ExecuteAction(Agent agent) {
			int partially = 1;

			// Execute the first action queued up
			if (agent.CurrentAction.Count == 0) {
				agent.Destination = new();
				agent.OccupiedCounter = 0;
				return; 
			}

			Action action = agent.CurrentAction.First.Value;

			if (action.Name != "travel_action")
				agent._lastAction = action.Name;
			
			if (agent.OccupiedCounter != 0){
				if (action.MinTime != 0)
					partially = (int)Math.Round((float)((action.MinTime - agent.OccupiedCounter)/action.MinTime), 0);
			}
			
			agent.Destination = new();
			action.Effects.ApplyActionEffects(agent, partially);
			agent.CurrentAction.RemoveFirst();
			agent.OccupiedCounter = 0;

			List<string> chainedActions = action.Effects.ChainActions; 
			if (chainedActions != null && chainedActions.Any()){
				chainedActions.Reverse(); 
				foreach (string nextAct in chainedActions)
				{
					agent.CurrentAction.AddFirst(GetActionByName(nextAct));
					// UnityEngine.Debug.LogFormat("{0} is adding {1} to action queue.", agent.Name, nextAct);
				}
				StartAction(agent);
			}
        }

		public static void ChainAction(Agent agent, Action action, LocationNode location=null){
			agent.CurrentAction.AddFirst(action);

			if (location != null && location != agent.CurrentLocation){
				StartTravelToLocation(agent, location);
			}
			else if (location == null || location == agent.CurrentLocation){
				StartAction(agent);
			}
		}

		/// <summary>
        /// Selects an action from a set of valid actions to be performed by this agent.
        /// Selects the action with the maximal utility of the agent (motive increase / time).
        /// </summary>
        public static void SelectNextAction(Agent agent)
        {
            List<Tuple<float, Action, LocationNode>> possibleActions = new();
			float last = 0;

            foreach(Action action in ActionManager.Actions.Values)
            {
                if (action.Hidden==true || action.Name==agent._lastAction) 
					continue;

                float travelTime;
                List<RMotive> rMotives = action.Requirements.Motives;
                

                if (rMotives != null && !AgentManager.AgentSatisfiesMotiveRequirement(agent, rMotives)){
					continue;
				}
                
				// tests locations against people/location requirements
				LocationNode nearestLocation = LocationManager.GetNearestLocationSatisfyingRequirements(action, agent);
                if (nearestLocation != null)
                {
                    travelTime = LocationManager.DistanceMatrix[agent.CurrentLocation, nearestLocation];
                    float deltaUtility = action.Effects.GetEffectDeltaForEffects(agent); //ActionManager.GetEffectDeltaForAgentAction(agent, action);
                    float denom = action.MinTime + travelTime;
                    
					if (denom != 0)
                        deltaUtility /= denom;

					last += deltaUtility;
					possibleActions.Add(new(last, action, nearestLocation));
                }
            }

			
			float selectedUtil = UnityEngine.Random.Range(0.0f, last);
			Tuple<float, Action, LocationNode> selectedAction = null;
			while (possibleActions.Any() && (selectedAction == null || selectedAction.Item1 <= selectedUtil)){
				selectedAction = possibleActions[0];
				possibleActions.RemoveAt(0);
			}

			LocationNode dest = selectedAction.Item3;
			Action choice = selectedAction.Item2;
			choice.selectedLocation = dest;
			
			ChainAction(agent, choice, dest);

        }
    }
}
