using System;
using System.Collections.Generic;

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
        public static ActionContainer Actions { get; set; } = new();

        /// <summary>
        /// Compiled list of all actions of both scheduled and primary actions.
        /// </summary>
        public static List<Action> AllActions { get; set; } = new();

        /// <summary>
        /// Initializes/resets all action manager variables.
        /// </summary>
        /// <param name="path">Path of actions JSON file.</param>
        public static void Init(string path)
        {
            Actions.ScheduleActions.Clear();
            Actions.PrimaryActions.Clear();
            AllActions.Clear();
            World.ReadWrite.LoadActionsFromFile(path);

            foreach (Action action in Actions.ScheduleActions)
            {
                AllActions.Add(action);
            }
            foreach (Action action in Actions.PrimaryActions)
            {
                AllActions.Add(action);
            }
        }

        /// <summary>
        /// Clears all actions from the system.
        /// </summary>
        public static void Reset()
        {
            Actions.ScheduleActions.Clear();
            Actions.PrimaryActions.Clear();
            AllActions.Clear();
        }

        /// <summary>
        /// Adds the given action to both action structures.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public static void AddAction(Action action)
        {
            Actions.AddAction(action);
            AllActions.Add(action);
        }

        /// <summary>
        /// Retrieves an action with the specified name from the set of actions available in the simulation.
        /// </summary>
        /// <param name="actionName">The name of the action to find.</param>
        /// <returns>The action with specified name.</returns>
        /// <exception cref="Exception">Thrown when action cannot be found.</exception>
        public static Action GetActionByName(string actionName)
        {
            bool HasName(Action action)
            {
                return action.Name == actionName;
            }
            Action? action = AllActions.Find(HasName);
            return action ?? throw new Exception("Action with name: " + actionName + " cannot be found.");
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
            float deltaUtility = 0f;

            if (action is PrimaryAction pAction)
            {
                foreach (KeyValuePair<string, float> e in pAction.Effects)
                {
                    float delta = e.Value;
                    float current = agent.Motives[e.Key];
                    deltaUtility += Math.Clamp(delta + current, Motive.MIN, Motive.MAX) - current;
                }
                return deltaUtility;
            }
            else if (action is ScheduleAction sAction)
            {
                return GetEffectDeltaForAgentAction(agent, GetActionByName(sAction.InstigatorAction));
            }

            return deltaUtility;
        }

		/// <summary>
        /// Starts an action (if the agent is at a location where the action can be performed).
        /// Else, makes the agent travel to a suitable location to perform the action.
        /// </summary>
        public static void StartAction(Agent agent)
        {
            Action action = agent.CurrentAction.First.Value;
            agent.OccupiedCounter = action.MinTime;
            
            if (action is ScheduleAction)
            {
                action.CurrentTargets.Clear();
                foreach (string name in agent.CurrentLocation.AgentsPresent)
                {
                    action.CurrentTargets.Add(AgentManager.GetAgentByName(name));
                }
            }
        }

		/// <summary>
        /// Applies the effect of an action to this agent.
        /// </summary>
        public static void ExecuteAction(Agent agent) {
            agent.Destination = new();
            agent.OccupiedCounter = 0;

			// Execute the first action queued up
            if (agent.CurrentAction.Count > 0)
            {
                Action action = agent.CurrentAction.First.Value;
                agent.CurrentAction.RemoveFirst();

                if (action is PrimaryAction pAction)
                {
                    foreach (KeyValuePair<string, float> e in pAction.Effects)
                    {
                        float delta = e.Value;
                        float current = agent.Motives[e.Key];
                        agent.Motives[e.Key] = Math.Clamp(delta + current, Motive.MIN, Motive.MAX);
                    }
                }
                else if (action is ScheduleAction sAction)
                {
                    if (sAction.Interrupt)
                    {
                        agent.CurrentAction.AddFirst(ActionManager.GetActionByName(sAction.InstigatorAction));
                    }
                    else
                    {
                        agent.CurrentAction.AddLast(ActionManager.GetActionByName(sAction.InstigatorAction));
                    }
                    foreach(Agent target in action.CurrentTargets)
                    {
                        if (sAction.Interrupt)
                        {
                            target.CurrentAction.AddFirst(ActionManager.GetActionByName(sAction.TargetAction));
                        }
                        else
                        {
                            target.CurrentAction.AddLast(ActionManager.GetActionByName(sAction.TargetAction));
                        }
                    }
                }
            }
        }

		/// <summary>
        /// Selects an action from a set of valid actions to be performed by this agent.
        /// Selects the action with the maximal utility of the agent (motive increase / time).
        /// </summary>
        public static void SelectNextAction(Agent agent)
        {
            float maxDeltaUtility = 0f;
            List<Action> currentChoice = new();
            List<LocationNode> currentDest = new();
            List<string> actionSelectLog = new();
            // LocationNode currentLoc = LocationManager.LocationsByName[CurrentLocation];

            foreach(Action action in ActionManager.AllActions)
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
                    float deltaUtility = ActionManager.GetEffectDeltaForAgentAction(agent, action);
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
                agent.StartTravelToLocation(dest, World.Time);
            }
            else if (dest == null || dest == agent.CurrentLocation)
            {
                StartAction(agent);
            }
        }
    }
}
