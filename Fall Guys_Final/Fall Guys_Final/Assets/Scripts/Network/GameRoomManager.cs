using System.Collections.Generic;
using Colyseus;
using Colyseus.Schema;
using UnityEngine;

/// <summary>
/// Place on a GameObject in the InGame scene (NOT DontDestroyOnLoad).
/// Binds to the already-joined Colyseus room owned by ColyseusManager,
/// spawns remote players, and bridges game-phase events to UIManager.
///
/// Setup:
///   1. Assign networkPlayerPrefab — prefab with NetworkPlayer component.
///   2. Assign localPlayerRoot    — the local player GameObject (has LHS_MainPlayer).
/// </summary>
public class GameRoomManager : MonoBehaviour
{
    public static GameRoomManager Instance { get; private set; }

    [Header("Prefabs / References")]
    [SerializeField] private GameObject networkPlayerPrefab;
    [SerializeField] private GameObject localPlayerRoot;

    private Room<GameState> _room;
    private StateCallbackStrategy<GameState> _callbacks;
    private readonly Dictionary<string, NetworkPlayer> _remotePlayers = new Dictionary<string, NetworkPlayer>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // No DontDestroyOnLoad — this manager lives only in the InGame scene.
    }

    private void Start()
    {
        BindToRoom();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Binds to the room already created in ColyseusManager.
    /// Call this instead of JoinGame() — the room is joined in the Lobby scene.
    /// </summary>
    public void BindToRoom()
    {
        _room = ColyseusManager.Instance?.Room;
        if (_room == null)
        {
            Debug.LogWarning("[GameRoomManager] No active room found. Playing offline.");
            return;
        }
        BindRoomEvents();
    }

    // ── Room event binding ────────────────────────────────────────────────────

    private void BindRoomEvents()
    {
        _callbacks = Colyseus.Schema.Callbacks.Get(_room);

        // Players
        _callbacks.OnAdd(state => state.players, (sessionId, player) => OnPlayerAdded(player, sessionId));
        _callbacks.OnRemove(state => state.players, (sessionId, player) => OnPlayerRemoved(player, sessionId));

        // Game phase broadcasts
        _room.OnMessage("countdown",           (CountdownMsg msg)             => OnCountdown(msg));
        _room.OnMessage("countdown_cancelled", (Dictionary<string, object> _) => Debug.Log("[Room] Countdown cancelled"));
        _room.OnMessage("round_start",         (Dictionary<string, object> _) => OnRoundStart());
        _room.OnMessage("round_end",           (RoundEndMsg msg)              => OnRoundEnd(msg));
        _room.OnMessage("player_finished",     (FinishedMsg msg)              => OnPlayerFinished(msg));
        _room.OnMessage("emote",               (EmoteMsg msg)                 => OnEmote(msg));
        _room.OnMessage("room_reset",          (Dictionary<string, object> _) => OnRoomReset());

        _room.OnLeave += (int code) => Debug.Log($"[Room] Left with code {code}");
    }

    // ── Player lifecycle ──────────────────────────────────────────────────────

    private void OnPlayerAdded(PlayerState state, string sessionId)
    {
        if (sessionId == _room.SessionId) return;

        var go   = Instantiate(networkPlayerPrefab, new Vector3(state.x, state.y, state.z), Quaternion.identity);
        go.name  = $"RemotePlayer_{state.name}";
        var netP = go.GetComponent<NetworkPlayer>();

        _remotePlayers[sessionId] = netP;
        _callbacks.OnChange(state, () => netP.ApplyState(state));

        Debug.Log($"[Room] Player joined: {state.name} ({sessionId})");
    }

    private void OnPlayerRemoved(PlayerState state, string sessionId)
    {
        if (!_remotePlayers.TryGetValue(sessionId, out var netP)) return;
        if (netP != null) Destroy(netP.gameObject);
        _remotePlayers.Remove(sessionId);
        Debug.Log($"[Room] Player left: {state.name} ({sessionId})");
    }

    // ── Game phase handlers ───────────────────────────────────────────────────

    private void OnCountdown(CountdownMsg msg)
    {
        Debug.Log($"[Room] Countdown: {msg.count}");
    }

    private void OnRoundStart()
    {
        Debug.Log("[Room] Round started");
    }

    private void OnRoundEnd(RoundEndMsg msg)
    {
        Debug.Log($"[Room] Round ended. Finished: {msg.finishedCount}/{msg.playerCount}");

        PlayerState localState;
        if (_room.State.players.TryGetValue(_room.SessionId, out localState))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.limitTime = 0;
        }
    }

    private void OnPlayerFinished(FinishedMsg msg)
    {
        Debug.Log($"[Room] {msg.sessionId} finished #{msg.rank}");
        if (UIManager.Instance != null)
            UIManager.Instance.CurRank = msg.rank;
    }

    private void OnEmote(EmoteMsg msg)
    {
        if (_remotePlayers.TryGetValue(msg.sessionId, out var netP))
            netP.PlayEmote(msg.id);
    }

    private void OnRoomReset()
    {
        Debug.Log("[Room] Room reset — new round starting soon");
        foreach (var pair in _remotePlayers)
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        _remotePlayers.Clear();
    }

    // ── Message types ─────────────────────────────────────────────────────────

    private class CountdownMsg { public int count; }
    private class RoundEndMsg  { public int finishedCount; public int playerCount; }
    private class FinishedMsg  { public string sessionId; public int rank; }
    private class EmoteMsg     { public string sessionId; public int id; }
}
