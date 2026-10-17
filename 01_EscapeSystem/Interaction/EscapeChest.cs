using UnityEngine;

public class EscapeChest : LockedInteractable
{
    [SerializeField] private int _targetCount = 5; // N1
    private int _currentCount = 0;

    public override string ActionGuideText => "탈출구 열쇠 제작";
    public override string ProgressLabelText => "열쇠 제작 중";

    protected override void OnComplete()
    {
        _currentCount++;
        ResetInteraction();
        ConsumeItem();

        if (_currentCount >= _targetCount)
        {
            _isOpened.Value = true;
        }
    }

    public void ResetCount()
        => _currentCount = 0;
}