public class ProximitySensorAdapter : IHudAdapter
{
    private readonly IProximitySensorProvider _source;
    private readonly ProximitySensorWidget _widget;

    public ProximitySensorAdapter(IProximitySensorProvider source, ProximitySensorWidget widget)
    {
        _source = source;
        _widget = widget;
    }

    public void Connect()
    {
        _source.OnProximityChanged += Apply;
        Apply(_source.IsDetected); // 부착 및 접속 시 기존 상태 적용
    }

    public void Disconnect()
    {
        _source.OnProximityChanged -= Apply;
        _widget.SetVisible(false);
    }

    private void Apply(bool isDetected) => _widget.SetVisible(isDetected);
}