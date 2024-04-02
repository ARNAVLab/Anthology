using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public static float GetEffectDeltaForAgentAction(Agent agent, Action action)
        {
			return action.Effects.GetEffectDeltaForEffects(agent);
        }

		/// <summary>
        /// Starts an action (if the agent is at a location where the action can be performed).
        /// Else, makes the agent travel to a suitable location to perform the action.
        /// </summary>
        public static void StartAction(Agent agent)
        {
            Action action = agent.CurrentAction.First.Value;
			List<RPeople> rPeople = action.Requirements.People;

			if(rPeople != null && !agent.CurrentLocation.SatisfiesPeopleRequirements(rPeople[0], agent)){
				UnityEngine.Debug.LogFormat("{0}: Foiled from performing:{1} by others! PeopleRequirement has failed.", agent.Name, action.Name);
				agent.Destination = new();
				agent.OccupiedCounter = 0;
				agent.CurrentAction.RemoveFirst();
				
				if (agent.CurrentAction.Count == 0) SelectNextAction(agent);
			}
			else{
				agent.OccupiedCounter = action.MinTime;
			}
            
            
            // if (action is ScheduleAction)
            // {
			// 	ScheduleAction schedAct = (ScheduleAction)action;
            //     schedAct.CurrentTargets.Clear();
            //     foreach (string name in agent.CurrentLocation.AgentsPresent)
            //     {
            //         schedAct.CurrentTargets.Add(AgentManager.GetAgentByName(name));
            //     }
            // }
        }

		/// <summary>
        /// Starts travel to the agent's destination.
        /// </summary>
        /// <param name="destination">The agent's destination.</param>
        /// <param name="time">The time in which the agent started traveling.</param>
		public static void StartTravelToLocation(Agent agent, LocationNode destination, float time)
        {
			List<LocationNode> path = LocationManager.FindPathsBetween(agent.CurrentLocation, destination);
			agent.Destination = path;
			agent.OccupiedCounter = path.Count;
			Action _currentAction = agent.CurrentAction.First.Value;
        }

        /// <summary>
        /// Moves closer to the agent's destination.
        /// Uses the manhattan distance to move the agent, so either moves along the x or y axis during any tick.
        /// </summary>
        public static void MoveCloserToDestination(Agent agent)
        {
            if (agent.Destination.Count == 0){
				// if (agent.CurrentAction.Count >= 0) StartAction(agent);
				return;
			}
			agent.CurrentLocation = agent.Destination[0]; 
			agent.Destination.RemoveAt(0);
			// agent.OccupiedCounter--;
        }

		/// <summary>
        /// Applies the effect of an action to this agent.
        /// </summary>
        public static void ExecuteAction(Agent agent) {
			agent.Destination = new();
            agent.OccupiedCounter = 0;

			// Execute the first action queued up
			// UnityEngine.Debug.LogFormat("");
			Action action = agent.CurrentAction.First.Value;
			agent._lastAction = action.Name;

			action.Effects.ApplyActionEffects(agent);
			agent.CurrentAction.RemoveFirst();
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

				// if (agent.Name == "Thomas") UnityEngine.Debug.LogFormat("{0}: Looking at {1}", agent.Name, action.Name);
                
				// actionSelectLog.Add("Action: " + action.Name);

                float travelTime;
                List<LocationNode> possibleLocations = new();
                List<RMotive> rMotives = action.Requirements.Motives;
                List<RLocation> rLocations = action.Requirements.Locations;
                List<RPeople> rPeople = action.Requirements.People;

                if (rMotives != null && !AgentManager.AgentSatisfiesMotiveRequirement(agent, rMotives)){
					continue;
				}
                
                if (rLocations != null)
                {
                    possibleLocations = LocationManager.LocationsSatisfyingLocationRequirement(rLocations[0]);
                }
                else
                {
                    possibleLocations.AddRange(LocationManager.LocationsByName.Values);
                }
                
				if (rPeople != null)
                {
                    possibleLocations = LocationManager.LocationsSatisfyingPeopleRequirement(rPeople[0], agent, possibleLocations);
                }

                if (possibleLocations.Count > 0)
                {
                    LocationNode nearestLocation = LocationManager.FindNearestLocationFrom(agent.CurrentLocation, possibleLocations);

                    travelTime = LocationManager.DistanceMatrix[agent.CurrentLocation, nearestLocation];
                    float deltaUtility = ActionManager.GetEffectDeltaForAgentAction(agent, action);
                    float denom = action.MinTime + travelTime;
                    
					if (denom != 0)
                        deltaUtility /= denom;

					last += deltaUtility;
					possibleActions.Add(new(last, action, nearestLocation));
                }
            }

			float selectedUtil = UnityEngine.Random.Range(0.0f, last);
			Tuple<float, Action, LocationNode> selectedAction = possibleActions[0];
			while (possibleActions.Count>0 && selectedAction.Item1 < selectedUtil){
				possibleActions.RemoveAt(0);
				selectedAction = possibleActions[0];
			}

			Action choice = selectedAction.Item2;
			LocationNode dest = selectedAction.Item3;
			agent.CurrentAction.AddFirst(choice);

			if (dest != null && dest != agent.CurrentLocation)
			{
				agent.CurrentAction.AddFirst(ActionManager.GetActionByName("travel_action"));
				StartTravelToLocation(agent, dest, World.Time);
			}
			else if (dest == null || dest == agent.CurrentLocation)
			{
				StartAction(agent);
			}

        }
    }
}
