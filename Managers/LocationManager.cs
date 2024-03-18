using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Jint.Parser;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using Random = System.Random;
using Vector2 = System.Numerics.Vector2;

namespace Anthology.Models
{
    /// <summary>
    /// Provides functionality for checking location-centric conditions.
    /// </summary>
    public static class LocationManager
    {
        /// <summary>
        /// Locations in the simulation stored by name.
        /// </summary>
        public static Dictionary<string, LocationNode> LocationsByName { get; set; } = new();

        /// <summary>
        /// Locations in the simulation stored by (X,Y) position.
        /// </summary>
        public static Dictionary<Vector2, LocationNode> LocationsByPosition { get; set; } = new();

        /// <summary>
        /// Locations in the simulation stored by tags for action selection.
        /// </summary>
        public static Dictionary<string, List<LocationNode>> LocationsByTag { get; set; } = new();

        /// <summary>
        /// The total number of locations in the simulation.
        /// </summary>
        public static int LocationCount { get; set; } = 0;

		private static Random rand = new Random();

		public static MKDictionary<LocationNode, LocationNode, List<LocationNode>> DiscoveredPaths { get; set; } = new MKDictionary<LocationNode, LocationNode, List<LocationNode>>();

        /// <summary>
        /// The directed distance matrix for determining the travel distance between
        /// any two locations. To obtain the distance from A to B, index into the
        /// matrix like: [A.ID * LocationCount + B.ID].
        /// </summary>
        // public static float[] DistanceMatrix { get; set; } = Array.Empty<float>();
		public static MKDictionary<LocationNode, LocationNode, float> DistanceMatrix { get; set; } = new MKDictionary<LocationNode, LocationNode, float>();
		public static MKDictionary<LocationNode, LocationNode, LocationNode> NextInPath { get; set; } = new MKDictionary<LocationNode, LocationNode, LocationNode>();

		// public static Dictionary<string, List<LocationNode>> LocationsByTag { get; set; } = new();

        /// <summary>
        /// Initialize/reset all static location manager variables compute the distance matrix.
        /// </summary>
        /// <param name="path">Path of JSON file to load locations from.</param>
        public static void Init(string path)
        {
            Reset();
            World.ReadWrite.LoadLocationsFromFile(path);
            UpdateDistanceMatrix();
        }

        /// <summary>
        /// Resets all storage of locations in the simulation.
        /// </summary>
        public static void Reset()
        {
            LocationsByName.Clear();
            LocationsByPosition.Clear();
            LocationsByTag.Clear();
			DistanceMatrix = new();
            LocationCount = 0;
        }

		public static LocationNode GetRandomLocation(){
			LocationNode location = LocationsByName.ElementAt(rand.Next(0, LocationsByName.Count)).Value;
			return location;
		}

        /// <summary>
        /// Adds a location accordingly to each location data structure.
        /// </summary>
        /// <param name="node">The location to add to the simulation.</param>
        public static void AddLocation(LocationNode node)
        {
            LocationsByName.Add(node.Name, node);
            LocationsByPosition.Add(new(node.X, node.Y), node);
            node.ID = LocationCount++;

            foreach (string tag in node.Tags)
            {
                if (!LocationsByTag.ContainsKey(tag))
                    LocationsByTag.Add(tag, new());
                LocationsByTag[tag].Add(node);
            }
        }

        /// <summary>
        /// Creates and adds a location to each of the static data structures.
        /// </summary>
        /// <param name="name">Name of the location.</param>
        /// <param name="x">X-coordinate of the location.</param>
        /// <param name="y">Y-coordinate of the location.</param>
        /// <param name="tags">Relevant tags of the location.</param>
        /// <param name="connections">Connections from the location to others.</param>
		/// 	Sasha delete?
 ///        // public static void AddLocation(string name, float x, float y, IEnumerable<string> tags, Dictionary<LocationNode, float> connections)
        // {
        //     List<string> newTags = new();
        //     newTags.AddRange(tags);
        //     AddLocation(new() { Name = name, X = x, Y = y, Tags = newTags, Connections = connections });
        // }

        /// <summary>
        /// Resets and populates the static distance matrix with all-pairs-shortest-path
        /// via the Floyd-Warshall algorithm.
        /// </summary>
        public static void UpdateDistanceMatrix()
        {
			LocationManager.UpdateConnections();

			foreach (LocationNode loc1 in LocationsByName.Values)
			{
				foreach (LocationNode loc2 in LocationsByName.Values)
				{
					if (loc1 == loc2) DistanceMatrix[loc1,loc2] = 0;
					DistanceMatrix[loc1,loc2] = (float.MaxValue / 2f) - 1f;
				}
			}

            foreach (LocationNode loc1 in LocationsByName.Values){
                foreach (KeyValuePair<LocationNode, float> connection in loc1.Connections)
                {
                    LocationNode loc2 = connection.Key;
                    DistanceMatrix[loc1, loc2] = connection.Value;

					// Initializing the nextArray if there's an edge between the nodes 
					NextInPath[loc1, loc2] = loc2;
                }
            };

			foreach (LocationNode locK in LocationsByName.Values){
				foreach (LocationNode locI in LocationsByName.Values) {
					foreach (LocationNode locJ in LocationsByName.Values) {
						float d = DistanceMatrix[locI, locK] + DistanceMatrix[locK, locJ];
						if (DistanceMatrix[locI, locJ] > d){
							DistanceMatrix[locI, locJ] = d;
							NextInPath[locI, locJ] = NextInPath[locI, locK];
						}
					}
				}
			}
		}



        /// <summary>
        /// Filter all locations to find those locations that satisfy conditions specified in the location requirement.
        /// Returns an enumerable of locations that match the HasAllOf, HasOneOrMOreOf, and HasNoneOf constraints.
        /// </summary>
        /// <param name="requirements">Requirements that locations must satisfy in order to be returned.</param>
        /// <returns>Returns all the locations that satisfied the given requirement, or an empty enumerable if none match.</returns>
        public static List<LocationNode> LocationsSatisfyingLocationRequirement(RLocation requirements)
        {
            List<LocationNode> matches = new();
            if (requirements.HasOneOrMoreOf.Count() > 0)
            {
                foreach (string tag in requirements.HasOneOrMoreOf)
                {
                    matches.AddRange(LocationsByTag[tag]);
                }
            }
            else
            {
                matches.AddRange(LocationsByName.Values);
            }
            if (requirements.HasAllOf.Count() > 0)
            {
                foreach (string tag in requirements.HasAllOf)
                {
                    matches = matches.Intersect(LocationsByTag[tag]).ToList();
                }
            }
            if (requirements.HasNoneOf.Count() > 0)
            {
                foreach (string tag in requirements.HasNoneOf)
                {
                    matches = matches.Except(LocationsByTag[tag]).ToList();
                }
            }
            return matches;
        }

        /// <summary>
        /// Filter given locations to find those locations that satisfy conditions specified in the people requirement.
        /// Returns locations that match the MinNumPeople, MaxNumPeople, SpecificPeoplePresent, SpecificPeopleAbsent,
        /// RelationshipsPresent, and RelationshipsAbsent requirements.
        /// </summary>
        /// <param name="locations">The set of locations to filter.</param>
        /// <param name="requirements">Requirements that locations must satisfy to be returned.</param>
        /// <param name="agent">Agent relevant for handling agent requirement(s).</param>
        /// <returns>Returns all the locations that satisfied the given requirement, or an empty enumerable if none match.</returns>
        public static List<LocationNode> LocationsSatisfyingPeopleRequirement(IEnumerable<LocationNode> locations, RPeople requirements, string agent_name = "")
        {
			bool IsLocationValid(LocationNode location){
                if (agent_name == "" || location.AgentsPresent.Contains(agent_name)) {
                    return location.SatisfiesRequirements(requirements);
                }
                else {
					// making sure the People requirements take the agent into account for the test
                    location.AgentsPresent.Add(agent_name);
                    bool valid = location.SatisfiesRequirements(requirements);
                    location.AgentsPresent.Remove(agent_name);
                    return valid;
                }
            }

            List<LocationNode> matches = new();
            foreach (LocationNode location in locations)
            {
                if (IsLocationValid(location)) matches.Add(location);
            }

            return matches;
        }

        /// <summary>
        /// Finds the nearest location of a given set from a specified location.
        /// </summary>
        /// <param name="from">The source location.</param>
        /// <param name="locations">The locations to filter for the closest.</param>
        /// <returns></returns>
        public static LocationNode FindNearestLocationFrom(LocationNode from, IEnumerable<LocationNode> locations)
        {
            IEnumerator<LocationNode> enumerator = locations.GetEnumerator();
            enumerator.MoveNext();
            LocationNode nearest = enumerator.Current;
            float dist = DistanceMatrix[from, nearest];
            
            while (enumerator.MoveNext())
            {
				LocationNode check = enumerator.Current;
                if (dist > DistanceMatrix[from, check])
                {
                    nearest = enumerator.Current;
                    dist = DistanceMatrix[from, check];
                }
            }
            return nearest;
        }

		internal static void UpdateConnections()
		{
			foreach (LocationNode road in LocationsByTag["Road"])
			{
				List<LocationNode> around = FindPOIsAroundLocation(road.X, road.Y);
				foreach (LocationNode location in around)
				{
					road.Connections[location] = 1;
					location.Connections[road] = 1;
				}
			}
		}

		internal static List<LocationNode> FindPathsBetween(LocationNode startLoc, LocationNode endLoc){
			if(DiscoveredPaths.ContainsKey(startLoc, endLoc)){
				return DiscoveredPaths[startLoc, endLoc];
			}
			else {
				if(!NextInPath.ContainsKey(startLoc,endLoc)) {
					return new();
				}
				else {
					List<LocationNode> path = new List<LocationNode>();
					path.Add(startLoc);
					
					LocationNode temp = startLoc;
					while(temp != endLoc){
						temp = NextInPath[temp, endLoc];
						path.Add(temp);
					}

					// storing path so it's only calculated once
					DiscoveredPaths[startLoc, endLoc] = path;
					List<LocationNode> _oppPath = new List<LocationNode>(path);
					_oppPath.Reverse();

					// assumption: path is bidirectional (for simplicity) - storing path 
					DiscoveredPaths[endLoc, startLoc] = _oppPath; 

					return path;
				}
			}
		}

		private static List<LocationNode> FindPOIsAroundLocation(float x, float y)
		{
			List<LocationNode> around = new();
			LocationNode _temp;
			if(LocationsByPosition.TryGetValue(new(x + 1, y), out _temp)){
				around.Add(_temp);
			} 
			if(LocationsByPosition.TryGetValue(new(x, y + 1), out _temp)){
				around.Add(_temp);
			} 
			if(LocationsByPosition.TryGetValue(new(x - 1, y), out _temp)){
				around.Add(_temp);
			} 
			if(LocationsByPosition.TryGetValue(new(x, y - 1), out _temp)){
				around.Add(_temp);
			} 
			return around;
		}
	}

	public class MKDictionary<TKey1,TKey2,TValue> :  Dictionary<Tuple<TKey1, TKey2>, TValue>, IDictionary<Tuple<TKey1, TKey2>, TValue> {
		public TValue this[TKey1 key1, TKey2 key2] {
			get { return base[Tuple.Create(key1, key2)]; }
			
			set { base[Tuple.Create(key1, key2)] = value; }
		}

		public void Add(TKey1 key1, TKey2 key2, TValue value)
		{
			base.Add(Tuple.Create(key1, key2), value);
		}

		public bool ContainsKey(TKey1 key1, TKey2 key2)
		{
			return base.ContainsKey(Tuple.Create(key1, key2));
		}
	}

}
