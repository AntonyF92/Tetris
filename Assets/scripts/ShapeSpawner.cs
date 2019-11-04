using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShapeSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject[] shapeList;

    [SerializeField] private GameObject nextShapePoint;

   
    private Queue<int> _usedElements = new Queue<int>(); //to prevent situations like this: L->S->L
    private GameObject _spawnedObj;
    private ShapeProps _spawnedSp;

    public void Init()
    {
        NextShape();
    }
    
    void SaveToUsedElements(int value)
    {
        _usedElements.Enqueue(value);
        if (_usedElements.Count > 4)
            _usedElements.Dequeue();
    }

    public ShapeProps SpawnNext()
    {
        _spawnedObj.transform.SetParent(transform);
        _spawnedObj.transform.position = transform.position;
        var lowestPoint = _spawnedSp.LowestPoint();
        int deltaY = Tetris.PositionY(transform) - (int) Mathf.Round(lowestPoint.y);
        if (deltaY > 0)
        {
            _spawnedObj.transform.position = new Vector3(transform.position.x, transform.position.y + deltaY, 0);
        }
        
        ShapeProps sp = _spawnedSp;
        
        NextShape();
        
        return sp;
    }
    
    void NextShape()
    {
        int next = 0;
        for (int i = 0; i < 4; i++)
        {
            next = Random.Range(0, 10000) % shapeList.Length;
            if (!_usedElements.Contains(next))
                break;
        }

        _spawnedObj = Instantiate(shapeList[next], nextShapePoint.transform);
        _spawnedSp = _spawnedObj.GetComponent<ShapeProps>();
        
        for (int i = 0; i < (int) Random.Range(0, 4); i++)
            _spawnedSp.RotateRight();

        SaveToUsedElements(next);
    }

    public void DestroySpawnedShapes()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
