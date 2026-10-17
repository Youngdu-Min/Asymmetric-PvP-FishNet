using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

public class MapGenerationMasterRunner : NetworkBehaviour
{
    public static event Action<MapGenerationResult> OnMapGenerated;
    public static MapGenerationResult LastResult { get; private set; }

    [Header("── Seed Settings ───────────")]
    [Tooltip("맵 생성 기본 시드. randomizeSeedOnRun 체크 시 무시됨")]
    [SerializeField] private int baseSeed = 0;

    [Tooltip("실행마다 서버에서 랜덤 시드 결정")]
    [SerializeField] private bool randomizeSeedOnRun = false;

    [Tooltip("파이프라인 실패 시 최대 재시도 횟수")]
    [SerializeField] private int maxRetryCount = 10;

    private readonly SyncVar<int> _syncedSeed = new SyncVar<int>();

    [Header("── Pipeline Runners ──────────")]
    [SerializeField] private MandatoryCorridorTestRunner mandatoryRunner;
    [SerializeField] private OptionalCorridorTestRunner optionalRunner;
    [SerializeField] private DeadEndCleanupTestRunner cleanupRunner;
    [SerializeField] private StartGoalTestRunner startGoalRunner;
    [SerializeField] private ValidationTestRunner validationRunner;
    [SerializeField] private TilemapBakeTestRunner bakeRunner;

    private bool m_hasGenerated = false;

    public override void OnStartServer()
    {
        base.OnStartServer();

        int selectedSeed = randomizeSeedOnRun ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : baseSeed;
        if (selectedSeed == 0) selectedSeed = 1;

        _syncedSeed.Value = selectedSeed;
        GeneratePipelineOnce(selectedSeed);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _syncedSeed.OnChange += OnSeedChanged;

        if (_syncedSeed.Value != 0 && !m_hasGenerated)
        {
            GeneratePipelineOnce(_syncedSeed.Value);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _syncedSeed.OnChange -= OnSeedChanged;
    }

    private void OnSeedChanged(int prev, int next, bool asServer)
    {
        if (asServer) return;
        if (next != 0)
        {
            GeneratePipelineOnce(next);
        }
    }

    [ContextMenu("Run Full Pipeline")]
    public void RunAllInEditor()
    {
        int seed = randomizeSeedOnRun ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : baseSeed;
        m_hasGenerated = false;
        GeneratePipelineOnce(seed);
    }

    private void GeneratePipelineOnce(int initialSeed)
    {
        if (m_hasGenerated) return;

        int currentSeed = initialSeed;
        bool isSuccess = false;

        // 성공할 때까지 시드를 변경하며 최대 maxRetryCount회 재시도
        for (int attempt = 0; attempt < maxRetryCount; attempt++)
        {
            Debug.Log($"[MasterRunner] Attempt {attempt + 1}/{maxRetryCount} with Seed: {currentSeed}");

            // 1~4단계
            mandatoryRunner.RunPipeline(currentSeed);

            // 5~8단계
            optionalRunner.Run();
            cleanupRunner.Run();
            startGoalRunner.Run();
            validationRunner.Run();

            // Start/Goal 배치 검증
            if (startGoalRunner.LastResult != null && startGoalRunner.LastResult.Success)
            {
                isSuccess = true;
                break; // 성공 시 루프 탈출
            }

            Debug.LogWarning($"[MasterRunner] Seed {currentSeed} failed at Start/Goal placement. Reason: {startGoalRunner.LastResult?.FailureReason}. Retrying next seed...");

            // 실패 시 시드 변경 (overflow 방지)
            currentSeed = (currentSeed == int.MaxValue) ? int.MinValue : currentSeed + 1;
        }

        if (!isSuccess)
        {
            Debug.LogError($"[MasterRunner] Map generation failed after {maxRetryCount} attempts!");
            return;
        }

        m_hasGenerated = true;

        // 서버권한에서 재시도로 최종 확정된 시드로 SyncVar 갱신 (클라이언트 동기화)

        if ((!Application.isPlaying || IsServerInitialized) && _syncedSeed.Value != currentSeed)
        {
            _syncedSeed.Value = currentSeed;
        }

        var result = new MapGenerationResult
        {
            Rooms = mandatoryRunner.Rooms,
            Grid = mandatoryRunner.Grid,
            StartGoalResult = startGoalRunner.LastResult,
            PropBakeSeed = mandatoryRunner.SeedContext.GetSource(MapGenerationPass.DeadEndCleanup).Next()
        };

        LastResult = result;

        // 9단계 Tilemap & NavMesh Bake
        bakeRunner.Bake();

        // 맵 생성 완결 이벤트 발행
        OnMapGenerated?.Invoke(result);
        Debug.Log($"[MasterRunner] Map Generation Successfully Finished with Seed: {currentSeed}");
    }
}