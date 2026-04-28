using System.Collections.Generic;
using Colyseus;
using Colyseus.Schema;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Place on a persistent GameObject in the OnlineLobby scene.
/// Spawns remote player avatars and shows live player list + countdown.
///
/// Scene setup:
///   - Flat Plane tagged "Floor" so LHS_MainPlayer can land/jump
///   - Local player prefab (LHS_MainPlayer + LHS_Camera + LocalPlayerNetwork) at spawn point
///   - This component on its own GameObject with Canvas children
///
/// Inspector:
///   remotePlayerPrefab  — prefab with NetworkPlayer component
///   lobbySpawnCenter    — Transform at centre of lobby area
///   spawnRadius         — radius around centre to scatter remote avatars
///   playerCountText     — "X / 20"
///   playerListText      — one name per line
///   countdownText       — large number, hidden until countdown starts
///   statusText          — "Waiting…" / "Starting…"
/// </summary>
public class OnlineLobbyManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject remotePlayerPrefab;
    [SerializeField] private Transform  lobbySpawnCenter;
    [SerializeField] private float      spawnRadius = 8f;

    [Header("UI")]
    [SerializeField] private Text playerCountText;
    [SerializeField] private Text playerListText;
    [SerializeField] private Text countdownText;
    [SerializeField] private Text statusText;

    private Room<GameState>                  _room;
    private StateCallbackStrategy<GameState> _callbacks;

    // sessionId → remote avatar GO
    private readonly Dictionary<string, GameObject> _remoteAvatars = new Dictionary<string, GameObject>();

    private void Start()
    {
        _room = ColyseusManager.Instance?.Room;

        if (_room == null)
        {
            if (statusText) statusText.text = "Not connected to server.";
            return;
        }

        if (countdownText) countdownText.gameObject.SetActive(false);

        _callbacks = Colyseus.Schema.Callbacks.Get(_room);

        // Remote players join/leave
        _callbacks.OnAdd(state => state.players, (sessionId, player) =>
        {
            if (sessionId == _room.SessionId) { RefreshUI(); return; }
            SpawnRemotePlayer(sessionId, player);
            RefreshUI();
        });

        _callbacks.OnRemove(state => state.players, (sessionId, player) =>
        {
            RemoveRemotePlayer(sessionId);
            RefreshUI();
        });

        // Countdown broadcast from server
        _room.OnMessage("countdown", (CountdownMsg msg) =>
        {
            if (countdownText)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = msg.count > 0 ? msg.count.ToString() : "GO!";
            }
            if (statusText) statusText.text = "Game starting...";
        });

        _room.OnMessage("countdown_cancelled", (Dictionary<string, object> _) =>
        {
            if (countdownText) countdownText.gameObject.SetActive(false);
            if (statusText) statusText.text = "Waiting for players...";
        });

        // Round starts → go to Intro cutscene
        _room.OnMessage("round_start", (Dictionary<string, object> _) =>
        {
            SceneManager.LoadScene("Intro");
        });

        if (statusText) statusText.text = "Waiting for players...";
        RefreshUI();
    }

    // ── Spawn / remove ────────────────────────────────────────────────────────

    private void SpawnRemotePlayer(string sessionId, PlayerState state)
    {
        if (remotePlayerPrefab == null) return;

        var center   = lobbySpawnCenter ? lobbySpawnCenter.position : Vector3.zero;
        var angle    = Random.Range(0f, 360f);
        var offset   = Quaternion.Euler(0, angle, 0) * Vector3.forward * Random.Range(1f, spawnRadius);
        var spawnPos = center + offset;

        var go  = Instantiate(remotePlayerPrefab, spawnPos, Quaternion.identity);
        go.name = $"LobbyRemote_{state.name}";
        _remoteAvatars[sessionId] = go;

        var netPlayer = go.GetComponent<NetworkPlayer>();
        if (netPlayer != null)
            _callbacks.OnChange(state, () => netPlayer.ApplyState(state));
    }

    private void RemoveRemotePlayer(string sessionId)
    {
        if (!_remoteAvatars.TryGetValue(sessionId, out var go)) return;
        if (go != null) Destroy(go);
        _remoteAvatars.Remove(sessionId);
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        int count = _room?.State?.players?.Count ?? 0;
        if (playerCountText) playerCountText.text = count + " / 20";

        if (playerListText && _room?.State?.players != null)
        {
            var sb = new System.Text.StringBuilder();
            _room.State.players.ForEach((id, p) =>
            {
                string marker = id == _room.SessionId ? " ◀ (you)" : "";
                sb.AppendLine(p.name + marker);
            });
            playerListText.text = sb.ToString();
        }
    }

    private class CountdownMsg { public int count; }
}
