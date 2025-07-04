using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Snake : MonoBehaviour
{
    [Header("Snake Settings")]
    public Transform segmentPrefab;
    public Vector2Int direction = Vector2Int.right;
    public float speed = 20f;
    public float speedMultiplier = 1f;
    public int initialSize = 4;
    public bool moveThroughWalls = false;

    [Header("Arrow Head Settings")]
    public Sprite arrowSprite;  // 箭头图片，默认向下
    
    private readonly List<Transform> segments = new List<Transform>();
    private Vector2Int input;
    private float nextUpdate;
    private SpriteRenderer headRenderer;
    private Vector2Int nextTurnDirection;  // 下一次转向方向（预告）

    private void Start()
    {
        headRenderer = GetComponent<SpriteRenderer>();
        SetupArrowHead();
        ResetState();
    }

    private void Update()
    {
        // 触屏输入检测
        if (Input.GetMouseButtonDown(0))
        {
            TriggerTurn();
        }
        
        // 移动端触摸检测
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TriggerTurn();
        }
    }

    private void FixedUpdate()
    {
        // Wait until the next update before proceeding
        if (Time.time < nextUpdate) {
            return;
        }

        // Set the new direction based on the input
        if (input != Vector2Int.zero) {
            direction = input;
        }

        // Set each segment's position to be the same as the one it follows. We
        // must do this in reverse order so the position is set to the previous
        // position, otherwise they will all be stacked on top of each other.
        for (int i = segments.Count - 1; i > 0; i--) {
            segments[i].position = segments[i - 1].position;
        }

        // Move the snake in the direction it is facing
        // Round the values to ensure it aligns to the grid
        int x = Mathf.RoundToInt(transform.position.x) + direction.x;
        int y = Mathf.RoundToInt(transform.position.y) + direction.y;
        transform.position = new Vector2(x, y);

        // Set the next update time based on the speed
        nextUpdate = Time.time + (1f / (speed * speedMultiplier));
    }

    /// <summary>
    /// 设置箭头头部
    /// </summary>
    private void SetupArrowHead()
    {
        if (arrowSprite != null)
        {
            headRenderer.sprite = arrowSprite;
            headRenderer.sortingOrder = 3;  // 确保箭头在最上层
        }
        else
        {
            Debug.LogWarning("Arrow sprite not assigned, using default square");
        }
    }

    /// <summary>
    /// 更新箭头旋转方向（显示预告的转向方向）
    /// </summary>
    private void UpdateArrowRotation()
    {
        float angle = 0f;
        
        if (nextTurnDirection == Vector2Int.down)
            angle = 0f;    // 向下,不旋转
        else if (nextTurnDirection == Vector2Int.right)
            angle = 90f;   // 向右,顺时针90°
        else if (nextTurnDirection == Vector2Int.up)
            angle = 180f;  // 向上,顺时针180°
        else if (nextTurnDirection == Vector2Int.left)
            angle = 270f;  // 向左,顺时针270°
        
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    /// <summary>
    /// 触发转向（按预告方向转向）
    /// </summary>
    private void TriggerTurn()
    {
        // 按照预告方向转向
        direction = nextTurnDirection;
        
        // 生成新的预告方向
        GenerateNextTurnDirection();
    }

    /// <summary>
    /// 生成下一次转向的预告方向
    /// </summary>
    private void GenerateNextTurnDirection()
    {
        Vector2Int[] possibleDirections = GetPerpendicularDirections(direction);
        nextTurnDirection = possibleDirections[Random.Range(0, possibleDirections.Length)];
        UpdateArrowRotation();  // 更新箭头显示预告方向
    }

    /// <summary>
    /// 获取垂直方向的可选转向
    /// </summary>
    private Vector2Int[] GetPerpendicularDirections(Vector2Int currentDirection)
    {
        // 如果当前是垂直移动（上/下），可以转向水平（左/右）
        if (currentDirection.x == 0) 
        {
            return new Vector2Int[] { Vector2Int.left, Vector2Int.right };
        }
        // 如果当前是水平移动（左/右），可以转向垂直（上/下）
        else 
        {
            return new Vector2Int[] { Vector2Int.up, Vector2Int.down };
        }
    }

    public void Grow()
    {
        Transform segment = Instantiate(segmentPrefab);
        segment.position = segments[segments.Count - 1].position;
        segments.Add(segment);
    }

    public void ResetState()
    {
        direction = Vector2Int.right;
        transform.position = Vector3.zero;
        
        // 生成初始预告方向
        GenerateNextTurnDirection();

        // Start at 1 to skip destroying the head
        for (int i = 1; i < segments.Count; i++) {
            Destroy(segments[i].gameObject);
        }

        // Clear the list but add back this as the head
        segments.Clear();
        segments.Add(transform);

        // -1 since the head is already in the list
        for (int i = 0; i < initialSize - 1; i++) {
            Grow();
        }
    }

    public bool Occupies(int x, int y)
    {
        foreach (Transform segment in segments)
        {
            if (Mathf.RoundToInt(segment.position.x) == x &&
                Mathf.RoundToInt(segment.position.y) == y) {
                return true;
            }
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Food"))
        {
            Grow();
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            ResetState();
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            if (moveThroughWalls) {
                Traverse(other.transform);
            } else {
                ResetState();
            }
        }
    }

    private void Traverse(Transform wall)
    {
        Vector3 position = transform.position;

        if (direction.x != 0f) {
            position.x = Mathf.RoundToInt(-wall.position.x + direction.x);
        } else if (direction.y != 0f) {
            position.y = Mathf.RoundToInt(-wall.position.y + direction.y);
        }

        transform.position = position;
    }

}
