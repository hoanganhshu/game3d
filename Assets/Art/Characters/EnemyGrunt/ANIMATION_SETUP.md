# Enemy Grunt — Meshy/Unity animation setup

## Assets currently available

| File | Unity state | Loop | Purpose |
|---|---|---:|---|
| `Character_output.fbx` | Base rig / bind pose | No | Main skinned character |
| `Animation_Walking_withSkin.fbx` | Walk | Yes | Patrol movement |
| `Animation_Running_withSkin.fbx` | Run | Yes | Chase movement |

`Tools > Graphics > Build Meshy Enemy` imports these files, creates
`Generated/EnemyGrunt.controller`, and creates `Generated/EnemyGruntVisual.prefab`.

## Animator parameters

| Parameter | Type | Driven by |
|---|---|---|
| `Speed` | Float | NavMeshAgent velocity (0–1) |
| `Combat` | Bool | Enemy Attack state |
| `Attack` | Trigger | Each shot |
| `Hit` | Trigger | Damage received |
| `Dead` | Bool | Death state |

## States

| State | Current motion | Required final Meshy motion |
|---|---|---|
| Idle | Bind-pose fallback | Idle or Combat Idle |
| Walk | Walking FBX | Complete |
| Run | Running FBX | Complete |
| Attack | Short pose fallback | Rifle Shot / Side Shot |
| Hit | Short pose fallback | Hit Reaction |
| Death | Code fall + pose fallback | Shot and Fall Backward |

Meshy returned HTTP 400 when adding more motions to the current animation child asset.
When a new rig task is available, export Idle, Hit Reaction, and Shot and Fall Backward as
separate FBX files using the same Mixamo skeleton, 30 FPS, without root motion. They can then
replace the three fallback states without changing gameplay code.
