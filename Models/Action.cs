using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using JsonConverter = Newtonsoft.Json.JsonConverter;
using JsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;


namespace Anthology.Models
{
    /// <summary>
    /// Action class all actions should inherit from.
    /// All actions have at least a name, requirements, and minimum time taken.
    /// </summary>
    // [JsonConverter(typeof(ActionConverter))]
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
        [JsonPropertyOrder(1)][JsonPropertyName("Effects")]
        public EffectContainer Effects { get; set; } = new(); 
    }

	/// <summary>
    /// List of values that determine who the target effects of an action (if applicable), are applied to.
    /// This roughly mirrors the parameters of the people requirement.
    /// </summary>
    // public static class TargetType
    // {
    //     /// <summary>
    //     /// All agents present when the action is started (ie. at the action's associated location when the
    //     /// agent begins to perform it) receive the target effects.
    //     /// This target type could be used for an agent making a public speech.
    //     /// </summary>
    //     public const string ALL = "all";

    //     /// <summary>
    //     /// Only agents who are present when the action is started (ie. at the action's associated location 
    //     /// when the agent begins to perform it) and who fit the SpecificPeoplePresent criteria of the people
    //     /// requirement receive the target effects.
    //     /// This is used for actions that only apply to a certain few people.
    //     /// </summary>
    //     public const string SPECIFIC = "specific";

    //     /// <summary>
    //     /// A single, random agent who is present when the action is started (ie. at the action's associated 
    //     /// location when the agent begins to perform it) and who fits the SpecificPeoplePresent criteria of the
    //     /// people requirement receives the target effects.
    //     /// This could be used for an action that has one agent chat with a single friend out of a few specific options.
    //     /// </summary>
    //     public const string SPECIFIC_SINGLE = "specific_single";

    //     /// <summary>
    //     /// A single, random agent who is present when the action is started (ie. at the action's associated
    //     /// location when the agent begins to perform it) receives the target effects.
    //     /// This can be used for an action such as asking a stranger for directions.
    //     /// </summary>
    //     public const string RANDOM_PRESENT = "random_present";
    // }

    /// <summary>
    /// Action or behavior to be executed by an agent (ex. sleep).
    /// </summary>
    // public class PrimaryAction : Action
    // {
    //     public string ActType { get; set; } = "Primary";
    // }

    // /// <summary>
    // /// Action or Behavior to be executed by an agent (ex. go to dinner).
    // /// </summary>
    // public class ScheduleAction : Action
    // {
    //     public string ActType { get; set; } = "Scheduled";

	// 	/// <summary>
    //     /// Optional flag to be set if this action is performed immediately rather than scheduled for later.
    //     /// </summary>
    //     public bool Interrupt = false;

    //     /// <summary>
    //     /// Primary action that will be performed by the instigator of this action.
    //     /// </summary>
    //     // [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    //     public string InstigatorAction { get; set; } = string.Empty;

    //     /// <summary>
    //     /// Primary action that will be performed by the target of this action.
    //     /// </summary>
    //     // [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    //     public string TargetAction { get; set; } = string.Empty;

    //     /// <summary>
    //     /// The method of choosing which agent(s) will be the target of this action.
    //     /// NOTE: some target methods require the action to have a people requirement to function properly.
    //     /// </summary>
    //     // [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    //     public string Target { get; set; } = string.Empty;

	// 	/// <summary>
    //     /// List of current targets for the this action.
    //     /// </summary>
    //     public List<Agent> CurrentTargets { get; set; } = new();
    // }
}
