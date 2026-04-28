using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// SceneManager object in every scene.
/// Enable networkMode in the Inspector for scenes that use Colyseus.
/// </summary>
public class SceneChange : MonoBehaviour
{
    [Header("Network")]
    [Tooltip("Disable mouse-click scene skips and enable Colyseus join flow.")]
    public bool networkMode = false;

    [Tooltip("InputField where player types their name (Lobby scene only).")]
    [SerializeField] private InputField playerNameInput;

    void Update()
    {
        if (networkMode) return; // network flow drives transitions — no click-to-skip

        if (SceneManager.GetActiveScene().name == "Intro")
        {
            if (Input.GetMouseButtonDown(0))
                SceneManager.LoadScene("InGame");
        }

        if (SceneManager.GetActiveScene().name == "InGame")
        {
            if (Input.GetMouseButtonDown(0))
                SceneManager.LoadScene("Ending");
        }
    }

    // ── Called by Login scene button ──────────────────────────────────────────
    public void LoginSceneChange()
    {
        SceneManager.LoadScene("Lobby");
    }

    // ── Called by Lobby scene Play button ─────────────────────────────────────
    // With networkMode = true: joins Colyseus room then loads WaitingUser.
    // With networkMode = false: loads WaitingUser directly (offline/test mode).
    public async void LobbySceneChange()
    {
        if (networkMode)
        {
            string name = playerNameInput != null ? playerNameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(name)) name = "Player";

            try
            {
                await ColyseusManager.Instance.JoinOrCreateRoom(name);
            }
            catch
            {
                Debug.LogError("[SceneChange] Failed to join room. Check server is running.");
                return;
            }
        }

        SceneManager.LoadScene("WaitingUser");
    }

    // ── Other transitions (keep for non-network scenes) ───────────────────────
    public void WaitingPalyersSceneChange()
    {
        SceneManager.LoadScene("Intro");
    }

    public void IntroSceneChange()
    {
        SceneManager.LoadScene("InGame");
    }

    public void InGameSceneChange()
    {
        SceneManager.LoadScene("Ending");
    }
}
