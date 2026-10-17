using FishNet.Object;
using UnityEngine;

public enum WormState { Idle, Move, DashPrepare, DashProgress, DashStun, Dead }

public class WormFSM : NetworkBehaviour, ICharacterFSM<WormState>
{
    private WormState _current = WormState.Idle;
    private WormNetworkHandler _networkHandler;
    private WormLocomotion _locomotion;

    public void Initialize(WormNetworkHandler networkHandler, WormLocomotion locomotion)
    {
        _networkHandler = networkHandler;
        _locomotion = locomotion;
    }

    public void OnHitAnimationEnd()
    {
        if (!_networkHandler.IsLocalOwner) return;
        bool isMoving = _locomotion.MoveInput.sqrMagnitude > 0.01f;
        TransitionTo(isMoving ? WormState.Move : WormState.Idle);
    }

    public WormState Current => _current;

    // 기획서 매핑: 돌진 준비, 돌진 진행, 돌진 경직, 무력화(사망) 시 이동 불가
    public bool IsMovementBlocked =>
        _current == WormState.DashPrepare ||
        _current == WormState.DashProgress ||
        _current == WormState.DashStun ||
        _current == WormState.Dead;

    public bool IsDashBlocked =>
        _current == WormState.DashPrepare ||
        _current == WormState.DashProgress ||
        _current == WormState.DashStun ||
        _current == WormState.Dead;

    // 바라보는 방향(커서 조준) 변경이 막히는 상태 플래그
    public bool IsAimRotationBlocked =>
        _current == WormState.DashPrepare ||
        _current == WormState.DashProgress ||
        _current == WormState.DashStun;

    public void Tick(Vector2 moveInput)
    {
        if (IsMovementBlocked) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        if (isMoving && _current == WormState.Idle)
            TransitionTo(WormState.Move);
        else if (!isMoving && _current == WormState.Move)
            TransitionTo(WormState.Idle);
    }

    public void TransitionTo(WormState next)
    {
        if (!IsOwner) return;

        if (IsServerInitialized) // 호스트인 경우 즉시 변경
        {
            if (!CanTransition(_current, next)) return;
            _current = next;
        }
        else // 클라이언트인 경우 서버에 상태 변경을 요청
        {
            _current = next; // 내 화면에서 먼저 변경 (예측)
            CmdServerTransitionTo(next);
        }
    }

    [ServerRpc]
    private void CmdServerTransitionTo(WormState newState)
    {
        _current = newState; // 서버가 값을 승인하면 다른 원격 클라이언트들에게 동기화됨
    }

    public void ForceTransitionTo(WormState next) => _current = next;

    private bool CanTransition(WormState from, WormState to)
    {
        if (from == to) return false;
        if (from == WormState.Dead && to != WormState.Idle) return false; // 부활할 때만 Dead 탈출 가능
        return true;
    }
}