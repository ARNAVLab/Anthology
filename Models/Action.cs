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

	// public class EffectsConverter : Newtonsoft.Json.JsonConverter<EffectContainer> {
	// public class EffectsConverter : System.Text.Json.Serialization.JsonConverter<EffectContainer> {
	// 	// private readonly Dictionary<string, Location> _locationDictionary = new();
	// 	private readonly EffectContainer _effectContainer = new();

	// 	public EffectsConverter()
	// 	{
	// 	}

	// 	public EffectsConverter(EffectContainer effectContainer)
	// 	{
	// 		_effectContainer = effectContainer;
	// 	}

	// 	public override EffectContainer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	// 	{
	// 		// JObject jObject = JObject.Load(reader);
	// 		// JObject jObject = JObject.Parse(reader);
	// 		if (reader.TokenType == JsonTokenType.Null)
	// 		{
	// 			return null;
	// 		}

	// 		using var document = JsonDocument.ParseValue(ref reader);


	// 		JObject jObjMotives = (JObject)jObject["Motives"];
	// 		JObject jObjRelationships = (JObject)jObject["Relationships"];

	// 		EffectContainer effectContainer = new();
			
	// 		foreach (var _motive in jObjMotives){
	// 			MotiveEffect motiveEffect = Newtonsoft.Json.JsonConvert.DeserializeObject<MotiveEffect>(_motive.ToString());
	// 			effectContainer.Motives.Add(motiveEffect);
	// 		}

	// 		foreach (var _motive in jObjRelationships)
	// 		{
	// 			RelationshipEffect motiveEffect = Newtonsoft.Json.JsonConvert.DeserializeObject<RelationshipEffect>(_motive.ToString());	
	// 			effectContainer.Relationships.Add(motiveEffect);
	// 		}
	// 		return effectContainer;
	// 	}

	// 	// public override EffectContainer ReadJSON(ref Newtonsoft.Json.JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	// 	// public override EffectContainer ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, EffectContainer existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
	// 	// {
	// 	// 	JObject jObject = JObject.Load(reader);
	// 	// 	JObject jObjMotives = (JObject)jObject["Motives"];
	// 	// 	JObject jObjRelationships = (JObject)jObject["Relationships"];

	// 	// 	EffectContainer effectContainer = new();
			
	// 	// 	foreach (var _motive in jObjMotives){
	// 	// 		MotiveEffect motiveEffect = Newtonsoft.Json.JsonConvert.DeserializeObject<MotiveEffect>(_motive.ToString());
	// 	// 		effectContainer.Motives.Add(motiveEffect);
	// 	// 	}

	// 	// 	foreach (var _motive in jObjRelationships)
	// 	// 	{
	// 	// 		RelationshipEffect motiveEffect = Newtonsoft.Json.JsonConvert.DeserializeObject<RelationshipEffect>(_motive.ToString());	
	// 	// 		effectContainer.Relationships.Add(motiveEffect);
	// 	// 	}
	// 	// 	return effectContainer;
	// 	// }

	// 	public override void Write(Utf8JsonWriter writer, EffectContainer value, JsonSerializerOptions options)
	// 	{
	// 		throw new NotImplementedException();
	// 	}

	// 	public override void WriteJson(Newtonsoft.Json.JsonWriter writer, EffectContainer value, Newtonsoft.Json.JsonSerializer serializer)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	// }
}
