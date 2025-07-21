using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class LinkController : MonoBehaviour
{
    public Transform obstacleA;
    public Transform obstacleB;

    // 呼吸效果参数
    public float minWidth = 0.05f;
    public float maxWidth = 0.2f;
    public float breathSpeed = 3f;

    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;
    private float breathTimer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();
    }

    void OnEnable()
    {
        // 当从对象池中激活时，随机化呼吸相位
        breathTimer = Random.Range(0f, 100f);
    }

    void Update()
    {
        // 如果连接的任一障碍物被销毁，则立即将自己归还到对象池
        if (obstacleA == null || obstacleB == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // 1. 更新位置
        lineRenderer.SetPosition(0, obstacleA.position);
        lineRenderer.SetPosition(1, obstacleB.position);

        // 2. 更新碰撞体
        // 需要将世界坐标转换为对撞机本地空间的坐标
        Vector2[] points = new Vector2[2];
        points[0] = transform.InverseTransformPoint(obstacleA.position);
        points[1] = transform.InverseTransformPoint(obstacleB.position);
        edgeCollider.points = points;

        // 3. 更新宽度 (呼吸效果)
        breathTimer += Time.deltaTime * breathSpeed;
        float sinValue = (Mathf.Sin(breathTimer) + 1f) / 2f; // 归一化到 [0, 1]
        float currentWidth = Mathf.Lerp(minWidth, maxWidth, sinValue);
        lineRenderer.startWidth = lineRenderer.endWidth = currentWidth;
    }
} 