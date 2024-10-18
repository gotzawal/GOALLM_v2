using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionControl : MonoBehaviour
{
    // 아이템과 Place의 참조를 저장할 클래스
    [System.Serializable]
    public class ItemObject
    {
        public string itemName;
        public GameObject itemGameObject;
        public Place currentPlace; // 현재 속한 Place
    }

    public List<ItemObject> items; // Inspector에서 설정

    private Dictionary<string, ItemObject> itemDict;
    private GOAPExample goapExample; // GOAPExample 참조
    private Transform handTransform;

    void Start()
    {
        goapExample = FindObjectOfType<GOAPExample>();
        if (goapExample == null)
        {
            Debug.LogError("GOAPExample script not found in the scene.");
            return;
        }

        // Places가 초기화되었는지 확인
        if (goapExample.Places == null || goapExample.Places.Count == 0)
        {
            Debug.LogError("GOAPExample.Places is not initialized or empty.");
            return;
        }

        itemDict = new Dictionary<string, ItemObject>();
        foreach (var item in items)
        {
            if (!itemDict.ContainsKey(item.itemName))
            {
                itemDict.Add(item.itemName, item);
                AssignItemToClosestPlace(item);
            }
        }

        // 주기적으로 아이템 위치를 추적하여 Place의 Inventory 업데이트
        InvokeRepeating(nameof(UpdateItemLocations), 1f, 1f);
        
        handTransform = goapExample.handTransform;
        if (handTransform == null)
        {
            Debug.LogError("GOAPExample에서 Hand Transform이 할당되지 않았습니다.");
            return;
        }
    }

    /// <summary>
    /// 가장 가까운 Place에 아이템을 할당하여 Inventory에 추가
    /// </summary>
    private void AssignItemToClosestPlace(ItemObject item)
    {
        if (item.itemGameObject == null)
        {
            Debug.LogError($"ItemGameObject for '{item.itemName}' is not assigned.");
            return;
        }

        Place closestPlace = FindClosestPlace(item.itemGameObject.transform.position);
        if (closestPlace != null)
        {
            closestPlace.Inventory.Add(item.itemName);
            item.currentPlace = closestPlace;
            Debug.Log($"Assigned item '{item.itemName}' to Place '{closestPlace.Name}'.");
        }
        else
        {
            Debug.LogWarning($"No Place found to assign item '{item.itemName}'.");
        }
    }

    /// <summary>
    /// 가장 가까운 Place를 찾는 메서드
    /// </summary>
    private Place FindClosestPlace(Vector3 position)
    {
        if (goapExample == null)
        {
            Debug.LogError("goapExample is null.");
            return null;
        }

        if (goapExample.Places == null)
        {
            Debug.LogError("goapExample.Places is null.");
            return null;
        }

        float minDistance = Mathf.Infinity;
        Place closestPlace = null;

        foreach (var place in goapExample.Places.Values)
        {
            if (place.GameObject == null)
            {
                Debug.LogWarning($"Place '{place.Name}' GameObject is null.");
                continue;
            }

            float distance = Vector3.Distance(position, place.GameObject.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPlace = place;
            }
        }

        if (closestPlace == null)
        {
            Debug.LogWarning("No closest Place found.");
        }

        return closestPlace;
    }
    /// <summary>
    /// NPC가 아이템을 집을 때 호출되는 메서드
    /// </summary>
    public void PickUpItem(string itemName, NPCState npcState, WorldState worldState)
    {
        if (itemDict.ContainsKey(itemName))
        {
            ItemObject item = itemDict[itemName];
            if (item.currentPlace != null && item.currentPlace.Inventory.Contains(itemName))
            {
                // Place의 Inventory에서 아이템 제거
                item.currentPlace.Inventory.Remove(itemName);

                // NPC의 Inventory에 아이템 추가
                npcState.Inventory.Add(itemName);
                npcState.UpperBody["hold"] = itemName;

                // 아이템의 부모를 손 Transform으로 설정
                item.itemGameObject.transform.SetParent(handTransform);

                // 로컬 위치와 회전 초기화
                item.itemGameObject.transform.localPosition = Vector3.zero;
                item.itemGameObject.transform.localRotation = Quaternion.identity;

                Debug.Log($"NPC가 '{item.currentPlace.Name}'에서 '{itemName}'을(를) 집었습니다.");
            }
            else
            {
                Debug.LogWarning($"아이템 '{item.currentPlace?.Name ?? "unknown"}'의 인벤토리에 '{itemName}'이(가) 없습니다.");
            }
        }
        else
        {
            Debug.LogError($"InteractionControl에서 아이템 '{itemName}'을(를) 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// NPC가 아이템을 떨어뜨릴 때 호출되는 메서드
    /// </summary>
    public void DropItem(string itemName, NPCState npcState, WorldState worldState)
    {
        if (itemDict.ContainsKey(itemName))
        {
            ItemObject item = itemDict[itemName];
            if (npcState.Inventory.Contains(itemName))
            {
                // NPC의 Inventory에서 아이템 제거
                npcState.Inventory.Remove(itemName);
                npcState.UpperBody["hold"] = "none";

                // 손 Transform에서 아이템 분리
                item.itemGameObject.transform.SetParent(null);

                // NPC의 현재 위치에서 가장 가까운 Place 찾기
                Vector3 npcPosition = npcState.GameObject.transform.position;
                Place closestPlace = FindClosestPlace(npcPosition);
                if (closestPlace != null)
                {
                    // Place의 Inventory에 아이템 추가
                    closestPlace.Inventory.Add(itemName);
                    item.currentPlace = closestPlace;

                    // 아이템을 Place의 위치로 이동
                    item.itemGameObject.transform.position = closestPlace.GameObject.transform.position;

                    Debug.Log($"NPC가 '{closestPlace.Name}'에 '{itemName}'을(를) 떨어뜨렸습니다.");
                }
                else
                {
                    Debug.LogWarning($"아이템 '{itemName}'을(를) 떨어뜨릴 Place를 찾을 수 없습니다.");

                    // 아이템을 NPC의 현재 위치에 배치
                    item.itemGameObject.transform.position = npcPosition;
                }
            }
            else
            {
                Debug.LogWarning($"NPC는 아이템 '{itemName}'을(를) 소지하고 있지 않습니다.");
            }
        }
        else
        {
            Debug.LogError($"InteractionControl에서 아이템 '{itemName}'을(를) 찾을 수 없습니다.");
        }
    }


    /// <summary>
    /// 주기적으로 아이템 위치를 추적하여 Place의 Inventory를 업데이트
    /// </summary>
    private void UpdateItemLocations()
    {
        foreach (var item in items)
        {
            if (item.currentPlace == null) // 수정된 부분
                continue;

            // 아이템이 NPC의 손에 있는지 확인
            if (item.itemGameObject.transform.parent == goapExample.NpcState.GameObject.transform)
            {
                // 이미 NPC의 Inventory에 있어야 함
                if (!goapExample.NpcState.Inventory.Contains(item.itemName))
                {
                    // 이상 상태 처리
                    Debug.LogWarning($"Item '{item.itemName}' is held by NPC but not in Inventory.");
                }
            }
            else
            {
                // 아이템이 Place에 있는지 확인
                if (!item.currentPlace.Inventory.Contains(item.itemName))
                {
                    // 아이템이 이동되었을 가능성
                    Place newClosestPlace = FindClosestPlace(item.itemGameObject.transform.position);
                    if (newClosestPlace != null && newClosestPlace != item.currentPlace)
                    {
                        // 이전 Place에서 제거하지 않았을 경우
                        if (item.currentPlace.Inventory.Contains(item.itemName))
                        {
                            item.currentPlace.Inventory.Remove(item.itemName);
                        }

                        // 새로운 Place에 추가
                        newClosestPlace.Inventory.Add(item.itemName);
                        item.currentPlace = newClosestPlace;

                        Debug.Log($"Item '{item.itemName}' moved to '{newClosestPlace.Name}'.");
                    }
                }
            }
        }
    }
}
