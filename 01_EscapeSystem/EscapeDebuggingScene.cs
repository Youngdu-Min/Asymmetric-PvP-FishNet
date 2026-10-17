using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EscapeDebuggingScene : AbstractScene
{
    protected override int SceneIdx => -1;

    [SerializeField] private InputHandler _inputHandler;
    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private ProjectilePoolingManager _projectilePoolingManager;
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private Tilemap _tilemap;
    private SurvivorInteractionHandler _survivorInteractionHandler;

    protected override void BindObjects() { }

    protected async override UniTask InitializeObjects()
    {
        _inputHandler.Init();
    }

    protected async override UniTask CreateObjects()
    {
        _playerManager.Initialize(_playerSpawner, _tilemap);
        _projectilePoolingManager.Initialize();
    }

    protected override void PrepareGame() { }

    protected override UniTask BeginGame() => UniTask.CompletedTask;

    private void OnDestroy()
    {
        _playerManager.Dispose();
        _projectilePoolingManager.Clear();
    }
}
