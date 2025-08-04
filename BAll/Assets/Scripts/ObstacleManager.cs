using System.Collections;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public float spawnDistance = 1.0f; // 障碍球生成在屏幕边缘外多远
    
    [Header("Wave Settings")]
    [Tooltip("屏幕上允许存在的最大障碍物数量")]
    public int maxOnscreenObstacles = 18; // 增加容量
    [Tooltip("一波可以生成的最大障碍物数量")]
    public int maxWaveSize = 5; // 增加力度
    [Tooltip("两波生成之间的最小等待时间（秒）")]
    public float minWaitBetweenWaves = 0.5f; // 加快反应
    [Tooltip("两波生成之间的最大等待时间（秒）")]
    public float maxWaitBetweenWaves = 2.0f; // 整体加快节奏

    public static ObstacleManager instance;
    private static int currentObstacleCount = 0;
    
    private Vector2 screenBottomLeft;
    private Vector2 screenTopRight;

    public static void OnObstacleDestroyed()
    {
        if (currentObstacleCount > 0)
        {
            currentObstacleCount--;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        screenBottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        screenTopRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, Camera.main.nearClipPlane));
        StartCoroutine(DynamicWaveRoutine());
    }

    IEnumerator DynamicWaveRoutine()
    {
        while (true)
        {
            // 1. 决策：决定这次要生成多少个
            int waveSize = CalculateWaveSize();

            // 2. 执行：如果决策的生成数大于0，就执行生成
            if (waveSize > 0)
            {
                int spawnEdge = Random.Range(0, 4);
                for (int i = 0; i < waveSize; i++)
                {
                    SpawnSingleObstacle(spawnEdge);
                    currentObstacleCount++;
                }
            }

            // 3. 等待：根据当前场上情况，计算下一次决策需要等多久
            float waitTime = CalculateWaitTime();
            yield return new WaitForSeconds(waitTime);
        }
    }

    int CalculateWaveSize()
    {
        if (currentObstacleCount >= maxOnscreenObstacles)
        {
            return 0; // 满了，一个都不生成
        }

        float occupancyRatio = (float)currentObstacleCount / maxOnscreenObstacles;
        int waveSize;

        if (occupancyRatio < 0.3f)
            waveSize = Random.Range(2, maxWaveSize + 1);
        else if (occupancyRatio < 0.7f)
            waveSize = Random.Range(1, 3);
        else
            waveSize = 1;
        
        int availableSlots = maxOnscreenObstacles - currentObstacleCount;
        return Mathf.Min(waveSize, availableSlots);
    }

    float CalculateWaitTime()
    {
        float occupancyRatio = (float)currentObstacleCount / maxOnscreenObstacles;
        return Mathf.Lerp(minWaitBetweenWaves, maxWaitBetweenWaves, occupancyRatio);
    }

    void SpawnSingleObstacle(int edge)
    {
        Vector2 spawnPosition;
        if (edge == 0)
            spawnPosition = new Vector2(Random.Range(screenBottomLeft.x, screenTopRight.x), screenTopRight.y + spawnDistance);
        else if (edge == 1)
            spawnPosition = new Vector2(Random.Range(screenBottomLeft.x, screenTopRight.x), screenBottomLeft.y - spawnDistance);
        else if (edge == 2)
            spawnPosition = new Vector2(screenBottomLeft.x - spawnDistance, Random.Range(screenBottomLeft.y, screenTopRight.y));
        else
            spawnPosition = new Vector2(screenTopRight.x + spawnDistance, Random.Range(screenBottomLeft.y, screenTopRight.y));

        Vector2 targetPosition = new Vector2(
            Random.Range(screenBottomLeft.x, screenTopRight.x),
            Random.Range(screenBottomLeft.y, screenTopRight.y)
        );

        GameObject obstacleInstance = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
        obstacleInstance.GetComponent<ObstacleController>().SetTarget(targetPosition);
    }
} 