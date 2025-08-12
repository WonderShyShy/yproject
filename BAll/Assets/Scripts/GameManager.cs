using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Screen Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    [Header("Time Stop Settings")]
    public float timeStopDuration = 1.5f; // 时间停止的持续时长

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
} 