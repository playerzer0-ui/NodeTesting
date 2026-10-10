# NodeTesting

A small 2D game engine built on top of **MonoGame (DesktopGL)**: a set of models that make game development easier — rendering at a fixed resolution, sprites and animations, tilemaps, colliders, a camera, input mapping, paths and text.

All models live in the `NodeTesting.models` namespace.

---

## Contents

- [Setup](#setup)
- [The render loop](#the-render-loop)
- Rendering
  - [Canvas](#-canvas)
  - [Camera & CameraFocus](#-camera--camerafocus)
- Sprites
  - [Sprite](#sprite)
  - [SpriteSheet](#spritesheet)
  - [SpriteAnimation & SpriteAnimationMultiRow](#-spriteanimation--spriteanimationmultirow)
- Maps
  - [TileMap](#-tilemap)
  - [CollisionMap](#-collisionmap)
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
| `Globals.graphics` | `GraphicsDeviceManager` | Used by `Camera` and `Canvas.SetResolution` |
| `Globals.Input` | `InputManager` | A ready-made input manager (see [InputManager](#-inputmanager)) |

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
    tilemap.Draw();
    player.Draw(Color.White);
    _spriteBatch.End();

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

---

## 🎥 Camera & CameraFocus

A 2D camera with pan, zoom and rotation, plus smooth "focus" transitions.

#### Constructor

**public Camera()** — starts at `(0, 0)`, zoom `1`, with its origin at the center of the viewport.

#### Properties

| Property | Description |
|---|---|
| `Position` | World position the camera looks at |
| `Zoom` | Zoom level (`1` = normal) |
| `Rotation` | Rotation in radians |
| `Origin` | Screen point the camera is centered on |
| `IsFocusing` | `true` while moving toward or locked on a focus |

#### Methods

| Method | Description |
|---|---|
| `Transform()` | The view matrix. Pass it to `SpriteBatch.Begin(transformMatrix: ...)`. |
| `MoveTo(CameraFocus focus)` | Starts moving toward a focus point. |
| `Update(GameTime)` | Drives the focus transition. Call once per frame. |
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

---

## Sprite

A single texture drawn at a position.

**public Sprite(string texture, Vector2 pos)**
- texture: the path to the texture in the Content folder.
- pos: the position of the sprite.

**public void Draw(Color color)**
- color: the color to draw the sprite with (usually `Color.White`).

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
| `DrawFrame(int index, Vector2 position)` | Draws one frame (0-based) |
| `DrawFrame(int index, Vector2 position, float scale)` | …with a scale |
| `DrawFrame(int index, Vector2 position, float scale, Color color)` | …with a scale and tint |
| `DrawFrame(int index, Vector2 position, float rotation, float scale, SpriteEffects effects = None)` | …with full control |

---

## 🏃 SpriteAnimation & SpriteAnimationMultiRow

Frame-based animation from a sprite sheet. `SpriteAnimationMultiRow` supports a sheet where **each row is a different state** (idle, walk, jump…). `SpriteAnimation` is the single-row shortcut.

#### Constructors

**public SpriteAnimation(string texturePath, int frames, int fps)**
- A one-row strip with `frames` frames, playing at `fps`.

**public SpriteAnimationMultiRow(string texturePath, int frameWidth, int frameHeight, int columns, int rows)**
- frameWidth / frameHeight: size of one frame in pixels.
- columns / rows: grid size of the sheet.

**public SpriteAnimationMultiRow(string texturePath, int columns, int rows)**
- Same, but the frame size is worked out from the texture size.

#### Fields

`Position`, `Color`, `Scale`, `Rotation`, `Origin` and `SpriteEffect` control how it is drawn.

#### Methods

| Method | Description |
|---|---|
| `AddState(string name, int rowIndex, int frameCount, int fps = 8, bool looping = true, int startFrame = 0)` | Adds a named state using a row of the sheet |
| `AddState(string name, int rowIndex, int fps = 8, bool looping = true)` | Same, using every column in the row |
| `Play(string stateName, bool restart = true)` | Switches to a state |
| `Update(GameTime)` | Advances the animation (loops) |
| `UpdateOnce(GameTime)` | Advances without looping (stops on the last frame) |
| `Draw()` | Draws the current frame |
| `DrawFrame(int index)` | Draws a specific frame |
| `SetFrame(int frameIndex)` | Jumps to a frame |
| `Reset()` | Back to the start of the current state |
| `ToggleReverse()` | Plays backwards / forwards |
| `GetCurrentState()` | Name of the playing state |
| `IsAnimationFinished()` | `true` when a non-looping state has ended |
| `GetCurrentFrameRect()`, `GetFrameRect(int index)` | Source rectangles in the texture |
| `GetRowCount()`, `GetColumnCount()`, `GetTotalFrames()` | Sheet info |

```csharp
var hero = new SpriteAnimationMultiRow("hero", 16, 16, 4, 2);
hero.AddState("idle", 0);
hero.AddState("walk", 1, fps: 12);
hero.Play("walk");

// Update: hero.Update(gameTime);
// Draw:   hero.Draw();
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
| `Draw()` | Draws the map |

```csharp
tilemap = new TileMap("tilemap", 16, 16, "tilemap.csv");
tilemap.RegisterAnimation(20, new[] { 20, 21, 22, 23 }, 0.15f); // animated water
```

---

## 🧱 CollisionMap

A map used for collision instead of visuals. In its CSV, **`0` means solid**. Each solid tile becomes a static `CollisionRect`.

**public CollisionMap(string texturePath, int tileWidth, int tileHeight, string csvPath)**

| Method | Description |
|---|---|
| `CheckCollision(CollisionRect collider)` | `true` if the collider touches a solid tile |
| `CheckCollision(Rectangle rectangle)` | Same, for a plain `Rectangle` |
| `GetCollisionRects()` | All solid tiles as `CollisionRect`s |
| `GetIntersectingTiles(Rectangle target)` | Grid positions of the tiles a rectangle overlaps |
| `ResolveCollisionHorizontal(ref Rectangle rect, float velocityX)` | Pushes `rect` out of walls on the X axis |
| `ResolveCollisionVertical(ref Rectangle rect, float velocityY)` | Pushes `rect` out of walls on the Y axis |
| `Draw()` | Draws the solid tiles (debugging) |
| `DrawAllCollisionRects(Color color)` | Draws every collision rectangle (debugging) |

> Move and resolve one axis at a time: apply X velocity → `ResolveCollisionHorizontal`, then Y velocity → `ResolveCollisionVertical`.

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

---

## 🛤️ Path2D

A list of waypoints something can travel along, with looping, ping-pong, reversing and per-waypoint delays.

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
| `CurrentWaypointIndex` | The waypoint being moved toward |
| `IsLooping` | Go back to the start after the last point |
| `IsPingPong` | Turn around at each end |
| `IsReversed` | Currently travelling backwards |
| `IsActive` | Set `false` to pause |
| `IsFinished` | `true` once a non-looping path has ended |

#### Methods

| Method | Description |
|---|---|
| `AddWaypoint(Vector2 point, float delay = 0f)` | Appends a point; `delay` = seconds to wait there |
| `Update(GameTime)` | Moves along the path. Call once per frame. |
| `ReversePath()` | Reverses the waypoint order without a visible jump |
| `ReverseDirection()` | Flips the travel direction without reordering |
| `Reset()` | Back to the start |
| `Clear()` | Removes all waypoints |

```csharp
var patrol = new Path2D();
patrol.AddWaypoint(new Vector2(100, 100));
patrol.AddWaypoint(new Vector2(300, 100), delay: 1f);
patrol.IsPingPong = true;

// Update: patrol.Update(gameTime); enemy.Position = patrol.Position;
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

---

## 🎨 PicoPallete

The 16-color [PICO-8](https://www.lexaloffle.com/pico-8.php) palette as ready-made `Color`s:

`black`, `dark_blue`, `dark_purple`, `dark_green`, `brown`, `dark_grey`, `light_grey`, `white`, `red`, `orange`, `yellow`, `green`, `blue`, `lavender`, `pink`, `light_peach`

```csharp
GraphicsDevice.Clear(PicoPallete.dark_blue);
```
