﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using SimManager.SimulationManager;

namespace Anthology.Models
{
    /// <summary>
    /// Locations as used by the graph-based location system.
    /// </summary>
    public class LocationNode
    {
        /// <summary>
        /// The name of the location.
        /// </summary>
		[JsonPropertyName("Name")]	
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The X-coordinate of this location.
        /// </summary>
        [JsonPropertyName("X")]
		public float X { get; set; }

        /// <summary>
        /// The Y-coordinate of this location.
        /// </summary>
		[JsonPropertyName("Y")]
        public float Y { get; set; }

        /// <summary>
        /// The tags associated with this location.
        /// </summary>
		[JsonPropertyName("Tags")]
        public List<string> Tags { get; set;} = new();

        /// <summary>
        /// The connections between this location and others (directed edges).
        /// </summary>
        [JsonIgnore]
		public Dictionary<LocationNode, float> Connections { get; set; } = new();

        /// <summary>
        /// The agents located at this location.
        /// </summary>
        [JsonIgnore]
        public HashSet<string> AgentsPresent { get; set; } = new();

        /// <summary>
        /// The ID of this location as assigned by LocationManager when added.
        /// This is primarily useful for indexing into the distance matrix.
        /// </summary>
        [JsonIgnore]
        public int ID { get; set; }

		/// <summary>
		/// Returns connecting nodes 
		/// Currently assuming only 4 point connections (not diagnals)
		/// </summary>
		/// <returns>Array of LocationNodes</returns>
		public List<LocationNode> GetNeighbours()
		{
			return new List<LocationNode>(Connections.Keys);
		}

        /// <summary>
        /// Checks if this location satisfies all of the passed location requirements.
        /// </summary>
        /// <param name="reqs">Requirements to check for location.</param>
        /// <returns>True if location satisfies all requirements.</returns>
        public bool SatisfiesLocationRequirements(RLocation reqs)
        {
            return HasAllOf(reqs.HasAllOf) &&
                   HasOneOrMoreOf(reqs.HasOneOrMoreOf) &&
                   HasNoneOf(reqs.HasNoneOf);
        }

        /// <summary>
        /// Checks if this location satisfies all of the passed location requirements.
        /// </summary>
        /// <param name="reqs">Requirements to check for location.</param>
        /// <returns>True if location satisfies all requirements.</returns>
        public bool SatisfiesPeopleRequirements(RPeople peopleReq, Agent agent)
        {
            bool test =  HasMinNumPeople(peopleReq.MinNumPeople) &&
                   HasNotMaxNumPeople(peopleReq.MaxNumPeople) &&
                   SpecificPeoplePresent(peopleReq.SpecificPeoplePresent) &&
                   SpecificPeopleAbsent(peopleReq.SpecificPeopleAbsent) &&
                   RelationshipsPresent(peopleReq.RelationshipsPresent, agent) &&
				   RelationshipsAbsent(peopleReq.RelationshipsAbsent, agent) && 
				   HasRelationships(peopleReq.HasRelationships, agent);
			return test; 
        }

		private bool HasRelationships(List<string> hasRelationships, Agent agent)
		{
			if(agent.Relationships.Any(rel => hasRelationships.Contains(rel.Type))) return true; 
			return false; 
		}

		public List<Agent> FindTargetsForAction(RPeople peopleReq, Agent agent){
			HashSet<string> targets = new(); 
			
			if (!AgentsPresent.Any(person => person != agent.Name)){
				if (peopleReq.MinNumPeople <= 1){
					// Meets people requirements, but no targets
					return new();
				}
				else{
					// Fails people requirements
					return null;
				}
			}

			foreach (string person in peopleReq.SpecificPeoplePresent){
				if (AgentsPresent.Contains(person)) targets.Add(person);
			}

			foreach (string person in peopleReq.SpecificPeopleAbsent){
				if (AgentsPresent.Contains(person)) targets.Remove(person);
			}
			
			foreach(Relationship rel in agent.Relationships){
				if (peopleReq.RelationshipsPresent.Contains(rel.Type) && AgentsPresent.Contains(rel.With)){
					targets.Add(rel.With);
				}
				if (peopleReq.HasRelationships.Contains(rel.Type)){
					targets.Add(rel.With);
				}
				if (peopleReq.RelationshipsAbsent.Contains(rel.Type) && AgentsPresent.Contains(rel.With)){
					targets.Remove(rel.With);
				}
			}
			

			targets.Remove(agent.Name);

			// Not including the agent executing the action
			if (targets.Count < (peopleReq.MinNumPeople-1)){
				// fails people requirements
				return null;
			}

			List<Agent> targetted = new(); 
			foreach(string target in targets.Take(peopleReq.MaxNumPeople)){
				Agent person = AgentManager.GetAgentByName(target);
				if (person != null) targetted.Add(person);
			}

			// can be zero returned 
			return targetted;
		}

        /// <summary>
        /// Checks if location has all tags specified.
        /// </summary>
        /// <param name="hasAllOf">All tags to check.</param>
        /// <returns>True if location has all tags given.</returns>
        private bool HasAllOf(IEnumerable<string> hasAllOf)
        {
			if(hasAllOf.Count() == 0) return true;

			if(hasAllOf.All(tag => Tags.Contains(tag)))
				return true; 
			
			return false;
        }

        /// <summary>
        /// Checks if location satisfies at least one tag specified.
        /// </summary>
        /// <param name="hasOneOrMoreOf">The set of tags to check.</param>
        /// <returns>True if location has at least one of the tags specified.</returns>
        private bool HasOneOrMoreOf(IEnumerable<string> hasOneOrMoreOf)
        {
			if(hasOneOrMoreOf.Count() == 0) return true;

			if(hasOneOrMoreOf.Any(tag => Tags.Contains(tag)))
				return true; 
			return false; 
        }

        /// <summary>
        /// Checks if this location has none of the given tags.
        /// </summary>
        /// <param name="hasNoneOf">The set of tags to check.</param>
        /// <returns>True if location has none of the given tags.</returns>
        private bool HasNoneOf(IEnumerable<string> hasNoneOf)
        {
			if(hasNoneOf.Count() == 0) return true;

			if(hasNoneOf.Any(tag => Tags.Contains(tag)))
				return false; 
			return true; 
        }

        /// <summary>
        /// Checks if this location has at least a given amount of people.
        /// </summary>
        /// <param name="minNumPeople">The minimum amount of people.</param>
        /// <returns>True if location has at least the given amount of people.</returns>
        public bool HasMinNumPeople(short minNumPeople)
        {
			if (minNumPeople == 0) return true;
			
			return AgentsPresent.Count >= minNumPeople-1;
        }

        /// <summary>
        /// Checks if location has less than or equal to given amount of people.
        /// </summary>
        /// <param name="maxNumPeople">The max amount of people.</param>
        /// <returns>True if location has less than or equal to given amount of people.</returns>
        public bool HasNotMaxNumPeople(short maxNumPeople)
        {
			if (maxNumPeople == short.MaxValue) return true;

            return AgentsPresent.Count <= maxNumPeople-1;
        }

        /// <summary>
        /// Checks if location has given people.
        /// </summary>
        /// <param name="specificPeoplePresent">The set of people to check.</param>
        /// <returns>True if location has given people.</returns>
        internal bool SpecificPeoplePresent(IEnumerable<string> specificPeoplePresent)
        {
			if(!specificPeoplePresent.Any()) return true; 

            if(specificPeoplePresent.Any(person => !AgentsPresent.Contains(person)))
				return false; 

			return true; 
        }

        /// <summary>
        /// Checks if location does not have given people.
        /// </summary>
        /// <param name="specificPeopleAbsent">The set of people to check.</param>
        /// <returns>True if location does not have the given people.</returns>
        private bool SpecificPeopleAbsent(IEnumerable<string> specificPeopleAbsent)
        {
			if(!specificPeopleAbsent.Any()) return true; 
			
			if(specificPeopleAbsent.Any(person => AgentsPresent.Contains(person)))
				return false; 

			return true; 
        }

        /// <summary>
        /// Checks if given relationships are present at location.
        /// </summary>
        /// <param name="relationshipsPresent">The relationships to check.</param>
		/// <param name="agent">The agent whose relationships we're checking against.</param>		
        /// <returns>True if given relationships are present at location.</returns>
        internal bool RelationshipsPresent(IEnumerable<string> relationshipsPresent, Agent agent)
        {
			foreach (string checkRel in relationshipsPresent){
				if (!agent.Relationships.Any(rel => rel.Type == checkRel && AgentsPresent.Contains(rel.With))){
					return false;
				}
			}
			return true;
        }

		/// <summary>
        /// Checks to ensure given relationships are absent at location.
        /// </summary>
        /// <param name="relationshipsPresent">The relationships to check.</param>
		/// <param name="agent">The agent whose relationships we're checking against.</param>	
        /// <returns>True if given relationships are present at location.</returns>
		internal bool RelationshipsAbsent(IEnumerable<string> relationshipsAbsent, Agent agent)
        {
			foreach (string checkRel in relationshipsAbsent){
				if (agent.Relationships.Any(rel => rel.Type == checkRel && AgentsPresent.Contains(rel.With))){
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Returns this location as a string object for debugging and printing
		/// </summary>
		/// <returns>Name and coordinatess of the location</returns>
		public override string ToString() {
			return string.Format("{0}({1},{2})", Name, X, Y);
		}

		/// <summary>
		/// When an agent enters a location, the agent's name is added to the "Agents Present" list 
		/// This is used by the GUI to display the agents at a location
		/// </summary>
		/// <param name="agent">Agent entering the location</param>
		public void EnterLocation(Agent agent){
			AgentsPresent.Add(agent.Name);
		}

		/// <summary>
		/// Allow tags to be added to a location
		/// </summary>
		/// <param name="tag">tag to be added</param>
		public void AddTags(List<string> tags){
			Tags.AddRange(tags);
			LocationManager.dirtyLocations.Add(Name);
		}

		/// <summary>
		/// Allow tags to be removed to a location
		/// </summary>
		/// <param name="tag">tag to be added</param>
		public void RemoveTags(List<string> tags){
			foreach(string tag in tags){
				Tags.Remove(tag);
			}

			LocationManager.dirtyLocations.Add(Name);
		}


		/// <summary>
		/// When an agent exits a location, the agent's name is removed from the "Agents Present" list 
		/// This is used by the GUI to display the agents at a location
		/// </summary>
		/// <param name="agent">Agent exiting the location</param>
		public void LeaveLocation(Agent agent){
			AgentsPresent.Remove(agent.Name);
		}
    }
}
