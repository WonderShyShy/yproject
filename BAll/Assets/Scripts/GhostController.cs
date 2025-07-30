using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GhostController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Transform targetToFollow;
    private Color originalColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color; // 捕获在Prefab上设置的初始（半透明）颜色
    }

    void OnEnable()
    {
        // 每次从对象池激活时，重置颜色和缩放，以防上次的状态残留
        spriteRenderer.color = originalColor;
        if (targetToFollow != null)
        {
            transform.localScale = targetToFollow.localScale;
        }
    }

    // 由BallController调用的启动方法
    public void Play(Transform target, float duration)
    {
        this.targetToFollow = target;
        StartCoroutine(AnimateAndFadeRoutine(duration));
    }

    private IEnumerator AnimateAndFadeRoutine(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            // 安全检查
            if (targetToFollow == null)
            {
                break; // 如果跟随目标消失，提前结束动画
            }

            // 1. 同步缩放
            transform.localScale = targetToFollow.localScale;

            // 2. 自我淡出
            float progress = timer / duration;
            spriteRenderer.color = Color.Lerp(originalColor, Color.clear, progress);

            timer += Time.deltaTime;
            yield return null;
        }

        // 动画结束，将自己归还到对象池
        gameObject.SetActive(false);
    }
} 