# [Project Name: R.A.T #200]

> **"작은 쥐가 되어 거대한 공포로부터 탈출하라"**
>
> 3D 1인칭 시점의 퍼즐 어드벤처 & 스텔스 호러 게임

<img width="1920" height="1080" alt="스크린샷 2026-01-03 175449" src="https://github.com/user-attachments/assets/6cc81146-dd55-4883-b99f-e749b8520afd" />

## About The Project
* **프로젝트 명:** R.A.T #200
* **개발 기간:** 2025.09.01 ~ 2026.01.03
* **개발 인원:** 4명
* **장르:** 3D 퍼즐 어드벤처 / 스텔스 호러
* **플랫폼:** PC (Windows)
* **개발 환경:** Unity 6, C#

## Summary
거대한 인간 연구원의 감시를 피해, 연구실 안에 늘어진 작은 틈과 도구들을 활용하여 퍼즐을 풀고 자유를 찾아 떠나는 한 실험쥐의 이야기.

## Contributors

<table width="100%">
  <tr>
    <td align="center" width="25%">
      <a href="https://github.com/koreansongaji">
        <img src="https://github.com/koreansongaji.png" width="100" style="border-radius: 50%;">
      </a>
      <br>
      <b><a href="https://github.com/koreansongaji"><b>koreansongaji</b></a><br>
        PM<br>Programming
    </td>
    <td align="center" width="25%">
      <a href="https://github.com/waguu07">
        <img src="https://github.com/waguu07.png" width="100" style="border-radius: 50%;">
      </a>
      <br>
      <b><a href="https://github.com/waguu07"><b>waguu07</b></a><br>
        Design
    </td>
    <td align="center" width="25%">
      <a href="https://github.com/afterglowss">
        <img src="https://github.com/afterglowss.png" width="100" style="border-radius: 50%;">
      </a>
      <br>
      <b><a href="https://github.com/afterglowss"><b>afterglowss</b></a><br>
        Programming
    </td>
    <td align="center" width="25%">
      <a href="https://github.com/Mulgyeol03">
        <img src="https://github.com/Mulgyeol03.png" width="100" style="border-radius: 50%;">
      </a>
      <br>
      <b><a href="https://github.com/Mulgyeol03"><b>Mulgyeol03</b></a><br>
        Art
    </td>
  </tr>
</table>

## Key Features

### 1. 마이크로 줌 (Micro-Zoom) 상호작용 시스템
* 일반적인 1인칭 시점과 달리, 특정 사물(실험 도구, 퍼즐 등)과 상호작용 시 **카메라가 줌인되며 정밀 조작 모드**로 전환됩니다.

### 2. 연구원 AI (The Researcher)
* 플레이어를 감시하는 거대 인간 적(Enemy) 시스템입니다.
* **상태 머신(State Machine):** `Idle` ↔ `Searching` ↔ `Focusing` ↔ `Capture` 상태를 오가며 플레이어를 압박합니다.
* **감지 시스템:** 소음(Noise System)에 반응하고, 시야각 내에 플레이어가 들어오면 사망합니다.
* **공포 연출:** 플레이어 발각 시 숨소리 → 암전 → 사망 사운드 → 현장 확인으로 이어지는 시네마틱한 게임 오버 연출을 구현했습니다.

### 3. 물리 기반 퍼즐 & 오브젝트
* **파괴 시스템:** DinoFracture 에셋을 활용하여, 특정 이벤트(폭발, 충돌) 발생 시 비커나 나무판자가 조각나며 깨지는 리얼한 연출을 구현했습니다.
* **사다리 시스템:** 흩어진 사다리 발판을 모아 설치하고, 높이에 따라 동적으로 상호작용할 수 있는 이동 시스템입니다.
* **드래그 & 드롭:** 3D 공간에서 마우스 드래그를 통해 물체를 옮기거나 조작할 때, 카메라 시점에 맞춰 정확하게 움직이도록 구현했습니다.

### 4. 몰입감 있는 컷신 연출
* **Cinemachine**과 **DOTween**을 적극 활용하여, 케이지가 열리는 오프닝부터 벤트를 통해 탈출하는 엔딩까지 부드러운 카메라 전환과 애니메이션을 연출했습니다.

## Tech Stack

* **Engine:** Unity
* **Language:** C#
* **Libraries & Assets:**
    * **VolFX:** 흑백 연출 및 Outline 생성 포스트 프로세싱
    * **Cinemachine:** 역동적인 카메라 연출 및 줌인/줌아웃 트랜지션
    * **DOTween:** UI 애니메이션 및 오브젝트의 부드러운 움직임 제어
    * **DinoFracture:** 실시간 오브젝트 파괴 및 파편 시뮬레이션
    * **TextMeshPro:** 고해상도 텍스트 렌더링

## Screenshots

| 연구실 | 화학 실험 퍼즐 |
|:---:|:---:|
| <img width="1920" height="1080" alt="스크린샷 2026-01-03 183352" src="https://github.com/user-attachments/assets/d3aa8ddf-d762-4a24-ac71-b84b47ff31f8" /> | <img width="1920" height="1080" alt="스크린샷 2026-01-03 183444" src="https://github.com/user-attachments/assets/89191c27-daae-450d-b936-8489c0fe7817" /> |
| **이곳에서 탈출하는 것이 당신의 목적입니다** | **폭발물을 제조하여 길을 뚫으세요** |

| 와이어 퍼즐 | 연구원 조우 |
|:---:|:---:|
| <img width="1920" height="1080" alt="스크린샷 2026-01-03 183605" src="https://github.com/user-attachments/assets/99150552-50c7-490e-8af5-bc48266569bf" /> | <img width="1920" height="1080" alt="스크린샷 2026-01-03 183510" src="https://github.com/user-attachments/assets/544a715c-9af9-44f2-8bdb-c99730aeaff0" /> |
| **전선을 연결해 장치를 작동시키세요** | **연구원의 감시를 피해 숨으세요** |

## Trouble Shooting
