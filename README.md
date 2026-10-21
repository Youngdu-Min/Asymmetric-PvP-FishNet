# Asymmetric PvP — Multiplayer Gameplay & 2D Procedural Map

## 시연 영상

[![Asymmetric 1v3 Survival PvP in Unity — FishNet Multiplayer & Procedural 2D Map](https://img.youtube.com/vi/6KWuzZ0zop4/maxresdefault.jpg)](https://www.youtube.com/watch?v=6KWuzZ0zop4)

## 프로젝트 소개

- **게임**: 백룸풍 실내 공간에서 괴물 1명과 생존자 3명이 대결하는 2D 탑다운 비대칭 서바이벌 PvP. 생존자는 제한 시간 안에 탈출해야 하고, 괴물은 이를 막아야 합니다.
- **개발**: 5인 팀 (기획 1 · 프로그래밍 2 · 아트 2) / 프로그래머로 참여 · 팀 프로젝트 코드명 ProjectFS
- **환경**: Unity 6 (URP), C#, FishNet (LAN 멀티플레이), UniTask
- **기간**: 2026.05 ~ 2026.08
- 이 저장소는 팀 프로젝트 중 **본인이 담당한 코드**만 발췌했습니다.

## 구현 범위

- **탈출 시스템**: 금속 조각을 모아 상자에서 열쇠를 만들고(1단계), 열쇠로 탈출구를 여는(2단계) 흐름. 서버가 판정하는 상호작용(한 명씩 점유, 피격 시 초기화)과 클라이언트 진행도 외삽, 목표 오브젝트 배치와 계층 동기화
- **괴물 캐릭터 Worm**: 플레이어가 조작하는 괴물의 FSM, 돌진·접촉 공격, 애니메이션 연동, 무력화·부활
- **생존자 추적 시스템**: 생존자가 잠긴 상호작용을 시작하면 그 생존자까지의 경로를 **괴물에게만** 노출(TargetRpc)하고 NavMesh로 경로 렌더링. 주변 생존자 감지 센서(해제 지연으로 깜빡임 방지)와 HUD
- **2D 절차적 맵 생성**: 방 배치 → 방 연결 구조(MST + 순환 경로 보장) → 필수 통로 → 선택 통로 → 막다른 길 정리 → 시작/목표 배치 → 검증 → 타일맵·NavMesh 베이크. 단계마다 독립된 난수를 쓰고, 맵 데이터 대신 시드만 서버에서 클라이언트로 동기화

## 코드 구성

| 폴더 | 설명 |
|---|---|
| `01_EscapeSystem` | `Interaction/`: 상호작용 베이스, 서버 검증과 SyncVar를 쓰는 잠금 상호작용, 탈출문, 상자. `Item/`: 아이템, 탄창 스포너. `ObjectiveStateController`: 목표 오브젝트 스폰과 계층 동기화. `EscapeDebuggingScene`: 탈출 흐름 디버그 씬 |
| `02_Worm` | Worm 상태(이동 · 돌진 준비/진행/경직 · 무력화) FSM, 돌진 타이머 시퀀스와 접촉 피해를 처리하는 무기 핸들러, 애니메이션 핸들러 |
| `03_SurvivorTracking` | 서버가 추적 대상·지속 시간·동시 추적 수를 관리하고 주기적으로 동기화합니다. 경로 렌더러, 근접 감지 센서가 있고, `UI/`에 HUD 위젯 연동이 있습니다. |
| `04_LevelGeneration_2D` | 2D 절차적 맵 생성 파이프라인 (아래 표) |
| `05_Shared` | 서버 전용 판정 체커(Ray / Circle / Box), 콜라이더 체커, 피격 깜빡임 효과, 늦게 접속해도 활성 상태가 맞춰지는 `RPCActivator`, `ITakeDamage` |

### 04_LevelGeneration_2D

| 폴더 | 설명 |
|---|---|
| `Rooms` | 방 배치 (버퍼 칸을 포함한 겹침 검사) |
| `Topology` | 방 거리 그래프로 최소 신장 트리(MST)를 만들고, 간선을 추가해 순환 경로를 최소 1개 보장 |
| `Corridors/Mandatory` | 문 후보 탐색, A\*로 필수 통로 생성, 통로 폭 확장 |
| `Corridors/Optional` | 선택 통로: 합류 · 순환(최소 길이 강제) · 막다른 길(랜덤 워크) · 확장형 막다른 공간 |
| `Corridors/Cleanup` | 막다른 길 정리 |
| `Pathfinding` | 통로 생성용 A\*. 비용 함수를 외부에서 주입하는 구조입니다(Vazgriz DungeonPathfinder2D 기반). |
| `Placement` | 방 인접 그래프, BFS를 두 번 돌려 그래프상 가장 먼 방을 시작/목표 후보로 배치 |
| `Validation` | Flood Fill로 모든 방에 도달 가능한지 검사하고 단계별 결과를 요약 |
| `Tilemap` | 생성 결과를 타일맵으로 베이크 |
| `Core` · `Grid` | 시드 컨텍스트, 난수 소스 추상화, 맵 그리드 |
| `Runners` | 단계별 테스트 러너. 마스터 러너는 생성에 실패하면 시드를 바꿔 다시 시도하고, 확정된 시드를 SyncVar로 동기화합니다. |

> 방 배치·MST·A\* 통로의 기본 골격은 Vazgriz의 절차적 던전 생성 글과 `DungeonPathfinder2D`를 참고했습니다.

## 기술 문서

내부 설계(네트워크 동기화 원칙, 맵 생성 파이프라인 단계별 알고리즘, 탈출 흐름, Worm 돌진 시퀀스, 생존자 추적)는 [기술 문서](docs/TechnicalDesign.md)에 정리했습니다.
