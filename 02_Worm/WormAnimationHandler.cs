using FishNet.Component.Animating;
using System;

public class WormAnimationHandler : AnimationHandler
{
    public event Action<DirectionResolver.EightDir> OnDirChanged;
    public WormAnimationHandler(NetworkAnimator animator) : base(animator) { }
    public override void SetDirection(DirectionResolver.EightDir dir)
    {
        base.SetDirection(dir);
        OnDirChanged?.Invoke(dir);
    }
}