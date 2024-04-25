using System;
using System.Collections.Generic;
using System.Linq;
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
		private LocationNode _currentLocation = LocationManager.GetRandomLocation();
		internal List<Agent> Targets = new();

		[JsonPropertyName("AtLocation")]
		public string AtLocation {
			get => _currentLocation.Name; 
			set {
				CurrentLocation = LocationManager.LocationsByName[value];
			}
		}
        
		// [JsonPropertyName("CurrentLocation")]
		public LocationNode CurrentLocation { 
			get => _currentLocation; 
			set {
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
        public int OccupiedCounter { get; set; } = 0;

		/// <summary>
		/// Is true if the agent is currently moving
		/// </summary>
		// private bool movement = false;

        /// <summary>
        /// A queue containing the next few actions being executed by the agent.
        /// </summary>
        public LinkedList<Action> CurrentAction { get; set; } = new();

		public string _lastAction { get; set; } = string.Empty;

        /// <summary>
        /// The path to the destination that this agent is heading towards. 
        /// Can be an empty list if the agent has reached their previous
        /// destination and is executing an action.
        /// </summary>
        public List<string> Destination { get; set; } = new();

		public Agent DeserializeJSONAgent(string json_agent){
			Agent agent = new();
			
			return agent;
		}

		public Action getCurrentAction(){
			if (CurrentAction.Any())
				return CurrentAction.First.Value;
			else
				return null;
		}

		public Relationship getRelationshipWithType(string rel_type, string with){
			foreach(Relationship relationship in Relationships){
				if(relationship.isRelationshipTypeWith(rel_type, with)) return relationship; 
			}
			return null;
		}

		/// <summary>
		/// Returns this location as a string object for debugging and printing
		/// </summary>
		/// <returns>Name of the agent</returns>
		public override string ToString() {
			return string.Format("Agent:{0}", Name);
		}

    }
}
