using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public int timeStopThreshold = 7;    // 触发时间停止的最小数量（>6）

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

    void FixedUpdate()
    {
        rb.MoveRotation(rb.rotation - rotationSpeed * Time.fixedDeltaTime);
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("游戏结束！");
            Time.timeScale = 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Link"))
        {
            // BFS 构造整簇
            HashSet<Transform> cluster = new HashSet<Transform>();
            Queue<Transform> queue = new Queue<Transform>();

            LinkController hitLink = other.gameObject.GetComponent<LinkController>();
            if (hitLink == null || hitLink.obstacleA == null || hitLink.obstacleB == null) return;

            queue.Enqueue(hitLink.obstacleA); cluster.Add(hitLink.obstacleA);
            queue.Enqueue(hitLink.obstacleB); cluster.Add(hitLink.obstacleB);

            while (queue.Count > 0)
            {
                Transform cur = queue.Dequeue();
                if (LinkManager.instance == null) continue;
                var neigh = LinkManager.instance.GetNeighborsOf(cur);
                foreach (var n in neigh)
                {
                    if (n != null && !cluster.Contains(n))
                    {
                        cluster.Add(n);
                        queue.Enqueue(n);
                    }
                }
            }

            int cnt = cluster.Count;
            if (cnt >= timeStopThreshold)
            {
                // 大连锁：时间停止 + 逐个传递
                other.gameObject.SetActive(false);
                var adjacency = BuildAdjacencySnapshot(cluster);
                var order = GenerateEliminationOrder(hitLink, cluster, adjacency);
                GameManager.instance.TriggerCascadeTimeStop(order);
                return;
            }
            else if (cnt >= screenShakeThreshold)
            {
                GameManager.instance.TriggerScreenShake();
            }

            // 小连锁：整簇立即清理
            CleanupCluster(cluster, other.gameObject);
        }
    }

    private void CleanupCluster(HashSet<Transform> cluster, GameObject brokenLink)
    {
        if (LinkManager.instance != null)
        {
            LinkManager.instance.RemoveLinksForCluster(cluster);
        }
        foreach (var t in cluster)
        {
            if (t != null) Destroy(t.gameObject);
        }
        if (brokenLink != null) brokenLink.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            rb.velocity *= (1 - brakeFactor);
            rb.AddForce(-transform.up * moveForce, ForceMode2D.Impulse);
            StopCoroutine("PulseRoutine");
            StartCoroutine(PulseRoutine());
            SpawnGhost();
        }
    }

    IEnumerator PulseRoutine()
    {
        float timer = 0f;
        Vector3 startScale = originalScale;
        Vector3 maxScale = originalScale * pulseMaxSize;
        while (timer < expandDuration)
        {
            float p = timer / expandDuration;
            float eased = Easing.EaseOutQuad(p);
            transform.localScale = Vector3.LerpUnclamped(startScale, maxScale, eased);
            timer += Time.deltaTime;
            yield return null;
        }
        timer = 0f;
        while (timer < shrinkDuration)
        {
            float p = timer / shrinkDuration;
            transform.localScale = Vector3.Lerp(maxScale, startScale, p);
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
            float total = expandDuration + shrinkDuration;
            ghost.GetComponent<GhostController>().Play(transform, total);
        }
    }

    GameObject GetGhostFromPool()
    {
        foreach (var g in ghostPool)
        {
            if (!g.activeInHierarchy) return g;
        }
        return null;
    }

    public static class Easing
    {
        public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);
    }

    // 构建邻接快照（仅限 cluster 内）
    private Dictionary<Transform, List<Transform>> BuildAdjacencySnapshot(HashSet<Transform> cluster)
    {
        var adj = new Dictionary<Transform, List<Transform>>();
        foreach (var node in cluster)
        {
            var neigh = LinkManager.instance != null ? LinkManager.instance.GetNeighborsOf(node) : new List<Transform>();
            adj[node] = neigh.Where(n => n != null && cluster.Contains(n)).ToList();
        }
        return adj;
    }

    // 生成逐个传递顺序（从被撞线端点中择一个起点，贪心最近邻覆盖）
    private List<Transform> GenerateEliminationOrder(LinkController hitLink, HashSet<Transform> cluster, Dictionary<Transform, List<Transform>> adjacency)
    {
        Transform a = hitLink.obstacleA, b = hitLink.obstacleB;
        Transform start = a;
        Vector2 v = rb != null ? rb.velocity.normalized : Vector2.right;
        if (a != null && b != null)
        {
            float angA = Vector2.Angle(v, ((Vector2)a.position - (Vector2)transform.position).normalized);
            float angB = Vector2.Angle(v, ((Vector2)b.position - (Vector2)transform.position).normalized);
            start = angA <= angB ? a : b;
        }
        if (start == null) start = cluster.FirstOrDefault();

        List<Transform> order = new List<Transform>();
        HashSet<Transform> vis = new HashSet<Transform>();
        Transform cur = start;
        while (cur != null && order.Count < cluster.Count)
        {
            order.Add(cur);
            vis.Add(cur);

            Transform next = null; float best = float.MaxValue;
            if (adjacency.TryGetValue(cur, out var neigh))
            {
                foreach (var n in neigh)
                {
                    if (n != null && !vis.Contains(n))
                    {
                        float d = Vector2.Distance(cur.position, n.position);
                        if (d < best) { best = d; next = n; }
                    }
                }
            }
            if (next == null)
            {
                foreach (var n in cluster)
                {
                    if (n != null && !vis.Contains(n))
                    {
                        float d = Vector2.Distance(cur.position, n.position);
                        if (d < best) { best = d; next = n; }
                    }
                }
            }
            cur = next;
        }
        return order;
    }
} 