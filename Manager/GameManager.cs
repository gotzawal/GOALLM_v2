using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public TMP_InputField userInputField;
    public TextMeshProUGUI chatHistoryText;
    public UnityEngine.AI.NavMeshAgent npcAgent;
    //public GameObject[] targetObjects;
    // public Animator characterAnimator; // 제거: HandleGesture 기능이 GOAPExample로 이동
    // public float animationDuration = 2f; // 제거: HandleGesture 기능이 GOAPExample로 이동

    public GOAPExample goapExample; // GOAPExample 스크립트에 대한 참조

    private string serverUrl = "";
    private bool isServerUrlSet = false;


    [System.Serializable]
    public class ServerRequest
    {
        public NPCStatus npc_status; // NPC 상태 정보
        public string userInput;      // 사용자 입력

        public ServerRequest(NPCStatus status, string input)
        {
            npc_status = status;
            userInput = input;
        }
    }

    [System.Serializable]
    private class ServerResponse
    {
        public string TalkGoal;         // npcDialogue 대신 사용
        public string MoveGoal;         // Move Goal
        public string ItemGoal;         // Item Goal
        public string Gesture;          // 기존 Gesture
        public string Think;            // Think
    }

    void Start()
    {
        userInputField.onEndEdit.AddListener(OnInputFieldSubmit);
        chatHistoryText.text = "Enter Server URL";
    }

    void OnInputFieldSubmit(string input)
    {
        if (!isServerUrlSet)
        {
            serverUrl = input.TrimEnd('/');  // 뒤에 있는 '/' 제거
            isServerUrlSet = true;
            chatHistoryText.text = "Start chatting";
            userInputField.text = "";
        }
        else
        {
            // 이미 코루틴이 실행 중인 경우 중복 실행 방지
            if (!IsInvoking("CommunicateWithServer"))
            {
                StartCoroutine(CommunicateWithServer(input));
            }
        }
    }

    IEnumerator CommunicateWithServer(string userInput)
    {
        // 현재 NPC 상태를 가져와서 ServerRequest 생성
        ServerRequest request = new ServerRequest(goapExample.CurrentNPCStatus, userInput);

        Debug.Log("NPC Status: " + goapExample.CurrentNPCStatus);

        string jsonRequest = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(serverUrl + "/api/game", "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonRequest);
            webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + webRequest.error);
                UpdateChatHistory(userInput, "Error.");
            }
            else
            {
                string jsonResponse = webRequest.downloadHandler.text;
                ServerResponse response = JsonUtility.FromJson<ServerResponse>(jsonResponse);
                Debug.Log(response.TalkGoal);
                UpdateChatHistory(userInput, response.TalkGoal);
                // UpdateNPCLocation(response.updateNPCLocation); // 제거

                // HandleGesture 호출 제거

                // GOAPExample에 목표 전달
                if (goapExample != null)
                {
                    Debug.Log(response.Gesture);
                    Debug.Log(response.MoveGoal);
                    Debug.Log(response.ItemGoal);
                    goapExample.SetGoals(response.Gesture, response.MoveGoal, response.ItemGoal);
                }
                else
                {
                    Debug.LogError("GOAPExample reference is not set in GameManager.");
                }

                userInputField.text = ""; // 입력 필드 초기화
            }
        }
    }

    void UpdateChatHistory(string userInput, string talkGoal)
    {
        chatHistoryText.text += "\nUser: " + userInput;
        chatHistoryText.text += "\nNPC: " + talkGoal;
    }

    // UpdateNPCLocation과 HandleGesture 메서드 제거
}




