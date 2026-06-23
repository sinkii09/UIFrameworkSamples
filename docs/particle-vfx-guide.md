# Bullet Particle VFX Setup Guide

Unity 6 — Aircraft Striker 2D (top-down portrait, orthographic camera)

---

## Material Setup (do this once first)

Create two materials in `Assets/UIFramework/Features/AircraftStriker/Art/Materials/`:

| Material | Shader | Blend Mode | Use |
|----------|--------|-----------|-----|
| `Bullet-Additive.mat` | Universal Render Pipeline/Particles/Unlit | Additive | Glow / Trail layers |
| `Bullet-Alpha.mat` | Universal Render Pipeline/Particles/Unlit | Alpha Blend | Core body layers |

> For non-URP projects: use `Particles/Additive` and `Particles/Alpha Blended` instead.

---

## Prefab Hierarchy (all 6 bullet types follow this pattern)

```
BulletController (root)
  Collider2D            ← existing, leave in place
  [BulletVFX]           ← new child — add BulletVFX component here
    Core                ← ParticleSystem child
    Trail               ← ParticleSystem child
    Glow                ← ParticleSystem child (omit on simple types)
```

Steps for each prefab:
1. Open prefab → right-click root → Create Empty → rename `[BulletVFX]`
2. Add `BulletVFX` component to `[BulletVFX]`
3. Under `[BulletVFX]`, create 2–3 child GameObjects, add ParticleSystem to each
4. Drag the ParticleSystem children into `BulletVFX._layers[]` in the Inspector (order: Core first, Trail second, Glow third)
5. On `BulletController`, assign the `[BulletVFX]` child to the `_vfx` field
6. Disable or delete the old `SpriteRenderer` once VFX looks correct

> **IMPORTANT on every ParticleSystem layer:**
> - `Play On Awake` = **OFF** (BulletVFX controls playback)
> - `Stop Action` = **None**
> - `Looping` = **ON** (unless stated otherwise)

---

## Universal Settings (apply to all types unless noted)

| Setting | Value |
|---------|-------|
| Duration | 1 |
| Looping | ON |
| Prewarm | OFF |
| Gravity Modifier | 0 |
| Simulation Space (Core/Glow) | **Local** |
| Simulation Space (Trail) | **World** |
| Play On Awake | OFF |
| Stop Action | None |

---

## Bullet Type Settings

### 1. PlayerBasic — Cyan Energy Shot

Fast, clean. White-hot core, cyan glow trail.

**CORE**
```
Start Lifetime:  0.06
Start Speed:     0
Start Size:      0.1
Start Color:     #FFFFFF
Max Particles:   8
Simulation Space: Local

Emission → Rate over Time: 60

Shape → Circle, Radius 0.01, Emit from Edge: OFF

Color over Lifetime:
  0%   #FFFFFF, alpha 255
  100% #44DDFF, alpha 0

Size over Lifetime: Curve 1 → 0

Renderer:
  Render Mode: Billboard
  Material: Bullet-Additive
  Order in Layer: 10
```

**TRAIL**
```
Start Lifetime:  0.15
Start Speed:     0
Start Size:      0.07
Start Color:     #00FFFF
Max Particles:   60
Simulation Space: World

Emission → Rate over Time: 80

Shape → Circle, Radius 0.005

Color over Lifetime:
  0%   #44EEEE, alpha 200
  100% #0066CC, alpha 0

Size over Lifetime: Curve 1 → 0

Renderer:
  Material: Bullet-Additive
  Order in Layer: 9
```

---

### 2. EnemyRed — Orange-Red Danger Round

Heavier feel, orange burst center, red smoke trail.

**CORE** (same structure as PlayerBasic, change colors/size)
```
Start Lifetime:  0.08
Start Size:      0.13
Start Color:     #FF8800

Color over Lifetime:
  0%   #FFCC00, alpha 255
  50%  #FF4400, alpha 200
  100% #CC0000, alpha 0
```

**TRAIL**
```
Start Lifetime:  0.2
Start Size:      0.1
Start Color:     #FF3300

Color over Lifetime:
  0%   #FF4400, alpha 180
  100% #330000, alpha 0

Size over Lifetime: Curve 1 → 0.1 (fatter trail, doesn't fully shrink)
```

---

### 3. EnemyBlue — Electric Fast Shot

Smaller, faster feel. Electric blue with white spark core.

**CORE**
```
Start Lifetime:  0.05
Start Size:      0.09
Start Color:     #FFFFFF

Color over Lifetime:
  0%   #FFFFFF, alpha 255
  60%  #4488FF, alpha 200
  100% #0000FF, alpha 0
```

**TRAIL**
```
Start Lifetime:  0.12
Start Size:      0.06
Start Color:     #2266FF

Color over Lifetime:
  0%   #88AAFF, alpha 220
  100% #000055, alpha 0
```

---

### 4. EnemyOrb — Pulsing Purple Orb

Round, slow, threatening. Rotating sparks around a fat core.

**CORE**
```
Start Lifetime:  0.1
Start Speed:     0
Start Size:      0.18
Start Color:     #9900FF
Max Particles:   10
Simulation Space: Local

Emission → Rate over Time: 30

Shape → Circle, Radius 0.04

Color over Lifetime:
  0%   #CC44FF, alpha 255
  100% #440088, alpha 0

Size over Lifetime: Curve: slight pulse — 0.8 → 1 → 0.8 (use 3-point curve)
```

**ORBIT PARTICLES** (extra layer — rename child "Orbit")
```
Start Lifetime:  0.4
Start Speed:     0.5           ← orbiting velocity
Start Size:      0.04
Start Color:     #FF88FF
Max Particles:   15
Simulation Space: Local

Emission → Rate over Time: 20

Shape → Circle, Radius 0.12, Emit from Edge: ON

Velocity over Lifetime:
  Orbital Z: 180 (degrees/sec — makes sparks rotate around center)

Color over Lifetime: #FF88FF → transparent
Size over Lifetime: 1 → 0

Renderer: Bullet-Additive
```

**TRAIL** (optional, subtle)
```
Start Lifetime:  0.25
Start Size:      0.09
Start Color:     #7700CC
Color: #AA55FF → transparent
```

---

### 5. EnemyLaser — Red Laser Bolt

Long, thin, fast. Stretched appearance.

**CORE**
```
Start Lifetime:  0.05
Start Speed:     0
Start Size 3D:
  X: 0.05   ← narrow
  Y: 0.25   ← long
  Z: 0.05
Start Color:     #FF0000
Max Particles:   5

Emission → Rate over Time: 50

Shape → Circle, Radius 0.005

Color over Lifetime:
  0%   #FFFFFF, alpha 255
  30%  #FF4444, alpha 230
  100% #FF0000, alpha 0

Renderer:
  Render Mode: Billboard  (sprite should be a vertical ellipse or stretched pill)
  Material: Bullet-Additive
```

**TRAIL**
```
Start Lifetime:  0.1
Start Size:      0.04
Start Color:     #FF2200
Color: #FF4400 → transparent
Emission rate:   120   (dense trail = laser feel)
```

> Tip: For the Core, use a tall oval sprite (2:1 or 3:1 aspect ratio) as the particle texture.
> Create a simple white oval gradient sprite in any image editor and import as a Sprite/Texture.

---

### 6. BossGold — Charged Gold Shot

Large, menacing. Gold shimmer core, sparks radiating outward.

**CORE**
```
Start Lifetime:  0.12
Start Speed:     0
Start Size:      0.22
Start Color:     #FFD700
Max Particles:   12

Emission → Rate over Time: 40

Shape → Circle, Radius 0.06

Color over Lifetime:
  0%   #FFFFFF, alpha 255
  40%  #FFDD00, alpha 240
  100% #FF6600, alpha 0

Size over Lifetime: Slight pulse (0.9 → 1.1 → 0.9)
```

**SPARKS** (rename child "Sparks")
```
Start Lifetime:  0.3
Start Speed:     0.8          ← radiate outward
Start Size:      0.035
Start Color:     #FFDD44
Max Particles:   30
Simulation Space: Local

Emission → Rate over Time: 50

Shape → Circle, Radius 0.08, Emit from Edge: ON

Color over Lifetime: #FFEE88 → #FF4400 → transparent
Size over Lifetime: 1 → 0

Renderer: Bullet-Additive
```

**TRAIL**
```
Start Lifetime:  0.25
Start Size:      0.12
Start Color:     #FFAA00
Color: #FFCC00 → transparent
Emission Rate:   60
```

---

## Sorting Layer Setup

To ensure bullets render above background and below UI:

1. Edit → Project Settings → Tags and Layers → Sorting Layers
2. Add layer `Bullets` between `Default` and `UI`
3. On each particle system Renderer → set **Sorting Layer: Bullets**, **Order in Layer: 10**
4. Trail layers: Order in Layer: 9 (renders behind core)
5. Glow/Orbit: Order in Layer: 8

---

## Quick Checklist per Prefab

- [ ] `[BulletVFX]` child exists with `BulletVFX` component
- [ ] `BulletController._vfx` field assigned in Inspector
- [ ] All particle systems have `Play On Awake = OFF`
- [ ] All particle systems have `Stop Action = None`
- [ ] Core + Glow → `Simulation Space = Local`
- [ ] Trail → `Simulation Space = World`
- [ ] Layers assigned to `BulletVFX._layers[]`
- [ ] Old `SpriteRenderer` disabled/removed
- [ ] Sorting layer set to `Bullets`
- [ ] Tested play in Editor: enter Play, fire bullets, confirm particles spawn and clear on return to pool

---

## Pool Behavior Notes

- `OnGetFromPool` → `BulletVFX.OnPlay()` → `Clear() + Play()` on all layers
- `OnReturnToPool` → `BulletVFX.OnStop()` → `Stop(StopEmittingAndClear)` on all layers
- Setting `SetActive(false)` (done by the pool) stops emission but doesn't clear particles — the `OnStop()` call before deactivation handles the clear.
- `Prewarm = OFF` is required — prewarm on pooled objects causes particle state to bleed between reuses.
