using UnityEngine;
using Unity.Netcode;

public class NetworkTreeState : NetworkBehaviour
{
    [Header("References")]
    public Transform tree;

    [Header("Growth")]
    public float growthSpeed = 0.5f;
    public float maxHeight = 28f;

    private NetworkVariable<float> networkHeight =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        if (tree == null)
        {
            Debug.LogError(
                "NetworkTreeState: Tree reference is missing."
            );
            return;
        }

        if (IsServer)
        {
            networkHeight.Value =
                tree.localScale.y;
        }

        ApplyHeight(networkHeight.Value);

        networkHeight.OnValueChanged +=
            OnHeightChanged;
    }

    public override void OnNetworkDespawn()
    {
        networkHeight.OnValueChanged -=
            OnHeightChanged;
    }

    void Update()
    {
        if (!IsSpawned ||
            !IsServer ||
            tree == null)
            return;

        // لا تنمو الشجرة أثناء اختيار اللاعبين
        if (GameStateManager.Instance == null)
            return;

        if (!GameStateManager.Instance.IsPlaying())
            return;

        if (networkHeight.Value >= maxHeight)
            return;

        networkHeight.Value =
            Mathf.Min(
                networkHeight.Value +
                growthSpeed *
                Time.deltaTime,
                maxHeight
            );
    }

    void OnHeightChanged(
        float previousHeight,
        float newHeight)
    {
        ApplyHeight(newHeight);
    }

    void ApplyHeight(float height)
    {
        if (tree == null)
            return;

        Vector3 scale =
            tree.localScale;

        float difference =
            height - scale.y;

        scale.y = height;
        tree.localScale = scale;

        tree.position +=
            Vector3.up *
            (difference * 0.5f);
    }
}