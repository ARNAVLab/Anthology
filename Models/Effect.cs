using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

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
		public string MotiveType { get; set; } = string.Empty;

        /// <summary>
        /// Valence of effect on the motive.
        /// </summary>
        [JsonPropertyName("Delta")]
		public float Delta { get; set; } = 0;

		public override float GetEffectDeltaForEffect(Agent agent)
		{	
			if (MotiveType == "")
				return 0; 
			
			float current = (float)agent.Motives[MotiveType];
			return Math.Clamp(Delta + current, Motive.MIN, Motive.MAX) - current;
		}
	}

	public class RelationshipEffect: Effect {
		/// <summary>
        /// Describes th emotive affected by this effected,
        /// eg. if an action affects the social motive of an agent,
        /// then motive = MotiveEnum.SOCIAL.
        /// </summary>
        public override string On => "Relationship";

		[JsonPropertyName("RelationshipType")]
		public string RelType {get; set;} = string.Empty;

		[JsonPropertyName("Delta")]
		public float ValenceDelta {get; set;} = 0;

		public List<Agent> targets = new();

		public override float GetEffectDeltaForEffect(Agent agent)
		{
			throw new NotImplementedException();
			// if (RelationshipType == "")
			// 	return 0; 
			
			// float current = (float)agent.Motives[RelationshipType];
			// return Math.Clamp(ValenceDelta + current, Motive.MIN, Motive.MAX) - current;
		}
	}

	public class EffectContainer {

		/// <summary>
		/// List of all the effects on the agents motive executing this action will have
		/// </summary> <summary>
		
		[JsonPropertyName("MotiveEffects")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<MotiveEffect> Motives {get; set;} = new();
		
		/// <summary>
		/// List of all the effects on the agents relationships executing this action will have
		/// </summary> <summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][JsonPropertyName("RelationshipEffects")]
		public List<RelationshipEffect> Relationships {get; set;} = new();

		public float GetEffectDeltaForEffects(Agent agent)
		{
			float deltaUtility = 0;
			foreach (MotiveEffect motiveEffect in Motives) {
				deltaUtility += motiveEffect.GetEffectDeltaForEffect(agent);
			}
			
			foreach (RelationshipEffect relEffect in Relationships) {
				throw new NotImplementedException();
			}
			
			return deltaUtility;
		}

		public void ApplyActionEffects(Agent agent){	
			foreach (MotiveEffect motiveEffect in Motives) {
				if(motiveEffect.MotiveType != "")
					agent.Motives[motiveEffect.MotiveType] = (float)agent.Motives[motiveEffect.MotiveType] + motiveEffect.Delta;
			}
			
			foreach (RelationshipEffect relEffect in Relationships) {
				throw new NotImplementedException();
			}
		}
	}

	
}