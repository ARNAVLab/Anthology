using System;
using System.Collections.Generic;
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
		public string MotiveType { get; set; } = string.Empty;

        /// <summary>
        /// Valence of effect on the motive.
        /// </summary>
        public float Delta { get; set; } = 0;
	}

	public class RelationshipEffect: Effect {
		/// <summary>
        /// Describes th emotive affected by this effected,
        /// eg. if an action affects the social motive of an agent,
        /// then motive = MotiveEnum.SOCIAL.
        /// </summary>
        public override string On => "Relationship";

		public string RelType {get; set;} = string.Empty;

		public Agent With {get; set;} = new();

		public float ValenceDelta {get; set;} = 0;

		public RelationshipEffect(){
			throw new NotImplementedException("Not implemented Relationship Effects yet");
		}
	}

	public class EffectContainer {

		/// <summary>
		/// List of all the effects on the agents motive executing this action will have
		/// </summary> <summary>
		public List<MotiveEffect> Motives = new();
		
		/// <summary>
		/// List of all the effects on the agents motive executing this action will have
		/// </summary> <summary>
		public List<RelationshipEffect> Relationships = new();
	}

	
}