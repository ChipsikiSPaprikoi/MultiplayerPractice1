using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Сетевые статы")]
    public NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private static int _playerIndex = 0;
    private const float PLAYER_SPACING = 3f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
        
        Nickname.OnValueChanged += OnNicknameChanged;
        HP.OnValueChanged += OnHPChanged;
    }

    public override void OnNetworkDespawn()
    {
        Nickname.OnValueChanged -= OnNicknameChanged;
        HP.OnValueChanged -= OnHPChanged;
        base.OnNetworkDespawn();
        if (IsServer)
        {
            transform.position = new Vector3(_playerIndex++ * PLAYER_SPACING, 1f, 0f);
        }
        
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        if (IsServer && Nickname.Value.IsEmpty)
        {
            transform.position = new Vector3(_playerIndex++ * PLAYER_SPACING, 1f, 0f);
        }
        string safeNickname = string.IsNullOrWhiteSpace(nickname) 
            ? $"Player_{OwnerClientId}" 
            : nickname.Trim().Substring(0, Mathf.Min(30, nickname.Length));
            
        Nickname.Value = safeNickname;
    }
    
    private void OnNicknameChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        Debug.Log($"Ник изменен: {previous} -> {current}");
    }
    
    private void OnHPChanged(int previous, int current)
    {
        Debug.Log($"HP изменен: {previous} -> {current}");
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(int damage)
    {
        HP.Value = Mathf.Max(0, HP.Value - damage);
    }
}
