using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using UnityEngine;
using Random = System.Random;

namespace Anthology.Models
{
	public class Motive { 
		/// <summary>
        /// The maximum value of a motive.
        /// </summary>
        public const int MAX = 5;

        /// <summary>
        /// The minimum value of a motive.
        /// </summary>
        public const int MIN = 1;

		private static Random random = new Random();

		// Motives we're currently allowing the agents to have 
		private float _accomplishment = random.Next(MIN,MAX);
		
		[JsonPropertyName("accomplishment")]
		public float Accomplishment { 
			get => _accomplishment;
			set {
				_accomplishment = Math.Clamp(value, Motive.MIN, Motive.MAX);
			}
		}

		private float _emotional = random.Next(MIN,MAX);

		[JsonPropertyName("emotional")]
		public float Emotional { 
			get => _emotional;
			set {
				_emotional = Math.Clamp(value, Motive.MIN, Motive.MAX);
			}
		}

		private float _financial = random.Next(MIN,MAX);

		[JsonPropertyName("financial")]
		public float Financial { 
			get => _financial;
			set {
				_financial = Math.Clamp(value, Motive.MIN, Motive.MAX);
			}
		}

		private float _social = random.Next(MIN,MAX);

		[JsonPropertyName("social")]
		public float Social { 
			get => _social;
			set {
				_social = Math.Clamp(value, Motive.MIN, Motive.MAX);
			}
		}
 
		private float _physical = random.Next(MIN,MAX);

		[JsonPropertyName("physical")]
		public float Physical { 
			get => _physical;
			set {
				_physical = Math.Clamp(value, Motive.MIN, Motive.MAX);
			}
		}

		/// <summary>
		/// Lets us dynamically set the properties of the Motive instance as though it were a dictionary
		/// Eg. setter: agent.Motives["Accomplishment"] = 10;
		/// Eg. getter: float something = agent.Motives["Financial"];
		/// </summary>
		/// <param name="motiveName">Motive Name (eg. Accomplishment, Financial, etc)</param>
		/// <returns>Property value</returns>
		public float this[string motiveName]
		{
			get { 
				PropertyInfo prop = this.GetType().GetProperty(motiveName);
				if(prop == null)
					throw new ArgumentException(String.Format(
						"{0} is not a Motive.",
						motiveName));
				return  (float)prop.GetValue(this, null); 
			}
			set {
				PropertyInfo prop = this.GetType().GetProperty(motiveName);
				if(prop == null)
					throw new ArgumentException(String.Format(
						"{0} is not a Motive.",
						motiveName));
				prop.SetValue(this, (float)value, null); 
			}
		}

		/// <summary>
        /// Returns whether the agent is content, ie. checks to see if an agent has the maximum motives.
        /// </summary>
        /// <returns>True if all motives are at max.</returns>
        public bool IsContent()
        {
			if (Accomplishment < MIN | Emotional < MIN | Financial < MIN | Social < MIN | Physical < MIN) return true;
			
			return false;
        }

		public bool checkReqThreshold(RMotive motiveReq)
		{
			string t = motiveReq.MotiveType;
			float c = motiveReq.Threshold;
			switch (motiveReq.Operation)
			{
				case BinOps.EQUALS:
					if (!((float) this[t] == c)) return false;
					break;

				case BinOps.LESS:
					if (!((float) this[t] < c)) return false;
					break;

				case BinOps.GREATER:
					if (!((float) this[t] > c)) return false;
					break;

				case BinOps.LESS_EQUALS:
					if (!((float) this[t] <= c)) return false;
					break;

				case BinOps.GREATER_EQUALS:
					if (!((float) this[t] >= c)) return false;
					break;

				default:
					Console.WriteLine("ERROR - JSON BinOp specification mistake for Motive Requirement for action");
					return false;
			}
			return true;
		}

		public Dictionary<string, float> ToDictionary(){
			return new Dictionary<string, float>()
				{
					{"Accomplishment", _accomplishment},
					{"Emotional", _emotional},
					{"Financial", _financial},
					{"Social", _social},
					{"Physical", _physical}
				};
		}

		public void FromDictionary(Dictionary<string, float> motiveDict){
			Accomplishment = motiveDict["Accomplishment"];
			Emotional = motiveDict["Emotional"];
			Financial = motiveDict["Financial"];
			Social = motiveDict["Social"];
			Physical = motiveDict["Physical"];
		}

		/// <summary>
		/// Decrements all motives by 1 over time 
		/// </summary>
		public void DecrementMotives(){
			Accomplishment = _accomplishment - 1;
			Emotional =  _emotional - 1;
			Financial =  _financial - 1;
			Social =  _social - 1;
			Physical = _physical - 1;
		}

		/// <summary>
		/// Randomly generate motives (currently only used in the AnthologyFactory for benchmarks)
		/// </summary>
		public static Motive MakeRandomMotives(){
			System.Random random = new();
			return new(){
				Accomplishment = random.Next(4)+1,
				Emotional = random.Next(4)+1,
				Financial =  random.Next(4)+1,
				Social =  random.Next(4)+1,
				Physical = random.Next(4)+1
			};
		}
	}
}