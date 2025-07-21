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

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 检查碰撞到的物体标签是否为 "Wall"
        if (collision.gameObject.CompareTag("Wall"))
        {
            // 获取碰撞前的速度方向
            Vector2 inDirection = rb.velocity;
            // 获取碰撞点的法线 (即垂直于墙面的方向)
            Vector2 inNormal = collision.contacts[0].normal;
            // 使用 Vector2.Reflect 计算反射后的向量
            Vector2 newVelocity = Vector2.Reflect(inDirection, inNormal);
            // 将计算出的新速度应用到刚体上
            rb.velocity = newVelocity;
        }
    }
} 