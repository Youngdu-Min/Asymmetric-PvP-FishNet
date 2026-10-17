using System;

public interface IProximitySensorProvider : IHudProvider
{
    bool IsDetected { get; }
    event Action<bool> OnProximityChanged;
}