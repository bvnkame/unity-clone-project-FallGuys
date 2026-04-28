// Auto-matched to server src/schema/PlayerState.ts — keep field order in sync.
using Colyseus.Schema;

public class PlayerState : Schema
{
    [Type(0, "string")]  public string  sessionId = default;
    [Type(1, "string")]  public string  name      = default;
    [Type(2, "float32")] public float   x         = default;
    [Type(3, "float32")] public float   y         = default;
    [Type(4, "float32")] public float   z         = default;
    [Type(5, "float32")] public float   rotY      = default;
    [Type(6, "string")]  public string  animState = default;
    [Type(7, "boolean")] public bool    isAlive   = default;
    [Type(8, "uint8")]   public byte    rank      = default;
}
