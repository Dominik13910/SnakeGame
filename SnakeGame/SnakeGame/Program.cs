using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var tickRate = TimeSpan.FromMilliseconds(100);
        var snakeGame = new SnakeGame();

        using (var cts = new CancellationTokenSource())
        {
            async Task MonitorKeyPresses()
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true).Key;
                        snakeGame.OnKeyPress(key);
                    }

                    await Task.Delay(10);
                }
            }

            var monitorKeyPresses = MonitorKeyPresses();

            do
            {
                snakeGame.OnGameTick();
                snakeGame.Render();
                await Task.Delay(tickRate);
            } while (!snakeGame.GameOver);

            // Allow time for the user to weep before the application exits.
            for (var i = 0; i < 3; i++)
            {
                Console.Clear();
                await Task.Delay(500);
                snakeGame.Render();
                await Task.Delay(500);
            }

            cts.Cancel();
            await monitorKeyPresses;
        }
    }
}

enum Direction
{
    Up,
    Down,
    Left,
    Right
}

interface IRenderable
{
    void Render();
}

readonly struct Position
{
    public Position(int top, int left)
    {
        Top = top;
        Left = left;
    }
    public int Top { get; }
    public int Left { get; }

    public Position RightBy(int n) => new Position(Top, Left + n);
    public Position DownBy(int n) => new Position(Top + n, Left);
}

class Mause : IRenderable
{
    public Mause(Position position)
    {
        Position = position;
    }

    public Position Position { get; }

    public void Render()
    {
        Console.SetCursorPosition(Position.Left, Position.Top);
        Console.Write("M");
    }
}

class Snake : IRenderable
{
    private List<Position> _body;
    private int _growthSpurtsRemaining;

    public Snake(Position spawnLocation, int initialSize = 1)
    {
        _body = new List<Position> { spawnLocation };
        _growthSpurtsRemaining = Math.Max(0, initialSize - 1);
        Dead = false;
    }

    public bool Dead { get; set; }
    public Position Head => _body.First();
    public IEnumerable<Position> Body => _body.Skip(1);

    public void Move(Direction direction)
    {
        if (Dead) throw new InvalidOperationException();

        Position newHead;

        switch (direction)
        {
            case Direction.Up:
                newHead = Head.DownBy(-1);
                break;

            case Direction.Left:
                newHead = Head.RightBy(-1);
                break;

            case Direction.Down:
                newHead = Head.DownBy(1);
                break;

            case Direction.Right:
                newHead = Head.RightBy(1);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_body.Contains(newHead) || !PositionIsValid(newHead))
        {
            Dead = true;
            return;
        }

        _body.Insert(0, newHead);

        if (_growthSpurtsRemaining > 0)
        {
            _growthSpurtsRemaining--;
        }
        else
        {
            _body.RemoveAt(_body.Count - 1);
        }
    }

    public void Grow()
    {
        if (Dead) throw new InvalidOperationException();

        _growthSpurtsRemaining++;
    }

    public void Render()
    {
        Console.SetCursorPosition(Head.Left, Head.Top);
        Console.Write("1");

        foreach (var position in Body)
        {
            Console.SetCursorPosition(position.Left, position.Top);
            Console.Write("■");
        }
    }

    public void Render2()
    {
        Console.SetCursorPosition(Head.Left, Head.Top);
        Console.Write("2");

        foreach (var position in Body)
        {
            Console.SetCursorPosition(position.Left, position.Top);
            Console.Write("■");
        }
    }

    private static bool PositionIsValid(Position position) =>
        position.Top >= 0 && position.Left >= 0;
}

class SnakeGame : IRenderable
{
    private static readonly Position Origin = new Position(0, 0);

    private Direction _currentDirection1;
    private Direction _nextDirection1;
    private Snake _snake1;

    private Direction _currentDirection2;
    private Direction _nextDirection2;
    private Snake _snake2;

    private Mause _apple;

    public SnakeGame()
    {
        _snake1 = new Snake(Origin, initialSize: 5);
        _snake2 = new Snake(new Position(10, 10), initialSize: 5); // Starting position for the second snake
        _apple = CreateApple();
        _currentDirection1 = Direction.Right;
        _nextDirection1 = Direction.Right;

        _currentDirection2 = Direction.Right;
        _nextDirection2 = Direction.Right;
    }

    public bool GameOver => _snake1.Dead || _snake2.Dead;

    Direction newDirection1 = Direction.Right;
    Direction newDirection2 = Direction.Right;
    public void OnKeyPress(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.W:
                newDirection1 = Direction.Up;
                break;

            case ConsoleKey.A:
                newDirection1 = Direction.Left;
                break;

            case ConsoleKey.S:
                newDirection1 = Direction.Down;
                break;

            case ConsoleKey.D:
                newDirection1 = Direction.Right;
                break;

            case ConsoleKey.UpArrow:
                newDirection2 = Direction.Up;
                break;

            case ConsoleKey.LeftArrow:
                newDirection2 = Direction.Left;
                break;

            case ConsoleKey.DownArrow:
                newDirection2 = Direction.Down;
                break;

            case ConsoleKey.RightArrow:
                newDirection2 = Direction.Right;
                break;

            default:
                return;
        }

        if (newDirection1 != OppositeDirectionTo(_currentDirection1))
        {
            _nextDirection1 = newDirection1;
        }

        if (newDirection2 != OppositeDirectionTo(_currentDirection2))
        {
            _nextDirection2 = newDirection2;
        }
    }

    public void OnGameTick()
    {
        if (GameOver) throw new InvalidOperationException();

        _currentDirection1 = _nextDirection1;
        _snake1.Move(_currentDirection1);

        _currentDirection2 = _nextDirection2;
        _snake2.Move(_currentDirection2);

        if (_snake1.Head.Equals(_snake2.Head))
        {
            _snake1.Dead = true;
            _snake2.Dead = true;
        }

        if (_snake1.Head.Top == -1 || _snake1.Head.Top == Console.WindowHeight - 1 ||
            _snake1.Head.Left == -1 || _snake1.Head.Left == Console.WindowWidth - 1)
        {
            _snake1.Dead = true;
        }

        if (_snake2.Head.Top == 0 || _snake2.Head.Top == Console.WindowHeight - 1 ||
            _snake2.Head.Left == 0 || _snake2.Head.Left == Console.WindowWidth - 1)
        {
            _snake2.Dead = true;
        }

        if (_snake1.Head.Equals(_apple.Position))
        {
            _snake1.Grow();
            _apple = CreateApple();
        }

        if (_snake2.Head.Equals(_apple.Position))
        {
            _snake2.Grow();
            _apple = CreateApple();
        }

        if (_snake1.Body.Any(bodyPart => bodyPart.Equals(_snake1.Head)) ||
            _snake2.Body.Any(bodyPart => bodyPart.Equals(_snake2.Head)) ||
            _snake1.Body.Any(bodyPart => bodyPart.Equals(_snake2.Head)) ||
            _snake2.Body.Any(bodyPart => bodyPart.Equals(_snake1.Head))
            )
        {
            _snake1.Dead = true;
            _snake2.Dead = true;
        }
    }

    public void Render()
    {
        Console.Clear();
        _snake1.Render();
        _snake2.Render2();
        _apple.Render();
        Console.SetCursorPosition(0, 0);
    }

    private static Direction OppositeDirectionTo(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
            case Direction.Down: return Direction.Up;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private static Mause CreateApple()
    {
        const int numberOfRows = 20;
        const int numberOfColumns = 20;

        var random = new Random();
        var top = random.Next(0, numberOfRows + 1);
        var left = random.Next(0, numberOfColumns + 1);
        var position = new Position(top, left);
        var Mause = new Mause(position);

        return Mause;
    }
}
