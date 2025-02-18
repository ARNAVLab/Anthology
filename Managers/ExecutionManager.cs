﻿using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
// using Utils;

namespace Anthology.Models
{
	/// <summary>
	/// Manages execution and lifetime of the simulation.
	/// </summary>
	public static class ExecutionManager
    {

		// public PriorityQueue<ScheduledAction,int> scheduledActions = new();

        /// <summary>
        /// Initializes the simulation by initializing the world.
        /// </summary>
        /// <param name="pathToFiles">Path of JSON file containing relevant paths to other JSON files.</param>
        public static void Init(string pathToFiles)
        {
            World.ReadWrite.InitWorldFromPaths(pathToFiles);
        }

        /// <summary>
        /// Executes a turn for each agent every tick.
        /// Executes a single turn and then must be called again.
        /// </summary>
        /// <param name="steps">Number of steps to advance the simulation by.</param>
        public static void RunSim(int steps = 1)
        {
            for (int i = 0; i < steps; i++)
            {
                if (ToContinue())
                {
					foreach (Agent agent in AgentManager.Agents.Values) {
						Turn(agent);
					}
					
                    // Parallel.ForEach(AgentManager.Agents.Values, agent =>
                    // {
                    //     Turn(agent);
                    // });

                    World.IncrementTime();
                }
                else if(!UI.Paused)
                {
                    Console.WriteLine("Simulation ended.");
                }
            }

            UI.Update();
        }

        /// <summary>
        /// Tests whether the simulation should continue.
        /// First checks whether the stopping function for the simulation has been met.
        /// Next checks if the user has paused the simulation.
        /// </summary>
        /// <returns>True if the simulation should continue.</returns>
        public static bool ToContinue()
        {
            return !(UI.Paused || AgentManager.AllAgentsContent());
        }

        /// <summary>
        /// Updates movement and occupation counters for an agent.
        /// May decrement the motives of an agent once every 10 hours. Chooses or executes an action when necessary.
        /// </summary>
        /// <param name="agent">The agent whose turn will happen.</param>
        /// <returns>True if agent moved from original position.</returns>
        public static bool Turn(Agent agent)
        {
            bool movement = false;
			if (agent.CurrentAction.Any()){
				if (agent.CurrentAction.First.Value.Name == "travel_action" && agent.Destination.Any()) {
					movement = true;
					ActionManager.MoveCloserToDestination(agent);
				}

				if (agent.OccupiedCounter > 0){
					agent.OccupiedCounter--;                
				}

				// If not travelling (i.e. arrived at destination), and end of occupied, execute planned action effects, select/start next.
				else
				{
					ActionManager.ExecuteAction(agent);
					
					// if (!agent.Motives.IsContent())
					// {
						if (agent.CurrentAction.Count == 0)
						{
							ActionManager.SelectNextAction(agent);
						}
						else
						{
							ActionManager.StartAction(agent);
						}
					// }
				}
				return movement;
			}
			ActionManager.SelectNextAction(agent);
			return false;
        }

        /// <summary>
        /// Interrupt the agent whose name matches given name.
        /// </summary>
        /// <param name="agentName">The name of the agent to interrupt.</param>
        // public static void Interrupt(string agentName)
        // {
        //     Agent agent = AgentManager.GetAgentByName(agentName);
        //     if (agent != null)
        //     {
        //         ActionManager.Interrupt(agent);
        //     }
        // }
    }
}
