using System.Text.Json;

namespace Anthology.Models
{
    public static class ActionManager
    {
        /** Actions available in the simulation */
        public static ActionContainer Actions { get; set; } = new();

        /** Initialize/reset all action manager variables */
        public static void Init(string path)
        {
            Actions.ScheduleActions.Clear();
            Actions.PrimaryActions.Clear();
            LoadActionsFromFile(path);
        }

        /** Retrieves an action with the specified name from the set of actions available in the simulation */
        public static Action GetActionByName(string actionName)
        {
            bool HasName(Action action)
            {
                return action.Name == actionName;
            }

            Action action;
            try
            {
                action = Actions.PrimaryActions.First(HasName);
            }
            catch (Exception)
            {
                try
                {
                    action = Actions.ScheduleActions.First(HasName);
                }
                catch (Exception)
                {
                    throw new Exception("Action with name: " + actionName + " cannot be found.");
                }
            }
            return action;
        }

        /**
         * Returns the net effect for an action for a specific agent
         * Takes into account the agent's current motivation statuses
         */
        public static float GetEffectDeltaForAgentAction(Agent agent, Action? action)
        {
            float deltaUtility = 0f;

            if (action is PrimaryAction pAction)
            {
                foreach (Effect e in pAction.Effects)
                {
                    float delta = e.Delta;
                    float current = agent.Motives[e.Type].Amount;
                    deltaUtility += Math.Clamp(delta + current, Motive.MIN, Motive.MAX) - current;
                }
                return deltaUtility;
            }
            else if (action is ScheduleAction sAction)
            {
                return GetEffectDeltaForAgentAction(agent, GetActionByName(sAction.InstigatorAction));
            }

            return deltaUtility;
        }

        /** Returns a JSON string representing the set of all actions in the simulation */
        public static string SerializeAllActions()
        {
            return JsonSerializer.Serialize(Actions, UI.Jso);
        }

        /**
         * Populates the set of actions in the simulation from the given file path
         * If the given file cannot be read or is formatted incorrectly, an exception is thrown
         */
        public static void LoadActionsFromFile(string path)
        {
            string actionsText = File.ReadAllText(path);
            ActionContainer? actions = JsonSerializer.Deserialize<ActionContainer>(actionsText, UI.Jso);
            if (actions == null) return;
            Actions = actions;
        }
    }
}
