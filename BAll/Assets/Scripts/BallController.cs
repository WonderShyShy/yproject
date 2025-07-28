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

    [Header("Visual Feedback")]
    public float pulseMaxSize = 1.2f;  // 放大到的最大尺寸倍数
    public float pulseDuration = 0.2f; // 完成一次放大缩小动画的总时长

    private Rigidbody2D rb;
    private Vector3 originalScale;

    void Awake()
    {
        // 将游戏的目标帧率锁定在60FPS
        Application.targetFrameRate = 60;
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
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
        Debug.Log("Ball collided with: " + collision.gameObject.name + " which has tag: " + collision.gameObject.tag);

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("游戏结束！");
        }
        else if (collision.gameObject.CompareTag("Link"))
        {
            LinkController link = collision.gameObject.GetComponent<LinkController>();
            if (link != null)
            {
                // 1. 保存对即将被销毁的两个障碍物的引用
                Transform obsA = link.obstacleA;
                Transform obsB = link.obstacleB;

                // 2. 立即回收被撞的主连接线
                collision.gameObject.SetActive(false);
                
                // 3. 命令 LinkManager 清理所有相关的次级连接线
                if (LinkManager.instance != null)
                {
                    LinkManager.instance.RemoveAllLinksConnectedTo(obsA);
                    LinkManager.instance.RemoveAllLinksConnectedTo(obsB);
                }
                
                // 4. 最后，销毁两个核心障碍物
                if (obsA != null) Destroy(obsA.gameObject);
                if (obsB != null) Destroy(obsB.gameObject);

                Debug.Log("连锁反应被触发!");
            }
        }
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
        }
    }
    
    IEnumerator PulseRoutine()
    {
        float timer = 0f;
        float halfDuration = pulseDuration / 2f;
        Vector3 startScale = originalScale;
        Vector3 maxScale = originalScale * pulseMaxSize;

        // 放大阶段
        while (timer < halfDuration)
        {
            float progress = timer / halfDuration;
            transform.localScale = Vector3.Lerp(startScale, maxScale, progress);
            timer += Time.deltaTime;
            yield return null;
        }

        // 缩小阶段
        timer = 0f;
        while (timer < halfDuration)
        {
            float progress = timer / halfDuration;
            transform.localScale = Vector3.Lerp(maxScale, startScale, progress);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = startScale; // 保证动画结束时恢复到精确的原始大小
    }
} 