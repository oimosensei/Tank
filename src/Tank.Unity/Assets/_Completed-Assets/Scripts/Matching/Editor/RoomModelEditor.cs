using UnityEngine;
using UnityEditor;
using Nakatani.Matching;

[CustomEditor(typeof(RoomModel))]
public class RoomModelEditor : Editor
{
    private string roomNameToCreate = "TestRoom";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("Debug Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh Room List"))
        {
            RefreshRoomList();
        }

        GUILayout.Space(5);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Room Name:", GUILayout.Width(80));
        roomNameToCreate = GUILayout.TextField(roomNameToCreate);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Create Room"))
        {
            CreateRoom(roomNameToCreate);
        }
    }

    private async void RefreshRoomList()
    {
        var roomModel = target as RoomModel;
        if (roomModel == null)
        {
            Debug.LogError("RoomModel not found");
            return;
        }

        if (Application.isPlaying)
        {
            await roomModel.RefreshRoomList();
            Debug.Log("Room list refresh requested");
        }
        else
        {
            Debug.LogWarning("Room refresh only works in Play mode");
        }
    }

    private async void CreateRoom(string roomName)
    {
        var roomModel = target as RoomModel;
        if (roomModel == null)
        {
            Debug.LogError("RoomModel not found");
            return;
        }

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("Room name cannot be empty");
            return;
        }

        if (Application.isPlaying)
        {
            var room = await roomModel.CreateRoom(roomName);
            if (room != null)
            {
                Debug.Log($"Created room: {room.RoomName} ({room.RoomId})");
            }
        }
        else
        {
            Debug.LogWarning("Room creation only works in Play mode");
        }
    }
}