using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a GameObject in the Lobby scene.
/// Wire: nameInput, playButton, statusText in Inspector.
/// Call OnPlayClicked() from the Play button's OnClick event.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [SerializeField] private InputField nameInput;
    [SerializeField] private Button     playButton;
    [SerializeField] private Text       statusText;

    public async void OnPlayClicked()
    {
        var playerName = nameInput != null ? nameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(playerName)) playerName = "Player";

        playButton.interactable = false;
        if (statusText) statusText.text = "Connecting...";

        try
        {
            await ColyseusManager.Instance.JoinOrCreateRoom(playerName);
            SceneManager.LoadScene("WaitingUser");
        }
        catch
        {
            if (statusText) statusText.text = "Connection failed. Try again.";
            playButton.interactable = true;
        }
    }
}
