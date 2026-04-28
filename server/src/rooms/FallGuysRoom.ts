import { Room, Client } from "colyseus";
import { GameState } from "../schema/GameState";
import { PlayerState } from "../schema/PlayerState";

const LIMIT_TIME      = 180;   // seconds
const MIN_PLAYERS     = 2;     // start countdown when >= this many players
const COUNTDOWN_SECS  = 3;
const RESET_DELAY_MS  = 10000; // auto-reset room after round ends
const PATCH_RATE_MS   = 50;    // 20 Hz state sync

interface MoveMsg  { x: number; y: number; z: number; rotY: number; animState: string; }
interface EmoteMsg { id: number; }

export class FallGuysRoom extends Room<GameState> {
    maxClients = 20;

    private countdownTimer?: ReturnType<typeof setInterval>;
    private gameTimer?:      ReturnType<typeof setInterval>;

    onCreate(_options: unknown) {
        this.setState(new GameState());
        this.setPatchRate(PATCH_RATE_MS);

        this.onMessage<MoveMsg>("move", (client, data) => {
            const p = this.state.players.get(client.sessionId);
            if (!p || !p.isAlive || this.state.phase !== "playing") return;
            p.x         = data.x;
            p.y         = data.y;
            p.z         = data.z;
            p.rotY      = data.rotY;
            p.animState = data.animState;
        });

        this.onMessage("die", (client) => {
            const p = this.state.players.get(client.sessionId);
            if (!p || !p.isAlive) return;
            p.isAlive   = false;
            p.animState = "die";
            this.state.aliveCount = Math.max(0, this.state.aliveCount - 1);
            this.checkRoundEnd();
        });

        this.onMessage("reach_goal", (client) => {
            const p = this.state.players.get(client.sessionId);
            if (!p || p.rank > 0) return;
            this.state.finishedCount++;
            p.rank    = this.state.finishedCount;
            p.isAlive = false;
            this.state.aliveCount = Math.max(0, this.state.aliveCount - 1);
            this.broadcast("player_finished", { sessionId: client.sessionId, rank: p.rank });
            this.checkRoundEnd();
        });

        this.onMessage<EmoteMsg>("emote", (client, data) => {
            this.broadcast("emote", { sessionId: client.sessionId, id: data.id }, { except: client });
        });
    }

    onJoin(client: Client, options: { name?: string }) {
        const p        = new PlayerState();
        p.sessionId    = client.sessionId;
        p.name         = options?.name ?? `Player_${client.sessionId.slice(0, 4)}`;
        // Stagger spawn positions along start line
        const slot     = this.state.players.size;
        p.x            = (slot % 5) * 2.5 - 5;
        p.y            = 1;
        p.z            = 0;

        this.state.players.set(client.sessionId, p);
        this.state.aliveCount = this.state.players.size;

        console.log(`[+] ${p.name} joined (${this.state.players.size}/${this.maxClients})`);

        if (this.state.phase === "waiting" && this.state.players.size >= MIN_PLAYERS) {
            this.startCountdown();
        }
    }

    onLeave(client: Client, _consented: boolean) {
        const p = this.state.players.get(client.sessionId);
        if (p?.isAlive) {
            this.state.aliveCount = Math.max(0, this.state.aliveCount - 1);
        }
        this.state.players.delete(client.sessionId);
        console.log(`[-] ${p?.name ?? client.sessionId} left (${this.state.players.size} remain)`);

        if (this.state.players.size < MIN_PLAYERS && this.state.phase !== "playing") {
            this.cancelCountdown();
        }
        if (this.state.phase === "playing") {
            this.checkRoundEnd();
        }
    }

    onDispose() {
        this.cancelCountdown();
        clearInterval(this.gameTimer);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private startCountdown() {
        if (this.state.phase !== "waiting") return;
        this.state.phase = "countdown";
        let count = COUNTDOWN_SECS;
        this.broadcast("countdown", { count });

        this.countdownTimer = setInterval(() => {
            count--;
            if (count > 0) {
                this.broadcast("countdown", { count });
            } else {
                clearInterval(this.countdownTimer);
                this.startGame();
            }
        }, 1000);
    }

    private cancelCountdown() {
        clearInterval(this.countdownTimer);
        if (this.state.phase === "countdown") {
            this.state.phase = "waiting";
            this.broadcast("countdown_cancelled", {});
        }
    }

    private startGame() {
        this.state.phase         = "playing";
        this.state.timer         = LIMIT_TIME;
        this.state.aliveCount    = this.state.players.size;
        this.state.finishedCount = 0;

        // Reset any stale state from previous round
        this.state.players.forEach(p => {
            p.isAlive   = true;
            p.rank      = 0;
            p.animState = "idle";
        });

        this.broadcast("round_start", {});

        // Tick timer at 10 Hz — good enough for display, low overhead
        this.gameTimer = setInterval(() => {
            this.state.timer -= 0.1;
            if (this.state.timer <= 0) {
                this.state.timer = 0;
                this.endRound();
            }
        }, 100);
    }

    private checkRoundEnd() {
        if (this.state.phase !== "playing") return;
        const all = Array.from(this.state.players.values());
        const allDone = all.every(p => !p.isAlive || p.rank > 0);
        if (allDone && all.length > 0) this.endRound();
    }

    private endRound() {
        if (this.state.phase === "roundOver") return;
        clearInterval(this.gameTimer);
        this.state.phase = "roundOver";

        this.broadcast("round_end", {
            finishedCount: this.state.finishedCount,
            playerCount:   this.state.players.size,
        });

        this.clock.setTimeout(() => this.resetRoom(), RESET_DELAY_MS);
    }

    private resetRoom() {
        clearInterval(this.countdownTimer);
        clearInterval(this.gameTimer);
        this.state.phase         = "waiting";
        this.state.timer         = LIMIT_TIME;
        this.state.finishedCount = 0;
        this.state.players.forEach(p => {
            p.isAlive   = true;
            p.rank      = 0;
            p.animState = "idle";
        });
        this.state.aliveCount = this.state.players.size;
        this.broadcast("room_reset", {});

        if (this.state.players.size >= MIN_PLAYERS) {
            this.startCountdown();
        }
    }
}
