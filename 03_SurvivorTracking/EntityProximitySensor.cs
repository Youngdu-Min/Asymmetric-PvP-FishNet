using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;

public class EntityProximitySensor : NetworkBehaviour
{
    private MonsterConfig _config;
    private WormFSM _fsm;

    public event Action<bool> OnProximityChanged;

    private Coroutine _checkCoroutine;
    private CountdownTimer _releaseTimer;
    private bool _isDetected;
    private bool _isInitialized;

    public bool IsDetected => _isDetected;

    public void Initialize(MonsterConfig config, WormFSM fsm)
    {
        _config = config;
        _fsm = fsm;

        _releaseTimer = new CountdownTimer(_config.ProximityReleaseDelay);
        _releaseTimer.OnTimerStop += () => ChangeState(false);

        _isInitialized = true;
        if (IsServerInitialized) TryStartCheckLoop();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_isInitialized) TryStartCheckLoop();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        StopCheckLoop();
    }

    private void TryStartCheckLoop()
    {
        if (_checkCoroutine != null || !IsServerInitialized || !_isInitialized) return;

        _checkCoroutine = StartCoroutine(ProximityCheckLoop());
    }

    private void StopCheckLoop()
    {
        if (_checkCoroutine == null) return;

        StopCoroutine(_checkCoroutine);
        _checkCoroutine = null;
    }

    private IEnumerator ProximityCheckLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(_config.ProximityUpdateInterval);

        while (IsServerInitialized && _isInitialized)
        {
            PerformProximityCheck();
            yield return wait;
        }
    }

    private void PerformProximityCheck()
    {
        if (_fsm != null && _fsm.Current == WormState.Dead)
        {
            if (_isDetected) ChangeState(false);
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(transform.position, _config.ProximityRadius, _config.SurvivorLayer);
        bool hasSurvivor = hit != null;

        if (hasSurvivor)
        {
            _releaseTimer.Stop();
            ChangeState(true);
            return;
        }

        if (_isDetected && !_releaseTimer.IsRunning)
        {
            _releaseTimer.Reset(_config.ProximityReleaseDelay);
            _releaseTimer.Start();
        }
    }

    private void ChangeState(bool newState)
    {
        if (_isDetected == newState) return;

        _isDetected = newState;
        ObserversNotifyProximity(_isDetected);
    }

    [ObserversRpc]
    private void ObserversNotifyProximity(bool isDetected)
    {
        _isDetected = isDetected; // 클라이언트 로컬 상태 갱신
        OnProximityChanged?.Invoke(isDetected);
    }

    public void Cleanup()
    {
        _isInitialized = false;
        StopCheckLoop();
        _releaseTimer?.Dispose();
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;

        Gizmos.color = _isDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _config.ProximityRadius);
    }
}