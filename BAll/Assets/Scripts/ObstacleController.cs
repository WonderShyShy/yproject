using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    public float speed = 3f;
    private Rigidbody2D rb;

    private Vector2 screenBottomLeft;
    private Vector2 screenTopRight;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 精确计算屏幕边界，用于后续的销毁检测
        screenBottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        screenTopRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, Camera.main.nearClipPlane));
    }

    // 由 ObstacleManager 调用，来设置飞行目标
    public void SetTarget(Vector2 target)
    {
        // 确保 Rigidbody 已经初始化
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        
        // 计算从当前位置到目标点的方向
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.velocity = direction * speed;
    }

    void Update()
    {
        // 检查是否超出精确的边界，并增加一个小的缓冲区域
        if (transform.position.x > screenTopRight.x + 1 || transform.position.x < screenBottomLeft.x - 1 ||
            transform.position.y > screenTopRight.y + 1 || transform.position.y < screenBottomLeft.y - 1)
        {
            Destroy(gameObject);
        }
    }
} 