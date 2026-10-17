using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ColliderChecker : MonoBehaviour
{
    public event Action<Collider2D> OnCollEnter;
    private Collider2D _coll;
    private CountdownTimer _activeTimer;
    private bool _initialized;

    public void SetActive(bool isTrue)
    {
        EnsureInitialized();
        _coll.enabled = isTrue;
    }
    public void ActiveDuringTime(float duration)
    {
        EnsureInitialized();
        _activeTimer.Reset(duration);
        _activeTimer.Start();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        _coll = GetComponent<Collider2D>();
        _activeTimer = new CountdownTimer(0);
        _activeTimer.OnTimerStart += () => SetActive(true);
        _activeTimer.OnTimerStop += () => SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        OnCollEnter?.Invoke(other);
    }
}
