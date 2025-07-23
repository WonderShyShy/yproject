using System;
using System.Collections.Generic;
using UnityEngine;

public class LinkManager : MonoBehaviour
{
    public GameObject linkPrefab; // 连接线预制体
    public float linkDistanceThreshold = 3f; // 触发连接的距离阈值
    public int poolSize = 20; // 对象池大小

    private List<GameObject> linkPool;
    private Dictionary<Tuple<Transform, Transform>, GameObject> activeLinks;
    private List<Transform> obstacles;

    void Start()
    {
        // 1. 初始化对象池
        linkPool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject link = Instantiate(linkPrefab, transform);
            link.SetActive(false);
            linkPool.Add(link);
        }

        // 2. 初始化数据结构
        activeLinks = new Dictionary<Tuple<Transform, Transform>, GameObject>();
        obstacles = new List<Transform>();
    }

    void Update()
    {
        // 更新障碍物列表 (这是一种简单实现，更优化的方式是障碍物自行注册/注销)
        UpdateObstacleList();
        
        // 维护现有连接
        PruneLinks();
        
        // 发现新连接
        DiscoverNewLinks();
    }

    void UpdateObstacleList()
    {
        obstacles.Clear();
        GameObject[] obstacleObjects = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var obj in obstacleObjects)
        {
            obstacles.Add(obj.transform);
        }
    }
    
    void PruneLinks()
    {
        List<Tuple<Transform, Transform>> linksToRemove = new List<Tuple<Transform, Transform>>();
        foreach (var link in activeLinks)
        {
            Transform obsA = link.Key.Item1;
            Transform obsB = link.Key.Item2;

            if (obsA == null || obsB == null || Vector2.Distance(obsA.position, obsB.position) > linkDistanceThreshold)
            {
                linksToRemove.Add(link.Key);
                link.Value.SetActive(false); // 归还到对象池
            }
        }

        foreach (var key in linksToRemove)
        {
            activeLinks.Remove(key);
        }
    }

    void DiscoverNewLinks()
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            for (int j = i + 1; j < obstacles.Count; j++)
            {
                Transform obsA = obstacles[i];
                Transform obsB = obstacles[j];
                
                // 确保顺序一致，避免 (A,B) 和 (B,A) 被当成两个key
                if (obsA.GetInstanceID() > obsB.GetInstanceID())
                {
                    var temp = obsA;
                    obsA = obsB;
                    obsB = temp;
                }

                var linkKey = Tuple.Create(obsA, obsB);
                if (activeLinks.ContainsKey(linkKey))
                {
                    continue; // 连接已存在
                }

                if (Vector2.Distance(obsA.position, obsB.position) < linkDistanceThreshold)
                {
                    GameObject linkObject = GetFromPool();
                    if (linkObject != null)
                    {
                        // 正确的顺序：先激活，再初始化
                        linkObject.SetActive(true);
                        LinkController controller = linkObject.GetComponent<LinkController>();
                        controller.Initialize(obsA, obsB);
                        
                        activeLinks.Add(linkKey, linkObject);
                    }
                }
            }
        }
    }

    GameObject GetFromPool()
    {
        foreach (var link in linkPool)
        {
            if (!link.activeInHierarchy)
            {
                return link;
            }
        }
        // 如果池中不够用，可以动态创建或返回null
        return null;
    }
} 