using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("抖动参数")]
    public float defaultIntensity = 0.15f;    // 默认抖动强度
    public float defaultDuration = 0.1f;      // 默认持续时间
    public AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);  // 抖动衰减曲线
    
    [Header("设置选项")]
    public bool enableShake = true;           // 是否启用抖动
    public float intensityMultiplier = 1f;    // 强度倍数（可用于设置）
    
    // 单例模式
    public static CameraShake Instance { get; private set; }
    
    private Camera targetCamera;
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;
    private bool isShaking = false;
    
    private void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 获取相机组件
        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            Debug.LogError("CameraShake: 未找到Camera组件！");
            enabled = false;
            return;
        }
        
        // 保存原始位置
        originalPosition = transform.position;
    }
    
    /// <summary>
    /// 触发相机抖动
    /// </summary>
    /// <param name="intensity">抖动强度，-1使用默认值</param>
    /// <param name="duration">持续时间，-1使用默认值</param>
    public void TriggerShake(float intensity = -1f, float duration = -1f)
    {
        if (!enableShake) return;
        
        // 使用默认值
        if (intensity < 0) intensity = defaultIntensity;
        if (duration < 0) duration = defaultDuration;
        
        // 应用强度倍数
        intensity *= intensityMultiplier;
        
        // 如果正在抖动，先停止
        if (isShaking && shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        // 开始新的抖动
        shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }
    
    /// <summary>
    /// 停止抖动
    /// </summary>
    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        
        isShaking = false;
        transform.position = originalPosition;
    }
    
    /// <summary>
    /// 设置抖动开关
    /// </summary>
    public void SetShakeEnabled(bool enabled)
    {
        enableShake = enabled;
        if (!enabled)
        {
            StopShake();
        }
    }
    
    /// <summary>
    /// 设置强度倍数
    /// </summary>
    public void SetIntensityMultiplier(float multiplier)
    {
        intensityMultiplier = Mathf.Clamp(multiplier, 0f, 2f);
    }
    
    /// <summary>
    /// 抖动协程
    /// </summary>
    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            // 计算当前强度（使用衰减曲线）
            float currentIntensity = intensity * shakeCurve.Evaluate(elapsedTime / duration);
            
            // 生成随机偏移量
            float offsetX = Random.Range(-1f, 1f) * currentIntensity;
            float offsetY = Random.Range(-1f, 1f) * currentIntensity;
            
            // 应用偏移
            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 恢复原始位置
        transform.position = originalPosition;
        isShaking = false;
        shakeCoroutine = null;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // 调试用的公共方法
    [System.Obsolete("仅用于调试，请使用TriggerShake()")]
    public void TestShake()
    {
        TriggerShake();
    }
} 