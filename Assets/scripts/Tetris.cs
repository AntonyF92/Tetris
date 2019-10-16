using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

public abstract class BaseShape 
{
    protected int ShapeColor; //color which will be used to draw on game grid
    protected int[,] ShapeGrid; //matrix m*n to store shape itself

    public int Color => ShapeColor;

    protected BaseShape(int color)
    {
        ShapeColor = color;
    }

    private void RotateImpl(out int[,] newRotation)
    {
        newRotation = new int[ShapeGrid.GetLength(1), ShapeGrid.GetLength(0)];
        for (int x = 0; x < ShapeGrid.GetLength(0); x++)
        {
            for (int y = 0; y < ShapeGrid.GetLength(1); y++)
            {
                newRotation[newRotation.GetLength(0) - y - 1, x] = ShapeGrid[x, y];
            }
        }
    }
    
    public virtual void Rotate()
    {
        int[,] tmp;
        RotateImpl(out tmp);
        ShapeGrid = tmp;
    }

    public virtual bool RotateInPos(in Point pos, ref int[,] gameGrid)
    {
        int[,] tmp;
        RotateImpl(out tmp);
        CleanFromShape(in pos, ref gameGrid);
        bool result = ActionAllowed(in pos, in gameGrid, in tmp);
        if (result)
        {
            ShapeGrid = tmp;
        }
        DrawShapeOnGrid(in pos, ref gameGrid);

        return result;
    }

    private void CleanFromShape(in Point pos, ref int[,] gameGrid)
    {
        for (int x = 0; x < ShapeGrid.GetLength(0); x++)
        {
            for (int y = 0; y < ShapeGrid.GetLength(1); y++)
            {
                int gameGridX = pos.X + x, gameGridY = pos.Y + y;
                if (gameGridX >= gameGrid.GetLength(0) || gameGridY >= gameGrid.GetLength(1))
                    continue;
                if (gameGrid[gameGridX, gameGridY] > 0 && gameGrid[gameGridX, gameGridY] == ShapeGrid[x, y])
                    gameGrid[gameGridX, gameGridY] = 0;
            }
        }
    }

    public void DrawShapeOnGrid(in Point pos, ref int[,] gameGrid)
    {
        for (int x = 0; x < ShapeGrid.GetLength(0); x++)
        {
            for (int y = 0; y < ShapeGrid.GetLength(1); y++)
            {
                int gameGridX = pos.X + x, gameGridY = pos.Y + y;
                if (gameGridX >= gameGrid.GetLength(0) || gameGridY >= gameGrid.GetLength(1))
                    continue;
                if (gameGrid[gameGridX, gameGridY] == 0 && ShapeGrid[x, y] > 0)
                    gameGrid[gameGridX, gameGridY] = ShapeColor;
            }
        }
    }

    public bool Move(in Point oldPos, in Point newPos, ref int[,] gameGrid)
    {
        CleanFromShape(in oldPos, ref gameGrid);
        bool result = ActionAllowed(in newPos, in gameGrid, in ShapeGrid);
        if (result)
        {
            DrawShapeOnGrid(in newPos, ref gameGrid);
        }
        else
        {
            DrawShapeOnGrid(in oldPos, ref gameGrid);
        }

        return result;
    }

    private bool ActionAllowed(in Point newPos, in int[,] gameGrid, in int[,] shapeGrid)
    {
        if (newPos.X >= gameGrid.GetLength(0) || newPos.Y >= gameGrid.GetLength(1) || newPos.X < 0 || newPos.Y < 0)
            return false;
        bool result = true;
        for (int x = newPos.X, i = 0; i < shapeGrid.GetLength(0); i++)
        {
            for (int y = newPos.Y, j = 0; j < shapeGrid.GetLength(1); j++)
            {
                if (shapeGrid[i, j] > 0 && (x + i >= gameGrid.GetLength(0) || y + j >= gameGrid.GetLength(1)))
                {
                    result = false;
                    break;
                }
                if (shapeGrid[i, j] > 0 && gameGrid[x + i, y + j] > 0)
                {
                    result = false;
                    break;
                }
            }

            if (!result)
                break;
        }

        return result;
    }

    public bool CanBeSpawned(in Point pos, in int[,] gameGrid)
    {
        return ActionAllowed(in pos, in gameGrid, in ShapeGrid);
    }

    public int Width => ShapeGrid.GetLength(0);

    public int Height => ShapeGrid.GetLength(1);
}

public class OBlock : BaseShape
{
    public OBlock(int color) : base(color)
    {
        ShapeGrid = new int[,]
        {
            {ShapeColor, ShapeColor},
            {ShapeColor, ShapeColor}
        };
    }

    public override void Rotate()
    {
        //do nothing
    }

    public override bool RotateInPos(in Point pos, ref int[,] gameGrid)
    {
        return false; //do nothing, so we shouldn't refresh our game grid
    }
}

public class TBlock : BaseShape
{
    public TBlock(int color) : base(color)
    {
        ShapeGrid = new int[,]
        {
            {    0,      ShapeColor,     0},
            {ShapeColor, ShapeColor, ShapeColor}
        };
    }
}

public class IBlock : BaseShape
{
    public IBlock(int color) : base(color)
    {
        ShapeGrid = new int[,]
        {
            {ShapeColor, ShapeColor, ShapeColor, ShapeColor}
        };
    }
}

public class LBlock : BaseShape
{
    public LBlock(int color) : base(color)
    {
        ShapeGrid = new int[,]
        {
            {ShapeColor,     0},
            {ShapeColor,     0},
            {ShapeColor, ShapeColor}
        };
    }
}

public class JBlock : BaseShape
{
    public JBlock(int color) : base(color)
    {
        ShapeGrid = new int[,]
        {
            {    0,      ShapeColor},
            {    0,      ShapeColor},
            {ShapeColor, ShapeColor}
        };
    }
}

public class SBlock : BaseShape
{
    public SBlock(int color) : base(color)
    {
        ShapeGrid = new int[,]
        {
            {    0,      ShapeColor, ShapeColor},
            {ShapeColor, ShapeColor,     0}
        };
    }
}

public class ZBlock : BaseShape
{
    public ZBlock(int color) : base(color)
    {
        ShapeGrid = new int[,]
        {
            {ShapeColor, ShapeColor,     0},
            {    0,      ShapeColor, ShapeColor}
        };
    }
}

public class Tetris : MonoBehaviour
{
    public GameObject BlockPfb;
    public GameObject GameOverPanel;
    private Text _gameSpeedText;
    private Text _scoresText;
    private Text _linesRemovedText;
    private GameObject _pauseButton;

    Dictionary<int, Color> _gridColors = new Dictionary<int, Color>()
    {
        {0, new Color(.3f, .3f, .3f, .5f)},
        {1, Color.blue},
        {2, Color.cyan},
        {3, Color.green},
        {4, Color.red},
        {5, Color.yellow},
        {6, new Color(0.5399f, 0f, 0.7735f, 1f)},
        {7, new Color(0.7924f, 0.4469f, 0f, 1f)}
    };

    int[,] _gameGrid = new int[10, 20];
    GameObject[,] _gameField;
    private SpriteRenderer[,] _spriteField;
    Point _gameFieldPos = new Point(-13, 10);
    Point _startPosition;

    private Queue<int> _usedElements = new Queue<int>(); //to prevent situations like this: L->S->L

    private int[,] _nextShapeGrid = new int[6, 6];
    private GameObject[,] _nextShapeField = new GameObject[6,6];
    private Point _nextShapePos = new Point(3, 8);


    BaseShape[] _shapes =
        {new OBlock(5), new TBlock(6), new IBlock(2), new SBlock(3), new ZBlock(4), new JBlock(1), new LBlock(7)};

    private int _selectedShape = 0;
    private const int ScoresPerLine = 100;
    private const int GameSpeedScoreStep = 1500;
    private float _gameSpeedCoef = 1f;
    private int _gameSpeedLevel = 0;
    private int _totalScores = 0;
    private int _linesRemoved = 0;

    BaseShape _currentShape;
    Point _currentPos;
    private const float BaseMovementDeltaTime = 1f;
    private const float MinMovementDeltaTime = 0.2f;
    private const float MaxMovementDeltaTime = 2f;
    private float _currentMovementDeltaTime;
    private const float DeltaTimeStep = 0.1f;
    private const float ForceMovementDeltaTime = 0.1f;
    private float _forceMovementLastTime = .0f;
    private float _currentTime = .0f;
    private bool _firstStep = true;
    private bool _gameFinished = false;
    private bool _onPause;

    void FillField()
    {
        _gameField = new GameObject[_gameGrid.GetLength(0),_gameGrid.GetLength(1)];
        _spriteField = new SpriteRenderer[_gameGrid.GetLength(0), _gameGrid.GetLength(1)];
        for (int x = 0; x < _gameField.GetLength(0); x++)
        {
            for (int y = 0; y < _gameField.GetLength(1); y++)
            {
                _gameField[x, y] = GameObject.Instantiate(BlockPfb);
                _gameField[x, y].transform.position =
                    new Vector3((float) (_gameFieldPos.X + x), (float) (_gameFieldPos.Y - y), 0);
                _spriteField[x, y] = _gameField[x, y].GetComponent<SpriteRenderer>();
                _spriteField[x, y].color = _gridColors[0];
            }
        }

        for (int x = 0; x < _nextShapeField.GetLength(0); x++)
        {
            for (int y = 0; y < _nextShapeField.GetLength(1); y++)
            {
                _nextShapeField[x, y] = GameObject.Instantiate(BlockPfb);
                _nextShapeField[x, y].transform.position = new Vector3(_nextShapePos.X + x, _nextShapePos.Y - y, 0);
                _nextShapeField[x, y].GetComponent<SpriteRenderer>().color = _gridColors[0];
                _nextShapeField[x, y].SetActive(false);
            }
        }
    }

    void DrawField()
    {
        for (int x = 0; x < _spriteField.GetLength(0); x++)
        {
            for (int y = 0; y < _spriteField.GetLength(1); y++)
            {
                _spriteField[x, y].color = _gridColors[_gameGrid[x, y]];
            }
        }
    }

    void DrawNextShapeField()
    {
        for (int x = 0; x < _nextShapeField.GetLength(0); x++)
        {
            for (int y = 0; y < _nextShapeField.GetLength(1); y++)
            {
                _nextShapeField[x, y].GetComponent<SpriteRenderer>().color = _gridColors[_nextShapeGrid[x, y]];
                if (_nextShapeGrid[x, y] == 0)
                    _nextShapeField[x, y].SetActive(false);
                else
                {
                    _nextShapeField[x, y].SetActive(true);
                }
            }
        }
    }
    
    
    // Start is called before the first frame update
    void Start()
    {
        FillField();
        _startPosition = new Point(_gameGrid.GetLength(0)/2, 0);
        _currentMovementDeltaTime = BaseMovementDeltaTime;
        
        _gameSpeedText = GameObject.Find("GameSpeedValue").GetComponent<Text>();
        _scoresText = GameObject.Find("ScoresValue").GetComponent<Text>();
        _linesRemovedText = GameObject.Find("LinesRemovedValue").GetComponent<Text>();
        _pauseButton = GameObject.Find("PauseButton");
        
        _gameSpeedText.text = _gameSpeedCoef.ToString("F2");
        _scoresText.text = _totalScores.ToString();
        _linesRemovedText.text = _linesRemoved.ToString();
    }

    bool ForceMoveShape(in Point newPos)
    {
        if (Time.time - _forceMovementLastTime < ForceMovementDeltaTime)
            return false;
        bool result = _currentShape.Move(_currentPos, newPos, ref _gameGrid);
        if (result)
        {
            _currentPos = newPos;
            DrawField();
        }

        _forceMovementLastTime = Time.time;
        return result;
    }

    void SaveToUsedElements(int value)
    {
        _usedElements.Enqueue(value);
        if (_usedElements.Count > 4)
            _usedElements.Dequeue();
    }
    void NextShape()
    {
        BaseShape shape;
        if (_selectedShape >= 0)
        {
            for (int x = 0; x < _nextShapeGrid.GetLength(0); x++)
            {
                for (int y = 0; y < _nextShapeGrid.GetLength(1); y++)
                {
                    _nextShapeGrid[x, y] = 0;
                }
            }
        }

        int next = 0;
        for (int i = 0; i < 4; i++)
        {
            next = Random.Range(0, 10000) % 7;
            if (!_usedElements.Contains(next))
                break;
        }

        _selectedShape = next;

        shape = _shapes[_selectedShape];
        for (int i = 0; i < (int) Random.Range(0, 3); i++)
            shape.Rotate();
        Point tmp = new Point(_nextShapeField.GetLength(0) / 2 - shape.Width / 2, 1);
        shape.DrawShapeOnGrid(tmp, ref _nextShapeGrid);
        DrawNextShapeField();
    }

    void ChangeGameSpeed(bool increase = true)
    {
        float tmp = _currentMovementDeltaTime + (increase ? (-DeltaTimeStep) : DeltaTimeStep);
        tmp = Mathf.Round(tmp * 100f) / 100f;
        if (tmp >= MinMovementDeltaTime && tmp <= MaxMovementDeltaTime)
        {
            _currentMovementDeltaTime = tmp;
            _gameSpeedCoef = BaseMovementDeltaTime / _currentMovementDeltaTime;
            _gameSpeedText.text = _gameSpeedCoef.ToString("F2");
        }
    }

    bool CheckFilledLines()
    {
        int[,] tmpGrid = new int[_gameGrid.GetLength(0),_gameGrid.GetLength(1)];
        int line = tmpGrid.GetLength(1) - 1;
        int removedCount = 0;
        for (int y = _gameGrid.GetLength(1) - 1; y >= 0; y--)
        {
            bool lineFilled = true;
            for (int x = 0; x < _gameGrid.GetLength(0); x++)
            {
                tmpGrid[x, line] = _gameGrid[x, y];
                if (_gameGrid[x, y] == 0)
                {
                    lineFilled = false;
                }
            }

            if (!lineFilled)
            {
                line--;
            }
            else
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _gameGrid = tmpGrid;
            int count = removedCount;
            _linesRemoved += removedCount;
            while (count > 0)
            {
                _totalScores += count * ScoresPerLine;
                count--;
            }

            _scoresText.text = _totalScores.ToString();
            _linesRemovedText.text = _linesRemoved.ToString();
            int scoresRatio = _totalScores / GameSpeedScoreStep;
            if (scoresRatio > _gameSpeedLevel)
            {
                _gameSpeedLevel++;
                ChangeGameSpeed();
            }
        }

        return removedCount > 0;
    }
    
    // Update is called once per frame
    void Update()
    {
        
        if (_gameFinished || _onPause)
            return;
        
        if (Time.time - _currentTime >= _currentMovementDeltaTime)
        {
            bool needRefresh = false;
            if (_currentShape is null)
            {
                _currentShape = _shapes[_selectedShape];
                SaveToUsedElements(_selectedShape);

                NextShape();
                
                Point shapePoint = new Point(_startPosition.X - _currentShape.Width / 2, _startPosition.Y);
                if (!_currentShape.CanBeSpawned(shapePoint, _gameGrid))
                {
                    FinishGame();
                    return;
                }
                _currentPos = shapePoint;
                _currentShape.DrawShapeOnGrid(in _currentPos, ref _gameGrid);
                needRefresh = true;
                _firstStep = true;
            }
            else
            {
                Point newPos = new Point(_currentPos.X, _currentPos.Y + 1);
                needRefresh = _currentShape.Move(in _currentPos, in newPos, ref _gameGrid);
                if (_firstStep && !needRefresh)
                {
                    FinishGame();
                    return;
                }

                if (_firstStep)
                    _firstStep = false;
                if (needRefresh)
                    _currentPos = newPos;
                else
                {
                    _currentShape = null;
                    needRefresh = CheckFilledLines();
                }
            }

            if (needRefresh)
                DrawField();
            _currentTime = Time.time;
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.Space) && _currentShape != null)
        {
            if (_currentShape.RotateInPos(_currentPos, ref _gameGrid))
                DrawField();
            return;
        }

        if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) && _currentShape != null)
        {
            Point tmpPos = new Point(_currentPos.X - 1, _currentPos.Y);
            ForceMoveShape(tmpPos);
            return;
        }

        if ((Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) && _currentShape != null)
        {
            Point tmpPos = new Point(_currentPos.X + 1, _currentPos.Y);
            ForceMoveShape(tmpPos);
            return;
        }

        if ((Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) && _currentShape != null)
        {
            Point tmpPos = new Point(_currentPos.X, _currentPos.Y + 1);
            if (ForceMoveShape(tmpPos))
            {
                _firstStep = false;
                _currentTime = Time.time;
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            ChangeGameSpeed();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            ChangeGameSpeed(false);
            return;
        }

    }

    public void PauseOrContinue()
    {
        if (Time.timeScale > 0)
        {
            Time.timeScale = 0;
            _pauseButton.GetComponentInChildren<Text>().text = "Continue";
            _onPause = true;
        }
        else
        {
            Time.timeScale = 1;
            _pauseButton.GetComponentInChildren<Text>().text = "Pause";
            _onPause = false;
        }
    }

    public void CloseApplication()
    {
        Application.Quit();
    }

    void FinishGame()
    {
        _gameFinished = true;
        GameOverPanel.SetActive(true);
        GameObject.Find("GameOverText").GetComponent<Text>().text =
            $"Game over! You've reached the score of {_totalScores}! Do you want to play once more?";
        Time.timeScale = 0;
        _pauseButton.GetComponent<Button>().interactable = false;
    }

    public void ResetGame()
    {
        GameOverPanel.SetActive(false);
        _pauseButton.GetComponent<Button>().interactable = true;
        
        _totalScores = 0;
        _gameSpeedCoef = 1;
        _linesRemoved = 0;
        
        _gameSpeedText.text = _gameSpeedCoef.ToString("F2");
        _scoresText.text = _totalScores.ToString();
        _linesRemovedText.text = _linesRemoved.ToString();

        _currentShape = null;
        _selectedShape = 0;
        _usedElements.Clear();

        _gameGrid = new int[10, 20];
        _nextShapeGrid = new int[6, 6];
        DrawField();
        DrawNextShapeField();

        _currentTime = 0;
        _currentMovementDeltaTime = BaseMovementDeltaTime;
        _gameSpeedLevel = 0;
        _currentTime = 0;
        _forceMovementLastTime = 0;
        _firstStep = true;
        Time.timeScale = 1;
        _gameFinished = false;
    }
}
