    // MoveToClickPoint.cs
        using UnityEngine;
        using UnityEngine.AI;
    
        public class MoveToClickPoint : MonoBehaviour {
            NavMeshAgent agent;
        
            void Start() {
                agent = GetComponent<NavMeshAgent>();
            }
        
            void Update() {
                if (Input.GetMouseButtonDown(0)) {
                    Debug.Log("Mouse clicked");
                    RaycastHit hit;
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100)) {
                        Debug.Log("Raycast hit at: " + hit.point);
                        agent.destination = hit.point;
                    } else {
                        Debug.Log("Raycast did not hit anything");
                    }
                }
            }
        }