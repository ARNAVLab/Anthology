using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jint.Parser;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

namespace Anthology.Models
{
    /// <summary>
    /// Describes the agent object or NPCs in the simulation.
    /// </summary>
	// [JsonConverter(typeof(AgentJSONConverter))]
    public class Agent
    {
        /// <summary>
        /// Name of the agent.
        /// </summary>
		[JsonPropertyName("Name")]	
        public string Name { get; set; } = string.Empty;

		[JsonPropertyName("Motives")]
		public Motive Motives {get; set;} = new();

        /// <summary>
        /// List of all the relationships of this agent.
        /// </summary>
        [JsonPropertyName("Relationships")]
		public List<Relationship> Relationships { get; set; } = new();

        /// <summary>
        /// The current location of this agent 
		/// 	The property ensures we record the movement and update the GUI accordingly.
        /// </summary>
		private LocationNode _currentLocation = new();


		[JsonPropertyName("CurrentLocation")]
		private string LocationName {
			get => _currentLocation.Name; 
			set {
				CurrentLocation = LocationManager.LocationsByName[value];
			}
		}
        
		[JsonPropertyName("CurrentLocation")]
		public LocationNode CurrentLocation { 
			get => _currentLocation; 
			set {
				// if (value is string)
				if (value != _currentLocation){
					_currentLocation.LeaveLocation(this);
					_currentLocation = value; 
					_currentLocation.EnterLocation(this);
				}
			}
		}

        /// <summary>
        /// How long the agent will be occupied with the current action they are executing.
        /// </summary>
        public int OccupiedCounter { get; set; }

		/// <summary>
		/// Is true if the agent is currently moving
		/// </summary>
		private bool movement = false;

        /// <summary>
        /// A queue containing the next few actions being executed by the agent.
        /// </summary>
        public LinkedList<Action> CurrentAction { get; set; } = new();

        /// <summary>
        /// The path to the destination that this agent is heading towards. 
        /// Can be an empty list if the agent has reached their previous
        /// destination and is executing an action.
        /// </summary>
        public List<LocationNode> Destination { get; set; } = new();

		public Agent DeserializeJSONAgent(string json_agent){
			Agent agent = new();
			
			return agent;
		}
    }

    /// <summary>
    /// Agent class received from a JSON file.
    /// The action is provided as a string and matched to the Agent.CurrentAction object accordingly.
    /// </summary>
    // public class SerializableAgent
    // {
    //     /// <summary>
    //     /// Initialized to the name of the agent.
    //     /// </summary>
    //     public string Name { get; set; } = string.Empty;

    //     /// <summary>
    //     /// Motives intiailized with values for the agent.
    //     /// </summary>
    //     public Dictionary<string, float> Motives { get; set; } = new();

    //     /// <summary>
    //     /// Starting location of this agent.
    //     /// </summary>
    //     public string CurrentLocation { get; set; } = string.Empty;

    //     /// <summary>
    //     /// Describes whether the agent is currently occupied.
    //     /// </summary>
    //     public int OccupiedCounter { get; set; }

    //     /// <summary>
    //     /// Queue containing the next few actions being executed by the agent.
    //     /// </summary>
    //     public string CurrentAction { get; set; } = string.Empty;

    //     /// <summary>
    //     /// Location this agent is currently heading towards.
    //     /// </summary>
    //     public string Destination { get;set; } = string.Empty;

    //     /// <summary>
    //     /// List of targets for the agent's current action.
    //     /// </summary>
    //     public List<string> CurrentTargets { get; set; } = new List<string>();

    //     /// <summary>
    //     /// List of relationships that the agent possesses.
    //     /// </summary>
    //     public List<Relationship> Relationships { get; set; } = new List<Relationship>();

    //     /// <summary>
    //     /// Creates a serializable agent from the given agent for file I/O.
    //     /// </summary>
    //     /// <param name="agent">The agent to serialize.</param>
    //     /// <returns>A serialized version of agent.</returns>
    //     public static string SerializeAgent(Agent agent)
    //     {
	// 		string serializableAgent = Newtonsoft.Json.JsonConvert.SerializeObject(agent);

    //         // Agent serializableAgent = Newtonsoft.Json.JsonConvert.SerializeObject({
    //         //     Name = agent.Name,
    //         //     Motives = JsonConvert.SerializeObject(agent.Motives),
    //         //     CurrentLocation = agent.CurrentLocation.Name,
    //         //     OccupiedCounter = agent.OccupiedCounter,
    //         //     CurrentAction = agent.CurrentAction.Count > 0 ? agent.CurrentAction.First.Value.Name : "wait_action",
    //         //     Destination = agent.Destination[0].Name,
    //         //     Relationships = new()
    //         // };

    //         // if (agent.CurrentAction.Count > 0)
    //         // {
    //         //     serializableAgent.CurrentAction = agent.CurrentAction.First.Value.Name;
    //         // }
    //         // else
    //         // {
    //         //     serializableAgent.CurrentAction = "wait_action";
    //         // }

    //         // foreach (Relationship r in agent.Relationships)
    //         // {
    //         //     serializableAgent.Relationships.Add(r);
    //         // }

    //         // foreach(KeyValuePair<string, float> m in agent.Motives)
    //         // {
    //         //     serializableAgent.Motives.Add(m.Key, m.Value);
    //         // }

	// 		Debug.Log(serializableAgent);
    //         return serializableAgent;
    //     }

    //     /// <summary>
    //     /// Creates an agent from the given serializable agent for file I/O.
    //     /// </summary>
    //     /// <param name="sAgent">The agent to deserialize.</param>
    //     /// <returns>Raw Agent type that was deserialized.</returns>
    //     public static Agent DeserializeToAgent(SerializableAgent sAgent)
    //     {
	// 		List<LocationNode> _destination = new();
	// 		LocationNode _currentlocation = new();
	// 		// Debug.LogFormat("Agent:{0} | CurrLoc:{1} | Dest:{2}", sAgent.Name, sAgent.CurrentLocation, sAgent.Destination);
			
	// 		if(sAgent.Destination!="" && LocationManager.LocationsByName.ContainsKey(sAgent.Destination)){
	// 			_destination.Add(LocationManager.LocationsByName[sAgent.Destination]);
	// 		}
	// 		if(sAgent.CurrentLocation!="" && LocationManager.LocationsByName.ContainsKey(sAgent.CurrentLocation)){
	// 			_currentlocation = LocationManager.LocationsByName[sAgent.CurrentLocation];
	// 		}
			
    //         Agent agent = new() {
    //             Name = sAgent.Name,
    //             CurrentLocation = _currentlocation,
    //             OccupiedCounter = sAgent.OccupiedCounter,
    //             Destination = _destination
    //         };

    //         agent.CurrentAction.AddFirst(ActionManager.GetActionByName(sAgent.CurrentAction));

    //         foreach (KeyValuePair<string, float> e in sAgent.Motives)
    //         {
    //             agent.Motives[e.Key] = e.Value;
    //         }
    //         foreach (Relationship r in sAgent.Relationships)
    //         {
    //             agent.Relationships.Add(r);
    //         }

    //         return agent;
    //     }
    // }
}
