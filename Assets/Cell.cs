using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public SpriteRenderer spriteRenderer {  get
        {
            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr != null) return sr;
            return gameObject.AddComponent<SpriteRenderer>();
        }
    }
    public bool _isBusy { get; private set; }

    private void Start()
    {
        _isBusy = false;
    }

    public Cell InitCell(Vector2 pos, Vector2 scale, Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
        transform.localScale = scale;
        transform.localPosition = pos;
        return this;
    }

    public Cell SetPosition(Vector2 pos)
    {
        transform.localPosition = pos;
        return this;
    }


    // Move the cell from its position to target, withing duration.
    // Returns true if it starts moving successfully, false vice versa.
    public bool Move(Vector2 targetPos, float duration)
    {
        if (_isBusy)
        {
            Debug.LogWarning("Cannot move cell since it's moving!");
            return false;
        }
        else
        {
            _isBusy = true;
            StartCoroutine(MoveRoutine(targetPos, duration));
            return true;
        }
    }

    // Moving Coroutine...
    // Currently it moves in same speed.
    IEnumerator MoveRoutine(Vector2 targetPos, float duration) {
        Vector2 startPos = transform.localPosition;
        for (float time = 0; time <= duration; time += Time.deltaTime)
        {
            transform.localPosition = Vector2.Lerp(startPos, targetPos, time / duration);
            yield return null;
        }
        transform.localPosition = targetPos;
        _isBusy = false;
    }

    // Destroy the cell.
    // Probably add some vfx and delay in the future.
    public void Destroy()
    {
        Destroy(gameObject);
    }
}
