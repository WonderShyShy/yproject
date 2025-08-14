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

    private Vector3 initialCameraPosition;

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
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsedTime = 0f;
        if (Camera.main == null) yield break;

        Transform cameraTransform = Camera.main.transform;

        while (elapsedTime < shakeDuration)
        {
            Vector3 randomOffset = (Vector3)Random.insideUnitCircle * shakeMagnitude;
            cameraTransform.position = initialCameraPosition + randomOffset;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cameraTransform.position = initialCameraPosition;
    }

    // 新增：级联时间停止逐个传递清除
    public void TriggerCascadeTimeStop(List<Transform> eliminationOrder)
    {
        StartCoroutine(CascadeEliminationRoutine(eliminationOrder));
    }

    private IEnumerator CascadeEliminationRoutine(List<Transform> eliminationOrder)
    {
        // 冻结时间（基于Realtime做演出）
        Time.timeScale = 0f;

        for (int i = 0; i < eliminationOrder.Count; i++)
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

            // 步进等待（Realtime）
            yield return new WaitForSecondsRealtime(cascadeStepDelay);
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