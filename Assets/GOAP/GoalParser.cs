using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq; // LINQ 사용을 위해 추가
public static class GoalParser
{
    public static Goal ParseSentenceToGoal(string sentence, List<GOAPAction> actions, float weight = 1f)
    {
        if (string.IsNullOrWhiteSpace(sentence) || sentence.Trim().ToLower() == "none")
            return null;

        sentence = sentence.Trim().TrimEnd('.');

        // "Do <action> in/on <location>"
        var matchActionAtLocation = Regex.Match(sentence, @"Do\s+(.+?)\s+(in|on)\s+(.+)", RegexOptions.IgnoreCase);
        if (matchActionAtLocation.Success)
        {
            string actionName = matchActionAtLocation.Groups[1].Value.Trim();
            string location = matchActionAtLocation.Groups[3].Value.Trim();
            string goalName = $"Do_{actionName}_at_{location}";

            var action = actions.Find(a => a.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));
            if (action != null)
            {
                Func<NPCState, WorldState, bool> condition = (npcState, worldState) =>
                {
                    if (!npcState.LowerBody.ContainsKey("location") || npcState.LowerBody["location"].ToString() != location)
                        return false;

                    foreach (var effect in action.Effects)
                    {
                        if (!CheckEffectApplied(effect.Key, effect.Value, npcState, worldState))
                            return false;
                    }
                    return true;
                };

                return new Goal(goalName, condition, weight, new Dictionary<string, object>(action.Effects));
            }
            else
            {
                Console.WriteLine($"Action '{actionName}' not found.");
                return null;
            }
        }

        // "Change <state> of <object> to <value>"
        var matchChangeState = Regex.Match(sentence, @"Change\s+(.+?)\s+of\s+(.+?)\s+to\s+(.+)", RegexOptions.IgnoreCase);
        if (matchChangeState.Success)
        {
            string stateKey = matchChangeState.Groups[1].Value.Trim();
            string objName = matchChangeState.Groups[2].Value.Trim();
            string desiredValue = matchChangeState.Groups[3].Value.Trim();
            string goalName = $"Change_{stateKey}_of_{objName}_to_{desiredValue}";

            Func<NPCState, WorldState, bool> condition = (npcState, worldState) =>
            {
                return CheckState(objName, stateKey, desiredValue, npcState, worldState);
            };

            return new Goal(goalName, condition, weight);
        }

        // "Go to <location>"
        var matchGoToLocation = Regex.Match(sentence, @"Go\s+to\s+(.+)", RegexOptions.IgnoreCase);
        if (matchGoToLocation.Success)
        {
            string location = matchGoToLocation.Groups[1].Value.Trim();
            string goalName = $"Go_to_{location}";

            Func<NPCState, WorldState, bool> condition = (npcState, worldState) =>
            {
                return npcState.LowerBody.ContainsKey("location") && npcState.LowerBody["location"].ToString() == location;
            };

            return new Goal(goalName, condition, weight);
        }

        // 1. "Pick up <item> at <place>" 패턴 검사
        var matchPickUpAt = Regex.Match(sentence, @"Pick\s+up\s+(.+?)\s+at\s+(.+)", RegexOptions.IgnoreCase);
        if (matchPickUpAt.Success)
        {
            string itemName = matchPickUpAt.Groups[1].Value.Trim();
            string placeName = matchPickUpAt.Groups[2].Value.Trim();
            string goalName = $"Pick_up_{itemName}_at_{placeName}";

            Func<NPCState, WorldState, bool> condition = (npcState, worldState) =>
            {
                // Goal is achieved when NPC has the item
                return npcState.Inventory.Contains(itemName);
            };

            return new Goal(goalName, condition, weight);
        }
        
        // "Pick up <item>"
        var matchPickUp = Regex.Match(sentence, @"Pick\s+up\s+(.+)", RegexOptions.IgnoreCase);
        if (matchPickUp.Success)
        {
            string itemName = matchPickUp.Groups[1].Value.Trim();
            string goalName = $"Pick_up_{itemName}";

            Func<NPCState, WorldState, bool> condition = (npcState, worldState) =>
            {
                return npcState.Inventory.Contains(itemName);
            };

            return new Goal(goalName, condition, weight);
        }
        
        // "Drop <item> at <location>"
        var matchDropItem = Regex.Match(sentence, @"Drop\s+(.+?)\s+at\s+(.+)", RegexOptions.IgnoreCase);
        if (matchDropItem.Success)
        {
            string itemName = matchDropItem.Groups[1].Value.Trim();
            string location = matchDropItem.Groups[2].Value.Trim();
            string goalName = $"Drop_{itemName}_at_{location}";

            Func<NPCState, WorldState, bool> condition = (npcState, worldState) =>
            {
                if (!npcState.LowerBody.ContainsKey("location") || npcState.LowerBody["location"].ToString() != location)
                    return false;

                if (!npcState.Inventory.Contains(itemName))
                    return false;

                if (!worldState.Places.ContainsKey(location))
                    return false;

                return worldState.Places[location].Inventory.Contains(itemName);
            };

            return new Goal(goalName, condition, weight);
        }

        // "Do <action>"
        var matchAction = Regex.Match(sentence, @"Do\s+(.+)", RegexOptions.IgnoreCase);
        if (matchAction.Success)
        {
            string actionName = matchAction.Groups[1].Value.Trim();
            string goalName = $"Do_{actionName}";

            var action = actions.Find(a => a.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));
            if (action != null)
            {
                Func<NPCState, WorldState, bool> condition = (npcState, worldState) =>
                {
                    if (action.Effects.Count == 0)
                        return npcState.StateData.ContainsKey($"did_{actionName}") && (bool)npcState.StateData[$"did_{actionName}"];

                    foreach (var effect in action.Effects)
                    {
                        if (!CheckEffectApplied(effect.Key, effect.Value, npcState, worldState))
                            return false;
                    }
                    return true;
                };

                return new Goal(goalName, condition, weight, new Dictionary<string, object>(action.Effects));
            }
            else
            {
                Console.WriteLine($"Action '{actionName}' not found.");
                return null;
            }
        }

        Console.WriteLine("Sentence parsing failed.");
        return null;
    }

    private static bool CheckEffectApplied(string effectKey, object effectValue, NPCState npcState, WorldState worldState)
    {
        bool result = false;
        if (npcState.UpperBody.ContainsKey(effectKey))
        {
            result = npcState.UpperBody[effectKey].ToString() == effectValue.ToString();
        }
        else if (npcState.LowerBody.ContainsKey(effectKey))
        {
            result = npcState.LowerBody[effectKey].ToString() == effectValue.ToString();
        }
        else if (npcState.Resources.ContainsKey(effectKey))
        {
            result = npcState.Resources[effectKey] == Convert.ToSingle(effectValue);
        }
        else if (effectKey == "hold")
        {
            result = npcState.UpperBody.ContainsKey("hold") && npcState.UpperBody["hold"].ToString() == effectValue.ToString();
        }
        else if (effectKey.StartsWith("place_state:"))
        {
            var parts = effectKey.Split(':');
            string placeName = parts[1];
            string stateKey = parts[2];
            if (worldState.Places.ContainsKey(placeName))
            {
                var place = worldState.Places[placeName];
                result = place.State.ContainsKey(stateKey) && place.State[stateKey].ToString() == effectValue.ToString();
            }
        }
        else if (effectKey.StartsWith("item_state:"))
        {
            var parts = effectKey.Split(':');
            string itemName = parts[1];
            string stateKey = parts[2];
            if (worldState.Items.ContainsKey(itemName))
            {
                var item = worldState.Items[itemName];
                result = item.State.ContainsKey(stateKey) && item.State[stateKey].ToString() == effectValue.ToString();
            }
        }
        else if (effectKey == "pose")
        {
            result = npcState.LowerBody.ContainsKey("pose") && npcState.LowerBody["pose"].ToString() == effectValue.ToString();
        }
        else if (effectKey == "use_item")
        {
            result = npcState.StateData.ContainsKey($"used_{effectValue}") && (bool)npcState.StateData[$"used_{effectValue}"];
        }
        else if (effectKey == "pickup_item")
        {
            string itemName = effectValue.ToString();
            result = npcState.Inventory.Contains(itemName);
        }
        else if (effectKey == "drop_item")
        {
            string itemName = effectValue.ToString();
            string currentLocation = npcState.LowerBody.ContainsKey("location") ? npcState.LowerBody["location"].ToString() : "";
            if (worldState.Places.ContainsKey(currentLocation))
            {
                result = worldState.Places[currentLocation].Inventory.Contains(itemName);
            }
        }
        else
        {
            result = npcState.StateData.ContainsKey(effectKey) && npcState.StateData[effectKey].ToString() == effectValue.ToString();
        }

        Console.WriteLine($"Effect '{effectKey}': '{effectValue}' check result: {result}");
        return result;
    }

    private static bool CheckState(string objName, string stateKey, string desiredValue, NPCState npcState, WorldState worldState)
    {
        if (objName.Equals("NPC", StringComparison.OrdinalIgnoreCase))
        {
            if (npcState.UpperBody.ContainsKey(stateKey))
            {
                return npcState.UpperBody[stateKey].ToString() == desiredValue;
            }
            else if (npcState.LowerBody.ContainsKey(stateKey))
            {
                return npcState.LowerBody[stateKey].ToString() == desiredValue;
            }
            else
            {
                return npcState.StateData.ContainsKey(stateKey) && npcState.StateData[stateKey].ToString() == desiredValue;
            }
        }
        else if (worldState.Places.ContainsKey(objName))
        {
            var place = worldState.Places[objName];
            return place.State.ContainsKey(stateKey) && place.State[stateKey].ToString() == desiredValue;
        }
        else if (worldState.Items.ContainsKey(objName))
        {
            var item = worldState.Items[objName];
            return item.State.ContainsKey(stateKey) && item.State[stateKey].ToString() == desiredValue;
        }
        else
        {
            return false;
        }
    }
}
