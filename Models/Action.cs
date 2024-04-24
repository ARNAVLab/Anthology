using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using UnityEditor.UI;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;


namespace Anthology.Models
{
    /// <summary>
    /// Action class all actions should inherit from.
    /// All actions have at least a name, requirements, and minimum time taken.
    /// </summary>
	public class Action
    {
        /// <summary>
        /// Name of the action.
        /// </summary>
		[JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The minimum amount of time an action takes to execute.
        /// </summary>
        [JsonPropertyName("MinTime")]
		public int MinTime { get; set; }

        /// <summary>
        /// Optional flag to be set if this action cannot be selected by agents normally.
        /// </summary>
        [JsonPropertyName("Hidden")]
		public bool Hidden { get; set; } = false;

        /// <summary>
        /// Container of preconditions or requirements that must be fulfilled for this action to execute.
        /// </summary>
        [JsonPropertyName("Requirements")]
		public RequirementContainer Requirements { get; set; } = new();

		/// <summary>
        /// List of resulting changes to the motives of the agent that occur after this action is executed.
        /// </summary>
        [JsonPropertyName("Effects")]  // [JsonConverter(typeof(EffectsConverter))]
		public EffectContainer Effects {get; set;} = new();

		public LocationNode selectedLocation = null;
    }

	public class ScheduledAction
	{
		private Action _action {get; set;} = null;
		public string Action {
			get => _action.Name; 
			set {
				_action = ActionManager.GetActionByName(value);
			}
		}

		private List<Agent> _agents {get; set;} = new();

		public List<string> Agents {
			get => _agents.Select(person => person.Name).ToList(); 
			set {
				foreach (string name in value){
					_agents.Add(AgentManager.GetAgentByName(name));
				}
			}
		}

		public int AtTime {get; set;} = int.MinValue;

		private LocationNode _atLocation {get; set;} = null;
		
		public string AtLocation {
			get => _atLocation.Name;
			set {
				_atLocation = LocationManager.LocationsByName[value];
			}
		}

		public void ExecuteScheduledAction(){
			foreach (Agent agent in _agents){
				
			}
		}
	}
}
