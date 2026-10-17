using System;

public class ProximitySensorProvider : IProximitySensorProvider, IDisposable
{
    private readonly EntityProximitySensor _sensor;

    public bool IsDetected => _sensor != null && _sensor.IsDetected;
    public event Action<bool> OnProximityChanged;

    public ProximitySensorProvider(EntityProximitySensor sensor)
    {
        _sensor = sensor;
        if (_sensor == null) return;

        _sensor.OnProximityChanged += RaiseProximityChanged;
    }

    public void Dispose()
    {
        if (_sensor == null) return;
        _sensor.OnProximityChanged -= RaiseProximityChanged;
    }

    private void RaiseProximityChanged(bool isDetected) => OnProximityChanged?.Invoke(isDetected);
}