using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public float moveForce = 10f;    // 每次点击施加的力
    public float maxSpeed = 12f;     // 小球的最大速度
    public float rotationSpeed = 200f; // 箭头的旋转速度
    [Range(0, 1)]
    public float brakeFactor = 0.9f; // 刹车力度, 0.9代表瞬间抵消90%的速度

    [Header("Chain Reaction Rewards")]
    public int screenShakeThreshold = 2; // 触发屏幕抖动的最小数量
    public int timeStopThreshold = 5;    // 触发时间停止的最小数量

    [Header("Visual Feedback")]
    public float pulseMaxSize = 1.25f;      // 放大到的最大尺寸倍数
    public float expandDuration = 0.07f;   // 快速放大的时长
    public float shrinkDuration = 0.18f;   // 缓慢缩小的时长

    [Header("Ghost Effect")]
    public GameObject ghostPrefab;
    public int ghostPoolSize = 5;
    private List<GameObject> ghostPool;

    private Rigidbody2D rb;
    private Vector3 originalScale;

    void Awake()
    {
        // 将游戏的目标帧率锁定在60FPS
        Application.targetFrameRate = 60;
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
        InitializeGhostPool();
    }

    void InitializeGhostPool()
    {
        ghostPool = new List<GameObject>();
        for (int i = 0; i < ghostPoolSize; i++)
        {
            GameObject ghost = Instantiate(ghostPrefab, transform.position, Quaternion.identity);
            ghost.SetActive(false);
            ghostPool.Add(ghost);
        }
    }

    void Start()
    {
        // rb = GetComponent<Rigidbody2D>(); // This line is moved to Awake
    }

    void FixedUpdate()
    {
        // 使用物理引擎进行旋转，以避免与物理计算冲突
        rb.MoveRotation(rb.rotation - rotationSpeed * Time.fixedDeltaTime);

        // 增加速度上限控制
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Ball COLLIDED with: " + collision.gameObject.name + " which has tag: " + collision.gameObject.tag);

        // OnCollisionEnter2D 现在只处理会产生物理反弹的碰撞
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("游戏结束！");
            // 在这里添加真正的游戏结束逻辑
            Time.timeScale = 0f; // 一个简单的暂停效果
        }
        // 您可能需要在这里加回与 "Wall" 碰撞的逻辑
        // else if (collision.gameObject.CompareTag("Wall")) { ... }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Ball TRIGGERED with: " + other.gameObject.name + " which has tag: " + other.gameObject.tag);
        if (other.gameObject.CompareTag("Link"))
        {
            // --- 开始雪崩算法 ---
            
            // 1. 初始化数据结构
            HashSet<Transform> markedForDeath = new HashSet<Transform>();
            Queue<Transform> workQueue = new Queue<Transform>();

            LinkController hitLink = other.gameObject.GetComponent<LinkController>();
            if (hitLink == null || hitLink.obstacleA == null || hitLink.obstacleB == null) return;

            // 2. 将最初被撞断的两个球加入队列和标记集
            workQueue.Enqueue(hitLink.obstacleA);
            markedForDeath.Add(hitLink.obstacleA);
            workQueue.Enqueue(hitLink.obstacleB);
            markedForDeath.Add(hitLink.obstacleB);

            // 3. 开始广度优先搜索（BFS），像波纹一样扩散
            while (workQueue.Count > 0)
            {
                Transform currentBall = workQueue.Dequeue();
                
                if (LinkManager.instance == null) continue;
                List<Transform> neighbors = LinkManager.instance.GetNeighborsOf(currentBall);

                foreach (Transform neighbor in neighbors)
                {
                    if (neighbor != null && !markedForDeath.Contains(neighbor))
                    {
                        markedForDeath.Add(neighbor);
                        workQueue.Enqueue(neighbor);
                    }
                }
            }

            int eliminatedCount = markedForDeath.Count;
            Debug.Log("雪崩式消除！总共波及 " + eliminatedCount + " 个障碍球。");

            // --- 4. 决策与奖励 ---
            if (eliminatedCount >= timeStopThreshold)
            {
                Debug.Log("触发高级奖励：时间停止！");
                // TODO: 在这里根据 eliminatedCount 计算并累加分数
                GameManager.instance.TriggerTimeStop();
            }
            else if (eliminatedCount >= screenShakeThreshold)
            {
                Debug.Log("触发中级奖励：屏幕抖动！");
                // TODO: 在这里根据 eliminatedCount 计算并累加分数
                GameManager.instance.TriggerScreenShake();
            }

            // 5. 清理被标记的集群
            CleanupCluster(markedForDeath, other.gameObject);
        }
    }

    private void CleanupCluster(HashSet<Transform> cluster, GameObject brokenLink)
    {
        if (LinkManager.instance != null)
        {
            LinkManager.instance.RemoveLinksForCluster(cluster);
        }
        foreach (Transform ballTransform in cluster)
        {
            if (ballTransform != null)
            {
                Destroy(ballTransform.gameObject);
            }
        }
        if (brokenLink != null) brokenLink.SetActive(false);
    }

    void Update()
    {
        // 1. 检测屏幕点击
        if (Input.GetMouseButtonDown(0)) // 0代表鼠标左键或屏幕单点
        {
            // 2. 先刹车：瞬间抵消掉大部分当前速度
            rb.velocity *= (1 - brakeFactor);

            // 3. 再加速：朝箭头方向施加一个瞬时冲量
            rb.AddForce(-transform.up * moveForce, ForceMode2D.Impulse);

            // 停止所有正在运行的同名协程，以避免动画冲突
            StopCoroutine("PulseRoutine");
            // 启动新的缩放动画
            StartCoroutine(PulseRoutine());
            
            // 召唤残影
            SpawnGhost();
        }
    }
    
    IEnumerator PulseRoutine()
    {
        float timer = 0f;
        Vector3 startScale = originalScale;
        Vector3 maxScale = originalScale * pulseMaxSize;

        // 放大阶段 (使用 EaseOut 曲线，开始快，结尾慢)
        while (timer < expandDuration)
        {
            float linearProgress = timer / expandDuration;
            float easedProgress = Easing.EaseOutQuad(linearProgress);
            transform.localScale = Vector3.LerpUnclamped(startScale, maxScale, easedProgress);
            timer += Time.deltaTime;
            yield return null;
        }

        // 缩小阶段 (使用线性插值，匀速)
        timer = 0f;
        while (timer < shrinkDuration)
        {
            float progress = timer / shrinkDuration;
            transform.localScale = Vector3.Lerp(maxScale, startScale, progress);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = startScale;
    }

    void SpawnGhost()
    {
        GameObject ghost = GetGhostFromPool();
        if (ghost != null)
        {
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.SetActive(true);
            
            float totalPulseDuration = expandDuration + shrinkDuration;
            ghost.GetComponent<GhostController>().Play(transform, totalPulseDuration);
        }
    }

    GameObject GetGhostFromPool()
    {
        foreach (var ghost in ghostPool)
        {
            if (!ghost.activeInHierarchy)
            {
                return ghost;
            }
        }
        return null;
    }

    // 一个小型的静态帮助类，用于存放缓动函数
    public static class Easing
    {
        public static float EaseOutQuad(float t)
        {
            return 1 - (1 - t) * (1 - t);
        }
    }
} 