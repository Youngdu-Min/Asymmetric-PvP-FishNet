using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

public class EntityPathRevealController : NetworkBehaviour
{
    private class TrackInfo
    {
        public Survivor Target;
        public CountdownTimer DurationTimer;
    }

    private MonsterConfig _config;
    private WormFSM _fsm;
    private EntityPathRenderer _pathRenderer;

    private readonly List<TrackInfo> _activeTracks = new();
    private CountdownTimer _pathUpdateTimer;

    public void Initialize(MonsterConfig config, WormFSM fsm, EntityPathRenderer pathRenderer)
    {
        _config = config;
        _fsm = fsm;
        _pathRenderer = pathRenderer;

        _pathUpdateTimer = new CountdownTimer(_config.PathUpdateInterval);
        _pathUpdateTimer.OnTimerStop += UpdateAndSendTargets;
    }

    [Server]
    public void StartTracking(Survivor survivor)
    {
        if (survivor == null || _config == null) return;

        // 1. 이미 추적 중인 동일 생존자면 지속 시간 갱신
        TrackInfo existing = _activeTracks.Find(t => t.Target == survivor);
        if (existing != null)
        {
            existing.DurationTimer.Reset(_config.PathDuration);
            existing.DurationTimer.Start();
            return;
        }

        // 2. 동시 추적 최대 수 제한
        if (_activeTracks.Count >= _config.MaxTrackCount) return;

        TrackInfo newTrack = new TrackInfo
        {
            Target = survivor,
            DurationTimer = new CountdownTimer(_config.PathDuration)
        };

        // 지속 시간 종료 시 추적 제거
        newTrack.DurationTimer.OnTimerStop += () => RemoveTrack(newTrack);

        // 생존자 사망 이벤트 구독 (사망 시 즉시 제거)
        SurvivorHealth targetHealth = survivor.GetComponent<SurvivorHealth>();
        if (targetHealth != null)
        {
            System.Action deadHandler = null;
            deadHandler = () =>
            {
                targetHealth.OnDead -= deadHandler;
                RemoveTrack(newTrack);
            };
            targetHealth.OnDead += deadHandler;
        }

        _activeTracks.Add(newTrack);
        newTrack.DurationTimer.Start();

        if (!_pathUpdateTimer.IsRunning)
        {
            _pathUpdateTimer.Reset();
            _pathUpdateTimer.Start();
        }
    }

    [Server]
    public void OnSurvivorEscaped(Survivor survivor)
    {
        TrackInfo track = _activeTracks.Find(t => t.Target == survivor);
        if (track != null) RemoveTrack(track);
    }

    // 0.25초 주기마다 추적 중인 생존자들의 Transform 참조 목록만 클라이언트로 동기화
    [Server]
    private void UpdateAndSendTargets()
    {
        if (_activeTracks.Count == 0)
        {
            TargetClearPaths(Owner);
            return;
        }

        _pathUpdateTimer.Reset();
        _pathUpdateTimer.Start();

        // 웜 무력화(Dead) 상태 시 추적 표시 차단
        if (_fsm != null && _fsm.Current == WormState.Dead)
        {
            TargetClearPaths(Owner);
            return;
        }

        List<Transform> targetTransforms = new List<Transform>();
        foreach (var track in _activeTracks)
        {
            if (track.Target != null)
            {
                targetTransforms.Add(track.Target.transform);
            }
        }

        TargetSyncTargets(Owner, targetTransforms);
    }

    [Server]
    private void RemoveTrack(TrackInfo track)
    {
        track.DurationTimer.Dispose();
        _activeTracks.Remove(track);
    }

    [TargetRpc]
    private void TargetSyncTargets(NetworkConnection conn, List<Transform> targets)
    {
        _pathRenderer?.SetTargetTransforms(targets);
    }

    [TargetRpc]
    private void TargetClearPaths(NetworkConnection conn)
    {
        _pathRenderer?.ClearPaths();
    }

    public void Cleanup()
    {
        _pathUpdateTimer?.Dispose();
        foreach (var track in _activeTracks)
        {
            track.DurationTimer?.Dispose();
        }
        _activeTracks.Clear();
    }
}