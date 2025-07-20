using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class BoundaryManager : MonoBehaviour
{
    public PhysicsMaterial2D bouncyMaterial;

    void Start()
    {
        EdgeCollider2D edgeCollider = GetComponent<EdgeCollider2D>();
        Camera mainCamera = Camera.main;

        Vector2[] edgePoints = new Vector2[5];

        // 获取屏幕四个角的世界坐标
        edgePoints[0] = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        edgePoints[1] = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, mainCamera.nearClipPlane));
        edgePoints[2] = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        edgePoints[3] = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, mainCamera.nearClipPlane));
        edgePoints[4] = edgePoints[0]; // 闭合边缘

        edgeCollider.points = edgePoints;

        // 应用高弹性物理材质
        if (bouncyMaterial != null)
        {
            edgeCollider.sharedMaterial = bouncyMaterial;
        }
    }
} 