using System;
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
		public float Valence { get; set; } = 0;


		// Other class properties
		// public Relationship(string with, string rel_type, float valence){
		// 	With = with; 
		// 	Type = rel_type;
		// 	Valence = valence;
		// }
		
		public bool isRelationshipTypeWith(string rel_type, string with){
			return Type==rel_type && With==with;
		}

		/// <summary>
		/// Returns this Relationship as a string object for debugging and printing
		/// </summary>
		/// <returns>string</returns>
		public override string ToString() {
			return string.Format("Relationship => {0},{1}:{2}", Type, With, Valence);
		}


    }

}