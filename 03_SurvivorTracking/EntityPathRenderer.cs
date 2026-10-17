using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EntityPathRenderer : MonoBehaviour, IFixedUpdateObserver
{
    [SerializeField] private LineRenderer _lineRendererPrefab;
    [SerializeField] private Transform _linesParent;

    private readonly List<LineRenderer> _linePool = new();
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    private NavMeshPath _sharedNavPath;
    private readonly List<Transform> _targetTransforms = new();

    private float _baseAlpha = 0.7f;
    private float _updateInterval = 0.25f;
    private float _timer;

    private FixedUpdateDisposer _fixedUpdateDisposer;

    private void Awake()
    {
        _sharedNavPath = new NavMeshPath();
        _propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        _fixedUpdateDisposer = new FixedUpdateDisposer(this);
    }

    private void OnDisable()
    {
        _fixedUpdateDisposer?.Dispose();
        _fixedUpdateDisposer = null;
    }

    public void Initialize(MonsterConfig config)
    {
        if (config == null) return;

        _updateInterval = config.PathUpdateInterval;
        _linesParent = _linesParent ?? transform;

        ClearPool();

        for (int i = 0; i < config.MaxTrackCount; i++)
        {
            LineRenderer line = Instantiate(_lineRendererPrefab, _linesParent);
            line.gameObject.SetActive(false);
            _linePool.Add(line);
        }
    }

    public void SetTargetTransforms(List<Transform> targets)
    {
        _targetTransforms.Clear();
        if (targets != null) _targetTransforms.AddRange(targets);

        if (_targetTransforms.Count == 0)
        {
            ClearPaths();
            return;
        }

        UpdatePaths();
    }

    private void UpdatePaths()
    {
        Vector3 currentWormPos = transform.position;
        _timer = 0f;

        for (int i = 0; i < _linePool.Count; i++)
        {
            Transform target = (i < _targetTransforms.Count) ? _targetTransforms[i] : null;

            if (target == null)
            {
                _linePool[i].gameObject.SetActive(false);
                continue;
            }

            Vector3 startPos = new Vector3(currentWormPos.x, currentWormPos.y, 0f);
            Vector3 targetPos = new Vector3(target.position.x, target.position.y, 0f);

            bool calcSuccess = NavMesh.CalculatePath(startPos, targetPos, NavMesh.AllAreas, _sharedNavPath);
            bool isComplete = _sharedNavPath.status == NavMeshPathStatus.PathComplete;

            if (!calcSuccess || !isComplete)
            {
                _linePool[i].gameObject.SetActive(false);
                continue;
            }

            RenderSinglePath(_linePool[i], _sharedNavPath.corners);
            SetLineAlpha(_linePool[i], _baseAlpha);
        }
    }

    public void ObserverFixedUpdate(float fixedDt)
    {
        print("ObserverFixedUpdate");
        _timer += fixedDt;
        float alpha = Mathf.Clamp01(_baseAlpha - (_timer / _updateInterval));

        foreach (LineRenderer line in _linePool)
        {
            if (!line.gameObject.activeSelf) continue;
            SetLineAlpha(line, alpha);
        }
    }

    private void RenderSinglePath(LineRenderer line, Vector3[] corners)
    {
        if (corners == null || corners.Length == 0)
        {
            line.gameObject.SetActive(false);
            return;
        }

        line.positionCount = corners.Length;
        for (int j = 0; j < corners.Length; j++)
        {
            Vector3 corner = corners[j];
            corner.z = 0f;
            line.SetPosition(j, corner);
        }

        line.gameObject.SetActive(true);
    }

    private void SetLineAlpha(LineRenderer line, float alpha)
    {
        line.GetPropertyBlock(_propBlock);

        bool hasBaseColor = line.sharedMaterial?.HasProperty(BaseColorPropertyId) ?? false;
        Color baseColor = hasBaseColor ? line.sharedMaterial.GetColor(BaseColorPropertyId) : Color.white;
        baseColor.a = alpha;
        print($"Alpha {alpha}");

        _propBlock.SetColor(BaseColorPropertyId, baseColor);
        line.SetPropertyBlock(_propBlock);
    }

    public void ClearPaths()
    {
        _targetTransforms.Clear();
        _timer = 0f;

        foreach (LineRenderer line in _linePool)
        {
            line?.gameObject.SetActive(false);
        }
    }

    private void ClearPool()
    {
        ClearPaths();
        foreach (LineRenderer line in _linePool)
        {
            if (line != null) Destroy(line.gameObject);
        }
        _linePool.Clear();
    }

    private void OnDestroy()
    {
        _fixedUpdateDisposer?.Dispose();
        _fixedUpdateDisposer = null;
    }
}