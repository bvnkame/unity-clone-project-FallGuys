using UnityEngine;

/// <summary>
/// Attached to a remotely-controlled player prefab.
/// Receives position/rotation snapshots from GameRoomManager and interpolates.
/// Animator state mirrors the server animState string.
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    private Animator _anim;

    private Vector3    _targetPos;
    private Quaternion _targetRot;

    [SerializeField] private float interpSpeed = 15f;

    private static readonly int HashIsMove  = Animator.StringToHash("isMove");
    private static readonly int HashIsJump  = Animator.StringToHash("isJump");
    private static readonly int HashDoDie   = Animator.StringToHash("doDie");
    private static readonly int HashDoDance1 = Animator.StringToHash("doDance01");
    private static readonly int HashDoDance2 = Animator.StringToHash("doDance02");
    private static readonly int HashDoVictory = Animator.StringToHash("doVictory");

    private string _lastAnimState = "";

    private void Awake()
    {
        _anim       = GetComponentInChildren<Animator>();
        _targetPos  = transform.position;
        _targetRot  = transform.rotation;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPos, interpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, interpSpeed * Time.deltaTime);
    }

    /// <summary>Called by GameRoomManager on each state patch.</summary>
    public void ApplyState(PlayerState state)
    {
        _targetPos = new Vector3(state.x, state.y, state.z);
        _targetRot = Quaternion.Euler(0, state.rotY, 0);
        ApplyAnim(state.animState);
    }

    /// <summary>Fires a one-shot emote trigger from a server broadcast.</summary>
    public void PlayEmote(int emoteId)
    {
        switch (emoteId)
        {
            case 1: _anim.SetTrigger(HashDoDance1);  break;
            case 2: _anim.SetTrigger(HashDoDance2);  break;
            case 3: _anim.SetTrigger(HashDoVictory); break;
        }
    }

    private void ApplyAnim(string animState)
    {
        if (animState == _lastAnimState) return;
        _lastAnimState = animState;

        _anim.SetBool(HashIsMove, animState == "run");
        _anim.SetBool(HashIsJump, animState == "jump");

        if (animState == "die")    _anim.SetTrigger(HashDoDie);
    }
}
