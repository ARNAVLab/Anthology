using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using MongoDB.Driver;
using UnityEngine.InputSystem.Controls;

namespace Anthology.Models
{
    /// <summary>
    /// Contains the delta change in the momtive of an agent implementing the action.
    /// One effect per motive type, eg. sleep action may affect the physical motive.
    /// </summary>
    public abstract class Effect
    {
		/// <summary>
        /// Describes th emotive affected by this effected,
        /// eg. if an action affects the social motive of an agent,
        /// then motive = MotiveEnum.SOCIAL.
        /// </summary>
        public abstract string On { get; }

		public abstract float GetEffectDeltaForEffect(Agent agent);

		public abstract void ApplyActionEffects(Agent agent, int partially=1);
    }

	public class MotiveEffect: Effect {
		/// <summary>
		/// Describes th emotive affected by this effected,
		/// eg. if an action affects the social motive of an agent,
		/// then motive = MotiveEnum.SOCIAL.
		/// </summary>
		public override string On => "Motive";

		/// <summary>
		/// Describes th emotive affected by this effected,
		/// eg. if an action affects the social motive of an agent,
		/// then motive = MotiveEnum.SOCIAL.
		/// </summary>
		[JsonPropertyName("MotiveType")]
		public string MotiveType { get; set; }

        /// <summary>
        /// Valence of effect on the motive.
        /// </summary>
        [JsonPropertyName("Delta")]
		public float Delta { get; set; }

		public override void ApplyActionEffects(Agent agent, int partially = 1) {
			agent.Motives[MotiveType] = agent.Motives[MotiveType] + (Delta*partially);	
		}

		public override float GetEffectDeltaForEffect(Agent agent)
		{	
			// if (MotiveType == "")
			// 	return 0; 
			float current = (float)agent.Motives[MotiveType];
			return Math.Clamp(Delta + current, Motive.MIN, Motive.MAX) - current;
		}
	}

	public class LocationEffect: Effect {
		/// <summary>
		/// Describes th emotive affected by this effected,
		/// eg. if an action affects the social motive of an agent,
		/// </summary>
		public override string On => "Location";

		/// <summary>
		/// Describes the location affected by this Effect.
		/// eg. if an action affects the tags of a location,
		/// </summary>
		// [JsonPropertyName("Location")]
		// public string Location { get; set; } = string.Empty;

		/// <summary>
		/// Describes the effect on the location.
		/// eg. Add, Remove tags
		/// </summary>
		[JsonPropertyName("Operation")]
		public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Valence of effect on the motive.
        /// </summary>
        [JsonPropertyName("Tags")]
		public List<string> Tags { get; set; } = new();

		public override void ApplyActionEffects(Agent agent, int partially = 1)
		{
			if(Operation == "Add"){
				agent.CurrentLocation.AddTags(Tags);
			}

			else if(Operation == "Remove"){
				agent.CurrentLocation.RemoveTags(Tags);
			}
		}

		public override float GetEffectDeltaForEffect(Agent agent)
		{	
			throw new NotImplementedException();
		}
	}

	public class RelationshipEffect: Effect {
		/// <summary>
        /// Describes th emotive affected by this effected,
        /// eg. if an action affects the social motive of an agent,
        /// then motive = MotiveEnum.SOCIAL.
        /// </summary>
        public override string On => "Relationship";

		[JsonPropertyName("RelType")]
		public string RelType {get; set;} = string.Empty;

		[JsonPropertyName("Delta")]
		public float Delta {get; set;} = 0;

		public List<Agent> targets = new();

		public override float GetEffectDeltaForEffect(Agent agent)
		{
			throw new NotImplementedException();
		}

		public override void ApplyActionEffects(Agent agent, int partially = 1)
		{
			foreach(Agent target in agent.Targets){
				Relationship toEdit = agent.getRelationshipWithType(RelType, target.Name); 

				if(toEdit == null){
					Relationship rel = new(); 
					rel.With = target.Name;
					rel.Type = RelType; 
					rel.Valence = Delta * partially;
					agent.Relationships.Add(rel);
					
				}
				else{
					toEdit.Valence += Delta * partially;
				}
			}

			// Reset targets
			agent.Targets = new();
		}
	}

	public class EffectContainer {

		/// <summary>
		/// List of all the effects on the agents motive executing this action will have
		/// </summary> <summary>
		
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][JsonPropertyName("MotiveEffects")]
		public List<MotiveEffect> Motives {get; set;} = new();
		
		/// <summary>
		/// List of all the effects on the agents relationships executing this action will have
		/// </summary> <summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][JsonPropertyName("RelationshipEffects")]
		public List<RelationshipEffect> Relationships {get; set;} = new();


		/// <summary>
		/// List of all the effects on the agents relationships executing this action will have
		/// </summary> <summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][JsonPropertyName("LocationEffects")]
		public List<LocationEffect> Locations {get; set;} = new();

		/// <summary>
		/// List of actions that will be performed next by this agent
		/// Eg. Be Slapped Action may have as an effect that the following actions be added to the agent's queue: ['Cry', 'Go Home Sad']
		/// </summary> <summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][JsonPropertyName("ChainActions")]
		public List<string> ChainActions {get; set;} = new();
		
		
		/// <summary>
		/// List of actions that will be performed on targetted agents
		/// Eg. Slap Action may have TargetActions: ['Be Slapped', 'Cry']
		/// </summary> <summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][JsonPropertyName("TargetActions")]
		public List<string> TargetActions {get; set;} = new();

		/// <summary>
        /// Returns the net effect for an action for a specific agent.
        /// Takes into account the agent's current motivation statuses.
        /// </summary>
        /// <param name="agent">The agent relevant to retrieve motives from.</param>
        /// <returns>How much to affect motive by.</returns>
		public float GetEffectDeltaForEffects(Agent agent)
		{
			float deltaUtility = 0;
			foreach (MotiveEffect motiveEffect in Motives) {
				deltaUtility += motiveEffect.GetEffectDeltaForEffect(agent);
			}
			
			// foreach (RelationshipEffect relEffect in Relationships) {
			// 	throw new NotImplementedException();
			// }
			
			return deltaUtility;
		}

		public void ApplyActionEffects(Agent agent, int partially=1){	
			
			foreach (MotiveEffect motiveEffect in Motives) {
				motiveEffect.ApplyActionEffects(agent, partially);
			}

			if (agent.Targets.Any()) {
				foreach (RelationshipEffect relEffect in Relationships) {
					relEffect.ApplyActionEffects(agent, partially);
				}
			}

			foreach(LocationEffect locEffect in Locations) {
				locEffect.ApplyActionEffects(agent, partially);
			}	
		}
	}
	
}