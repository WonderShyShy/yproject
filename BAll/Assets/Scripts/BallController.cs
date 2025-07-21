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

    private Rigidbody2D rb;

    void Awake()
    {
        // 将游戏的目标帧率锁定在60FPS
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("游戏结束！");
            // 在这里添加真正的游戏结束逻辑
        }
        else if (collision.gameObject.CompareTag("Link"))
        {
            // 撞到了连接线
            LinkController link = collision.gameObject.GetComponent<LinkController>();
            if (link != null)
            {
                // 销毁连接的两个障碍物
                if (link.obstacleA != null) Destroy(link.obstacleA.gameObject);
                if (link.obstacleB != null) Destroy(link.obstacleB.gameObject);
                
                // 销毁（回收）连接线本身
                collision.gameObject.SetActive(false);
                
                // 在此可以添加得分、音效、特效等
                Debug.Log("连接线被摧毁!");
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
        }
    }
} 