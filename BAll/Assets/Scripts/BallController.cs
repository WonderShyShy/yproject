using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public float moveSpeed = 8f;     // 球的移动速度
    public float rotationSpeed = 200f; // 箭头的旋转速度

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
    }

    void Update()
    {
        // 1. 检测屏幕点击
        if (Input.GetMouseButtonDown(0)) // 0代表鼠标左键或屏幕单点
        {
            // 2. 朝箭头方向发射
            // 因为箭头素材的初始方向是朝下的, 所以我们用-transform.up来获取正确的方向
            rb.velocity = -transform.up * moveSpeed;
        }
    }

    // OnCollisionEnter2D 方法将被移除，因为我们将使用 PhysicsMaterial2D 来处理反弹
} 