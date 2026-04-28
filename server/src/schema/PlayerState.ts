import { Schema, type } from "@colyseus/schema";

export class PlayerState extends Schema {
    @type("string")  sessionId: string = "";
    @type("string")  name: string = "Player";
    @type("float32") x: number = 0;
    @type("float32") y: number = 1;
    @type("float32") z: number = 0;
    @type("float32") rotY: number = 0;
    @type("string")  animState: string = "idle";
    @type("boolean") isAlive: boolean = true;
    @type("uint8")   rank: number = 0; // 0 = not finished yet
}
