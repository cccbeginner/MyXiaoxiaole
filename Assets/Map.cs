using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] int _width = 6, _height = 5; // map size in cells
    [SerializeField] Vector2 _cellSize;           // cell size in ppu
    [SerializeField] Sprite[] _cellSprites;        // assign some cell pictures
    [SerializeField] MoveOption _moveOption = MoveOption.AllToEnd;
    [SerializeField] EliminateOption _eliminateOption = EliminateOption.Connected3;

    public bool isBusy {  get; private set; }

    struct CellData
    {
        public GameObject gameObject;
        public int typeId;
        public CellData(GameObject gameObject, int typeId)
        {
            this.gameObject = gameObject;
            this.typeId = typeId;
        }
        public static CellData Empty = new CellData(null, -1);
        public static bool operator ==(CellData a, CellData b)
        {
            return ReferenceEquals(a.gameObject, b.gameObject) && a.typeId == b.typeId;
        }
        public static bool operator !=(CellData a, CellData b)
        {
            return !ReferenceEquals(a.gameObject, b.gameObject) || a.typeId != b.typeId;
        }
    }
    private CellData[,] _mapData;
    enum MoveOption
    {
        RowOneStep, RowToEnd, AllOneStep, AllToEnd,
    }
    enum EliminateOption
    {
        Connected2, Connected3, Straight3,
    }

    // Start is called before the first frame update
    void Awake()
    {
        _mapData = new CellData[_width, _height];
        isBusy = false;

        if (_cellSprites.Length == 0)
        {
            Debug.LogError("No cell sprite assigned! Give me some cell picture!");
        }
    }

    public Vector2 Cell2WorldUnit(Vector2 cellPos)
    {
        return cellPos * _cellSize;
    }

    // Spawn random cells in whole map.
    // If the map is not empty, the cells would be overwrite.
    public void SpawnRandomCellsInMap()
    {
        ClearAllCells();
        for (int i = 0; i < _width; i++) {
            for (int j = 0; j < _height; j++)
            {
                // let -1 be enpty cell (space), other be normal cells
                int cellSpriteId = UnityEngine.Random.Range(-1, _cellSprites.Length);
                if (cellSpriteId == -1)
                {
                    _mapData[i, j] = CellData.Empty;
                }
                else
                {
                    GameObject newCellObject = CreateNewCell(Cell2WorldUnit(new Vector2(i, j)), _cellSprites[cellSpriteId]);
                    _mapData[i, j] = new CellData(newCellObject, cellSpriteId);
                }
            }
        }
    }

    // Respawn cells using data in _mapData
    //public void RespawnCellsInMap()
    //{
    //    for (int i = 0; i < _width; i++)
    //    {
    //        for (int j = 0; j < _height; j++)
    //        {
    //            CellData cell = _MapData[i, j];
    //            if (cell.typeId != -1){
    //                cell.gameObject
    //                    .GetComponent<Cell>()
    //                    .SetPosition(Cell2WorldUnit(new Vector2(i,j)));
    //            }
    //        }
    //    }
    //}

    // Create a new gameobject to display cell.
    private GameObject CreateNewCell(Vector2 cellPos, Sprite sprite)
    {

        // Create new GameObject for cell
        GameObject spriteObject = new GameObject("MySprite");
        spriteObject.transform.parent = transform;
        spriteObject.AddComponent<Cell>()
            .InitCell(cellPos, _cellSize, sprite);

        return spriteObject;
    }

    // Move Cells when user inputs.
    // (dx, dy) should belongs to one of those: {(0,1), (0,-1), (1,0), (-1,0)}
    public void MoveCells(int dx, int dy, float duration)
    {
        // Don't let it move vertically & horizontally at the same time
        if (dx == 0 && dy == 0) return;
        if (dx != 0 && dy != 0) dx = 0;
        if (isBusy) return;


        // Calculate move distance (in cells) for each cells
        int[,] moveDistance = new int[_width, _height];
        Array.Clear(moveDistance, 0, moveDistance.Length);

        // Decide iteration order (ascending / descending) by input direction (dx, dy)
        int[] iterX = Enumerable.Range(0, _width).ToArray();
        int[] iterY = Enumerable.Range(0, _height).ToArray();
        if (dx > 0 || dy > 0) // Iterate in descending order
        {
            Array.Reverse(iterX);
            Array.Reverse(iterY);
        }

        // Calculate moveDistance for all cells, including empty cells
        foreach (int i in iterX)
        {
            foreach (int j in iterY)
            {
                int nextX = i + dx, nextY = j + dy;
                if (nextX < 0 || nextX >= _width) continue; // out of map
                if (nextY < 0 || nextY >= _height) continue; // out of map

                if (_moveOption == MoveOption.RowOneStep)
                {
                    if (_mapData[nextX, nextY] == CellData.Empty)
                    {
                        moveDistance[i, j] = 1;
                    }
                }
                else if (_moveOption == MoveOption.RowToEnd)
                {
                    if (_mapData[nextX, nextY] == CellData.Empty)
                    {
                        moveDistance[i, j] = moveDistance[nextX, nextY] + 1;
                    }
                }
                else if (_moveOption == MoveOption.AllOneStep)
                {
                    if (_mapData[nextX, nextY] == CellData.Empty || moveDistance[nextX, nextY] == 1)
                    {
                        moveDistance[i, j] = 1;
                    }
                }
                else if (_moveOption == MoveOption.AllToEnd)
                {
                    if (_mapData[nextX, nextY] == CellData.Empty)
                    {
                        moveDistance[i, j] = moveDistance[nextX, nextY] + 1;
                    }
                    else
                    {
                        moveDistance[i, j] = moveDistance[nextX, nextY];
                    }
                }
            }
        }

        // update map data
        foreach (int i in iterX)
        {
            foreach (int j in iterY)
            {
                if (moveDistance[i, j] > 0)
                {
                    int nextX = i + dx * moveDistance[i, j];
                    int nextY = j + dy * moveDistance[i, j];
                    Vector2 targetPos = Cell2WorldUnit(new Vector2(nextX, nextY));
                    _mapData[i, j].gameObject?.GetComponent<Cell>().Move(targetPos, duration);
                    _mapData[nextX, nextY] = _mapData[i, j];
                    _mapData[i, j] = CellData.Empty;
                }
            }
        }
        //RespawnCellsInMap();
    }

    // Eliminate Cells ... typically after movement.
    // Returns whether it removes any cell
    public bool EliminateCells()
    {
        // 4 directions
        int[] dx = { 1, 0, -1, 0 };
        int[] dy = { 0, 1, 0, -1 };

        // save those cells who is eliminated
        bool[,] shouldRemove = new bool[_width, _height];
        Array.Clear(shouldRemove, 0, shouldRemove.Length);

        // check every cells see who can be removed
        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                if (_mapData[i, j] == CellData.Empty) continue;

                // Check if the cells in 4 directions have same type with current cell.
                bool[] sameType = { false, false, false, false };
                for (int dir = 0; dir < 4; dir++)
                {
                    int nextX = i + dx[dir], nextY = j + dy[dir];
                    if (nextX < 0 || nextX >= _width) continue; // out of range
                    if (nextY < 0 || nextY >= _height) continue; // out of range
                    sameType[dir] = (_mapData[i, j].typeId == _mapData[nextX, nextY].typeId);
                }


                // Filter is the current cell don't have enough neighbors with same type.
                // Assume the current cell (i,j) is the middle cell of the 3. If not, filter it.
                if (_eliminateOption == EliminateOption.Connected2)
                {
                    if (sameType.Count(b => b == true) < 1) continue;
                }
                else if (_eliminateOption == EliminateOption.Connected3)
                {
                    if (sameType.Count(b => b == true) < 2) continue;
                }
                else if (_eliminateOption == EliminateOption.Straight3)
                {
                    if (sameType[0] == false || sameType[2] == false)
                    {
                        sameType[0] = sameType[2] = false;
                    }
                    if (sameType[1] == false || sameType[3] == false)
                    {
                        sameType[1] = sameType[3] = false;
                    }
                    if (sameType.Count(b => b == true) < 2) continue;
                }

                // This cell should be remove, also mark neighbors with same type.
                shouldRemove[i, j] = true;
                for (int dir = 0; dir < 4; dir++)
                {
                    if (sameType[dir])
                    {
                        int nextX = i + dx[dir], nextY = j + dy[dir];
                        shouldRemove[nextX, nextY] = true;
                    }
                }
            }
        }

        // Eliminate (destroy) cells
        // And also update _mapData
        bool removedSth = false;
        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                if (_mapData[i, j] == CellData.Empty) continue;
                if (shouldRemove[i, j])
                {
                    removedSth = true;
                    _mapData[i, j].gameObject.GetComponent<Cell>().Destroy();
                    _mapData[i, j] = CellData.Empty;
                }
            }
        }

        return removedSth;
    }

    // Clear all existing cells.
    public void ClearAllCells()
    {
        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                if (_mapData[i, j] == CellData.Empty) continue;
                Destroy( _mapData[i, j].gameObject);
            }
        }
    }
}
