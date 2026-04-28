// Auto-matched to server src/schema/GameState.ts — keep field order in sync.
using Colyseus.Schema;

public class GameState : Schema
{
    [Type(0, "string")]  public string  phase         = default;
    [Type(1, "float32")] public float   timer         = default;
    [Type(2, "uint8")]   public byte    aliveCount    = default;
    [Type(3, "uint8")]   public byte    finishedCount = default;

    [Type(4, "map", typeof(MapSchema<PlayerState>))]
    public MapSchema<PlayerState> players = new MapSchema<PlayerState>();
}
