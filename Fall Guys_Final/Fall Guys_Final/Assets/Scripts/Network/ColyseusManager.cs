using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using UnityEngine;

/// <summary>
/// Singleton. Owns the WebSocket connection and the active room reference.
/// Call ColyseusManager.Instance.Connect() once, then JoinOrCreateRoom().
/// </summary>
public class ColyseusManager : MonoBehaviour
{
    public static ColyseusManager Instance { get; private set; }

    [Header("Server")]
    [SerializeField] private string serverUrl = "ws://localhost:2567";

    public Room<GameState> Room { get; private set; }

    private Client _client;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Connect()
    {
        _client = new Client(serverUrl);
        Debug.Log($"[Colyseus] Connecting to {serverUrl}");
    }

    /// <param name="playerName">Display name sent as join option.</param>
    public async Task JoinOrCreateRoom(string playerName)
    {
        if (_client == null) Connect();

        try
        {
            var options = new Dictionary<string, object> { { "name", playerName } };
            Room = await _client.JoinOrCreate<GameState>("fall_guys_room", options);
            Debug.Log($"[Colyseus] Joined room {Room.RoomId} as {Room.SessionId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Colyseus] JoinOrCreate failed: {e.Message}");
            throw;
        }
    }

    public async Task LeaveRoom()
    {
        if (Room == null) return;
        await Room.Leave();
        Room = null;
    }

    private void OnDestroy()
    {
        _ = LeaveRoom();
    }
}
