using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Text.RegularExpressions; // 정규 표현식 사용을 위한 네임스페이스 추가

// Assuming the definitions for Place, Item, GOAPAction, NPCState, WorldState, GOAPPlanner, ActionFactory, and GoalParser are available

public class GOAPExample : MonoBehaviour
{
    // Public references to assign via Inspector
    public NavMeshAgent npcAgent;
    public Animator characterAnimator;

    // Array to hold target objects (assign via Inspector)
    public GameObject[] targetObjects;

    // Define Places
    public Dictionary<string, Place> Places { get; private set; }
    private Dictionary<string, List<string>> placeConnections;

    // Define Items
    private Dictionary<string, Item> items;

    // Define Actions
    private List<GOAPAction> actions;

    // NPC and World State
    [SerializeField] private NPCState npcState;

    public NPCState NpcState
    {
        get { return npcState; }
    }
    private WorldState worldState;

    // Execution Control
    private bool isExecutingPlan = false;

    public Transform handTransform; // Inspector에서 할당

    // Gesture names list
    private List<string> gestureNames = new List<string>
    {
        "Bashful",
        "Happy",
        "Crying",
        "Thinking",
        "Talking",
        "Looking",
        "No",
        "Fist Pump",
        "Agreeing",
        "Arguing",
        "Thankful",
        "Excited",
        "Clapping",
        "Rejected",
        "Look Around" // Ensure Animator has triggers with exact names, including spaces if any
    };

    void Start()
    {
        // Initialize Places
        InitializePlaces();

        // Initialize Items
        InitializeItems();

        // Initialize NPC State
        InitializeNPCState();

        // Initialize World State
        worldState = new WorldState(Places, items);

        // Initialize Actions
        InitializeActions();
    }

    private void InitializePlaces()
    {
        Places = new Dictionary<string, Place>();
        placeConnections = new Dictionary<string, List<string>>();

        // Place의 GameObject를 씬에서 찾아 할당합니다.
        GameObject pianoGO = GameObject.Find("Piano");
        GameObject pictureGO = GameObject.Find("Picture");
        GameObject tvGO = GameObject.Find("TV");
        GameObject mejaGO = GameObject.Find("Meja");
        GameObject sofaGO = GameObject.Find("Sofa");

        if (pianoGO == null || pictureGO == null || tvGO == null || mejaGO == null || sofaGO == null)
        {
            Debug.LogError("One or more Place GameObjects not found in the scene. Please check the names.");
            return;
        }

        Place piano = new Place("piano", pianoGO, new List<string>(), new Dictionary<string, object>());
        Place picture = new Place("picture", pictureGO, new List<string>(), new Dictionary<string, object>());
        Place tv = new Place("tv", tvGO, new List<string>(), new Dictionary<string, object> { { "tv_state", "off" } });
        Place meja = new Place("meja", mejaGO, new List<string> { "map", "snack" }, new Dictionary<string, object>());
        Place sofa = new Place("sofa", sofaGO, new List<string> { "pillow" }, new Dictionary<string, object>());

        Places.Add("piano", piano);
        Places.Add("picture", picture);
        Places.Add("tv", tv);
        Places.Add("meja", meja);
        Places.Add("sofa", sofa);

        placeConnections.Add("piano", new List<string> { "picture", "tv", "meja", "sofa" });
        placeConnections.Add("picture", new List<string> { "piano", "tv", "meja", "sofa" });
        placeConnections.Add("tv", new List<string> { "piano", "picture", "meja", "sofa" });
        placeConnections.Add("meja", new List<string> { "picture", "tv", "piano", "sofa" });
        placeConnections.Add("sofa", new List<string> { "picture", "tv", "meja", "piano" });

        Debug.Log("Places initialized successfully.");
    }



    private void InitializeItems()
    {
        Item snack = new Item(
            "snack",
            new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "use", new Dictionary<string, object>
                    {
                        { "conditions", new Dictionary<string, object>() },
                        { "effects", new Dictionary<string, object> { { "health", 10 } } }
                    }
                }
            }
        );

        Item mapItem = new Item("map", new Dictionary<string, Dictionary<string, object>>());
        Item pillow = new Item("pillow", new Dictionary<string, Dictionary<string, object>>());

        items = new Dictionary<string, Item>
        {
            { "snack", snack },
            { "map", mapItem },
            { "pillow", pillow }
        };
    }

    private void InitializeNPCState()
    {
        npcState = new NPCState(
            upperBody: new Dictionary<string, object> { { "hold", "none" } },
            lowerBody: new Dictionary<string, object> { { "location", "picture" }, { "pose", "stand" } },
            resources: new Dictionary<string, float> { { "time", 0f }, { "health", 100f }, { "mental", 100f } },
            inventory: new List<string>(),
            stateData: new Dictionary<string, object>()
        );
        npcState.GameObject = this.gameObject; // NPC의 GameObject 할당
    }

    private void InitializeActions()
    {
        actions = new List<GOAPAction>
        {
            new GOAPAction(
                name: "eat_snack",
                conditions: new Dictionary<string, Func<NPCState, WorldState, bool>>
                {
                    { "hold", (npc, world) => npc.UpperBody.ContainsKey("hold") && npc.UpperBody["hold"].ToString() == "snack" }
                },
                effects: new Dictionary<string, object>
                {
                    { "use_item", "snack" },
                    { "hold", "none" },
                    { "used_snack", true }
                },
                cost: new Dictionary<string, float>
                {
                    { "time", 0.5f },
                    { "health", 0f },
                    { "mental", 0f }
                }
            ),
            new GOAPAction(
                name: "turn_on_tv",
                conditions: new Dictionary<string, Func<NPCState, WorldState, bool>>
                {
                    { "location", (npc, world) => npc.LowerBody.ContainsKey("location") && npc.LowerBody["location"].ToString() == "tv" },
                    { 
                        "tv_power", 
                        (npc, world) => world.Places["tv"].State.ContainsKey("tv_state") && world.Places["tv"].State["tv_state"].ToString() == "off" 
                    }
                },
                effects: new Dictionary<string, object>
                {
                    { "place_state:tv:tv_state", "on" }
                },
                cost: new Dictionary<string, float>
                {
                    { "time", 0.5f },
                    { "health", 0f },
                    { "mental", 0f }
                }
            ),
            new GOAPAction(
                name: "sit_sofa",
                conditions: new Dictionary<string, Func<NPCState, WorldState, bool>>
                {
                    { "location", (npc, world) => npc.LowerBody.ContainsKey("location") && npc.LowerBody["location"].ToString() == "sofa" },
                    { "pose", (npc, world) => npc.LowerBody.ContainsKey("pose") && npc.LowerBody["pose"].ToString() == "stand" }
                },
                effects: new Dictionary<string, object>
                {
                    { "pose", "sit" }
                },
                cost: new Dictionary<string, float>
                {
                    { "time", 1f },
                    { "health", 1f },
                    { "mental", 1f }
                }
            ),
            new GOAPAction(
                name: "stand_sofa",
                conditions: new Dictionary<string, Func<NPCState, WorldState, bool>>
                {
                    { "location", (npc, world) => npc.LowerBody.ContainsKey("location") && npc.LowerBody["location"].ToString() == "sofa" },
                    { "pose", (npc, world) => npc.LowerBody.ContainsKey("pose") && npc.LowerBody["pose"].ToString() == "sit" }
                },
                effects: new Dictionary<string, object>
                {
                    { "pose", "stand" }
                },
                cost: new Dictionary<string, float>
                {
                    { "time", 1f },
                    { "health", 3f },
                    { "mental", 3f }
                }
            )
        };

        // Create Move Actions based on Place Connections
        foreach (var place in placeConnections)
        {
            string fromPlace = place.Key;
            foreach (var toPlace in place.Value)
            {
                float timeCost = 1.5f;
                float healthCost = 1.5f;

                if ((fromPlace == "sofa" && toPlace == "meja") || (fromPlace == "meja" && toPlace == "sofa") ||
                    (fromPlace == "tv" && toPlace == "piano") || (fromPlace == "piano" && toPlace == "tv"))
                {
                    timeCost = 0.5f;
                    healthCost = 0.5f;
                }

                actions.Add(ActionFactory.CreateMoveAction(fromPlace, toPlace, timeCost, healthCost));
            }
        }

        // 제스처 액션 초기화
        foreach (var gesture in gestureNames)
        {
            GOAPAction gestureAction = ActionFactory.CreateGestureAction(gesture);
            actions.Add(gestureAction);
        }

        // 아이템 액션 추가 (pick과 drop)
        foreach (var item in items.Keys)
        {
            GOAPAction pickAction = ActionFactory.CreatePickAction(item);
            GOAPAction dropAction = ActionFactory.CreateDropAction(item);
            actions.Add(pickAction);
            actions.Add(dropAction);
        }
    }

    /// <summary>
    /// Resets NPCState flags related to gestures to allow repeated gestures.
    /// </summary>
    private void ResetNPCState()
    {
        foreach (var gesture in gestureNames)
        {
            string flag = $"did_{gesture}";
            if (npcState.StateData.ContainsKey(flag))
            {
                npcState.StateData[flag] = false;
                Debug.Log($"NPCState {flag} reset to false.");
            }
        }
    }
    /// <summary>
    /// Sets the goals based on server response and initiates planning and execution.
    /// </summary>
    public void SetGoals(string gesture, string moveGoal, string itemGoal)
    {
        if (isExecutingPlan)
        {
            Debug.LogWarning("A plan is already being executed. Please wait until it finishes.");
            return;
        }

        // Reset NPCState before planning
        ResetNPCState();

        // Define Goals based on inputs
        List<Goal> parsedGoals = new List<Goal>();

        // 1. Gesture 유효성 검사
        if (!string.IsNullOrEmpty(gesture) && !gesture.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            if (!gestureNames.Exists(g => g.Equals(gesture, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogError($"Invalid gesture: '{gesture}' is not recognized.");
                return;
            }

            Goal gestureGoal = GoalParser.ParseSentenceToGoal($"Do {gesture}", actions, weight: 1f);
            if (gestureGoal != null)
                parsedGoals.Add(gestureGoal);
        }

        // 2. MoveGoal 유효성 검사
        if (!string.IsNullOrEmpty(moveGoal) && !moveGoal.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            string normalizedMoveGoal = moveGoal.ToLower();
            if (!Places.ContainsKey(normalizedMoveGoal))
            {
                Debug.LogError($"Invalid move goal: place '{moveGoal}' does not exist.");
                return;
            }

            Goal moveGoalObj = GoalParser.ParseSentenceToGoal($"Go to {moveGoal}", actions, weight: 1f);
            if (moveGoalObj != null)
                parsedGoals.Add(moveGoalObj);
        }

        // 3. ItemGoal 유효성 검사
        if (!string.IsNullOrEmpty(itemGoal) && !itemGoal.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            // 3a. "Pick up <item> at <place>" 패턴 검사
            var matchPickUpAt = Regex.Match(itemGoal, @"Pick\s+up\s+(.+?)\s+at\s+(.+)", RegexOptions.IgnoreCase);
            if (matchPickUpAt.Success)
            {
                string itemName = matchPickUpAt.Groups[1].Value.Trim().ToLower();
                string placeName = matchPickUpAt.Groups[2].Value.Trim().ToLower();

                // 아이템 존재 여부 확인
                if (!items.ContainsKey(itemName))
                {
                    Debug.LogError($"Invalid item goal: item '{itemName}' does not exist.");
                    return;
                }

                // 장소 존재 여부 확인
                if (!Places.ContainsKey(placeName))
                {
                    Debug.LogError($"Invalid item goal: location '{placeName}' does not exist.");
                    return;
                }

                // GoalParser를 통해 Goal 생성
                Goal pickUpAtGoal = GoalParser.ParseSentenceToGoal($"Pick up {itemName} at {placeName}", actions, weight: 1f);
                if (pickUpAtGoal != null)
                    parsedGoals.Add(pickUpAtGoal);
            }
            else
            {
                // 3b. "Pick up <item>" 패턴 검사
                var matchPickUp = Regex.Match(itemGoal, @"Pick\s+up\s+(.+)", RegexOptions.IgnoreCase);
                if (matchPickUp.Success)
                {
                    string itemName = matchPickUp.Groups[1].Value.Trim().ToLower();
                    if (!items.ContainsKey(itemName))
                    {
                        Debug.LogError($"Invalid item goal: item '{itemName}' does not exist.");
                        return;
                    }

                    // GoalParser를 통해 Goal 생성
                    Goal pickUpGoal = GoalParser.ParseSentenceToGoal($"Pick up {itemName}", actions, weight: 1f);
                    if (pickUpGoal != null)
                        parsedGoals.Add(pickUpGoal);
                }
                else
                {
                    // 3c. "Drop <item> at <location>" 패턴 검사
                    var matchDropItem = Regex.Match(itemGoal, @"Drop\s+(.+?)\s+at\s+(.+)", RegexOptions.IgnoreCase);
                    if (matchDropItem.Success)
                    {
                        string itemName = matchDropItem.Groups[1].Value.Trim().ToLower();
                        string location = matchDropItem.Groups[2].Value.Trim().ToLower();

                        // 아이템 존재 여부 확인
                        if (!items.ContainsKey(itemName))
                        {
                            Debug.LogError($"Invalid item goal: item '{itemName}' does not exist.");
                            return;
                        }

                        // 장소 존재 여부 확인
                        if (!Places.ContainsKey(location))
                        {
                            Debug.LogError($"Invalid item goal: location '{location}' does not exist.");
                            return;
                        }

                        // GoalParser를 통해 Goal 생성
                        Goal dropGoal = GoalParser.ParseSentenceToGoal($"Drop {itemName} at {location}", actions, weight: 1f);
                        if (dropGoal != null)
                            parsedGoals.Add(dropGoal);
                    }
                    else
                    {
                        // 유효하지 않은 itemGoal 형식
                        Debug.LogError($"Invalid item goal format: '{itemGoal}'. Expected formats are 'Pick up <item>', 'Pick up <item> at <place>', or 'Drop <item> at <location>'.");
                        return;
                    }
                }
            }
        }

        if (parsedGoals.Count == 0)
        {
            Debug.LogWarning("No valid goals provided.");
            return;
        }

        // Initialize Planner
        GOAPPlanner planner = new GOAPPlanner(parsedGoals, actions);

        // Execute Planner
        var planResult = planner.Plan(npcState, worldState);

        if (planResult != null)
        {
            Debug.Log("Plan successfully generated:");
            foreach (var action in planResult)
            {
                Debug.Log($"- {action.Name}");
            }

            // Start executing the plan
            StartCoroutine(ExecutePlan(planResult));
        }
        else
        {
            Debug.Log("Unable to achieve the goals.");
        }
    }



    // Property to get current NPCStatus
    public NPCStatus CurrentNPCStatus
    {
        get
        {
            string location = npcState.LowerBody.ContainsKey("location") ? npcState.LowerBody["location"].ToString() : "unknown";
            string inventory = npcState.Inventory != null && npcState.Inventory.Count > 0 ? string.Join(", ", npcState.Inventory) : "none";
            string pose = npcState.LowerBody.ContainsKey("pose") ? npcState.LowerBody["pose"].ToString() : "unknown";
            string holding = npcState.UpperBody.ContainsKey("hold") ? npcState.UpperBody["hold"].ToString() : "none";
            string health = npcState.Resources.ContainsKey("health") ? npcState.Resources["health"].ToString() : "0";
            string mental = npcState.Resources.ContainsKey("mental") ? npcState.Resources["mental"].ToString() : "0";

            return new NPCStatus(location, inventory, pose, holding, health, mental);
        }
    }

    /// <summary>
    /// Coroutine to execute the planned actions sequentially.
    /// </summary>
    private IEnumerator ExecutePlan(List<GOAPAction> plan)
    {
        isExecutingPlan = true;

        foreach (var action in plan)
        {
            Debug.Log($"Starting Action: {action.Name}");

            if (IsMoveAction(action.Name))
            {
                string targetPlace = ExtractTargetPlaceFromMoveAction(action.Name);
                if (!string.IsNullOrEmpty(targetPlace))
                {
                    // UpdateNPCLocation 메서드를 사용하여 이동 처리
                    UpdateNPCLocation(targetPlace);

                    // NPC가 목적지에 도착할 때까지 대기
                    while (npcAgent.pathPending || npcAgent.remainingDistance > npcAgent.stoppingDistance)
                    {
                        Debug.Log($"PathPending: {npcAgent.pathPending}, RemainingDistance: {npcAgent.remainingDistance}");
                        yield return null;
                    }

                    // 추가 확인: NPC가 실제로 도착했는지 확인
                    if (!npcAgent.hasPath || npcAgent.velocity.sqrMagnitude == 0f)
                    {
                        Debug.Log($"Reached {targetPlace}.");
                        npcState.LowerBody["location"] = targetPlace; // 위치 업데이트
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to reach {targetPlace}.");
                    }
                }
                else
                {
                    Debug.Log($"Unable to extract target place from action name '{action.Name}'.");
                }
            }
            else if (IsGestureAction(action.Name))
            {
                string gestureName = ExtractGestureName(action.Name);
                if (!string.IsNullOrEmpty(gestureName))
                {
                    characterAnimator.SetTrigger(gestureName);
                    Debug.Log($"Performing Gesture: {gestureName}");

                    // Wait for the duration of the gesture animation
                    //if (action.Cost.ContainsKey("time"))
                    //{
                    //    yield return new WaitForSeconds(action.Cost["time"]);
                    //}
                    //else
                    //{
                    //    yield return null;
                    //}

                    // 제스처 액션 효과 적용
                    if (action.Effects.ContainsKey($"did_{gestureName}"))
                    {
                        npcState.StateData[$"did_{gestureName}"] = true;
                        Debug.Log($"NPCState did_{gestureName} set to true.");
                    }
                    characterAnimator.SetTrigger("Standing Idle");
                }
                else
                {
                    Debug.Log($"Unable to extract gesture name from action name '{action.Name}'.");
                }
            }


            else if (IsPickAction(action.Name) || IsDropAction(action.Name))
            {
                string itemName = ExtractItemNameFromAction(action.Name);
                if (!string.IsNullOrEmpty(itemName))
                {
                    InteractionControl interaction = FindObjectOfType<InteractionControl>();
                    if (interaction != null)
                    {
                        if (IsPickAction(action.Name))
                        {
                            interaction.PickUpItem(itemName, npcState, worldState);
                            Debug.Log($"Picked up item: {itemName}");
                        }
                        else if (IsDropAction(action.Name))
                        {
                            interaction.DropItem(itemName, npcState, worldState);
                            Debug.Log($"Dropped item: {itemName}");
                        }

                        // 액션 소요 시간 대기
                        if (action.Cost.ContainsKey("time"))
                        {
                            yield return new WaitForSeconds(action.Cost["time"]);
                        }
                        else
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        Debug.LogError("InteractionControl script not found in the scene.");
                        yield break;
                    }
                }
                else
                {
                    Debug.Log($"Unable to extract item name from action name '{action.Name}'.");
                }
            }
            
            else
            {
                // Handle other action types (e.g., Think)
                Debug.Log($"Executing Action: {action.Name}");

                if (action.Cost.ContainsKey("time"))
                {
                    yield return new WaitForSeconds(action.Cost["time"]);
                }
                else
                {
                    yield return null;
                }

                // 예시로 Think 액션의 효과 적용
                foreach (var effect in action.Effects)
                {
                    npcState.StateData[effect.Key] = effect.Value;
                    Debug.Log($"NPCState {effect.Key} set to {effect.Value}.");
                }
            }
        }

        isExecutingPlan = false;
        Debug.Log("Plan execution completed.");
    }

    /// <summary>
    /// Determines if the action is a Move action based on its name.
    /// </summary>
    private bool IsMoveAction(string actionName)
    {
        return actionName.Contains("_to_", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the action is a Gesture action based on its name.
    /// </summary>
    private bool IsGestureAction(string actionName)
    {
        return gestureNames.Exists(g => g.Equals(actionName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extracts the target place from a Move action name.
    /// Example: "sofa_to_meja" -> "meja"
    /// </summary>
    private string ExtractTargetPlaceFromMoveAction(string actionName)
    {
        string[] parts = actionName.Split(new string[] { "_to_" }, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            return parts[1]; // 'to' 부분을 반환
        }
        return null;
    }

    /// <summary>
    /// Extracts the gesture name from a Gesture action name.
    /// Example: "Thinking" -> "Thinking"
    /// </summary>
    private string ExtractGestureName(string actionName)
    {
        // 제스처 액션의 이름이 바로 제스처 이름이므로, 그대로 반환
        return actionName;
    }

    /// <summary>
    /// Sets the NPC's destination based on the target object name.
    /// </summary>
    private void UpdateNPCLocation(string targetObjectName)
    {
        Debug.Log($"Attempting to find target object: {targetObjectName}");
        Debug.Log($"Number of target objects: {targetObjects.Length}");

        GameObject targetObject = Array.Find(targetObjects, obj => obj.name.Equals(targetObjectName, StringComparison.OrdinalIgnoreCase));
        
        if (targetObject != null)
        {
            Debug.Log($"Target object found: {targetObject.name}");
            npcAgent.SetDestination(targetObject.transform.position);
        }
        else
        {
            Debug.LogWarning($"Target object not found: {targetObjectName}");
            Debug.Log("Available target objects:");
            foreach (var obj in targetObjects)
            {
                Debug.Log(obj.name);
            }
        }
    }

    /// <summary>
    /// Determines if the action is a Pick action based on its name.
    /// </summary>
    private bool IsPickAction(string actionName)
    {
        return actionName.StartsWith("pick_", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the action is a Drop action based on its name.
    /// </summary>
    private bool IsDropAction(string actionName)
    {
        return actionName.StartsWith("drop_", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the item name from a Pick or Drop action name.
    /// Example: "pick_map" -> "map", "drop_snack" -> "snack"
    /// </summary>
    private string ExtractItemNameFromAction(string actionName)
    {
        string[] parts = actionName.Split('_');
        if (parts.Length >= 2)
        {
            return string.Join("_", parts, 1, parts.Length - 1).ToLower();
        }
        return null;
    }


}
