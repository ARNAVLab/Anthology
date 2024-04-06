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
			static string getNewLocName(LocationNode node){
				return node.Name + ":" + node.X + "," + node.Y;
			}
			
			if (!LocationsByPosition.ContainsKey(new(node.X, node.Y))){
				if (LocationsByName.ContainsKey(node.Name)){
					LocationsByName[node.Name].Name = getNewLocName(LocationsByName[node.Name]);
					node.Name = getNewLocName(node);
				}
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
			
            
        }

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
					DistanceMatrix[loc1,loc2] = float.MaxValue; // / 2f) - 1f;
				}
			}

            foreach (LocationNode loc1 in LocationsByName.Values){
                foreach (KeyValuePair<LocationNode, float> connection in loc1.Connections)
                {
                    LocationNode loc2 = connection.Key;
                    DistanceMatrix[loc1, loc2] = connection.Value;

					// Initializing the nextArray if there's an edge between the nodes 
					NextInPath[loc1, loc2] = loc2;
					NextInPath[loc2, loc1] = loc1;
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
        public static List<LocationNode> LocationsSatisfyingLocationRequirement(RLocation locationReq, IEnumerable<LocationNode> checkLocations = null)
        {
			List<LocationNode> matches = new();

			checkLocations ??= LocationsByName.Values;
			
			foreach (LocationNode location in checkLocations)
			{
				if(location.SatisfiesLocationRequirements(locationReq))
					matches.Add(location);
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
        public static List<LocationNode> LocationsSatisfyingPeopleRequirement(RPeople requirements, Agent agent, IEnumerable<LocationNode> checkLocations)
        {
			List<LocationNode> matches = new();
			checkLocations ??= LocationsByName.Values;

			bool IsLocationValid(LocationNode location){
                if (location.AgentsPresent.Contains(agent.Name)) {
                    return location.SatisfiesPeopleRequirements(requirements, agent);
                }
                else {
					// making sure the People requirements take the agent into account for the test
                    location.AgentsPresent.Add(agent.Name);
                    bool valid = location.SatisfiesPeopleRequirements(requirements, agent);
                    location.AgentsPresent.Remove(agent.Name);
                    return valid;
                }
            }

            foreach (LocationNode location in checkLocations)
            {
                if (IsLocationValid(location)) 
					matches.Add(location);
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

		/// <summary>
		/// Connect any given location to it's neighboring locations
		/// Adds an edge between the locations
		/// </summary>
		/// <param name="location">Location to be connected to neighbors</param>
		internal static void ConnectToNeighbors(LocationNode location){
			List<LocationNode> around = FindPOIsAroundLocation(location.X, location.Y);
			foreach (LocationNode neighbor in around){
				location.Connections[neighbor] = 1;
				neighbor.Connections[location] = 1;
			}
		}
		
		/// <summary>
		/// Creates connected edges in the graph between roads, and location nodes disconnected from roads in the graph
		/// An edge between two points implies that the agent can travel between them
		/// Currently we assume all neighboring distances are a single unit in distance. 
		/// </summary>
		internal static void UpdateConnections()
		{
			foreach (LocationNode road in LocationsByTag["Road"]){
				ConnectToNeighbors(road);
			}

			// Assumption: If there are any locations that are not near roads, connect them to nearby locations
			// Kind of the equivalent of traipsing through neighboring buildings and backyards till you reach a road
			foreach (LocationNode location in LocationsByName.Values){
				if (!location.Connections.Any()){
					ConnectToNeighbors(location);
				}
			}
		}

		/// <summary>
		/// Finds the path between any two locations on the map 
		/// Uses a lazy approach, once a path has been calculated it's saved so that it's not calculated again.
		/// </summary>
		/// <param name="startLoc">Start Location</param>
		/// <param name="endLoc">End Locatino</param>
		/// <returns></returns>
		internal static List<string> FindPathsBetween(LocationNode startLoc, LocationNode endLoc){
			if(DiscoveredPaths.ContainsKey(startLoc, endLoc)){
				return DiscoveredPaths[startLoc, endLoc].Select(loc => loc.Name).ToList();
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

					return path.Select(loc => loc.Name).ToList();
				}
			}
		}

		/// <summary>
		/// Finds the valid points on the location map around a given x,y coordinate
		/// Valid points are defined as those the agent can traverse to in using the cardinal (N,S,E,W) directions
		/// </summary>
		/// <param name="x">X Coordinate</param>
		/// <param name="y">Y Coordinate</param>
		/// <returns>List of valid locations around the point</returns>
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
