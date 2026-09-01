using UnityEngine;

public class TreeLevelManager : MonoBehaviour
{
    [Header("Tree")]
    public Transform tree;

    [Header("Levels")]
    public GameObject[] levels;
    public float[] unlockHeights;

    [Header("Paths")]
    public SpiralConnector spiralConnector;

    [Header("Navigation")]
    public RuntimeNavMeshUpdater navMeshUpdater;

    private bool[] unlocked;

    void Start()
    {
        unlocked = new bool[levels.Length];

        for (int i = 0; i < levels.Length; i++)
        {
            bool shouldBeActive =
                i == 0;

            levels[i].SetActive(
                shouldBeActive
            );

            unlocked[i] =
                shouldBeActive;
        }
    }

    void Update()
    {
        if (tree == null)
            return;

        float currentHeight =
            tree.localScale.y;

        for (int i = 1;
             i < levels.Length;
             i++)
        {
            if (!unlocked[i] &&
                i < unlockHeights.Length &&
                currentHeight >=
                unlockHeights[i])
            {
                UnlockLevel(i);
            }
        }
    }

    void UnlockLevel(int index)
    {
        // =========================
        // فتح المستوى
        // =========================

        levels[index].SetActive(true);

        unlocked[index] = true;

        // =========================
        // فتح الممر المؤدي إليه
        // =========================

        if (spiralConnector != null)
        {
            spiralConnector
                .SetSectionUnlocked(
                    index,
                    true
                );
        }

        Debug.Log(
            "Level " +
            (index + 1) +
            " unlocked with its paths!"
        );

        // =========================
        // تحديث الملاحة
        // =========================

        if (navMeshUpdater != null)
        {
            navMeshUpdater
                .RebuildNavMesh();
        }
    }
}