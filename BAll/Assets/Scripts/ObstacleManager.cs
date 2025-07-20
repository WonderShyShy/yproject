using System.Collections;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public float spawnInterval = 2f; // 每2秒生成一个
    public float spawnDistance = 1f; // 在屏幕边缘外多远距离生成

    private Vector2 screenBottomLeft;
    private Vector2 screenTopRight;

    void Start()
    {
        // 精确计算屏幕边界
        screenBottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        screenTopRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, Camera.main.nearClipPlane));

        // 启动持续生成的协程
        StartCoroutine(SpawnObstacleRoutine());
    }

    IEnumerator SpawnObstacleRoutine()
    {
        while (true)
        {
            SpawnObstacle();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnObstacle()
    {
        Vector2 spawnPosition;
        
        // 随机选择一个出生边缘 (0:上, 1:下, 2:左, 3:右)
        int edge = Random.Range(0, 4);

        if (edge == 0) // 上
            spawnPosition = new Vector2(Random.Range(screenBottomLeft.x, screenTopRight.x), screenTopRight.y + spawnDistance);
        else if (edge == 1) // 下
            spawnPosition = new Vector2(Random.Range(screenBottomLeft.x, screenTopRight.x), screenBottomLeft.y - spawnDistance);
        else if (edge == 2) // 左
            spawnPosition = new Vector2(screenBottomLeft.x - spawnDistance, Random.Range(screenBottomLeft.y, screenTopRight.y));
        else // 右
            spawnPosition = new Vector2(screenTopRight.x + spawnDistance, Random.Range(screenBottomLeft.y, screenTopRight.y));

        // 随机选择一个屏幕内的目标点
        Vector2 targetPosition = new Vector2(
            Random.Range(screenBottomLeft.x, screenTopRight.x),
            Random.Range(screenBottomLeft.y, screenTopRight.y)
        );

        // 创建障碍物实例
        GameObject obstacleInstance = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
        
        // 设置它的目标
        obstacleInstance.GetComponent<ObstacleController>().SetTarget(targetPosition);
    }
} 