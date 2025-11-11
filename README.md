# NodeTesting

a list of models created to make game development easier.

## setup
```csharp
// initialize the nodetesting models
using NodeTesting.models;

//put these in LoadContent()
Globals.Content = Content;
Globals.spriteBatch = _spriteBatch;
Globals.graphics = _graphics;
```

## Features

### Sprite
the sprite model allows you to create and manipulate sprites easily.

**public Sprite(string texture, Vector2 pos)**
- texture: the path to the texture in the content folder
- pos: the position of the sprite

**public void Draw(Color color)**
- color: the color to draw the sprite with (usually Color.White)

### 🟩 CollisionRect

The CollisionRect class represents a rectangular collider used for detecting collisions and overlaps with other colliders in the game world.

#### Constructor

**public CollisionRect(int x, int y, int width, int height)**

Creates a new rectangular collision box centered at the given position.

- x: The x-coordinate of the rectangle’s center.
- y: The y-coordinate of the rectangle’s center.
- width: The width of the rectangle.
- height: The height of the rectangle.

#### Properties

**public Rectangle Rect**

Gets or sets the internal Rectangle representing the collider’s bounds.

**public Vector2 Pos**

Returns the current top-left position of the rectangle.

#### Methods

**public bool Intersects(ICollider other)**

Checks if this rectangle intersects with another collider (rectangle, circle, etc.).

other – Another collider object implementing ICollider.

Returns: true if the colliders overlap; otherwise false.

**public bool Contains(Point target)**

Checks if a given point lies inside the rectangle.

target – The point to test.

Returns: true if the point is inside the rectangle.

**public void UpdateRect(int x, int y)**

Updates the position of the rectangle, keeping it centered around the given coordinates.

x – The new x-coordinate of the rectangle’s center.

y – The new y-coordinate of the rectangle’s center.

**public void SetOffsetExtra(int x, int y)**

Applies an additional offset to the rectangle’s position. Useful for aligning the collider with a sprite.

x – Horizontal offset in pixels.

y – Vertical offset in pixels.

**public void Draw(Color color)**

Draws the rectangle collider outline (for debugging or visualization).

color – The color to draw the collider with.

### ⚪ CollisionCircle

The CollisionCircle class represents a circular collider used for detecting collisions with rectangles, circles, and other colliders.

#### Constructor

**public CollisionCircle(int x, int y, int radius)**

Creates a new circular collider centered at the given coordinates.

- x: The x-coordinate of the circle’s center.
- y: The y-coordinate of the circle’s center.
- radius: The radius of the circle in pixels.

#### Properties

**public Vector2 Center**

Gets or sets the position of the circle’s center.

**public int Radius**

Gets or sets the radius of the circle.

#### Methods

**public bool Intersects(ICollider other)**

Checks if this circle intersects with another collider (circle, rectangle, etc.).

other – Another collider object implementing ICollider.

Returns: true if the colliders overlap; otherwise false.

**public bool Collides(Rectangle target)**

Performs a circle–rectangle collision test.

target – The rectangle to check against.

Returns: true if the circle overlaps the rectangle.

**public bool Contains(Point target)**

Checks if a given point lies within the circle’s radius.

target – The point to test.

Returns: true if the point is inside the circle.

**public void Draw(Color color)**

Draws the circle collider outline using line segments (for debugging or visualization).

color – The color to draw the collider with.

