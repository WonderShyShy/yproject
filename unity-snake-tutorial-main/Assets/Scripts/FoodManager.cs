using System.Collections.Generic;
using UnityEngine;

public class FoodManager : MonoBehaviour
{
    [Header("Food Settings")]
    public GameObject foodPrefab;
    public Collider2D gridArea;
    
    private Snake snake;
    private List<GameObject> activeFoods = new List<GameObject>();
    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    private void Awake()
    {
        snake = FindObjectOfType<Snake>();
    }

    private void Start()
    {
        // 生成初始食物
        SpawnFood();
    }

    /// <summary>
    /// 生成一个食物
    /// </summary>
    public void SpawnFood()
    {
        Vector2Int position = GetRandomEmptyPosition();
        if (position != Vector2Int.zero) // 找到了有效位置
        {
            CreateFoodAt(position);
        }
    }

    /// <summary>
    /// 生成多个食物
    /// </summary>
    public void SpawnMultipleFoods(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnFood();
        }
    }

    /// <summary>
    /// 食物被吃掉时调用
    /// </summary>
    public void OnFoodEaten(GameObject eatenFood)
    {
        // 移除被吃掉的食物
        RemoveFood(eatenFood);
        
        // 生成2个新食物
        SpawnMultipleFoods(2);
    }

    /// <summary>
    /// 在指定位置创建食物
    /// </summary>
    private void CreateFoodAt(Vector2Int position)
    {
        GameObject newFood = Instantiate(foodPrefab);
        newFood.transform.position = new Vector3(position.x, position.y, 0);
        
        // 设置食物的FoodManager引用
        Food foodComponent = newFood.GetComponent<Food>();
        if (foodComponent != null)
        {
            foodComponent.SetFoodManager(this);
        }
        
        activeFoods.Add(newFood);
        occupiedPositions.Add(position);
    }

    /// <summary>
    /// 移除食物
    /// </summary>
    private void RemoveFood(GameObject food)
    {
        if (activeFoods.Contains(food))
        {
            Vector2Int position = new Vector2Int(
                Mathf.RoundToInt(food.transform.position.x),
                Mathf.RoundToInt(food.transform.position.y)
            );
            
            activeFoods.Remove(food);
            occupiedPositions.Remove(position);
            Destroy(food);
        }
    }

    /// <summary>
    /// 获取随机空位置
    /// </summary>
    private Vector2Int GetRandomEmptyPosition()
    {
        Bounds bounds = gridArea.bounds;
        
        // 计算正确的网格范围
        // 对于16×24的网格，有效范围应该是:
        // X: -7 到 +7 (15个位置)
        // Y: -11 到 +11 (23个位置)
        int halfWidth = Mathf.FloorToInt(bounds.size.x / 2) - 1;
        int halfHeight = Mathf.FloorToInt(bounds.size.y / 2) - 1;
        
        int gridWidth = halfWidth * 2 + 1;  // 15
        int gridHeight = halfHeight * 2 + 1; // 23
        int maxAttempts = gridWidth * gridHeight * 2; // 增加尝试次数
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 在正确的网格范围内生成位置
            int x = Random.Range(-halfWidth, halfWidth + 1); // -7 到 +7
            int y = Random.Range(-halfHeight, halfHeight + 1); // -11 到 +11
            Vector2Int position = new Vector2Int(x, y);
            
            // 检查位置是否被占用
            if (!IsPositionOccupied(position))
            {
                return position;
            }
        }
        
        Debug.LogWarning($"FoodManager: 无法找到空位置生成食物！网格范围: X[{-halfWidth}, {halfWidth}], Y[{-halfHeight}, {halfHeight}]");
        return Vector2Int.zero; // 表示失败
    }

    /// <summary>
    /// 检查位置是否被占用
    /// </summary>
    private bool IsPositionOccupied(Vector2Int position)
    {
        // 检查是否被蛇占用
        if (snake.Occupies(position.x, position.y))
        {
            return true;
        }
        
        // 检查是否被其他食物占用
        if (occupiedPositions.Contains(position))
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 获取当前食物数量
    /// </summary>
    public int GetFoodCount()
    {
        return activeFoods.Count;
    }

    /// <summary>
    /// 清理所有食物（游戏重置时使用）
    /// </summary>
    public void ClearAllFoods()
    {
        foreach (GameObject food in activeFoods)
        {
            if (food != null)
            {
                Destroy(food);
            }
        }
        
        activeFoods.Clear();
        occupiedPositions.Clear();
    }
} 