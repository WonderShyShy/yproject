using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Food : MonoBehaviour
{
    private FoodManager foodManager;

    /// <summary>
    /// 设置FoodManager引用
    /// </summary>
    public void SetFoodManager(FoodManager manager)
    {
        foodManager = manager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 只有蛇头触发食物被吃
        if (other.gameObject.CompareTag("Player") || other.GetComponent<Snake>() != null)
        {
            // 通知FoodManager食物被吃掉
            if (foodManager != null)
            {
                foodManager.OnFoodEaten(gameObject);
            }
            else
            {
                // 如果没有FoodManager，直接销毁（兼容旧版本）
                Destroy(gameObject);
            }
        }
    }
}
