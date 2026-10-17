using UnityEngine;

public class ProximitySensorWidget : MonoBehaviour
{
    [SerializeField] private GameObject _root;

    public void SetVisible(bool visible)
    {
        if (_root != null) _root.SetActive(visible);
    }
}