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
        Color snakeColor = Color.Green;

        // Timing
        double moveTimer = 0;
        double moveInterval = 0.12; // seconds between moves

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
            obstacles = new HashSet<Point>();
            pendingGrowth = 0;
            PlaceApple();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
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

            if (kb.IsKeyDown(Keys.Escape))
                Exit();

            // WASD controls (no reverse)
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

            // Wrap-around
            if (newHead.X < 0) newHead.X = gridWidth - 1;
            if (newHead.X >= gridWidth) newHead.X = 0;
            if (newHead.Y < 0) newHead.Y = gridHeight - 1;
            if (newHead.Y >= gridHeight) newHead.Y = 0;

            // Collision with self or obstacles
            if (snake.Contains(newHead) || obstacles.Contains(newHead))
            {
                ResetGame();
                return;
            }

            snake.Insert(0, newHead);

            // Ate apple?
            if (newHead == apple)
            {
                pendingGrowth += 1;
                SpawnObstacle(); // **always spawn exactly 1 obstacle**
                PlaceApple();
            }

            if (pendingGrowth > 0)
            {
                pendingGrowth--;
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }
        }

        void PlaceApple()
        {
            Point p;
            int tries = 0;
            do
            {
                p = new Point(rnd.Next(0, gridWidth), rnd.Next(0, gridHeight));
                tries++;
                if (tries > 1000) break;
            } while (snake.Contains(p) || obstacles.Contains(p));
            apple = p;
        }

        void SpawnObstacle()
        {
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

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            // draw apple
            DrawCell(apple, Color.Red);

            // draw obstacles
            foreach (var o in obstacles)
                DrawCell(o, Color.Gray);

            // draw snake
            for (int i = 0; i < snake.Count; i++)
            {
                var seg = snake[i];
                DrawCell(seg, snakeColor);
            }

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
