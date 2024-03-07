using System;
using System.Collections.Generic;
using Jint.Parser;
using Unity.VisualScripting;
using UnityEngine;

namespace Anthology.Models
{
    /// <summary>
    /// Relationships are composed by agents, so the owning agent will always be the source of the relationship,
    /// eg. an agent that has the 'brother' relationship with Norma is Norma's brother.
    /// </summary>
    public class Relationship
    {
        /// <summary>
        /// The type of relationship, eg. 'student' or 'teacher'.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The agent that this relationship is with.
        /// </summary>
        public string With { get; set; } = string.Empty;

        /// <summary>
        /// How strong the relationship is.
        /// </summary>
        public float Valence { get; set; }
    }

    /// <summary>
    /// Describes the agent object or NPCs in the simulation.
    /// </summary>
    public class Agent
    {
        /// <summary>
        /// Name of the agent.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Container of all the motive properties of this agent.
        /// </summary>
        public Dictionary<string, float> Motives { get; set; } = new Dictionary<string, float>()
                                                                      {
                                                                        { "accomplishment", 1 },
                                                                        { "emotional", 1 },
                                                                        { "financial", 1 },
                                                                        { "social", 1 },
                                                                        { "physical", 1 } 
                                                                      };

        /// <summary>
        /// List of all the relationships of this agent.
        /// </summary>
        public List<Relationship> Relationships { get; set; } = new();

        /// <summary>
        /// Name of the current location of this agent.
        /// </summary>
        public LocationNode CurrentLocation { get; set; } =  new();  // string.Empty;

        /// <summary>
        /// How long the agent will be occupied with the current action they are executing.
        /// </summary>
        public int OccupiedCounter { get; set; }

        /// <summary>
        /// A queue containing the next few actions being executed by the agent.
        /// </summary>
        public LinkedList<Action> CurrentAction { get; set; } = new();

        /// <summary>
        /// The name of the destination that this agent is heading towards. 
        /// Can be the empty string if the agent has reached their previous
        /// destination and is executing an action.
        /// </summary>
        public List<LocationNode> Destination { get; set; } = new();

        /// <summary>
        /// List of targets for the agent's current action.
        /// </summary>
        public List<Agent> CurrentTargets { get; set; } = new();

        /// <summary>
        /// Starts travel to the agent's destination.
        /// </summary>
        /// <param name="destination">The agent's destination.</param>
        /// <param name="time">The time in which the agent started traveling.</param>
        
		public void StartTravelToLocation(LocationNode destination, float time)
        {
			if (Destination.Count > 0) MoveCloserToDestination();
			// LocationNode currentLoc = CurrentLocation;
            // Destination = destination.Name;

			List<LocationNode> path = LocationManager.FindPathsBetween(CurrentLocation, destination);
			Debug.LogFormat("{3}: From:{0} | To:{1} | Path:{2}", CurrentLocation, destination, string.Join(", ", path), Name);

			Destination = path;
			OccupiedCounter = path.Count;

			Action _currentAction = CurrentAction.First.Value;
            Console.WriteLine("time: " + time.ToString() + " | " + Name + ": Started " + _currentAction.Name + "; Destination: " + destination.Name);

        }

        /// <summary>
        /// Moves closer to the agent's destination.
        /// Uses the manhattan distance to move the agent, so either moves along the x or y axis during any tick.
        /// </summary>
        public void MoveCloserToDestination()
        {
            if (Destination.Count == 0) return;

            CurrentLocation.AgentsPresent.Remove(Name);
			CurrentLocation = Destination[0]; 
			Destination.RemoveAt(0);
			CurrentLocation.AgentsPresent.AddLast(Name);
        }

        /// <summary>
        /// Applies the effect of an action to this agent.
        /// </summary>
        public void ExecuteAction()
        {
            Destination = new();
            OccupiedCounter = 0;

            if (CurrentAction.Count > 0)
            {
                Action action = CurrentAction.First.Value;
                CurrentAction.RemoveFirst();

                if (action is PrimaryAction pAction)
                {
                    foreach (KeyValuePair<string, float> e in pAction.Effects)
                    {
                        float delta = e.Value;
                        float current = Motives[e.Key];
                        Motives[e.Key] = Math.Clamp(delta + current, Motive.MIN, Motive.MAX);
                    }
                }
                else if (action is ScheduleAction sAction)
                {
                    if (sAction.Interrupt)
                    {
                        CurrentAction.AddFirst(ActionManager.GetActionByName(sAction.InstigatorAction));
                    }
                    else
                    {
                        CurrentAction.AddLast(ActionManager.GetActionByName(sAction.InstigatorAction));
                    }
                    foreach(Agent target in CurrentTargets)
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
        /// Starts an action (if the agent is at a location where the action can be performed).
        /// Else, makes the agent travel to a suitable location to perform the action.
        /// </summary>
        public void StartAction()
        {
            Action action = CurrentAction.First.Value;
            OccupiedCounter = action.MinTime;
            
            if (action is ScheduleAction)
            {
                CurrentTargets.Clear();
                foreach (string name in CurrentLocation.AgentsPresent)
                {
                    CurrentTargets.Add(AgentManager.GetAgentByName(name));
                }
            }
        }

        /// <summary>
        /// Selects an action from a set of valid actions to be performed by this agent.
        /// Selects the action with the maximal utility of the agent (motive increase / time).
        /// </summary>
        public void SelectNextAction()
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
                    if (!AgentManager.AgentSatisfiesMotiveRequirement(this, rMotives))
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
                    LocationNode nearestLocation = LocationManager.FindNearestLocationFrom(CurrentLocation, possibleLocations);
                    /*if (nearestLocation == null) continue;*/
                    travelTime = LocationManager.DistanceMatrix[CurrentLocation, nearestLocation];
                    float deltaUtility = ActionManager.GetEffectDeltaForAgentAction(this, action);
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
            CurrentAction.AddLast(choice);

			if (dest != null && dest != CurrentLocation)
            {
				// Debug.LogFormat("Need to travel to: {0} from {1}", dest, CurrentLocation);
                CurrentAction.AddFirst(ActionManager.GetActionByName("travel_action"));
                StartTravelToLocation(dest, World.Time);
            }
            else if (dest == null || dest == CurrentLocation)
            {
                StartAction();
            }
        }

        /// <summary>
        /// Returns whether the agent is content, ie. checks to see if an agent has the maximum motives.
        /// </summary>
        /// <returns>True if all motives are at max.</returns>
        public bool IsContent()
        {
            foreach (float m in Motives.Values)
            {
                if (m < Motive.MAX) return false;
            }
            return true;
        }

        /// <summary>
        /// Decrements all the motives of this agent.
        /// </summary>
        public void DecrementMotives()
        {
            foreach(string m in Motives.Keys)
            {
                Motives[m] = Math.Clamp(Motives[m] - 1, Motive.MIN, Motive.MAX);
            }
        }
    }

    /// <summary>
    /// Agent class received from a JSON file.
    /// The action is provided as a string and matched to the Agent.CurrentAction object accordingly.
    /// </summary>
    public class SerializableAgent
    {
        /// <summary>
        /// Initialized to the name of the agent.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Motives intiailized with values for the agent.
        /// </summary>
        public Dictionary<string, float> Motives { get; set; } = new();

        /// <summary>
        /// Starting location of this agent.
        /// </summary>
        public string CurrentLocation { get; set; } = string.Empty;

        /// <summary>
        /// Describes whether the agent is currently occupied.
        /// </summary>
        public int OccupiedCounter { get; set; }

        /// <summary>
        /// Queue containing the next few actions being executed by the agent.
        /// </summary>
        public string CurrentAction { get; set; } = string.Empty;

        /// <summary>
        /// Location this agent is currently heading towards.
        /// </summary>
        public string Destination { get;set; } = string.Empty;

        /// <summary>
        /// List of targets for the agent's current action.
        /// </summary>
        public List<string> CurrentTargets { get; set; } = new List<string>();

        /// <summary>
        /// List of relationships that the agent possesses.
        /// </summary>
        public List<Relationship> Relationships { get; set; } = new List<Relationship>();

        /// <summary>
        /// Creates a serializable agent from the given agent for file I/O.
        /// </summary>
        /// <param name="agent">The agent to serialize.</param>
        /// <returns>A serialized version of agent.</returns>
        public static SerializableAgent SerializeAgent(Agent agent)
        {
            SerializableAgent serializableAgent = new()
            {
                Name = agent.Name,
                Motives = new(),
                CurrentLocation = agent.CurrentLocation.Name,
                OccupiedCounter = agent.OccupiedCounter,
                CurrentAction = string.Empty,
                Destination = agent.Destination[0].Name,
                Relationships = new()
            };

            if (agent.CurrentAction.Count > 0)
            {
                serializableAgent.CurrentAction = agent.CurrentAction.First.Value.Name;
            }
            else
            {
                serializableAgent.CurrentAction = "wait_action";
            }

            foreach (Relationship r in agent.Relationships)
            {
                serializableAgent.Relationships.Add(r);
            }

            foreach(KeyValuePair<string, float> m in agent.Motives)
            {
                serializableAgent.Motives.Add(m.Key, m.Value);
            }

            return serializableAgent;
        }

        /// <summary>
        /// Creates an agent from the given serializable agent for file I/O.
        /// </summary>
        /// <param name="sAgent">The agent to deserialize.</param>
        /// <returns>Raw Agent type that was deserialized.</returns>
        public static Agent DeserializeToAgent(SerializableAgent sAgent)
        {
			List<LocationNode> _destination = new();
			LocationNode _currentlocation = new();
			// Debug.LogFormat("Agent:{0} | CurrLoc:{1} | Dest:{2}", sAgent.Name, sAgent.CurrentLocation, sAgent.Destination);
			
			if(sAgent.Destination!="" && LocationManager.LocationsByName.ContainsKey(sAgent.Destination)){
				_destination.Add(LocationManager.LocationsByName[sAgent.Destination]);
			}
			if(sAgent.CurrentLocation!="" && LocationManager.LocationsByName.ContainsKey(sAgent.CurrentLocation)){
				_currentlocation = LocationManager.LocationsByName[sAgent.CurrentLocation];
			}
			
            Agent agent = new() {
                Name = sAgent.Name,
                CurrentLocation = _currentlocation,
                OccupiedCounter = sAgent.OccupiedCounter,
                Destination = _destination
            };

            agent.CurrentAction.AddFirst(ActionManager.GetActionByName(sAgent.CurrentAction));

            foreach (KeyValuePair<string, float> e in sAgent.Motives)
            {
                agent.Motives[e.Key] = e.Value;
            }
            foreach (Relationship r in sAgent.Relationships)
            {
                agent.Relationships.Add(r);
            }
            foreach (string name in sAgent.CurrentTargets)
            {
                agent.CurrentTargets.Add(AgentManager.GetAgentByName(name));
            }

            return agent;
        }
    }
}
