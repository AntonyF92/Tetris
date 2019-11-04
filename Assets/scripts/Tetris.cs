using UnityEngine;
using UnityEngine.UI;

public class Tetris : MonoBehaviour
{
    [SerializeField] private int fieldWidth = 10;
    [SerializeField] private int fieldHeight = 20;
    [SerializeField] private GameObject spawnPoint;
    private ShapeSpawner _spawner;
    private ShapeProps _currentShape;

    //for check position
    private int _deltaX;
    private int _shapeBaseX;
    private int _childX, _childY;


    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameSpeedText;
    [SerializeField] private Text scoresText;
    [SerializeField] private Text linesRemovedText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Text pauseButtonText;


    Transform[,] _gameField;

    private const int ScoresPerLine = 100;
    [SerializeField] private float scoreMult = 0.5f;
    [SerializeField] private int gameSpeedScoreStep = 1500;
    private float _gameSpeedCoef = 1f;
    private int _gameSpeedLevel = 0;
    private int _totalScores = 0;
    private int _linesRemoved = 0;

    [SerializeField] private float baseMovementDeltaTime = 1f;
    [SerializeField] private float minGameSpeedCoef = 0.5f;
    [SerializeField] private float maxGameSpeedCoef = 2f;
    private float _currentMovementDeltaTime;
    [SerializeField] private float changeGameSpeedStep = 0.25f;
    [SerializeField] private float forceMovementDeltaTime = 0.1f;
    private float _forceMovementLastTime = .0f;
    private float _currentTime = .0f;
    private bool _firstStep = true;
    private bool _gameFinished = false;
    private bool _onPause;

    public static int PositionX(Transform child)
    {
        return (int) Mathf.Round(child.position.x);
    }

    public static int PositionY(Transform child)
    {
        return (int) Mathf.Round(child.position.y);
    }

    bool CheckPosForShift(Transform shape)
    {
        _deltaX = 0;
        _shapeBaseX = PositionX(shape);
        for (int i = 0; i < shape.childCount; i++)
        {
            _childX = PositionX(shape.GetChild(i));
            _childY = PositionY(shape.GetChild(i));
            if (_childY < 0)
            {
                return false;
            }
            
            if (_childX < 0 || _childX >= fieldWidth)
            {
                _deltaX = _shapeBaseX > _childX ? 1 : -1;
                return false;
            }

            if (_childY >= fieldHeight)
                continue;

            if (_gameField[_childX, _childY] != null && _gameField[_childX, _childY].parent != shape)
            {
                _deltaX = _shapeBaseX > _childX ? 1 : -1;
                return false;
            }
        }
        
        return true;
    }

    bool PosIsValid(Transform shape)
    {
        for (int i = 0; i < shape.childCount; i++)
        {
            int x = PositionX(shape.GetChild(i));
            int y = PositionY(shape.GetChild(i));
            if (y < 0)
            {
                return false;
            }
            
            if (x < 0 || x >= fieldWidth)
            {
                return false;
            }

            if (y >= fieldHeight)
                continue;

            if (_gameField[x, y] != null && _gameField[x, y].parent != shape)
            {
                return false;
            }
        }

        return true;
    }

    void CleanField()
    {
        for (int x = 0; x < fieldWidth; x++)
        {
            for (int y = 0; y < fieldHeight; y++)
            {
                _gameField[x, y] = null;
            }
        }
    }

    public void UpdateGameField(Transform shape)
    {
        for (int x = 0; x < fieldWidth; x++)
        {
            for (int y = 0; y < fieldHeight; y++)
            {
                if (_gameField[x, y] != null && _gameField[x, y].parent == shape)
                    _gameField[x, y] = null;
            }
        }

        for (int i = 0; i < shape.childCount; i++)
        {
            var item = shape.GetChild(i);
            int x = PositionX(item);
            int y = PositionY(item);
            if (y >= fieldHeight)
                continue;
            _gameField[x, y] = item;
        }
    }

    bool IsLineFilled(int y)
    {
        for (int x = 0; x < fieldWidth; x++)
        {
            if (_gameField[x, y] == null)
                return false;
        }

        return true;
    }

    void CleanLine(int y)
    {
        for (int x = 0; x < fieldWidth; x++)
        {
            Destroy(_gameField[x, y].gameObject);
            _gameField[x, y] = null;
        }
    }

    void MoveLineDown(int y)
    {
        for (int x = 0; x < fieldWidth; x++)
        {
            if (_gameField[x, y] != null)
            {
                _gameField[x, y].position += new Vector3(0, -1, 0);
                _gameField[x, y - 1] = _gameField[x, y];
                _gameField[x, y] = null;
            }
        }
    }

    void RemoveEmptyLine(int y)
    {
        for (int i = y + 1; i < fieldHeight; i++)
        {
            MoveLineDown(i);
        }
    }

    bool CheckFilledLines()
    {
        bool result = false;
        float scoreStreak = 1;
        for (int y = 0; y < fieldHeight; y++)
        {
            if (IsLineFilled(y))
            {
                CleanLine(y);
                _totalScores += (int) (ScoresPerLine * scoreStreak);
                scoreStreak += scoreMult;
                _linesRemoved++;
                RemoveEmptyLine(y);
                result = true;
                y--;
            }
        }

        if (result)
        {
            int tmp = _totalScores / gameSpeedScoreStep;
            if (tmp > _gameSpeedLevel)
            {
                ChangeGameSpeed();
                _gameSpeedLevel = tmp;
            }
        }

        return result;
    }

    bool ForceMoveShape(Vector3 newPos)
    {
        if (Time.time - _forceMovementLastTime < forceMovementDeltaTime)
            return false;
        bool result = false;
        _currentShape.transform.position += newPos;
        if (result = PosIsValid(_currentShape.transform))
        {
            UpdateGameField(_currentShape.transform);
        }
        else
        {
            _currentShape.transform.position -= newPos;
        }

        _forceMovementLastTime = Time.time;
        return result;
    }

    void SpawnShape()
    {
        _currentShape = _spawner.SpawnNext();
        _firstStep = true;
        if (PosIsValid(_currentShape.transform))
        {
            UpdateGameField(_currentShape.transform);
        }
        else
        {
            Destroy(_currentShape.gameObject);
            FinishGame();
        }
    }

    bool TryShiftAfterRotate()
    {
        _currentShape.transform.position += new Vector3(_deltaX, 0, 0);
        bool wasLeft = _deltaX < 0;
        bool check = CheckPosForShift(_currentShape.transform);
        if (check)
        {
            return true;
        }
        else
        {
            if (_deltaX == 0 || wasLeft && _deltaX > 0 || !wasLeft && _deltaX < 0)
            {
                return false;
            }
        }

        return TryShiftAfterRotate();
    }

    void RotateShape()
    {
        _currentShape.RotateRight();
        var checkPos = CheckPosForShift(_currentShape.transform);
        if (checkPos)
        {
            UpdateGameField(_currentShape.transform);
        }
        else
        {
            if (_deltaX != 0)
            {
                var oldPos = _currentShape.transform.position;
                if (TryShiftAfterRotate())
                {
                    UpdateGameField(_currentShape.transform);
                }
                else
                {
                    _currentShape.transform.position = oldPos;
                    _currentShape.RotateLeft();
                }
            }
            else
            {
                _currentShape.RotateLeft();
            }
        }
    }

    
    // Start is called before the first frame update
    void Start()
    {
        _gameField = new Transform[fieldWidth, fieldHeight];
        _spawner = spawnPoint.GetComponent<ShapeSpawner>();
        _spawner.Init();
        
        _currentMovementDeltaTime = baseMovementDeltaTime;
        
        gameSpeedText.text = _gameSpeedCoef.ToString("F2");
        scoresText.text = _totalScores.ToString();
        linesRemovedText.text = _linesRemoved.ToString();

        SpawnShape();
    }

    void ChangeGameSpeed(bool increase = true)
    {
        _gameSpeedCoef += increase ? changeGameSpeedStep : -changeGameSpeedStep;
        if (_gameSpeedCoef > maxGameSpeedCoef)
            _gameSpeedCoef = maxGameSpeedCoef;
        else if (_gameSpeedCoef < minGameSpeedCoef)
            _gameSpeedCoef = minGameSpeedCoef;
        _currentMovementDeltaTime = baseMovementDeltaTime / _gameSpeedCoef;
        gameSpeedText.text = _gameSpeedCoef.ToString("F2");
    }

    void CheckUserInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && _currentShape != null)
        {
            RotateShape();
            return;
        }

        if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) && _currentShape != null)
        {
            ForceMoveShape(new Vector3(-1, 0, 0));
            return;
        }

        if ((Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) && _currentShape != null)
        {
            ForceMoveShape(new Vector3(1, 0, 0));
            return;
        }

        if ((Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) && _currentShape != null)
        {
            if (ForceMoveShape(new Vector3(0, -1, 0)))
            {
                _currentTime = Time.time;
                _firstStep = false;
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
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        if (_gameFinished || _onPause)
            return;

        if (Time.time - _currentTime >= _currentMovementDeltaTime)
        {
            _currentShape.transform.position += new Vector3(0, -1, 0);
            if (PosIsValid(_currentShape.transform))
            {
                UpdateGameField(_currentShape.transform);
                _firstStep = false;
            }
            else
            {
                if (_firstStep)
                {
                    FinishGame();
                    return;
                }

                _currentShape.transform.position += new Vector3(0, 1, 0);
                _currentShape = null;
                if (CheckFilledLines())
                {
                    scoresText.text = _totalScores.ToString();
                    linesRemovedText.text = _linesRemoved.ToString();
                }
                SpawnShape();
            }
            _currentTime = Time.time;
            return;
        }
        
        CheckUserInput();
        

    }

    public void PauseOrContinue()
    {
        if (Time.timeScale > 0)
        {
            Time.timeScale = 0;
            pauseButtonText.text = "Continue";
            _onPause = true;
        }
        else
        {
            Time.timeScale = 1;
            pauseButtonText.text = "Pause";
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
        gameOverPanel.SetActive(true);
        gameOverText.text =
            $"Game over! You've reached the score of {_totalScores}! Do you want to play once more?";
        Time.timeScale = 0;
        pauseButton.interactable = false;
    }

    public void ResetGame()
    {
        gameOverPanel.SetActive(false);
        pauseButton.interactable = true;
        
        _totalScores = 0;
        _gameSpeedCoef = 1;
        _linesRemoved = 0;
        
        gameSpeedText.text = _gameSpeedCoef.ToString("F2");
        scoresText.text = _totalScores.ToString();
        linesRemovedText.text = _linesRemoved.ToString();
        
        CleanField();
        _spawner.DestroySpawnedShapes();

        _currentShape = null;

        _currentTime = 0;
        _currentMovementDeltaTime = baseMovementDeltaTime;
        _gameSpeedLevel = 0;
        _currentTime = 0;
        _firstStep = true;
        Time.timeScale = 1;
        _currentShape = _spawner.SpawnNext();
        _gameFinished = false;
    }
}
