using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Jint.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Amazon.Runtime.Internal.Util;
using System.Diagnostics;

namespace Anthology.Models
{
    /// <summary>
    /// JSON serialization and deserialization manager for .NET framework to use.
    /// </summary>
    public class NetJson : JsonRW 
    {
        /// <summary>
        /// Gets the serialization options for JSON serialization.
        /// </summary>
        public static JsonSerializerOptions Jso { get; } = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
			// JsonSerializerOptions.Converters.Add(new AgentJSONConverter())
        };

        /// <summary>
        /// Initializes the world given a path to the JSON file containing paths for action, location, and agent
        /// JSON files.
        /// </summary>
        /// <param name="pathsFile">Path to JSON file containing paths to JSON files needed to initialize agents, locations, and actions.</param>
        /// <exception cref="FormatException">Thrown when JSON file does not have appropriate internal file paths.</exception>
        public override void InitWorldFromPaths(string pathsFile)
        {
            using FileStream os = File.OpenRead(pathsFile);
            Dictionary<string, string> filePaths = JsonSerializer.Deserialize<Dictionary<string, string>>(os, Jso);
            if (filePaths == null || filePaths.Count < 3) { throw new FormatException("Unable to load Anthology world state from file"); ; }
            World.Init(filePaths["Actions"], filePaths["Agents"], filePaths["Locations"]);
        }

        /// <summary>
        /// Loads all actions from a JSON file.
        /// </summary>
        /// <param name="path">Path to JSON file containing all actions.</param>
        public override void LoadActionsFromFile(string path) 
        {
            string actionsText = File.ReadAllText(path);
            ActionContainer actions = JsonSerializer.Deserialize<ActionContainer>(actionsText, Jso);
            if (actions == null) return;
            ActionManager.Actions = actions;
        }

        /// <summary>
        /// Serializes all actions and formats them to a string.
        /// </summary>
        /// <returns>String representation of all actions.</returns>
        public override string SerializeAllActions()
        {
            return JsonSerializer.Serialize(ActionManager.Actions, Jso);
        }

        /// <summary>
        /// Loads all agents from a JSON file.
        /// </summary>
        /// <param name="path">Path of JSON file to load agents from.</param>
        public override void LoadAgentsFromFile(string path) 
        {
			string agentsText = File.ReadAllText(path);
			// List<JObject> responseObjects = JsonConvert.DeserializeObject<List<JObject>>(agentsText); //Deserialized JSON to type of JObject
			JObject responseObjects = JObject.Parse(agentsText);
			

			// JObject responseObject = JsonConvert.DeserializeObject<JObject>(agentsText);

			foreach (var agentObj in responseObjects)
			{
				Console.WriteLine(agentObj.Key, agentObj.Value);
				// LocationNode _location = agentObj.CurrentLocation;
			}
			
			// var robert = responseObject["Robert"].ToObject<Student>();
            // string agentsText = File.ReadAllText(path);
			// List<Agent> sAgents = JsonSerializer.Deserialize<List<Agent>>(agentsText, Jso);
			// // // List<Agent> sAgents = JsonConvert.DeserializeObject<List<Agent>>(agentsText); //<wrapper>(json);

            // foreach (Agent deserialized_agent in sAgents)
            // {
            //     AgentManager.Agents.Add(deserialized_agent);
            // }
        }

        /// <summary>
        /// Serializes all agents and formats them into a string.
        /// </summary>
        /// <returns>String representation of all serialized agents.</returns>
        public override string SerializeAllAgents()
        {
            // List<SerializableAgent> sAgents = new();
            // foreach(Agent a in AgentManager.Agents)
            // {
            //     sAgents.Add(SerializableAgent.SerializeAgent(a));
            // }

            return JsonSerializer.Serialize(AgentManager.Agents, Jso);
        }

        /// <summary>
        /// Loads all locations from a JSON file.
        /// </summary>
        /// <param name="path">Path of JSON file to load locations from.</param>
        public override void LoadLocationsFromFile(string path) 
        {
            string locationsText = File.ReadAllText(path);
            IEnumerable<LocationNode> locationNodes = JsonSerializer.Deserialize<IEnumerable<LocationNode>>(locationsText, Jso);

            if (locationNodes == null) return;
            foreach (LocationNode node in locationNodes)
            {
                LocationManager.AddLocation(node);
            }
        }

        /// <summary>
        /// Serializes all locations and formats them into a string.
        /// </summary>
        /// <returns>String representation of all serialized locations.</returns>
        public override string SerializeAllLocations()
        {
            return JsonSerializer.Serialize(LocationManager.LocationsByName.Values, Jso);
        }
    }
	// public class AgentJSONConverter : System.Text.Json.Serialization.JsonConverter<Agent>
	// {
	// 	// Overrides the JSONConverter for Agent, and assigns the correct location instance to CurrentLocation
	// 	public override Agent ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, Agent existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer){
	// 	// public override Agent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	// 		JObject jObject = JObject.Load(reader);

	// 		string currLocName = jObject["CurrentLocation"].ToObject<string>();
	// 		Action _action = null; 

	// 		if (jObject["CurrentAction"] != null && (string)jObject["CurrentAction"] != "wait_action"){
	// 			string currActionName = jObject["CurrentAction"].ToObject<string>();
	// 			_action = ActionManager.GetActionByName(currActionName);
	// 		}
	// 		else{
	// 			_action = ActionManager.GetActionByName("wait_action");
	// 		}
	// 		LocationNode _currentLocation = LocationManager.LocationsByName[currLocName];

	// 		Agent agent = (Agent)base.ReadJson(jObject.CreateReader(), objectType, existingValue, serializer);
	// 		agent.CurrentLocation = _currentLocation;
	// 		agent.CurrentAction.AddFirst(_action);
			
	// 		return agent;
	// 	}

	// 	public override bool CanRead
	// 	{
	// 		get { return true; }
	// 	}
	// 	public override bool CanWrite
	// 	{
	// 		get { return false; }
	// 	}

	// 	public override void WriteJson(JsonWriter writer, Agent value, Newtonsoft.Json.JsonSerializer serializer)
	// 	{
	// 		//Writes the code as the value for the object
	// 		// writer.WriteValue(value);
	// 		// serializer.Serialize(writer, Convert.ToInt32(value));
	// 		writer.WriteStartObject();

	// 		Type vType = value.GetType();
	// 		MemberInfo[] properties = vType.GetProperties(BindingFlags.Public
	// 											| BindingFlags.Instance);

	// 		foreach (PropertyInfo property in properties)
	// 		{
	// 			object serValue = null;
	// 			if (property.Name == "CurrentLocation")
	// 			{
	// 				LocationNode _location = (LocationNode)property.GetValue(value, null); //    Convert.ToInt32(property.GetValue(value, null));
	// 				serValue = _location.Name;
	// 			}
	// 			else if(property.Name == "CurrentAction" ){
	// 				LinkedList<Action> _actions = (LinkedList<Action>)property.GetValue(value, null);
	// 				serValue = _actions.First.Value.Name;
	// 			}
	// 			else
	// 			{
	// 				serValue = property.GetValue(value, null);
	// 			}
	// 			writer.WritePropertyName(property.Name);
	// 			serializer.Serialize(writer, serValue);
	// 		}
	// 		writer.WriteEndObject();
	// 	}

	// 	public override Agent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	// 	{
	// 		throw new NotImplementedException();
	// 	}

	// 	public override void Write(Utf8JsonWriter writer, Agent value, JsonSerializerOptions options)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	// }
}