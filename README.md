# ProjectFS-Multiplayer-2DMap — 3D 맵 적용 전 (2026-04-20 ~ 2026-09-09)

> **ProjectFS 개인 기여 코드 시리즈** — 팀 프로젝트 ProjectFS(비공개 저장소)에서 본인이 작성한 코드만 추출·재분류한 저장소입니다.
> 1. [ProjectFS-MonsterAI](https://github.com/Youngdu-Min/ProjectFS-MonsterAI) — 몬스터 AI (~2026-04-19)
> 2. [ProjectFS-Multiplayer-2DMap](https://github.com/Youngdu-Min/ProjectFS-Multiplayer-2DMap) — 탈출·Worm·생존자 추적·2D 절차적 맵 생성 (2026-04-20 ~ 09-09)
> 3. [ProjectFS-3DMap-Rendering](https://github.com/Youngdu-Min/ProjectFS-3DMap-Rendering) — 3D 맵 생성·LevelModule·Convolution 포스트 프로세싱 (2026-09-10 ~)

- 대상 기간: 3D 맵 적용 커밋 `edd796bd`(2026-09-10) 이전
- 기준 스냅샷: `a4a3e6e7` (2026-07-28, `MYD-feature-DetectSystem`, 이후 Develop 병합)
- 관련 브랜치: `MYD-feature-EscapeSystem`, `MYD-feature-Worm`, `MYD-feature-LevelGeneration`, `MYD-feature-DetectSystem`

## 추출 기준

- `git blame -M -C -C -w`(이동·복사 추적, 공백 무시) 결과에서 **해당 기간 본인 작성 라인이 파일의 50% 이상**인 파일만 포함
- 50% 미만으로 일부 수정만 한 파일은 포함하지 않고 아래 "부분 기여" 표에 라인 수만 기록
- 파일 내용은 기준 스냅샷 시점 원본 그대로이며, 폴더 구조만 기능별로 재분류
- 서드파티(NavMeshPlus, Backrooms 에셋)와 자동 생성 코드(`UserInputAction.cs`)는 제외
- 프로젝트 의존성(FishNet, UniTask, Odin Inspector 등)이 없어 단독 컴파일되지 않음 — 열람용

## 폴더 구조

| 폴더 | 내용 |
|---|---|
| `01_EscapeSystem` | 탈출 시스템. `Interaction/` 상호작용 베이스·잠금 상호작용(SyncVar, 서버 검증)·탈출문·상자·감지기, `Item/` 아이템 데이터·월드 아이템·탄창 스포너, `ObjectiveStateController` 목표 오브젝트 배치·계층 동기화, 디버깅 씬 |
| `02_Worm` | 플레이어 조작 몬스터 Worm. 상태(Idle/Move/DashPrepare/DashProgress/DashStun/Dead) FSM, 돌진·접촉 공격 무기 핸들러(ServerRpc), 애니메이션 핸들러, 몬스터 설정 |
| `03_SurvivorTracking` | 생존자 추적. 서버에서 추적 대상·지속 시간·동시 추적 수 관리 후 0.25초 주기 동기화, 경로 렌더러, 근접 감지 센서(ObserversRpc), `UI/` HUD 어댑터·프로바이더·위젯 |
| `04_LevelGeneration_2D` | 2D 절차적 맵 생성 (아래 상세) |
| `05_Shared` | 공용 유틸 — 판정 체커(Ray/Circle/Box 프리셋), 콜라이더 체커, RPC 활성화, 피격 깜빡임(MaterialPropertyBlock), `ITakeDamage` |

### 04_LevelGeneration_2D 상세

| 하위 폴더 | 내용 |
|---|---|
| `Core` | 시드 컨텍스트, 난수 소스 추상화(`IRandomSource`), 생성 진단 |
| `Grid` | 맵 그리드 / 셀 타입 |
| `Rooms` | 방 배치 |
| `Topology` | 방 거리 그래프 → 최소 신장 트리(MST) + Extra Edge로 순환 경로 최소 1개 보장 |
| `Corridors/Mandatory` | 문 후보 탐색, A* 필수 통로 생성, 통로 폭 확장 |
| `Corridors/Optional` | 선택 통로 — 합류 / 순환(최소 길이 강제) / 막다른 길(랜덤 워크) / 확장형 막다른 공간 |
| `Corridors/Cleanup` | 막다른 길 정리 |
| `Pathfinding` | 통로용 A* (Vazgriz DungeonPathfinder2D 골격 기반, 비용 함수 외부 주입 구조로 재작성) |
| `Placement` | 방 인접 그래프, 시작/목표 지점 배치 |
| `Validation` | Flood Fill 도달 가능성·셀 통계·단계별 요약 검증 |
| `Tilemap` | 타일맵 베이크 |
| `Runners` | 단계별 테스트 러너, 마스터 러너(실패 시 시드 변경 재시도 → 서버 확정 시드를 SyncVar로 동기화) |
| `Spike` | 초기 검증용 스파이크 코드·테스트 픽스처 |
| `MapSpawnHelper.cs` | 맵 스폰 보조 |

## 부분 기여 (복사 제외, 본인 작성 50% 미만)

| 원본 경로 (`Assets/_Project/02_Scripts/` 기준) | 기간 내 본인 라인 / 전체 |
|---|---|
| `00_Core/01_UpdataPublisher/UpdateDisposer.cs` | 2 / 17 |
| `02_Scenes/LevelGenerationScene.cs` | 12 / 72 |
| `02_Scenes/SurvivorDebuggingScene.cs` | 1 / 63 |
| `03_Gameflow/Item/MagazineItem.cs` | 19 / 87 |
| `03_Gameflow/Player/Monster/EntityHealth.cs` | 44 / 129 |
| `03_Gameflow/Player/Monster/WormLocomotion.cs` | 28 / 94 |
| `03_Gameflow/Player/Monster/WormNetworkHandler.cs` | 8 / 34 |
| `03_Gameflow/Player/Survivor.cs` | 31 / 231 |
| `03_Gameflow/Player/SurvivorFSM.cs` | 17 / 78 |
| `03_Gameflow/Player/SurvivorHealth.cs` | 12 / 120 |
| `03_Gameflow/Player/SurvivorInteractionHandler.cs` | 43 / 139 |
| `03_Gameflow/Player/SurvivorInventory.cs` | 69 / 192 |
| `03_Gameflow/Player/SurvivorLocomotion.cs` | 3 / 77 |
| `03_Gameflow/Player/SurvivorNetworkHandler.cs` | 13 / 57 |
| `03_Gameflow/Player/SurvivorProjectile.cs` | 4 / 118 |
| `04_UI/Hud/EntityHud.cs` | 3 / 24 |
| `04_UI/Hud/GameplayHudInstaller.cs` | 8 / 128 |
| `05_Animation/AnimationHandler.cs` | 1 / 68 |
| `05_Animation/AnimationStrings.cs` | 1 / 10 |
| `07_Data/PlayerManager.cs` | 95 / 208 |
| `07_Data/PlayerManager_Legacy.cs` | 2 / 144 |
| `Assets/_Project/09_Data/03_Input/InputHandler.cs` | 34 / 203 |
| `Assets/_Project/09_Data/03_Input/UserInputAction.cs` | 135 / 1119 |

> `UserInputAction.cs`는 Input System 자동 생성 코드라 기여도 집계 의미 없음

## 원본 경로 매핑

| 분류 경로 | 원본 경로 (`Assets/_Project/02_Scripts/` 기준) | 기간 내 본인 라인 / 전체 |
|---|---|---|
| `01_EscapeSystem/Interaction/InteractableBase.cs` | `03_Gameflow/Map/Interaction/InteractableBase.cs` | 64 / 111 |
| `01_EscapeSystem/Interaction/LockedInteractable.cs` | `03_Gameflow/Map/Interaction/LockedInteractable.cs` | 94 / 107 |
| `01_EscapeSystem/Interaction/EscapeDoor.cs` | `03_Gameflow/Map/Interaction/EscapeDoor.cs` | 35 / 41 |
| `01_EscapeSystem/Interaction/EscapeChest.cs` | `03_Gameflow/Map/Interaction/EscapeChest.cs` | 22 / 25 |
| `01_EscapeSystem/Interaction/InteractionDetector.cs` | `03_Gameflow/Player/InteractionDetector.cs` | 32 / 32 |
| `01_EscapeSystem/Item/ItemData.cs` | `03_Gameflow/Item/ItemData.cs` | 22 / 26 |
| `01_EscapeSystem/Item/WorldItem.cs` | `03_Gameflow/Item/WorldItem.cs` | 19 / 24 |
| `01_EscapeSystem/Item/MagazineItemSpawner.cs` | `03_Gameflow/Item/MagazineItemSpawner.cs` | 80 / 80 |
| `01_EscapeSystem/Item/RandomSprite.cs` | `03_Gameflow/Item/RandomSprite.cs` | 14 / 14 |
| `01_EscapeSystem/ObjectiveStateController.cs` | `03_Gameflow/Map/ObjectiveStateController.cs` | 370 / 385 |
| `01_EscapeSystem/EscapeDebuggingScene.cs` | `02_Scenes/EscapeDebuggingScene.cs` | 20 / 38 |
| `02_Worm/Worm.cs` | `03_Gameflow/Player/Monster/Worm.cs` | 107 / 204 |
| `02_Worm/WormFSM.cs` | `03_Gameflow/Player/Monster/WormFSM.cs` | 76 / 87 |
| `02_Worm/WormWeaponHandler.cs` | `03_Gameflow/Player/Monster/WormWeaponHandler.cs` | 120 / 183 |
| `02_Worm/WormAnimationHandler.cs` | `05_Animation/WormAnimationHandler.cs` | 11 / 13 |
| `02_Worm/MonsterConfig.cs` | `07_Data/MonsterConfig.cs` | 71 / 97 |
| `03_SurvivorTracking/EntityDetectionConfig.cs` | `03_Gameflow/Player/Monster/EntityDetectionConfig.cs` | 15 / 15 |
| `03_SurvivorTracking/EntityProximitySensor.cs` | `03_Gameflow/Player/Monster/EntityProximitySensor.cs` | 120 / 124 |
| `03_SurvivorTracking/EntityPathRenderer.cs` | `03_Gameflow/Player/Monster/EntityPathRenderer.cs` | 172 / 174 |
| `03_SurvivorTracking/EntityPathRevealController.cs` | `03_Gameflow/Player/Monster/EntityPathRevealController.cs` | 147 / 147 |
| `03_SurvivorTracking/UI/IProximitySensorProvider.cs` | `09_Etc/IProximitySensorProvider.cs` | 7 / 7 |
| `03_SurvivorTracking/UI/ProximitySensorProvider.cs` | `04_UI/Hud/Providers/ProximitySensorProvider.cs` | 25 / 25 |
| `03_SurvivorTracking/UI/ProximitySensorAdapter.cs` | `04_UI/Hud/Adapters/ProximitySensorAdapter.cs` | 18 / 25 |
| `03_SurvivorTracking/UI/ProximitySensorWidget.cs` | `04_UI/Hud/Widgets/ProximitySensorWidget.cs` | 11 / 11 |
| `04_LevelGeneration_2D/Core/IRandomSource.cs` | `09_Etc/IRandomSource.cs` | 4 / 8 |
| `05_Shared/RaycastChecker.cs` | `03_Gameflow/Player/RaycastChecker.cs` | 108 / 115 |
| `05_Shared/ColliderChecker.cs` | `03_Gameflow/Player/ColliderChecker.cs` | 27 / 38 |
| `05_Shared/RPCActivator.cs` | `03_Gameflow/Shared/RPCActivator.cs` | 10 / 10 |
| `05_Shared/SpriteBlink.cs` | `06_Effect/SpriteBlink.cs` | 57 / 57 |
| `05_Shared/ITakeDamage.cs` | `09_Etc/ITakeDamage.cs` | 4 / 4 |
| `04_LevelGeneration_2D/Core/GenerationDiagnostics.cs` | `LevelGeneration/Core/GenerationDiagnostics.cs` | 41 / 41 |
| `04_LevelGeneration_2D/Core/MapGenerationPass.cs` | `LevelGeneration/Core/MapGenerationPass.cs` | 9 / 9 |
| `04_LevelGeneration_2D/Core/MapSeedContext.cs` | `LevelGeneration/Core/MapSeedContext.cs` | 38 / 38 |
| `04_LevelGeneration_2D/Core/SystemRandomSource.cs` | `LevelGeneration/Core/SystemRandomSource.cs` | 24 / 24 |
| `04_LevelGeneration_2D/Corridors/Cleanup/DeadEndCleanup.cs` | `LevelGeneration/Corridors/Cleanup/DeadEndCleanup.cs` | 124 / 127 |
| `04_LevelGeneration_2D/Corridors/Cleanup/DeadEndCleanupConfig.cs` | `LevelGeneration/Corridors/Cleanup/DeadEndCleanupConfig.cs` | 7 / 7 |
| `04_LevelGeneration_2D/Corridors/Cleanup/DeadEndCleanupResult.cs` | `LevelGeneration/Corridors/Cleanup/DeadEndCleanupResult.cs` | 9 / 9 |
| `04_LevelGeneration_2D/Corridors/Mandatory/CorridorData.cs` | `LevelGeneration/Corridors/Mandatory/CorridorData.cs` | 8 / 10 |
| `04_LevelGeneration_2D/Corridors/Mandatory/CorridorWidthExpander.cs` | `LevelGeneration/Corridors/Mandatory/CorridorWidthExpander.cs` | 48 / 50 |
| `04_LevelGeneration_2D/Corridors/Mandatory/DoorCandidate.cs` | `LevelGeneration/Corridors/Mandatory/DoorCandidate.cs` | 8 / 8 |
| `04_LevelGeneration_2D/Corridors/Mandatory/DoorCandidateFinder.cs` | `LevelGeneration/Corridors/Mandatory/DoorCandidateFinder.cs` | 86 / 88 |
| `04_LevelGeneration_2D/Corridors/Mandatory/MandatoryCorridorConfig.cs` | `LevelGeneration/Corridors/Mandatory/MandatoryCorridorConfig.cs` | 11 / 11 |
| `04_LevelGeneration_2D/Corridors/Mandatory/MandatoryCorridorGenerator.cs` | `LevelGeneration/Corridors/Mandatory/MandatoryCorridorGenerator.cs` | 92 / 94 |
| `04_LevelGeneration_2D/Corridors/Mandatory/MandatoryCorridorResult.cs` | `LevelGeneration/Corridors/Mandatory/MandatoryCorridorResult.cs` | 8 / 8 |
| `04_LevelGeneration_2D/Corridors/Optional/CorridorTerminationType.cs` | `LevelGeneration/Corridors/Optional/CorridorTerminationType.cs` | 8 / 8 |
| `04_LevelGeneration_2D/Corridors/Optional/OptionalCorridorConfig.cs` | `LevelGeneration/Corridors/Optional/OptionalCorridorConfig.cs` | 25 / 25 |
| `04_LevelGeneration_2D/Corridors/Optional/OptionalCorridorData.cs` | `LevelGeneration/Corridors/Optional/OptionalCorridorData.cs` | 7 / 9 |
| `04_LevelGeneration_2D/Corridors/Optional/OptionalCorridorGenerator.cs` | `LevelGeneration/Corridors/Optional/OptionalCorridorGenerator.cs` | 255 / 257 |
| `04_LevelGeneration_2D/Corridors/Optional/OptionalCorridorResult.cs` | `LevelGeneration/Corridors/Optional/OptionalCorridorResult.cs` | 8 / 8 |
| `04_LevelGeneration_2D/Grid/MapCellType.cs` | `LevelGeneration/Grid/MapCellType.cs` | 1 / 1 |
| `04_LevelGeneration_2D/Grid/MapGrid.cs` | `LevelGeneration/Grid/MapGrid.cs` | 22 / 22 |
| `04_LevelGeneration_2D/MapSpawnHelper.cs` | `LevelGeneration/MapSpawnHelper.cs` | 42 / 44 |
| `04_LevelGeneration_2D/Pathfinding/DungeonPathfinder2D.cs` | `LevelGeneration/Pathfinding/DungeonPathfinder2D.cs` | 100 / 104 |
| `04_LevelGeneration_2D/Placement/RoomAdjacencyGraph.cs` | `LevelGeneration/Placement/RoomAdjacencyGraph.cs` | 42 / 42 |
| `04_LevelGeneration_2D/Placement/StartGoalConfig.cs` | `LevelGeneration/Placement/StartGoalConfig.cs` | 6 / 6 |
| `04_LevelGeneration_2D/Placement/StartGoalPlacer.cs` | `LevelGeneration/Placement/StartGoalPlacer.cs` | 74 / 77 |
| `04_LevelGeneration_2D/Placement/StartGoalResult.cs` | `LevelGeneration/Placement/StartGoalResult.cs` | 27 / 27 |
| `04_LevelGeneration_2D/Rooms/RoomData.cs` | `LevelGeneration/Rooms/RoomData.cs` | 19 / 19 |
| `04_LevelGeneration_2D/Rooms/RoomPlacementConfig.cs` | `LevelGeneration/Rooms/RoomPlacementConfig.cs` | 10 / 12 |
| `04_LevelGeneration_2D/Rooms/RoomPlacementResult.cs` | `LevelGeneration/Rooms/RoomPlacementResult.cs` | 9 / 9 |
| `04_LevelGeneration_2D/Rooms/RoomPlacer.cs` | `LevelGeneration/Rooms/RoomPlacer.cs` | 68 / 70 |
| `04_LevelGeneration_2D/Runners/DeadEndCleanupTestRunner.cs` | `LevelGeneration/Runners/DeadEndCleanupTestRunner.cs` | 77 / 77 |
| `04_LevelGeneration_2D/Runners/DoorCandidateSpikeRunner.cs` | `LevelGeneration/Runners/DoorCandidateSpikeRunner.cs` | 135 / 137 |
| `04_LevelGeneration_2D/Runners/MandatoryCorridorTestRunner.cs` | `LevelGeneration/Runners/MandatoryCorridorTestRunner.cs` | 177 / 179 |
| `04_LevelGeneration_2D/Runners/MapGenerationMasterRunner.cs` | `LevelGeneration/Runners/MapGenerationMasterRunner.cs` | 145 / 145 |
| `04_LevelGeneration_2D/Runners/MapGenerationResult.cs` | `LevelGeneration/Runners/MapGenerationResult.cs` | 9 / 9 |
| `04_LevelGeneration_2D/Runners/OptionalCorridorTestRunner.cs` | `LevelGeneration/Runners/OptionalCorridorTestRunner.cs` | 110 / 112 |
| `04_LevelGeneration_2D/Runners/StartGoalTestRunner.cs` | `LevelGeneration/Runners/StartGoalTestRunner.cs` | 89 / 89 |
| `04_LevelGeneration_2D/Runners/TilemapBakeTestRunner.cs` | `LevelGeneration/Runners/TilemapBakeTestRunner.cs` | 350 / 350 |
| `04_LevelGeneration_2D/Runners/ValidationTestRunner.cs` | `LevelGeneration/Runners/ValidationTestRunner.cs` | 84 / 84 |
| `04_LevelGeneration_2D/Spike/SpikeValidator.cs` | `LevelGeneration/Spike/SpikeValidator.cs` | 65 / 67 |
| `04_LevelGeneration_2D/Spike/TestRoomFixtures.cs` | `LevelGeneration/Spike/TestRoomFixtures.cs` | 72 / 74 |
| `04_LevelGeneration_2D/Tilemap/TilemapBaker.cs` | `LevelGeneration/Tilemap/TilemapBaker.cs` | 62 / 62 |
| `04_LevelGeneration_2D/Topology/MinimumSpanningTree.cs` | `LevelGeneration/Topology/MinimumSpanningTree.cs` | 57 / 57 |
| `04_LevelGeneration_2D/Topology/RoomDistanceGraphBuilder.cs` | `LevelGeneration/Topology/RoomDistanceGraphBuilder.cs` | 47 / 49 |
| `04_LevelGeneration_2D/Topology/RoomGraphEdge.cs` | `LevelGeneration/Topology/RoomGraphEdge.cs` | 22 / 22 |
| `04_LevelGeneration_2D/Topology/RoomGraphNode.cs` | `LevelGeneration/Topology/RoomGraphNode.cs` | 15 / 15 |
| `04_LevelGeneration_2D/Topology/RoomTopologyBuilder.cs` | `LevelGeneration/Topology/RoomTopologyBuilder.cs` | 99 / 99 |
| `04_LevelGeneration_2D/Topology/RoomTopologyConfig.cs` | `LevelGeneration/Topology/RoomTopologyConfig.cs` | 6 / 6 |
| `04_LevelGeneration_2D/Topology/RoomTopologyResult.cs` | `LevelGeneration/Topology/RoomTopologyResult.cs` | 10 / 10 |
| `04_LevelGeneration_2D/Validation/MapValidator.cs` | `LevelGeneration/Validation/MapValidator.cs` | 163 / 165 |
