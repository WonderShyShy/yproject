using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Screen Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    [Header("Time Stop Settings")]
    public float timeStopDuration = 1.5f; // 时间停止的持续时长

    [Header("Cascade Settings")]
    public float cascadeStepDelay = 0.1f; // 逐个传递步进间隔（Realtime）
    public float startStepDelay = 0.20f;  // 首步较慢
    public float endStepDelay = 0.07f;    // 末步较快
    public float perStepShakeMagnitude = 0.07f; // 每步微震幅度

    private Vector3 initialCameraPosition;

    // 抖动冲量聚合器
    private struct ShakeImpulse
    {
        public float remaining;
        public float duration;
        public float magnitude;
    }
    private readonly List<ShakeImpulse> activeImpulses = new List<ShakeImpulse>();
    private bool shakeLoopRunning = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (Camera.main != null)
        {
            initialCameraPosition = Camera.main.transform.position;
        }
    }

    public void TriggerTimeStop()
    {
        StartCoroutine(TimeStopRoutine());
    }

    private IEnumerator TimeStopRoutine()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(timeStopDuration);
        Time.timeScale = 1f;
    }

    public void TriggerScreenShake()
    {
        // 改为基于 Unscaled 的冲量触发
        TriggerShakeImpulse(shakeMagnitude, shakeDuration);
    }

    // 对外：追加一次抖动冲量（不打断现有抖动）
    public void TriggerShakeImpulse(float magnitude, float duration)
    {
        activeImpulses.Add(new ShakeImpulse { remaining = duration, duration = duration, magnitude = magnitude });
        if (!shakeLoopRunning)
        {
            StartCoroutine(ShakeLoop());
        }
    }

    // 单通道抖动循环（Unscaled 时间）
    private IEnumerator ShakeLoop()
    {
        shakeLoopRunning = true;
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
        {
            activeImpulses.Clear();
            shakeLoopRunning = false;
            yield break;
        }

        while (activeImpulses.Count > 0)
        {
            float totalIntensity = 0f;
            for (int i = activeImpulses.Count - 1; i >= 0; i--)
            {
                var imp = activeImpulses[i];
                float weight = imp.duration > 0f ? Mathf.Clamp01(imp.remaining / imp.duration) : 0f;
                totalIntensity += imp.magnitude * weight;
                imp.remaining -= Time.unscaledDeltaTime;
                if (imp.remaining <= 0f)
                {
                    activeImpulses.RemoveAt(i);
                }
                else
                {
                    activeImpulses[i] = imp;
                }
            }

            Vector3 randomOffset = (Vector3)(Random.insideUnitCircle * totalIntensity);
            cam.position = initialCameraPosition + randomOffset;
            yield return null;
        }

        if (Camera.main != null)
        {
            Camera.main.transform.position = initialCameraPosition;
        }
        shakeLoopRunning = false;
    }

    // 新增：级联时间停止逐个传递清除（由慢到快 + 每步震动）
    public void TriggerCascadeTimeStop(List<Transform> eliminationOrder)
    {
        StartCoroutine(CascadeEliminationRoutine(eliminationOrder));
    }

    private IEnumerator CascadeEliminationRoutine(List<Transform> eliminationOrder)
    {
        // 冻结时间（基于Realtime做演出）
        Time.timeScale = 0f;

        int total = eliminationOrder != null ? eliminationOrder.Count : 0;
        for (int i = 0; i < total; i++)
        {
            Transform node = eliminationOrder[i];
            if (node == null || !node.gameObject.activeInHierarchy)
            {
                continue;
            }

            // 每步：移除与该节点相关的所有连线
            if (LinkManager.instance != null)
            {
                var single = new HashSet<Transform> { node };
                LinkManager.instance.RemoveLinksForCluster(single);
            }

            // 销毁该节点
            Object.Destroy(node.gameObject);

            // 每步震动：幅度固定，时长与当前步间隔匹配
            float t = (total > 1) ? (float)i / (total - 1) : 1f; // 0..1
            float eased = t * t; // EaseIn（由慢到快）
            float stepDelay = Mathf.Lerp(startStepDelay, endStepDelay, eased);
            float shakeDur = stepDelay * 0.8f;
            TriggerShakeImpulse(perStepShakeMagnitude, shakeDur);

            // 步进等待（Realtime）
            yield return new WaitForSecondsRealtime(stepDelay);
        }

        // 保底：尝试再次清理余留连线
        if (LinkManager.instance != null)
        {
            LinkManager.instance.RemoveLinksForCluster(new HashSet<Transform>(eliminationOrder));
        }

        // 结束：恢复时间
        Time.timeScale = 1f;
    }
} 