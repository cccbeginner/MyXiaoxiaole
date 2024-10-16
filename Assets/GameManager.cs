using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Map map;
    public bool isBusy { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        isBusy = false;
        if (map == null)
        {
            Debug.LogError("Please assign a map instance to game manager.");
        }
        else
        {
            InitMap();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Take input and move cells
        int dx = 0, dy = 0;
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            dy = -1;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            dy = 1;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            dx = -1;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            dx = 1;
        }


        if ((dx != 0 || dy != 0) && !isBusy)
        {
            StartCoroutine(MoveEliminateRoutine(dx, dy));
        }
    }

    IEnumerator MoveEliminateRoutine(int dx, int dy)
    {
        isBusy = true;
        map.MoveCells(dx, dy, 0.3f);
        yield return new WaitForSeconds(0.5f);
        map.EliminateCells();
        isBusy = false;
    }

    public void InitMap()
    {
        map.SpawnRandomCellsInMap();
        map.EliminateCells();
    }
}
