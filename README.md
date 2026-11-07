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
  - [Lighting & PointLight](#-lighting--pointlight)
- Sprites
  - [Sprite](#sprite)
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

Lights with no occluder within their radius cost the same as before. Each light that *does* have one in range needs an extra pass, so prefer `CastsShadows = false` on lots of small lights.

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

## 🏃 SpriteAnimation

Frame-based animation from a **single-row strip**: one texture with equally sized frames laid out left to right. Use one strip per animation (e.g. `hero_idle`, `hero_walk`).

#### Constructor

**public SpriteAnimation(string texturePath, int frames, int fps)**
- texturePath: the path to the texture in the Content folder.
- frames: how many frames are in the strip. Frame width = texture width ÷ frames.
- fps: playback speed in frames per second.

#### Fields

`Position`, `Color`, `Scale`, `Rotation`, `Origin` and `SpriteEffect` control how it is drawn. `FrameIndex`, `IsFinished` and `IsReversed` hold the playback state.

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
