using FishNet.Object;

public class RPCActivator : NetworkBehaviour
{
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    public void RpcSetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}