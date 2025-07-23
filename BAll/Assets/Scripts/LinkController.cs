using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class LinkController : MonoBehaviour
{
    // 这两个变量将由 Initialize 方法设置
    public Transform obstacleA { get; private set; }
    public Transform obstacleB { get; private set; }

    // 呼吸效果参数
    public float minWidth = 0.05f;
    public float maxWidth = 0.2f;
    public float breathSpeed = 3f;

    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;
    private float breathTimer;
    private bool isInitialized = false;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();
    }

    void OnEnable()
    {
        // 每次从对象池激活时，重置初始化状态
        isInitialized = false;
        // 随机化呼吸相位
        breathTimer = Random.Range(0f, 100f);
    }
    
    // 公共的初始化方法，由 LinkManager 调用
    public void Initialize(Transform a, Transform b)
    {
        obstacleA = a;
        obstacleB = b;
        isInitialized = true; // 任务已分配，准备开始工作
        
        // 关键：在初始化的瞬间，立刻更新一次所有状态
        UpdateLineState();
    }

    void Update()
    {
        // 如果还未被成功初始化，则不执行任何操作
        if (!isInitialized)
        {
            return;
        }

        // 如果连接的任一障碍物被销毁，则立即将自己归还到对象池
        if (obstacleA == null || obstacleB == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // 持续更新所有状态
        UpdateLineState();
    }

    // 将所有更新逻辑提取到一个可重用的方法中
    void UpdateLineState()
    {
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