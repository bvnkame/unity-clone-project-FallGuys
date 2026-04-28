using System.Collections;
using System.Collections.Generic;
using Colyseus;
using UnityEngine;

/// <summary>
/// Companion to LHS_MainPlayer. Reads position/anim state each frame and
/// sends "move" messages to the Colyseus server at a fixed rate.
/// Also sends "die", "reach_goal", and "emote" on demand.
/// Add this component to the same GameObject as LHS_MainPlayer.
/// </summary>
[RequireComponent(typeof(LHS_MainPlayer))]
public class LocalPlayerNetwork : MonoBehaviour
{
    [SerializeField] private float sendRate = 20f; // messages/second

    private Animator _anim;
    private float    _sendInterval;
    private float    _sendTimer;

    private Vector3 _lastPos;
    private float   _lastRotY;
    private string  _lastAnimState = "";

    private bool _goalReached = false;

    private void Awake()
    {
        _anim         = GetComponentInChildren<Animator>();
        _sendInterval = 1f / sendRate;
    }

    private void Start()
    {
        StartCoroutine(ListenForEmotes());
    }

    private void Update()
    {
        _sendTimer += Time.deltaTime;
        if (_sendTimer >= _sendInterval)
        {
            _sendTimer = 0;
            SendMove();
        }
    }

    private void SendMove()
    {
        var room = ColyseusManager.Instance?.Room;
        if (room == null) return;

        var pos   = transform.position;
        var rotY  = transform.eulerAngles.y;
        var anim  = GetAnimState();

        if (pos == _lastPos && Mathf.Approximately(rotY, _lastRotY) && anim == _lastAnimState) return;

        _lastPos       = pos;
        _lastRotY      = rotY;
        _lastAnimState = anim;

        _ = room.Send("move", new Dictionary<string, object>
        {
            { "x",         (object)pos.x },
            { "y",         (object)pos.y },
            { "z",         (object)pos.z },
            { "rotY",      (object)rotY  },
            { "animState", (object)anim  },
        });
    }

    /// <summary>Call from DestroyZone or death trigger when local player dies.</summary>
    public void SendDie()
    {
        var room = ColyseusManager.Instance?.Room;
        if (room != null) _ = room.Send("die");
    }

    /// <summary>Call when player crosses the finish trigger.</summary>
    public void SendReachGoal()
    {
        if (_goalReached) return;
        _goalReached = true;
        var room = ColyseusManager.Instance?.Room;
        if (room != null) _ = room.Send("reach_goal");
    }

    private IEnumerator ListenForEmotes()
    {
        while (true)
        {
            var room = ColyseusManager.Instance?.Room;
            if (room != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    _ = room.Send("emote", new Dictionary<string, object> { { "id", (object)1 } });
                if (Input.GetKeyDown(KeyCode.Alpha2))
                    _ = room.Send("emote", new Dictionary<string, object> { { "id", (object)2 } });
                if (Input.GetKeyDown(KeyCode.Alpha3))
                    _ = room.Send("emote", new Dictionary<string, object> { { "id", (object)3 } });
            }
            yield return null;
        }
    }

    private string GetAnimState()
    {
        if (_anim.GetCurrentAnimatorStateInfo(0).IsName("Die")) return "die";
        if (_anim.GetBool("isJump"))                            return "jump";
        if (_anim.GetBool("isMove"))                            return "run";
        return "idle";
    }
}
