# NodeTesting

A small 2D game engine built on top of **MonoGame (DesktopGL)**: a set of models that make game development easier — rendering at a fixed resolution, sprites and animations, tilemaps, colliders, a camera, input mapping, paths and text.

All models live in the `NodeTesting.models` namespace.

Each section describes the model, then ends with **Scenarios**: common game situations and the code to handle them.

---

## Contents

- [Setup](#setup)
- [The render loop](#the-render-loop)
- Rendering
  - [Canvas](#-canvas)
  - [Camera & CameraFocus](#-camera--camerafocus)
  - [Lighting & PointLight](#-lighting--pointlight)
- Sprites
  - [Sprite](#sprite)
  - [YSort (draw order)](#-ysort-draw-order)
  - [SpriteSheet](#spritesheet)
  - [SpriteAnimation](#-spriteanimation)
- Maps
  - [TileMap](#-tilemap)
  - [CollisionMap](#-collisionmap)
  - [ObjectMap](#-objectmap)
- Collision
  - [ICollider](#icollider)
  - [CollisionRect](#-collisionrect)
  - [CollisionCircle](#-collisioncircle)
- Gameplay
  - [InputManager](#-inputmanager)
  - [Path2D](#-path2d)
- Text
  - [TextBox](#textbox)
  - [TextInput](#textinput)
- [PicoPallete](#-picopallete)

---

## Setup

```csharp
using NodeTesting.models;

// put these in LoadContent(), before creating any other model
Globals.Content = Content;
Globals.spriteBatch = _spriteBatch;
Globals.graphics = _graphics;
```

`Globals` is the shared state every model reads from:

| Field | Type | Purpose |
|---|---|---|
| `Globals.Content` | `ContentManager` | Loads textures for sprites, maps, animations |
| `Globals.spriteBatch` | `SpriteBatch` | Every model's `Draw()` draws with this |
| `Globals.graphics` | `GraphicsDeviceManager` | Used by `Canvas.SetResolution` |
| `Globals.Input` | `InputManager` | A ready-made input manager (see [InputManager](#-inputmanager)) |
| `Globals.Pixel` | `Texture2D` | A shared 1×1 white texture for drawing boxes and lines. Created on first use. |

> Models load their textures in their constructors, so create them **after** `Globals` is set.

---

## The render loop

A typical frame looks like this:

```csharp
protected override void Update(GameTime gameTime)
{
    Globals.Input.Update();          // first, so JustPressed works this frame
    player.Update(gameTime, walls);
    tilemap.Update(gameTime);        // advances animated tiles
    camera.Update(gameTime);
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    // 1. Game layer: fixed resolution, pixel-art friendly
    canvas.Activate();
    _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: camera.Transform());
    tilemap.Draw(camera.VisibleArea); // only the tiles on screen
    player.Draw(Color.White);
    _spriteBatch.End();
    lighting.Draw(_spriteBatch, camera.Transform()); // darken the world, apply lights
    // (characters and other standing sprites go after this: see Lighting → Flat things vs standing things)

    // 2. UI layer: 1920x1080 coordinates, stays crisp at any window size
    canvas.BeginUI(_spriteBatch);
    _spriteBatch.DrawString(font, "Score: 10", new Vector2(40, 40), Color.White);
    canvas.EndUI(_spriteBatch);

    // 3. Put both layers on screen (optionally with a shader on the game layer)
    canvas.Draw(_spriteBatch);

    base.Draw(gameTime);
}
```

---

## 🖼️ Canvas

Renders the game at a fixed **logical resolution** and scales it to fit the window with letterboxing, so the game looks the same at any window size. It has two layers:

| Layer | Coordinates | Sampling | Use for |
|---|---|---|---|
| **Game** | `width` × `height` you pass in | `PointClamp` | World, sprites, tilemaps, pixel art |
| **UI** | `uiWidth` × `uiHeight` (default 1920×1080) | `LinearClamp` | Text, HUD, menus, painted (non pixel-art) images |

The UI layer works the same way Celeste's HUD does: it's laid out in 1920×1080 space but rendered at **the larger of 1080p and the on-screen size**. Small windows get a full-size UI that is scaled *down* (stays sharp), and big windows render the UI at native size. UI art is never stretched up from a small texture, which is what makes text blur.

> Make fonts and UI images at the size they should look at 1080p (e.g. a SpriteFont `<Size>` of 32–48, not 12).

#### Constructor

**public Canvas(GraphicsDevice graphicsDevice, GameWindow window, int width, int height, int uiWidth = 1920, int uiHeight = 1080)**

- graphicsDevice: the game's graphics device.
- window: the game window. The canvas re-fits itself when the window is resized.
- width / height: the logical resolution of the game layer (e.g. `320, 180` or `1280, 720`).
- uiWidth / uiHeight: the logical resolution of the UI layer.

```csharp
canvas = new Canvas(GraphicsDevice, Window, 320, 180);
```

#### Properties

| Property | Description |
|---|---|
| `Width`, `Height` | Logical size of the game layer |
| `UiWidth`, `UiHeight` | Logical size of the UI layer |

#### Methods

| Method | Description |
|---|---|
| `Activate()` | Redirects drawing into the game layer and clears it. Call before drawing the world. |
| `BeginUI(SpriteBatch, Matrix? transform = null, BlendState blendState = null, Effect effect = null)` | Starts the UI layer. Everything until `EndUI` uses UI coordinates. `transform` is applied on top of the UI scale. |
| `EndUI(SpriteBatch)` | Ends the UI layer. |
| `Draw(SpriteBatch, Effect effect = null)` | Draws the game layer to the screen (with `effect`, e.g. a CRT shader, applied to the game layer only), then the UI layer on top. The UI is only drawn if `BeginUI` was called this frame. |
| `GetTexture()` | The game layer's render target, for use in shaders. |
| `ScreenToCanvas(Vector2 screenPos)` | Window pixel → game-layer coordinates (accounts for letterbox bars and scale). |
| `ScreenToUI(Vector2 screenPos)` | Window pixel → UI-layer coordinates. Use for clicking UI elements. |
| `ScreenToWorld(Vector2 screenPos, Matrix cameraTransform)` | Window pixel → world coordinates (canvas + camera). |
| `ScreenToWorld(Matrix cameraTransform)` | Same, reading the mouse position for you. |
| `SetResolution(int width, int height)` | Changes the window size and re-fits the canvas. |
| `SetDestinationRectangle()` | Recalculates the letterbox rectangle. Called automatically on resize. |

```csharp
Vector2 mouseWorld = canvas.ScreenToWorld(camera.Transform());
```

#### Scenarios

**A pixel-art game at 320×180.** Pick a resolution that fits common screens a whole number of times (320×180 is ×4 at 1280×720 and ×6 at 1920×1080), so every pixel comes out the same size.

```csharp
canvas = new Canvas(GraphicsDevice, Window, 320, 180);
canvas.SetResolution(1280, 720);   // open the window at 4× size
Window.AllowUserResizing = true;   // any other size gets letterboxed
```

**Clicking something in the world** (select a unit, open a chest). Convert the mouse to world coordinates and test it against colliders:

```csharp
MouseState mouse = Mouse.GetState();
bool clicked = mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released;
previousMouse = mouse;

if (clicked)
{
    Point world = canvas.ScreenToWorld(camera.Transform()).ToPoint();
    foreach (Chest chest in chests)
        if (chest.Rect.Contains(world))
            chest.Open();
}
```

**Clicking a UI button.** UI positions are in 1920×1080 space, so convert with `ScreenToUI` instead:

```csharp
var playButton = new CollisionRect(960, 600, 300, 80);   // centered, in UI coordinates

// Update
Point ui = canvas.ScreenToUI(new Vector2(mouse.X, mouse.Y)).ToPoint();
if (clicked && playButton.Contains(ui))
    StartGame();

// Draw, between canvas.BeginUI and canvas.EndUI
playButton.Draw(playButton.Contains(ui) ? PicoPallete.blue : PicoPallete.dark_blue);
```

**A CRT or rain shader over the game, but not the HUD.** Pass the effect to `canvas.Draw`; the UI layer is drawn on top afterwards, untouched:

```csharp
crt = Content.Load<Effect>("CRT_TV");

// Draw, last thing in the frame
crt.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
canvas.Draw(_spriteBatch, crt);
```

---

## 🎥 Camera & CameraFocus

A 2D camera with pan, zoom and rotation, smooth "focus" transitions, and optional world bounds.

The camera views the [Canvas](#-canvas) game layer, so it's created from the canvas. Its `Origin` is the **center of the canvas** (e.g. `(640, 360)` for 1280×720): the screen point where the camera's `Position` gets drawn. Because the canvas size never changes, resizing the window can't throw the camera off.

#### Constructor

**public Camera(Canvas canvas)** — starts at `(0, 0)`, zoom `1`, with its origin at the center of the canvas.

```csharp
canvas = new Canvas(GraphicsDevice, Window, 1280, 720);
camera = new Camera(canvas);   // create the canvas first
```

#### Properties

| Property | Description |
|---|---|
| `Position` | World position the camera looks at |
| `Zoom` | Zoom level (`1` = normal) |
| `Rotation` | Rotation in radians |
| `Origin` | Screen point the camera is centered on |
| `IsFocusing` | `true` while moving toward or locked on a focus |
| `Bounds` | The world area the view is kept inside, or `null` |
| `VisibleArea` | The world area currently on screen (accounts for zoom and rotation). Pass it to `tilemap.Draw(...)` to skip off-screen tiles. |

#### Methods

| Method | Description |
|---|---|
| `Transform()` | The view matrix. Pass it to `SpriteBatch.Begin(transformMatrix: ...)`. |
| `MoveTo(CameraFocus focus)` | Starts moving toward a focus point. |
| `Update(GameTime)` | Drives the focus transition and applies the bounds. Call once per frame. |
| `SetBounds(Rectangle? worldBounds)` | Keeps the view inside a world rectangle. `null` removes the limit. |
| `Release()` | Stops following the current focus. |
| `Reset()` | Back to `(0, 0)`, zoom `1`, no focus. |

#### CameraFocus

**public CameraFocus(Vector2 position, float targetZoom = 2f, float transitionSpeed = 0.1f, FocusMode mode = FocusMode.Smooth, FocusAnchor anchor = FocusAnchor.OneShot)**

- position: the world position to focus on.
- targetZoom: the zoom to reach.
- transitionSpeed: lerp amount per frame (`0`–`1`, higher = snappier). Only used in `Smooth` mode.
- mode: `FocusMode.Instant` (snap) or `FocusMode.Smooth` (lerp).
- anchor: `FocusAnchor.OneShot` (move there, then release) or `FocusAnchor.Persistent` (keep following).

```csharp
// smooth zoom to a point, then release
camera.MoveTo(new CameraFocus(node.Position, targetZoom: 3f, transitionSpeed: 0.08f));

// keep following a moving target (update the focus Position each frame)
camera.MoveTo(new CameraFocus(enemy.Position, targetZoom: 2f, anchor: FocusAnchor.Persistent));

// instant snap
camera.MoveTo(new CameraFocus(node.Position, mode: FocusMode.Instant));

camera.Release();
```

#### Keeping the view inside the map

Without bounds, the camera keeps the player centered even at the map's edge, so the screen shows the empty area past it. `SetBounds` stops the camera's center half a screen away from each edge, so the map edge lines up with the screen edge and the player walks toward the side of the screen instead.

```csharp
camera.SetBounds(tilemap.PixelBounds);
camera.MoveTo(new CameraFocus(player.Position, targetZoom: 2f, anchor: FocusAnchor.Persistent));
```

How it works, for a 1280×720 canvas at zoom `1`: the camera sees 640 pixels either side of its center (`Origin / Zoom`), so its center can't go below `Left + 640` or above `Right − 640` (and likewise `Top + 360` / `Bottom − 360`).

- Zooming in shows less of the world, so the camera can get closer to the edges.
- If the map is smaller than the view on an axis, it's centered on that axis instead.
- A `OneShot` focus near an edge still arrives: it stops at the closest point the camera can reach.
- Bounds assume `Rotation` is `0`. A rotated view can still show past the edges.

#### Scenarios

**Follow the player with a soft lag.** Keep one `CameraFocus` and move it. `CameraFocus` is a class, so the camera sees the new position every frame:

```csharp
// LoadContent
camera.SetBounds(tilemap.PixelBounds);
playerFocus = new CameraFocus(player.Pos, targetZoom: 1f, transitionSpeed: 0.15f,
                              anchor: FocusAnchor.Persistent);
camera.MoveTo(playerFocus);

// Update, after moving the player
playerFocus.Position = player.Pos;
camera.Update(gameTime);
```

Lower `transitionSpeed` = lazier camera. Use `mode: FocusMode.Instant` for a camera locked rigidly to the player.

**Cutscene: show a door opening, then return to the player.** A `OneShot` focus stops on arrival, and `IsFocusing` turns `false`:

```csharp
// when the lever is pulled
camera.MoveTo(new CameraFocus(door.Position, targetZoom: 1.5f, transitionSpeed: 0.05f));
showingDoor = true;
doorTimer = 1.5f;

// Update
if (showingDoor && !camera.IsFocusing)          // arrived at the door
{
    doorTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    if (doorTimer <= 0)
    {
        showingDoor = false;
        camera.MoveTo(playerFocus);             // back to following
    }
}
```

**Zoom out when entering a boss room.** Call `MoveTo` every frame with whichever focus applies; a smooth focus just keeps easing toward the newest target:

```csharp
if (player.Rect.Intersects(bossRoom))
    camera.MoveTo(new CameraFocus(bossRoom.Center, targetZoom: 0.75f, transitionSpeed: 0.05f,
                                  anchor: FocusAnchor.Persistent));
else
    camera.MoveTo(playerFocus);
```

---

## 💡 Lighting & PointLight

Darkens the [Canvas](#-canvas) game layer and lets lights bring back the area around them, like a dark room lit by torches. Occluders (walls, trunks...) block light and cast shadows. The UI layer is never affected.

How it works: each frame a light map the size of the canvas is filled with the `Ambient` color, every light adds a soft glow onto it, and the game layer is multiplied by it. Where the light map is black the world is black; where it's white the world shows its true colors.

#### Lighting

**public Lighting(Canvas canvas, GraphicsDevice graphicsDevice)**

| Member | Description |
|---|---|
| `Ambient` | Light level where no light reaches. `Color.Black` = pitch dark, `Color.White` = no darkness. `(25, 25, 45)` reads as night. |
| `Enabled` | `false` turns lighting off |
| `Lights` | All lights |
| `Add(PointLight light)` | Adds a light and returns it |
| `Remove(PointLight light)` | Removes a light |
| `Occluders` | Everything that blocks light (`CollisionRect`s and `CollisionCircle`s). Read every frame, so moving one moves its shadow. |
| `AddOccluder(ICollider)` / `AddOccluders(IEnumerable<ICollider>)` | Makes colliders block light, e.g. `AddOccluders(walls)` or `AddOccluders(collisionMap.GetCollisionRects())` |
| `RemoveOccluder(ICollider)` | Stops a collider blocking light |
| `GetLightAt(Vector2 worldPosition)` | The light reaching one spot (ambient + every light that can see it past the occluders). Use it to tint standing sprites by their feet. Returns white while lighting is off. |
| `DrawnLightCount` | How many lights were drawn last frame. Lights whose glow doesn't reach the screen are skipped. |
| `Draw(SpriteBatch, Matrix cameraTransform)` | Builds the light map and applies it. Call after the world's `SpriteBatch.End()` and before `canvas.BeginUI(...)`, with the same camera matrix the world used. |

#### PointLight

A round glow, brightest at its center, fading to nothing at its radius.

**public PointLight(Vector2 position, float radius, Color? color = null, float intensity = 1f)**

| Field | Description |
|---|---|
| `Position` | Center, in world pixels. Update it each frame to make a light follow something. |
| `Radius` | How far it reaches, in world pixels |
| `Color` | White shows true colors; warm or cool colors tint what they light |
| `Intensity` | Brightness, `0`–`1` |
| `Enabled` | `false` switches it off without removing it |
| `CastsShadows` | `false` lets this light ignore occluders. Use it for small glows (coins, sparks); it's also cheaper. |

```csharp
lighting = new Lighting(canvas, GraphicsDevice) { Ambient = new Color(25, 25, 45) };

PointLight playerLight = lighting.Add(new PointLight(player.Pos, 180, new Color(255, 230, 190)));
lighting.Add(new PointLight(new Vector2(390, 130), 110, Color.Orange));   // a lamp

// Update: playerLight.Position = player.Pos;
// Draw:   after the world's _spriteBatch.End():
lighting.Draw(_spriteBatch, camera.Transform());
```

Lights where the [ObjectMap](#-objectmap) places things are easy: `foreach (var p in placements.GetPositions(2)) lighting.Add(new PointLight(p, 28, Color.Gold));`

> Lights can only reveal the art, never make it brighter than it really is. Overlapping lights add together up to full brightness.

#### Shadows

```csharp
lighting.AddOccluders(walls);   // the same colliders the player bumps into
```

Each edge of an occluder that faces away from a light is stretched away from it into a long dark band; together they form the shadow. Occluders themselves are never darkened, not even by each other's shadows, so walls stay lit and darkness only falls on open ground behind them. A light at the outer corner of an L-shaped wall lights both arms and leaves one dark wedge behind the corner.

#### Flat things vs standing things

The light map is a flat picture: it lights each pixel on its own, with no idea what's in front of what. That's right for the floor and wall tops, but wrong for a character standing in front of a wall. Their head overlaps the wall on screen, so it gets the wall's light while their body gets the floor's shadow.

So split drawing in two:

| Group | Examples | Lit by |
|---|---|---|
| **Flat** | floor tiles, wall tops, items lying on the ground | the light map: draw before `lighting.Draw` |
| **Standing** | player, NPCs, enemies, trees | one light level at their feet: draw after `lighting.Draw`, tinted with `GetLightAt(feet)` |

```csharp
// 1. Flat things
canvas.Activate();
_spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: camera.Transform());
tilemap.Draw(camera.VisibleArea);
_spriteBatch.End();

// 2. Lighting
lighting.Draw(_spriteBatch, camera.Transform());

// 3. Standing things, each lit at its feet
_spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: camera.Transform());
player.Draw(lighting.GetLightAt(player.Rect.Center));
_spriteBatch.End();
```

A character whose feet are in shadow is dark from head to toe; step into the light and the whole sprite lights up.

**Off-screen lights are skipped when drawing.** A light is only painted if its circle reaches the part of the world on screen, so a huge level with hundreds of lamps only pays for the ones near the camera. Skipped lights are still on: they're drawn again as soon as the camera gets near, and `GetLightAt` always counts every light, so gameplay checks like "is this enemy standing in light?" work anywhere in the level.

Lights with no occluder within their radius cost the same as before. Each light that *does* have one in range needs an extra pass, so prefer `CastsShadows = false` on lots of small lights.

#### Scenarios

**A flickering torch.** Change `Intensity` or `Radius` a little every frame:

```csharp
torch = lighting.Add(new PointLight(torchPos, 100, new Color(255, 150, 60)));

// Update
float t = (float)gameTime.TotalGameTime.TotalSeconds;
torch.Intensity = 0.85f + 0.15f * MathF.Sin(t * 13f) * MathF.Sin(t * 7f);
torch.Radius = 100f + 3f * MathF.Sin(t * 11f);
```

**Day/night cycle with street lamps.** Blend `Ambient` between a day and a night color, and switch lamps on when it gets dark:

```csharp
// Update
timeOfDay += dt * 0.05f;                                    // one full day every ~2 minutes
float daylight = (MathF.Sin(timeOfDay) + 1f) / 2f;          // 0 = midnight, 1 = noon
lighting.Ambient = Color.Lerp(new Color(25, 25, 45), Color.White, daylight);

foreach (PointLight lamp in streetLamps)
    lamp.Enabled = daylight < 0.4f;
```

**Stealth: can the guard see the player?** `GetLightAt` works anywhere in the level, on or off screen:

```csharp
Color light = lighting.GetLightAt(player.Rect.Center);
float brightness = (light.R + light.G + light.B) / (3f * 255f);
bool hiddenInShadow = brightness < 0.25f;
```

**A door that blocks light while closed:**

```csharp
lighting.AddOccluder(door.Rect);          // closed: light stops at the door
// when it opens:
lighting.RemoveOccluder(door.Rect);       // light spills into the next room
```

**A lantern pickup that widens the player's light:** `playerLight.Radius = hasLantern ? 180 : 60;`

---

## Sprite

A single texture drawn at a position.

**public Sprite(string texture, Vector2 pos)**
- texture: the path to the texture in the Content folder.
- pos: the position of the sprite.

**public void Draw(Color color)**
- color: the color to draw the sprite with (usually `Color.White`).

| Member | Description |
|---|---|
| `Pos` | Where the sprite is drawn. The texture's `Origin` lands on this point. |
| `Origin` | The point of the texture placed at `Pos`. Defaults to the **center** of the texture. |
| `Depth` | Draw order when the SpriteBatch uses `SpriteSortMode.FrontToBack`: higher is drawn on top. Set it with [`YSort.Depth(feetY)`](#-ysort-draw-order). Ignored in the default sort mode. |

#### Scenarios

**A full-screen background.** Move the origin to the top-left corner so `Pos` is the corner:

```csharp
background = new Sprite("background", Vector2.Zero);
background.Origin = Vector2.Zero;
```

**Something that stands on the ground** (characters, trees, signs). Put the origin at the bottom-middle so `Pos` is the feet, which also matches [ObjectMap](#-objectmap) positions:

```csharp
sign = new Sprite("sign", position);
sign.Origin = new Vector2(8, 16);   // bottom-middle of a 16×16 image
```

**Flash red when hurt, or fade out.** The draw color is a tint:

```csharp
hero.Draw(hurtTimer > 0 ? Color.Red : Color.White);
ghost.Draw(Color.White * 0.5f);     // half transparent
```

**A game object that is a sprite.** Inherit from `Sprite` and add your own logic, like `Player` does:

```csharp
public class Crate : Sprite
{
    public CollisionRect Rect { get; }

    public Crate(Vector2 pos) : base("crate", pos)
    {
        Rect = new CollisionRect((int)pos.X, (int)pos.Y, 16, 16) { IsStatic = true };
    }
}
```

---

## ↕️ YSort (draw order)

In a top-down game, things lower on screen are closer to the camera and should be drawn on top. Instead of ordering your draw calls by hand, give every standing sprite a `Depth` from its feet and let SpriteBatch sort them.

**public static float YSort.Depth(float feetY)**: the depth for something standing at `feetY` (world pixels). Bigger Y gives a bigger depth, drawn later. No setup needed; it works for worlds up to a million pixels in each direction.

`Sprite`, `SpriteSheet` and `SpriteAnimation` all have a `Depth` field.

```csharp
_spriteBatch.Begin(SpriteSortMode.FrontToBack, samplerState: SamplerState.PointClamp,
                   transformMatrix: camera.Transform());

player.Depth = YSort.Depth(player.FeetY);          // moving things: update every frame
player.Draw(Color.White);

foreach (var enemy in enemies) enemy.Draw();       // each sets its own Depth from its feet
foreach (var tree in trees) tree.Draw();           // trees don't move: Depth set once when created

_spriteBatch.End();   // sorted here: furthest back first, closest last
```

The order of the `Draw` calls doesn't matter; only `Depth` does. Sorting a few hundred sprites takes microseconds.

> Use the **feet** (where it touches the ground), not the center: a tall tree and a short player have very different centers, but where they stand decides who's in front.

> Flat things (floor tiles, items lying on the ground) don't need depths. Draw them in an earlier, normal `Begin()` so they're always underneath.

---

## SpriteSheet

A horizontal strip of equally sized frames in one texture.

**public SpriteSheet(string Texture, int frames)**
- Texture: the path to the texture in the Content folder.
- frames: how many frames are in the strip. Frame width = texture width ÷ frames.

| Member | Description |
|---|---|
| `FrameWidth`, `FrameHeight` | Size of one frame |
| `Draw()` | Draws the sheet using its own settings |
| `Depth` | Draw order with `SpriteSortMode.FrontToBack` (see [YSort](#-ysort-draw-order)) |
| `DrawFrame(int index, Vector2 position)` | Draws one frame (0-based) |
| `DrawFrame(int index, Vector2 position, float scale)` | …with a scale |
| `DrawFrame(int index, Vector2 position, float scale, Color color)` | …with a scale and tint |
| `DrawFrame(int index, Vector2 position, float rotation, float scale, SpriteEffects effects = None)` | …with full control |

Use a `SpriteSheet` when you pick the frame yourself; use [SpriteAnimation](#-spriteanimation) when frames should play on a timer. `position` is the **center** of the frame. `Draw()` always draws at `(0, 0)` unless you inherit and set `Position`, so use `DrawFrame` for normal use.

#### Scenarios

**A pixel-art health bar of hearts.** With a `hearts.png` strip of 3 frames (full, half, empty), drawn on the game layer without the camera so it stays sharp and fixed on screen:

```csharp
hearts = new SpriteSheet("hearts", 3);

// Draw: after the world and lighting, before canvas.BeginUI
_spriteBatch.Begin(samplerState: SamplerState.PointClamp);   // no camera transform
for (int i = 0; i < maxHealth / 2; i++)
{
    int left = health - i * 2;                     // health inside this heart: 2, 1 or 0
    int frame = left >= 2 ? 0 : left == 1 ? 1 : 2;
    hearts.DrawFrame(frame, new Vector2(12 + i * (hearts.FrameWidth + 2), 12));
}
_spriteBatch.End();
```

**Random variations** (rocks, grass, cracks). Choose a frame once when the object is created and keep it:

```csharp
variant = random.Next(4);
// Draw
rocks.DrawFrame(variant, position);
```

**Item icons in an inventory**, one frame per item ID: `items.DrawFrame(slot.ItemId, slotCenter, 2f);`

---

## 🏃 SpriteAnimation

Frame-based animation from a **single-row strip**: one texture with equally sized frames laid out left to right. Use one strip per animation (e.g. `hero_idle`, `hero_walk`).

#### Constructor

**public SpriteAnimation(string texturePath, int frames, int fps)**
- texturePath: the path to the texture in the Content folder.
- frames: how many frames are in the strip. Frame width = texture width ÷ frames.
- fps: playback speed in frames per second.

#### Fields

`Position`, `Color`, `Scale`, `Rotation`, `Origin`, `SpriteEffect` and `Depth` (see [YSort](#-ysort-draw-order)) control how it is drawn. `FrameIndex`, `IsFinished` and `IsReversed` hold the playback state.

#### Methods

| Method | Description |
|---|---|
| `Update(GameTime)` | Advances the animation (loops) |
| `UpdateOnce(GameTime)` | Advances without looping (stops on the last frame) |
| `Draw()` | Draws the current frame |
| `DrawFrame(int index)` | Draws a specific frame |
| `SetFrame(int frameIndex)` | Jumps to a frame |
| `Reset()` | Back to the first frame |
| `ToggleReverse()` | Plays backwards / forwards |
| `IsAnimationFinished()` | `true` once `UpdateOnce` has reached the end |
| `GetCurrentFrameRect()`, `GetFrameRect(int index)` | Source rectangles in the texture |
| `GetTotalFrames()` | Number of frames in the strip |

```csharp
var walk = new SpriteAnimation("hero_walk", 4, 12);
walk.Position = new Vector2(100, 100);

// Update: walk.Update(gameTime);
// Draw:   walk.Draw();
```

#### Scenarios

**Switching between idle and walk, facing left or right.** Keep one animation per state and swap which one is current:

```csharp
idle = new SpriteAnimation("hero_idle", 4, 6);
walk = new SpriteAnimation("hero_walk", 6, 12);
current = idle;

// Update
SpriteAnimation next = dir != Vector2.Zero ? walk : idle;
if (next != current)
{
    next.Reset();                 // start the new animation from its first frame
    current = next;
}
if (dir.X != 0) facingLeft = dir.X < 0;

current.Position = pos;
current.SpriteEffect = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
current.Update(gameTime);

// Draw
current.Draw();
```

**A one-shot explosion that removes itself.** `UpdateOnce` stops on the last frame and sets `IsFinished`:

```csharp
explosions.Add(new SpriteAnimation("explosion", 8, 16) { Position = enemy.Pos });

// Update
foreach (SpriteAnimation boom in explosions)
    boom.UpdateOnce(gameTime);
explosions.RemoveAll(boom => boom.IsFinished);
```

**A door that opens and closes.** Play once forwards to open, once backwards to close:

```csharp
// open
door.IsReversed = false;
door.IsFinished = false;

// close
door.IsReversed = true;
door.IsFinished = false;

// Update
door.UpdateOnce(gameTime);
bool blocksPlayer = door.FrameIndex == 0;   // only solid when fully closed
```

**Dealing damage on a specific frame** (the frame where the sword is extended):

```csharp
swing.UpdateOnce(gameTime);
if (swing.FrameIndex == 2 && !hitApplied)
{
    hitApplied = true;
    foreach (Enemy e in enemies)
        if (swordBox.Intersects(e.Rect)) e.Hurt(1);
}
```

---

## 🧱 TileMap

Draws a grid of tiles from a tileset texture and a CSV file.

**public TileMap(string texturePath, int tileWidth, int tileHeight, string csvPath)**
- texturePath: the tileset in the Content folder.
- tileWidth / tileHeight: size of one tile in pixels.
- csvPath: the map file. Each value is a tile ID (tiles in the tileset are numbered left to right, top to bottom, from `0`). Negative IDs (e.g. `-1`) are empty.

The CSV is looked up in the working directory and in `Content/`. Mark it as *Copy to Output Directory* in the `.csproj`.

#### Methods

| Method | Description |
|---|---|
| `RegisterAnimation(int baseTileId, int[] frameTileIds, float frameDurationSeconds)` | Every cell with `baseTileId` cycles through `frameTileIds` |
| `Update(GameTime)` | Advances tile animations. Call once per frame. |
| `Draw()` | Draws the whole map |
| `Draw(Rectangle visibleArea)` | Draws only the tiles inside `visibleArea` (world pixels). Use `camera.VisibleArea`. |
| `PixelBounds` | The map's area in world pixels, e.g. for `camera.SetBounds(tilemap.PixelBounds)` |

```csharp
tilemap = new TileMap("tilemap", 16, 16, "tilemap.csv");
tilemap.RegisterAnimation(20, new[] { 20, 21, 22, 23 }, 0.15f); // animated water
```

#### Scenarios

**Layers: ground, decorations, and things drawn over the player** (roofs, treetops). Export each layer to its own CSV, all using the same tileset; empty cells are `-1`:

```csharp
ground = new TileMap("tilemap", 16, 16, "ground.csv");
decor  = new TileMap("tilemap", 16, 16, "decor.csv");
above  = new TileMap("tilemap", 16, 16, "above.csv");

// Draw
ground.Draw(camera.VisibleArea);
decor.Draw(camera.VisibleArea);
player.Draw(Color.White);
above.Draw(camera.VisibleArea);    // the player walks "under" these
```

**Loading the next level:**

```csharp
void LoadLevel(int number)
{
    tilemap = new TileMap("tilemap", 16, 16, $"level{number}.csv");
    collisionMap = new CollisionMap("tilemap", 16, 16, $"level{number}_collision.csv");
    camera.SetBounds(tilemap.PixelBounds);
}
```

**Animated water, lava, torches on walls.** Paint the first frame's ID in the CSV and register the rest. Every cell using that ID animates in sync:

```csharp
tilemap.RegisterAnimation(28, new[] { 28, 29, 30, 31 }, 0.2f);   // lava
```

---

## 🧱 CollisionMap

A map used for collision instead of visuals. In its CSV, **every number `0` or above is solid**, and the number is the tile's **kind** (e.g. `0` = wall, `1` = ice, `2` = mud). Negative numbers (e.g. `-1`) are empty.

When the map loads, touching tiles **of the same kind** are merged into as few rectangles as possible: a 40-tile wall becomes one `CollisionRect`, not 40. Tiles of different kinds never merge with each other. Paint individual tiles in Tiled as usual; the merging is automatic.

```
Painted:              Merged:
0 0 0 0 . .           ┌───────┐
0 0 0 0 . .           │  4×2  │ . .
. . 0 0 0 0           └─┬─────┴─┐
                        │  4×1  │      10 tiles → 2 rectangles
```

Fewer rectangles makes everything that loops over them faster (collision checks, player collision, light shadows), and a player sliding along a long wall can't catch on the seams between tiles.

**public CollisionMap(string texturePath, int tileWidth, int tileHeight, string csvPath)**

| Method | Description |
|---|---|
| `GetCollisionRects()` | All solid rectangles, of every kind, already merged |
| `GetCollisionRects(int kind)` | The merged rectangles of one kind, e.g. only walls. Empty if the kind isn't used. |
| `Kinds` | Every kind used in the map |
| `GetKindAt(Vector2 worldPosition)` | The kind of the tile at a world position, or `-1` if empty. For "what am I standing on?" |
| `CheckCollision(CollisionRect collider, int? kind = null)` | `true` if the collider touches a solid tile (of `kind`, if given) |
| `CheckCollision(Rectangle rectangle, int? kind = null)` | Same, for a plain `Rectangle` |
| `GetIntersectingTiles(Rectangle target, int? kind = null)` | Grid positions of the solid tiles a rectangle overlaps |
| `ResolveCollisionHorizontal(ref Rectangle rect, float velocityX, int? kind = null)` | Pushes `rect` out of solid tiles on the X axis |
| `ResolveCollisionVertical(ref Rectangle rect, float velocityY, int? kind = null)` | Pushes `rect` out of solid tiles on the Y axis |
| `Draw()` / `Draw(Rectangle visibleArea)` | Draws the map's tiles (debugging), same as [TileMap](#-tilemap) |
| `DrawAllCollisionRects(Color color)` | Draws every merged rectangle (debugging) |
| `DrawCollisionRects(int kind, Color color)` | Draws the merged rectangles of one kind (debugging) |

> Move and resolve one axis at a time: apply X velocity → `ResolveCollisionHorizontal`, then Y velocity → `ResolveCollisionVertical`.

```csharp
const int Wall = 0, Ice = 1;
collisionMap = new CollisionMap("tilemap", 16, 16, "collision.csv");

// walls block the player and the light; ice is a floor the player walks over
walls = collisionMap.GetCollisionRects(Wall).ToArray();
lighting.AddOccluders(collisionMap.GetCollisionRects(Wall));

// Update: slide on ice
bool onIce = collisionMap.GetKindAt(player.Rect.Center) == Ice;
```

#### Kinds vs objects

| It's a... | Example | Put it in |
|---|---|---|
| surface type: the same everywhere | wall, ice, mud | `CollisionMap`, one number per kind (merged per kind) |
| thing with its own behavior or state | breakable block, door, retracting spikes | an [ObjectMap](#-objectmap) object with its own collider (never merged) |

#### Scenarios

**Top-down game: walls push the player out.** Use the merged rectangles as [ICollider](#icollider)s:

```csharp
walls = collisionMap.GetCollisionRects(Wall).ToArray();

// Update
foreach (ICollider wall in walls)
    wall.ResolveAgainst(player.Rect);
```

**Platformer: gravity, landing and hitting your head.** Move and resolve one axis at a time. Keep the real position as a float so slow movement isn't lost to rounding:

```csharp
// Update
float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
velocity.Y += 900f * dt;                                   // gravity

// X
pos.X += velocity.X * dt;
var box = new Rectangle((int)MathF.Round(pos.X), (int)MathF.Round(pos.Y), 12, 16);  // top-left + size
int wantedX = box.X;
collisionMap.ResolveCollisionHorizontal(ref box, velocity.X, Wall);
if (box.X != wantedX) { pos.X = box.X; velocity.X = 0; }   // hit a wall

// Y
pos.Y += velocity.Y * dt;
box.Y = (int)MathF.Round(pos.Y);
int wantedY = box.Y;
collisionMap.ResolveCollisionVertical(ref box, velocity.Y, Wall);
if (box.Y != wantedY) { pos.Y = box.Y; velocity.Y = 0; }   // landed, or bumped the ceiling

// On the ground if a 1-pixel strip under the feet touches a wall
onGround = collisionMap.CheckCollision(new Rectangle(box.X, box.Bottom, box.Width, 1), Wall);
if (onGround && Globals.Input.JustPressed("Jump"))
    velocity.Y = -300f;
```

**Ice and mud change how the player moves.** Leave them out of the walls so the player walks over them, and read the kind under the feet:

```csharp
const int Wall = 0, Ice = 1, Mud = 2;

// Update
int ground = collisionMap.GetKindAt(player.Rect.Center);
float grip     = ground == Ice ? 0.03f : 0.25f;   // how fast the velocity follows the input
float maxSpeed = ground == Mud ? 60f   : 150f;
velocity = Vector2.Lerp(velocity, inputDir * maxSpeed, grip);
```

**Spikes and pits that reset the player:**

```csharp
const int Spikes = 3;
if (collisionMap.CheckCollision(player.Rect, Spikes))
    Respawn();
```

---

## 📍 ObjectMap

A CSV map that **places objects instead of drawing tiles**. Each number stands for a kind of object (e.g. `1` = tree, `2` = coin), and `Spawn` creates your object at every cell with that number. The map draws nothing itself; the objects you create update and draw themselves, so they can have animations, colliders and their own logic.

**public ObjectMap(string csvPath, int tileWidth, int tileHeight)**
- csvPath: the placement CSV, looked up like [TileMap](#-tilemap)'s (mark it *Copy to Output Directory*). Empty cells can be `-1`.
- tileWidth / tileHeight: size of one cell. Use your tilemap's tile size so objects line up with the tiles.

| Member | Description |
|---|---|
| `Spawn<T>(int id, Func<Vector2, T> create)` | Calls `create` for every cell containing `id` (left to right, top to bottom) and returns the objects |
| `GetPositions(int id)` | The positions of every cell containing `id` |
| `Width`, `Height`, `PixelBounds` | Map size in cells / world pixels |

Positions are the **bottom-center** of the cell: where something standing there touches the ground. Tall objects grow upward from it (set the sprite's `Origin` to its bottom-middle); for something centered in the cell, subtract half a tile from Y.

```csharp
// objects.csv:  -1 = empty, 1 = tree, 2 = coin
var placements = new ObjectMap("objects.csv", 16, 16);
List<Tree> trees = placements.Spawn(1, position => new Tree(position));
List<Coin> coins = placements.Spawn(2, position => new Coin(position));

// Update: foreach (var coin in coins) coin.Update(gameTime);
// Draw:   foreach (var coin in coins) coin.Draw();
```

`Tree` and `Coin` in this repo are example objects: a tree with a trunk wall and a canopy that fades while the player is behind it, and a spinning coin using `SpriteAnimation`.

#### Scenarios

**The player's start point.** Paint a single cell with its own number and read its position:

```csharp
Vector2 spawn = placements.GetPositions(9)[0];   // 9 = player start
player = new Player("player", spawn);
```

**Objects that block the player and the light.** Collect their colliders after spawning:

```csharp
var wallList = new List<ICollider>(collisionMap.GetCollisionRects(Wall));
foreach (Tree tree in trees)
    wallList.Add(tree.Wall);
walls = wallList.ToArray();
lighting.AddOccluders(walls);
```

**Collecting coins.** Give your object a collider and remove it on touch (here `Coin` is assumed to have a `Hitbox`):

```csharp
score += coins.RemoveAll(coin => player.Rect.Intersects(coin.Hitbox));
```

**Enemies with their own behavior.** Anything with a constructor that takes a position works:

```csharp
List<Slime> slimes = placements.Spawn(3, position => new Slime(position, speed: 40f));
List<Lamp>  lamps  = placements.Spawn(4, position => new Lamp(position, lighting));   // adds its own PointLight
```

---

## ICollider

The interface shared by all colliders, so they can be tested against each other.

| Member | Description |
|---|---|
| `bool IsStatic` | Static colliders don't move and push moving colliders out |
| `bool Intersects(ICollider other)` | Overlap test against any collider |
| `bool Contains(Point point)` | Point test |
| `void Draw(Color color)` | Debug draw |
| `Vector2 ResolveAgainst(ICollider moving)` | If this collider is static, returns the correction that moves `moving` out of it (or `Vector2.Zero`) |

```csharp
foreach (ICollider wall in walls)
{
    Vector2 push = wall.ResolveAgainst(player.Rect);
    // apply push to the player's position
}
```

---

## 🟩 CollisionRect

A rectangular collider.

#### Constructor

**public CollisionRect(int x, int y, int width, int height)**

Creates a rectangle **centered** at the given position.

- x / y: the center of the rectangle.
- width / height: the size of the rectangle.

#### Properties

| Property | Description |
|---|---|
| `Rect` | The underlying `Rectangle` |
| `Pos` | Top-left position |
| `Center` | Center position |
| `IsStatic` | See [ICollider](#icollider) |

#### Methods

| Method | Description |
|---|---|
| `Intersects(ICollider other)` | `true` if the colliders overlap |
| `Contains(Point target)` | `true` if the point is inside |
| `UpdateRect(int x, int y)` | Moves the rectangle, keeping it centered on `(x, y)` |
| `Translate(int dx, int dy)` | Moves the rectangle by an offset |
| `SetOffsetExtra(int x, int y)` | Extra offset, for lining the collider up with a sprite |
| `ResolveAgainst(ICollider moving)` | See [ICollider](#icollider) |
| `Draw(Color color)` | Debug draw |

#### Scenarios

**A hitbox at the feet that walls push out.** This is how `Player` works: move the sprite, move the rectangle to match, let walls push the rectangle, then put the sprite back on top of it:

```csharp
rect = new CollisionRect(0, 0, 80, 30);
rect.SetOffsetExtra(0, 25);             // 25px below the sprite's center: around the feet

// Update
pos += dir * speed * dt;
rect.UpdateRect((int)MathF.Round(pos.X), (int)MathF.Round(pos.Y));
foreach (ICollider wall in walls)
    wall.ResolveAgainst(rect);          // pushes rect out of static colliders
pos = rect.Center - new Vector2(0, 25); // follow wherever the rect ended up
```

**A trigger zone** (level exit, checkpoint, cutscene start). Leave `IsStatic` off so it never pushes; check when the player enters:

```csharp
checkpoint = new CollisionRect(600, 40, 32, 16);

// Update
bool inside = player.Rect.Intersects(checkpoint);
if (inside && !wasInside)               // only the frame the player steps in
    SaveGame();
wasInside = inside;
```

**A sword hitbox in front of the player:**

```csharp
swordBox = new CollisionRect(0, 0, 20, 12);

// Update, while attacking
int side = facingLeft ? -20 : 20;
swordBox.UpdateRect((int)player.Pos.X + side, (int)player.Pos.Y);
foreach (Enemy enemy in enemies)
    if (swordBox.Intersects(enemy.Rect)) enemy.Hurt(1);
```

**Debug view.** Draw colliders half-transparent on top of the world while tuning sizes: `player.Rect.Draw(new Color(255, 0, 0, 128));`

---

## ⚪ CollisionCircle

A circular collider.

#### Constructor

**public CollisionCircle(int x, int y, int radius)**

- x / y: the center of the circle.
- radius: the radius in pixels.

#### Properties

| Property | Description |
|---|---|
| `Center` | Center position |
| `Radius` | Radius in pixels |
| `IsStatic` | See [ICollider](#icollider) |

#### Methods

| Method | Description |
|---|---|
| `Intersects(ICollider other)` | `true` if the colliders overlap |
| `Collides(Rectangle target)` | Circle vs. rectangle test |
| `Contains(Point target)` | `true` if the point is inside |
| `UpdateCenter(int x, int y)` | Moves the circle |
| `SetOffsetExtra(int x, int y)` | Extra offset, for lining the collider up with a sprite |
| `ResolveAgainst(ICollider moving)` | See [ICollider](#icollider) |
| `Draw(Color color)` | Debug draw (outline made of line segments) |

#### Scenarios

**Round obstacles** (pillars, boulders, barrels). Rectangles slide off them smoothly instead of catching on corners:

```csharp
var pillar = new CollisionCircle(300, 200, 12) { IsStatic = true };
wallList.Add(pillar);
```

**An enemy's sight or attack range:**

```csharp
sight = new CollisionCircle((int)Pos.X, (int)Pos.Y, 80);

// Update
sight.UpdateCenter((int)Pos.X, (int)Pos.Y);
if (sight.Intersects(player.Rect))
    Chase(player);
```

Combine it with [Lighting](#-lighting--pointlight) for stealth: `if (sight.Intersects(player.Rect) && !hiddenInShadow) Chase(player);`

**A pickup magnet.** A large circle around the player pulls in nearby coins:

```csharp
magnet.UpdateCenter((int)player.Pos.X, (int)player.Pos.Y);
foreach (Coin coin in coins)
    if (magnet.Contains(coin.Position.ToPoint()))
        coin.Position = Vector2.Lerp(coin.Position, player.Pos, 0.1f);
```

---

## 🎮 InputManager

Maps named **actions** to a keyboard key and/or a gamepad button, so game code asks for `"Jump"` instead of `Keys.Space`. A shared instance is available as `Globals.Input`.

**public InputManager(PlayerIndex playerIndex = PlayerIndex.One)**

#### Methods

| Method | Description |
|---|---|
| `Register(string name, Keys? key = null, Buttons? button = null)` | Creates an action |
| `RebindKey(string name, Keys newKey)` / `RebindButton(string name, Buttons newButton)` | Changes a binding |
| `ClearKey(string name)` / `ClearButton(string name)` | Removes a binding |
| `GetAction(string name)` | The `InputAction` behind a name |
| `Update()` | Reads the keyboard and gamepad. Call once at the **start** of `Update`. |
| `IsPressed(string name)` | Held down this frame |
| `JustPressed(string name)` | Went down this frame |
| `JustReleased(string name)` | Went up this frame |
| `KeyDown` / `KeyJustDown` / `KeyJustUp(Keys?)` | Raw keyboard checks |
| `ButtonDown` / `ButtonJustDown` / `ButtonJustUp(Buttons?)` | Raw gamepad checks |

```csharp
Globals.Input.Register("Jump", Keys.Space, Buttons.A);

if (Globals.Input.JustPressed("Jump"))
    player.Jump();
```

#### Scenarios

**8-direction movement with keyboard, D-pad or stick:**

```csharp
Vector2 dir = Vector2.Zero;
if (Globals.Input.IsPressed("MoveLeft"))  dir.X -= 1;
if (Globals.Input.IsPressed("MoveRight")) dir.X += 1;
if (Globals.Input.IsPressed("MoveUp"))    dir.Y -= 1;
if (Globals.Input.IsPressed("MoveDown"))  dir.Y += 1;

// No keys held: use the stick. Its Y points up, the world's points down, so flip it.
if (dir == Vector2.Zero)
    dir = new Vector2(Globals.Input.LeftStick.X, -Globals.Input.LeftStick.Y);

if (dir.Length() > 1) dir.Normalize();   // diagonals aren't faster
```

**Variable jump height:** tap for a short hop, hold for a full jump.

```csharp
if (Globals.Input.JustPressed("Jump") && onGround)
    velocity.Y = -300f;
if (Globals.Input.JustReleased("Jump") && velocity.Y < 0)
    velocity.Y *= 0.5f;                 // let go early: cut the jump short
```

**A charged attack:**

```csharp
if (Globals.Input.IsPressed("Attack"))
    charge = MathF.Min(charge + dt, 1.5f);
if (Globals.Input.JustReleased("Attack"))
{
    FireShot(power: charge);
    charge = 0;
}
```

**Rebinding a key from an options menu.** Wait for a key that was *just* pressed, so the key that opened the prompt (like Enter) isn't picked up:

```csharp
if (waitingForJumpKey)
{
    foreach (Keys key in Keyboard.GetState().GetPressedKeys())
    {
        if (Globals.Input.KeyJustDown(key))
        {
            Globals.Input.RebindKey("Jump", key);
            waitingForJumpKey = false;
            break;
        }
    }
}
```

**Two local players.** Give player 2 their own manager, gamepad and keys:

```csharp
player2Input = new InputManager(PlayerIndex.Two);
player2Input.Register("Jump", Keys.Up, Buttons.A);

// Update
player2Input.Update();
```

---

## 🛤️ Path2D

A list of waypoints something can travel along, with curves, looping, ping-pong, reversing and per-waypoint delays. It always moves at exactly `Speed`, however close together the waypoints are.

#### Constructors

**public Path2D()** — an empty path; add points with `AddWaypoint`.

**public Path2D(List&lt;Vector2&gt; waypoints, float speed = 100f)**
- waypoints: the positions to travel through, in order.
- speed: movement speed in pixels per second.

#### Fields & properties

| Member | Description |
|---|---|
| `Position` | Current position on the path. Copy it to your sprite each frame. |
| `Speed` | Pixels per second |
| `CurrentWaypointIndex` | The waypoint being moved toward. Curves are made of many small waypoints, so it counts those too. |
| `IsLooping` | Go back to the start after the last point |
| `IsPingPong` | Turn around at each end |
| `IsReversed` | Currently travelling backwards |
| `IsActive` | Set `false` to pause |
| `IsFinished` | `true` once a non-looping path has ended |

#### Methods

| Method | Description |
|---|---|
| `AddWaypoint(Vector2 point, float delay = 0f)` | Appends a point; `delay` = seconds to wait there |
| `AddCurve(Vector2 control, Vector2 end, float delay = 0f)` | Curves from the last waypoint to `end`, bent toward `control` (quadratic Bezier) |
| `AddCurve(Vector2 control1, Vector2 control2, Vector2 end, float delay = 0f)` | Same with two control points (cubic Bezier): `control1` shapes the start, `control2` the end. Allows S-shapes and circles. |
| `Update(GameTime)` | Moves along the path. Call once per frame. |
| `ReversePath()` | Reverses the waypoint order and turns around on the spot, heading back to the waypoint it just came from |
| `ReverseDirection()` | Flips the travel direction (without reordering) and turns around on the spot |
| `Reset()` | Back to the first waypoint, travelling forwards |
| `Clear()` | Removes all waypoints |

```csharp
var patrol = new Path2D();
patrol.AddWaypoint(new Vector2(100, 100));
patrol.AddWaypoint(new Vector2(300, 100), delay: 1f);
patrol.IsPingPong = true;

// Update: patrol.Update(gameTime); enemy.Position = patrol.Position;
```

#### Curves

A curve starts at the last waypoint, so add a starting point first. The path doesn't touch the control points; it's pulled toward them, like a rope bent toward a magnet.

```csharp
var swoop = new Path2D();
swoop.AddWaypoint(new Vector2(100, 400));
swoop.AddCurve(new Vector2(300, 200), new Vector2(500, 400));                      // a hump
swoop.AddCurve(new Vector2(600, 400), new Vector2(600, 600), new Vector2(800, 600)); // ease down a slope
swoop.AddWaypoint(new Vector2(1000, 600));                                          // straight again
```

**A circle** is four cubic curves, one per quarter. Put the control points `0.5523 × radius` along each quarter's tangent:

```csharp
Vector2 c = new Vector2(400, 300);  // center
float r = 100;                      // radius
float k = 0.5523f * r;

var orbit = new Path2D();
orbit.AddWaypoint(c + new Vector2(r, 0));
orbit.AddCurve(c + new Vector2(r, k),   c + new Vector2(k, r),   c + new Vector2(0, r));
orbit.AddCurve(c + new Vector2(-k, r),  c + new Vector2(-r, k),  c + new Vector2(-r, 0));
orbit.AddCurve(c + new Vector2(-r, -k), c + new Vector2(-k, -r), c + new Vector2(0, -r));
orbit.AddCurve(c + new Vector2(k, -r),  c + new Vector2(r, -k),  c + new Vector2(r, 0));
orbit.IsLooping = true;
```

This stays within 0.03% of a true circle. Pauses (`delay`) only happen at a curve's `end`, never partway along it.

#### Scenarios

**A moving platform that carries the player.** Measure how far the path moved this frame and move the rider by the same amount:

```csharp
Vector2 before = lift.Position;
lift.Update(gameTime);
Vector2 moved = lift.Position - before;

liftRect.UpdateRect((int)lift.Position.X, (int)lift.Position.Y);
if (player.Rect.Intersects(liftRect))   // the player's feet are on the platform
    player.Pos += moved;
```

**An elevator that only moves when called.** Keep it paused, start it from a button, and send it back the other way next time:

```csharp
elevator = new Path2D(new List<Vector2> { bottom, top }, speed: 60f) { IsActive = false };

// when the button is pressed
if (elevator.IsFinished)
    elevator.ReverseDirection();        // at one end: head to the other
elevator.IsActive = true;
```

**A guard patrol that stops to look around**, facing the way it walks:

```csharp
patrol = new Path2D { Speed = 40f, IsLooping = true };
patrol.AddWaypoint(new Vector2(100, 200), delay: 2f);
patrol.AddWaypoint(new Vector2(300, 200), delay: 2f);
patrol.AddWaypoint(new Vector2(300, 320), delay: 1f);

// Update
Vector2 before = patrol.Position;
patrol.Update(gameTime);
if (patrol.Position.X != before.X)
    facingLeft = patrol.Position.X < before.X;
guard.Pos = patrol.Position;
```

**A thrown grenade's arc.** One curve whose control point is above the middle of the throw:

```csharp
var arc = new Path2D { Speed = 250f };
arc.AddWaypoint(hand);
arc.AddCurve((hand + target) / 2f - new Vector2(0, 60), target);

// Update
arc.Update(gameTime);
grenade.Pos = arc.Position;
if (arc.IsFinished) Explode(target);
```

---

## TextBox

Draws a line of text **centered** on a point.

**public TextBox(SpriteFont font)**

| Member | Description |
|---|---|
| `Text` | The text to show |
| `Font` | The `SpriteFont` used |
| `rotation` | Rotation in radians |
| `scale` | Scale (prefer a bigger font size over scaling up — see [Canvas](#-canvas)) |
| `Draw(int x, int y, Color color)` | Draws the text centered at `(x, y)` |

#### Scenarios

**A score at the top of the screen**, on the UI layer:

```csharp
scoreText = new TextBox(font);

// Update
scoreText.Text = $"Score: {score}";

// Draw, between canvas.BeginUI and canvas.EndUI
scoreText.Draw(960, 60, Color.White);    // centered horizontally in 1920×1080
```

**A pulsing "PAUSED" title:**

```csharp
pausedText.Text = "PAUSED";
pausedText.scale = 1f + 0.05f * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 4f);
pausedText.Draw(960, 540, PicoPallete.yellow);
```

---

## TextInput

A clickable text field: click it to focus, type, press Enter to submit. Uses MonoGame's `Window.TextInput`, so keyboard layouts and held keys work properly.

**public TextInput(Game game, SpriteFont font, CollisionRect collider, string placeholder = "Type here...")**
- game: your `Game` (used for the window and graphics device).
- font: the font to type with.
- collider: the clickable area of the field.
- placeholder: text shown while the field is empty.

| Member | Description |
|---|---|
| `Text` | Current text |
| `Placeholder` | Text shown while empty |
| `MaxLength` | Maximum characters (default 256) |
| `IsFocused` | Whether it is receiving typing |
| `Bounds` / `Collider` | The field's area |
| `ScreenToLocal` | Converts mouse coordinates to the space the field is drawn in. Set it to `canvas.ScreenToUI` when drawing inside the UI layer. |
| `Submitted` | Event raised with the text when Enter is pressed |
| `TextChanged` | Event raised whenever the text changes |
| `Focus()` / `Blur()` | Gives / removes focus |
| `Update(GameTime)` | Handles clicking and typing. Call once per frame. |
| `Draw()` | Draws the field |
| `Dispose()` | Unhooks the window events. Call when the field is removed. |

```csharp
nameInput = new TextInput(this, font, new CollisionRect(960, 540, 600, 60), "Your name");
nameInput.ScreenToLocal = canvas.ScreenToUI;
nameInput.Submitted += name => playerName = name;
```

#### Scenarios

**A name entry screen, start to finish:**

```csharp
// LoadContent
nameInput = new TextInput(this, font, new CollisionRect(960, 540, 600, 60), "Your name");
nameInput.ScreenToLocal = canvas.ScreenToUI;   // it's drawn on the UI layer
nameInput.MaxLength = 12;
nameInput.Submitted += name => { playerName = name; StartGame(); };
nameInput.Focus();                             // ready to type right away

// Update
nameInput.Update(gameTime);

// Draw, between canvas.BeginUI and canvas.EndUI
nameInput.Draw();

// leaving the screen
nameInput.Dispose();
```

**Don't move the player while typing.** WASD would otherwise do both:

```csharp
if (!chatInput.IsFocused)
    player.Update(gameTime, walls);
```

**A chat box or debug console** that clears after each line:

```csharp
console.Submitted += line =>
{
    RunCommand(line);
    console.Text = "";
};
```

---

## 🎨 PicoPallete

The 16-color [PICO-8](https://www.lexaloffle.com/pico-8.php) palette as ready-made `Color`s:

`black`, `dark_blue`, `dark_purple`, `dark_green`, `brown`, `dark_grey`, `light_grey`, `white`, `red`, `orange`, `yellow`, `green`, `blue`, `lavender`, `pink`, `light_peach`

```csharp
GraphicsDevice.Clear(PicoPallete.dark_blue);
```
