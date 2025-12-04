using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakeMonoGame
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // Grid settings
        readonly int cellSize = 20;
        int gridWidth = 30;  // columns
        int gridHeight = 24; // rows

        // Game state
        List<Point> snake;
        Point direction = new Point(1, 0); // starts moving right
        Point apple;
        HashSet<Point> obstacles;
        Random rnd = new Random();
        Color snakeColor;

        // Timing
        double moveTimer = 0;
        double moveInterval = 0.12; // seconds between moves (speed)

        // Drawing
        Texture2D pixel;

        // Input helper
        KeyboardState prevKb;

        // Score / growth
        int pendingGrowth = 0;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Set window size according to grid
            graphics.PreferredBackBufferWidth = gridWidth * cellSize;
            graphics.PreferredBackBufferHeight = gridHeight * cellSize;
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            ResetGame();
        }

        void ResetGame()
        {
            snake = new List<Point>();
            int startX = gridWidth / 2;
            int startY = gridHeight / 2;
            snake.Add(new Point(startX, startY));
            snake.Add(new Point(startX - 1, startY));
            snake.Add(new Point(startX - 2, startY));

            direction = new Point(1, 0);
            snakeColor = RandomColor();
            obstacles = new HashSet<Point>();
            pendingGrowth = 0;
            moveInterval = 0.12;
            PlaceApple();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            // 1x1 white pixel texture
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        protected override void UnloadContent()
        {
            pixel.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            var kb = Keyboard.GetState();

            // Exit
            if (kb.IsKeyDown(Keys.Escape))
                Exit();

            // Input: WASD (no immediate reverse)
            if (WasKeyPressed(kb, Keys.W) && direction != new Point(0, 1))
                direction = new Point(0, -1);
            else if (WasKeyPressed(kb, Keys.S) && direction != new Point(0, -1))
                direction = new Point(0, 1);
            else if (WasKeyPressed(kb, Keys.A) && direction != new Point(1, 0))
                direction = new Point(-1, 0);
            else if (WasKeyPressed(kb, Keys.D) && direction != new Point(-1, 0))
                direction = new Point(1, 0);

            prevKb = kb;

            // Movement timing
            moveTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (moveTimer >= moveInterval)
            {
                MoveSnake();
                moveTimer = 0;
            }

            base.Update(gameTime);
        }

        bool WasKeyPressed(KeyboardState kb, Keys k) => kb.IsKeyDown(k) && !prevKb.IsKeyDown(k);

        void MoveSnake()
        {
            Point head = snake[0];
            Point newHead = new Point(head.X + direction.X, head.Y + direction.Y);

            // Wrap-around (no walls)
            if (newHead.X < 0) newHead.X = gridWidth - 1;
            if (newHead.X >= gridWidth) newHead.X = 0;
            if (newHead.Y < 0) newHead.Y = gridHeight - 1;
            if (newHead.Y >= gridHeight) newHead.Y = 0;

            // Check collision with self or obstacles -> if collides with self or obstacle, restart game
            bool hitSelf = snake.Skip(0).Any(s => s == newHead);
            if (hitSelf || obstacles.Contains(newHead))
            {
                // simple reset on collision
                ResetGame();
                return;
            }

            // Insert new head
            snake.Insert(0, newHead);

            // Ate apple?
            if (newHead == apple)
            {
                pendingGrowth += 1; // grow by 1 segment
                snakeColor = RandomColor(); // change color after each apple

                // After each apple: 30% chance to spawn 1-3 obstacles
                if (rnd.NextDouble() < 0.3)
                {
                    int count = rnd.Next(1, 4);
                    for (int i = 0; i < count; i++)
                        SpawnObstacle();
                }

                // Speed up slightly every few apples (optional)
                moveInterval = Math.Max(0.04, moveInterval - 0.005);

                PlaceApple();
            }

            if (pendingGrowth > 0)
            {
                pendingGrowth--;
                // don't remove tail (growth)
            }
            else
            {
                // remove tail
                snake.RemoveAt(snake.Count - 1);
            }
        }

        void PlaceApple()
        {
            // place apple on empty cell (not on snake nor obstacle)
            Point p;
            int tries = 0;
            do
            {
                p = new Point(rnd.Next(0, gridWidth), rnd.Next(0, gridHeight));
                tries++;
                if (tries > 1000) break; // safety
            } while (snake.Contains(p) || obstacles.Contains(p));
            apple = p;
        }

        void SpawnObstacle()
        {
            // spawn obstacle on a free cell (not on apple or snake or existing obstacles)
            Point p;
            int tries = 0;
            do
            {
                p = new Point(rnd.Next(0, gridWidth), rnd.Next(0, gridHeight));
                tries++;
                if (tries > 1000) return;
            } while (snake.Contains(p) || obstacles.Contains(p) || p == apple);

            obstacles.Add(p);
        }

        Color RandomColor()
        {
            // avoid too dark or too bright extremes
            int r = rnd.Next(60, 200);
            int g = rnd.Next(60, 200);
            int b = rnd.Next(60, 200);
            return new Color(r, g, b);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            // draw apple (red)
            DrawCell(apple, Color.Red);

            // draw obstacles (grey)
            foreach (var o in obstacles)
                DrawCell(o, Color.Gray);

            // draw snake segments
            for (int i = 0; i < snake.Count; i++)
            {
                var seg = snake[i];
                // head a little brighter
                if (i == 0)
                    DrawCell(seg, snakeColor);
                else
                    DrawCell(seg, snakeColor * 0.85f); // slightly dimmed
            }

            // Simple HUD: score
            string scoreText = $"Score: {snake.Count - 3}";
            // We don't have SpriteFont in this simple example; instead draw a tiny rectangle as header
            // (If you want text, add a SpriteFont asset and draw it)
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void DrawCell(Point cell, Color color)
        {
            Rectangle rect = new Rectangle(cell.X * cellSize, cell.Y * cellSize, cellSize, cellSize);
            spriteBatch.Draw(pixel, rect, color);
        }
    }
}
