import { Schema, MapSchema, type } from "@colyseus/schema";
import { PlayerState } from "./PlayerState";

export type GamePhase = "waiting" | "countdown" | "playing" | "roundOver";

export class GameState extends Schema {
    @type("string")  phase: GamePhase = "waiting";
    @type("float32") timer: number = 180;
    @type("uint8")   aliveCount: number = 0;
    @type("uint8")   finishedCount: number = 0;

    @type({ map: PlayerState })
    players = new MapSchema<PlayerState>();
}
