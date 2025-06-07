using UnityEngine;
using UnityEditor;
using System;
using Unity.Mathematics;

[CustomEditor(typeof(GameHubClient))]
public class GameHubClientTester : Editor
{
    private string testPlayerId = "";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameHubClient gameHubClient = (GameHubClient)target;

        GUILayout.Space(10);
        GUILayout.Label("Testing Functions", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Player ID:", GUILayout.Width(70));
        testPlayerId = GUILayout.TextField(testPlayerId);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Generate Random Player ID"))
        {
            testPlayerId = Guid.NewGuid().ToString();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Test OnPlayerLeft"))
        {
            if (Guid.TryParse(testPlayerId, out Guid playerId))
            {
                gameHubClient.OnPlayerLeft(playerId);
                Debug.Log($"Called OnPlayerLeft with ID: {playerId}");
            }
            else
            {
                Debug.LogError("Invalid Player ID format. Please enter a valid GUID.");
            }
        }

        if (GUILayout.Button("Test OnPlayerJoined"))
        {
            if (Guid.TryParse(testPlayerId, out Guid playerId))
            {
                Vector3 testPosition = new Vector3(
                    UnityEngine.Random.Range(-10f, 10f),
                    0,
                    UnityEngine.Random.Range(-10f, 10f)
                );
                gameHubClient.OnPlayerJoined(playerId, testPosition, false);
                Debug.Log($"Called OnPlayerJoined with ID: {playerId} at position: {testPosition}");
            }
            else
            {
                Debug.LogError("Invalid Player ID format. Please enter a valid GUID.");
            }
        }

        if (GUILayout.Button("Test OnMove"))
        {
            if (Guid.TryParse(testPlayerId, out Guid playerId))
            {
                Vector3 testPosition = new Vector3(
                    UnityEngine.Random.Range(-10f, 10f),
                    0,
                    UnityEngine.Random.Range(-10f, 10f)
                );
                gameHubClient.OnMove(playerId, testPosition, quaternion.identity);
                Debug.Log($"Called OnMove with ID: {playerId} to position: {testPosition}");
            }
            else
            {
                Debug.LogError("Invalid Player ID format. Please enter a valid GUID.");
            }
        }

        if (GUILayout.Button("Test OnAttack"))
        {
            if (Guid.TryParse(testPlayerId, out Guid playerId))
            {
                Guid targetId = Guid.NewGuid();
                gameHubClient.OnAttack(playerId, targetId);
                Debug.Log($"Called OnAttack: {playerId} attacked {targetId}");
            }
            else
            {
                Debug.LogError("Invalid Player ID format. Please enter a valid GUID.");
            }
        }
    }
}