using System.Collections;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public int maxOnscreenObstacles = 10;
    public float minSpawnInterval = 0.5f; // 场上空闲时的最快生成间隔
    public float maxSpawnInterval = 3f;  // 场上拥挤时的最慢生成间隔
    public int maxWaveSize = 3;         // 一次最多能生成的数量
    public float spawnDistance = 1f;

    private Vector2 screenBottomLeft;
    private Vector2 screenTopRight;
    private static int currentObstacleCount = 0;

    public static void OnObstacleDestroyed()
    {
        if (currentObstacleCount > 0)
        {
            currentObstacleCount--;
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
        return Mathf.Lerp(minSpawnInterval, maxSpawnInterval, occupancyRatio);
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