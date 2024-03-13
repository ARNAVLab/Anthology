using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("Type")]
		public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The agent that this relationship is with.
        /// </summary>
        [JsonPropertyName("With")]
		public string With { get; set; } = string.Empty;

        /// <summary>
        /// How strong the relationship is.
        /// </summary>
        [JsonPropertyName("Valence")]
		public float Valence { get; set; }
    }

}