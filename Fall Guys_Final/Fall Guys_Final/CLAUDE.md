# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6 (6000.4.0f1) Fall Guys clone. Single-player race with AI opponents, obstacle courses, and round-based elimination.

## Development

Open in Unity Hub with Unity 6000.4.0f1. There is no CLI build command — use the Unity Editor. Scripts in `Assets/Scripts/` and `Assets/LHS_0724/` compile automatically when saved.

Run tests via Unity Test Runner (Window > General > Test Runner). No external test CLI.

## Scene Flow

```
Login → Lobby → WaitingUser → Intro → InGame → Ending
```

`SceneChange.cs` handles all transitions via `SceneManager.LoadScene`. `SceneChanngeLobbytoWaiting.cs` handles the Lobby→WaitingUser hop separately.

## Architecture

**Player (`LHS_MainPlayer.cs`)** — `FixedUpdate` drives everything: input, movement, turning, jump, die, emotes. Movement is camera-relative (rotates `moveVec` by camera Y). Jump uses `isJump` bool gated by `OnCollisionEnter` with tags `"Floor"` and `"Platform"`. Wall collision (`"Wall"` tag) triggers bounce + die animation.

**AI (`AINavMesh.cs`)** — NavMeshAgent targeting a single GameObject named `"RealDestPos"`. Freezes angular velocity each `FixedUpdate` to match player behavior.

**Camera (`LHS_Camera.cs`)** — Mouse-orbit third-person in `LateUpdate`. Pitch clamped to [-10, 30]. `LHS_MainPlayer` reads `Camera.transform.eulerAngles.y` for camera-relative movement — camera and player must share the same scene.

**UI / Game State (`UIManager.cs`)** — Singleton (`UIManager.Instance`). Runs countdown timer; at time-zero checks `player.transform.position.z > 560` to decide success vs. failure. Calls `LHS_Particle.Instance.Success()` on win. `DestinationCount.cs` increments `UIManager.Instance.CurRank` on trigger (finish line collider).

**Countdown (`LHS_CountdownController.cs`)** — Sets `Time.timeScale = 0` on `Awake`, restores to 1 after 3-2-1-GO coroutine using `WaitForSecondsRealtime`.

**Respawn (`LHS_Respawn2.cs`)** — Raycasts downward on layer mask 7; if no ground within `distance`, plays falling animation. `OnTriggerEnter` with tag `"Player"` teleports player to `respawnPoint` and clears falling state.

**Particles / Win (`LHS_Particle.cs`)** — Singleton. `Success()` activates win UI and plays two particle systems.

## Key Tags & Layers

| Tag | Used by |
|-----|---------|
| `"Floor"` | Resets jump state on land |
| `"Platform"` | Rotating platforms — also resets jump |
| `"Wall"` | Bounce walls — triggers bounce force + die anim |
| `"Player"` | Respawn trigger, BounceWall targeting |

Layer 7 is the respawn detection layer (raycast mask in `LHS_Respawn2`).

## Packages (key)

- `com.unity.ai.navigation` 2.0.11 — NavMesh baking for AI
- `com.unity.cinemachine` 2.10.6 — available but not currently wired to player camera
- `com.unity.timeline` 1.8.11 — used in cutscenes/intro
- `com.unity.ugui` 2.0.0 — all UI (legacy `Text`, not TMP, for in-game HUD)

## Gotchas

- `LHS_MainPlayer` uses legacy `Text` (`UnityEngine.UI.Text`), not TextMeshPro. Don't swap without updating all UI references.
- `UIManager` finds player via `GameObject.Find("Player")` — the player GameObject must be named exactly `"Player"`.
- `AINavMesh` finds destination via `GameObject.Find("RealDestPos")` — that name must exist in the InGame scene.
- Countdown freezes `Time.timeScale` — any `WaitForSeconds` during countdown will hang; use `WaitForSecondsRealtime`.
- `BounceWall` and `LHS_MainPlayer` both handle wall bounce independently. Wall objects need both the `"Wall"` tag (for player self-bounce) and a `BounceWall` component (for AI bounce).
