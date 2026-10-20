using UnityEngine;
using Unity.AI.Navigation;

public class RuntimeNavMeshUpdater : MonoBehaviour
{
    public NavMeshSurface navMeshSurface;

    public void RebuildNavMesh()
    {
        if (navMeshSurface == null)
            return;

        navMeshSurface.BuildNavMesh();

        Debug.Log("NavMesh rebuilt.");
    }
}