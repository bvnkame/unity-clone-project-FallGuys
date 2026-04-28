using System.Collections.Generic;
using Colyseus;
using Colyseus.Schema;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a persistent GameObject in the WaitingUser scene.
/// Shows real online players from Colyseus room state.
///
/// Wire in Inspector:
///   playerListText  — scrollable Text listing one name per line
///   playerCountText — "X / 20" label
///   countdownText   — large center number, initially hidden
///   statusText      — "Waiting for players..." label
/// </summary>
public class WaitingRoomUI : MonoBehaviour
{
    [SerializeField] private Text playerListText;
    [SerializeField] private Text playerCountText;
    [SerializeField] private Text countdownText;
    [SerializeField] private Text statusText;

    private Room<GameState>                  _room;
    private StateCallbackStrategy<GameState> _callbacks;

    // sessionId → display name
    private readonly Dictionary<string, string> _players = new Dictionary<string, string>();

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

        _callbacks.OnAdd(state => state.players, (sessionId, player) =>
        {
            _players[sessionId] = player.name;
            RefreshUI();
        });

        _callbacks.OnRemove(state => state.players, (sessionId, player) =>
        {
            _players.Remove(sessionId);
            RefreshUI();
        });

        _room.OnMessage("countdown", (CountdownMsg msg) =>
        {
            if (countdownText)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = msg.count.ToString();
            }
            if (statusText) statusText.text = "Starting soon...";
        });

        _room.OnMessage("countdown_cancelled", (Dictionary<string, object> _) =>
        {
            if (countdownText) countdownText.gameObject.SetActive(false);
            if (statusText) statusText.text = "Waiting for players...";
        });

        _room.OnMessage("round_start", (Dictionary<string, object> _) =>
        {
            SceneManager.LoadScene("Intro");
        });

        if (statusText) statusText.text = "Waiting for players...";
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (playerCountText) playerCountText.text = _players.Count + " / 20";

        if (playerListText)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var name in _players.Values)
                sb.AppendLine(name);
            playerListText.text = sb.ToString();
        }
    }

    private class CountdownMsg { public int count; }
}
