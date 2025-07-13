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
    private bool isClockwise = false;  // 当前旋转方向（false=逆时针，true=顺时针）
    private FoodManager foodManager;

    private void Start()
    {
        headRenderer = GetComponent<SpriteRenderer>();
        foodManager = FindObjectOfType<FoodManager>();
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
        
        // 调试用：按T键测试相机抖动
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.TriggerShake();
            }
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
    /// 生成下一次转向的预告方向（根据当前旋转方向）
    /// </summary>
    private void GenerateNextTurnDirection()
    {
        // 根据当前旋转方向选择预告方向
        if (isClockwise)
        {
            nextTurnDirection = GetClockwiseDirection(direction);
        }
        else
        {
            nextTurnDirection = GetCounterClockwiseDirection(direction);
        }
        UpdateArrowRotation();  // 更新箭头显示预告方向
    }

    /// <summary>
    /// 获取逆时针方向
    /// </summary>
    private Vector2Int GetCounterClockwiseDirection(Vector2Int currentDirection)
    {
        if (currentDirection == Vector2Int.right) return Vector2Int.up;
        if (currentDirection == Vector2Int.up) return Vector2Int.left;
        if (currentDirection == Vector2Int.left) return Vector2Int.down;
        if (currentDirection == Vector2Int.down) return Vector2Int.right;
        return currentDirection;  // 默认返回原方向（安全措施）
    }

    /// <summary>
    /// 获取顺时针方向
    /// </summary>
    private Vector2Int GetClockwiseDirection(Vector2Int currentDirection)
    {
        if (currentDirection == Vector2Int.right) return Vector2Int.down;
        if (currentDirection == Vector2Int.down) return Vector2Int.left;
        if (currentDirection == Vector2Int.left) return Vector2Int.up;
        if (currentDirection == Vector2Int.up) return Vector2Int.right;
        return currentDirection;  // 默认返回原方向（安全措施）
    }

    /// <summary>
    /// 获取相反方向
    /// </summary>
    private Vector2Int GetOppositeDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return Vector2Int.down;
        if (direction == Vector2Int.down) return Vector2Int.up;
        if (direction == Vector2Int.left) return Vector2Int.right;
        if (direction == Vector2Int.right) return Vector2Int.left;
        return direction;  // 默认返回原方向（安全措施）
    }

    /// <summary>
    /// 反向箭头预告方向（吃食物时调用）
    /// </summary>
    private void ReverseArrowDirection()
    {
        // 反转预告方向
        nextTurnDirection = GetOppositeDirection(nextTurnDirection);
        
        // 反转旋转规律
        isClockwise = !isClockwise;
        
        UpdateArrowRotation();  // 立即更新箭头视觉
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
        isClockwise = false;  // 重置为逆时针
        transform.position = Vector3.zero;

        // Start at 1 to skip destroying the head
        for (int i = 1; i < segments.Count; i++) {
            Destroy(segments[i].gameObject);
        }

        // Clear the list but add back this as the head
        segments.Clear();
        segments.Add(transform);

        // 正确初始化蛇身：向左排列
        for (int i = 1; i < initialSize; i++) {
            Transform segment = Instantiate(segmentPrefab);
            segment.position = new Vector3(-i, 0, 0);  // 在蛇头左侧排列
            segments.Add(segment);
        }
        
        // 清理所有食物并生成新的初始食物
        if (foodManager != null)
        {
            foodManager.ClearAllFoods();
            foodManager.SpawnFood();
        }
        
        // 生成初始预告方向
        GenerateNextTurnDirection();
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
        if (other.gameObject.CompareTag("Food") || other.GetComponent<Food>() != null)
        {
            Grow();
            ReverseArrowDirection();  // 吃食物后立刻反向箭头
            
            // 触发相机抖动效果
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.TriggerShake();
            }
            
            // 注意：食物的销毁和新食物的生成由FoodManager处理
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
