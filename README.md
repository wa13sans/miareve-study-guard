# Miareve Study Guard — 베타 1.1

**제작자 Miareve · Windows · 무료 사용 / 유료 재배포 금지**

디자인 공부에서 벗어나면 화면 **정중앙에 큰 알림**을 띄워 주는 앱입니다. 한국어·영어, 저장되는 설정, 초 단위 타이머, 무료 로컬 비전 AI 연결을 지원합니다.

[메인 EXE 다운로드](https://github.com/wa13sans/miareve-study-guard/raw/refs/heads/main/MiareveStudyGuard.exe) · [CLI 브리지](https://github.com/wa13sans/miareve-study-guard/raw/refs/heads/main/MiareveAiBridge.exe) · [라이선스](LICENSE)

## 바로 사용하기

1. 기존 버전이 실행 중이면 **완전 종료**한 뒤 새 `MiareveStudyGuard.exe`를 실행합니다.
2. **설정 → 일반**에서 한국어/English와 알림 대기 시간을 정합니다. **0분 10초**처럼 입력할 수 있습니다. 최소 1초, 최대 24시간입니다.
3. **공부 단어** 탭에서 여러 줄로 단어를 편집하고 **저장·적용**합니다. 다음 실행에도 유지됩니다.
4. 메인 창에서 **화면 보기 허용 → 공부 시작**을 누릅니다. 매 실행마다 다시 허용해야 합니다.
5. **알림 테스트**로 중앙 팝업을 바로 확인하세요. 소리를 꺼도 팝업은 표시됩니다.
6. AI를 아직 준비하지 않았다면 설정에서 **Off**를 선택해 기존 앱·단어·입력 활동 규칙을 사용할 수 있습니다.

Windows 10/11 및 .NET Framework 4.8 환경을 대상으로 합니다. 내부 파일 버전은 `1.1.0.0`입니다. 코드 서명은 없는 베타 실행 파일입니다.

## 무료 AI 연결 — 선택 사항

**앱에는 AI 연결·설치 안내·모델 다운로드 기능만 포함됩니다.** Ollama와 대용량 모델을 사용자 동의 없이 설치하거나 다운로드하지 않습니다.

1. **설정 → AI / API / CLI → Ollama 공식 설치**를 눌러 [공식 Windows 다운로드](https://ollama.com/download/windows)에서 설치합니다.
2. Ollama를 실행합니다.
3. 앱의 서버 주소를 `http://127.0.0.1:11434`, 모델을 `qwen3-vl:2b-instruct`로 둡니다.
4. **추천 모델 받기**를 누르고 다운로드를 승인합니다. 공식 모델 파일은 약 **1.9GB**입니다. 저장 공간과 메모리를 추가로 사용합니다.
5. **연결 테스트 → 저장·적용**합니다. 테스트는 앱이 만든 작은 합성 이미지를 사용하며 실제 화면을 촬영하지 않습니다.
6. 메인 창에서 화면을 허용하고 공부 시작을 누릅니다.

[Ollama 공식 GitHub](https://github.com/ollama/ollama) · [Qwen3-VL 2B 공식 모델](https://ollama.com/library/qwen3-vl:2b-instruct) · [Ollama 비전 API](https://docs.ollama.com/capabilities/vision)

로컬 모델 추론에는 **API 사용료나 유료 토큰 구매가 필요 없습니다.** 모델 내부에서는 토큰 연산을 하지만 PC 자원을 사용합니다. 인터넷 다운로드·전기 비용은 별개입니다. 성능은 PC 메모리/GPU에 따라 달라집니다. 로컬 Ollama 모드는 루프백 주소만 허용하고 `-cloud` 모델을 거부합니다. 사용자가 로컬 서버 자체를 원격 서비스 프록시로 바꾸는 경우 그 서버 설정까지 앱이 보증하지는 않습니다.

공식 출처만 안내하며 모델이 생성한 명령을 실행하지 않습니다. 어떤 소프트웨어도 악성코드·취약점이 절대 없다고 보증할 수는 없습니다. 출처가 불분명한 수정 실행 파일은 사용하지 마세요. Ollama·Qwen 모델은 본 앱에 재배포하지 않으며 각각의 원래 라이선스가 적용됩니다.

## YouTube 영상과 Shorts 판단

브라우저의 **활성 창 제목과 화면 이미지 1~2장**을 AI에 전달합니다. Shorts도 같은 기준으로 판단합니다.

- **공부 허용 대상**: 그래픽 디자인, 모션그래픽, 타이포그래피, After Effects/Illustrator/Blender/Unity 학습, MV·BGA, VFX·이펙트·애니메이션 참고 및 분석 영상
- **게임 관련이어도 허용**: 게임 VFX 쇼케이스, 이펙트 제작 과정, 시네마틱·애니메이션·MV 참고 자료
- **놀이 대상**: 디자인과 관련 없는 오락·밈·브이로그·일반 게임 플레이 및 오락성 Shorts
- **판단 보류**: 광고, 읽기 어려운 화면, 의도가 불분명한 게임 장면, 낮은 확신도, 연결 실패

이 앱은 사용자의 실제 학습 의도를 확정하지 못합니다. 음성이나 전체 영상·자막을 다운로드해서 분석하는 방식도 아닙니다. 화면에 근거가 부족하면 `unknown`으로 보류합니다. 판단 보류 중에는 이탈 시간을 누적하지 않습니다. 특히 게임 플레이와 VFX 참고 시청은 같은 화면일 수 있으므로 오판 가능성이 있습니다.

화면 샘플은 약 2초 간격, AI 요청은 기본 20초 간격입니다. 큰 장면 변화가 있으면 더 빨리 재검사를 시도하지만 최소 5초의 요청 간격을 둡니다. AI 서버 응답이 느리면 실제 검사는 더 늦어집니다. **알림 대기 시간은 이탈로 판단한 뒤부터** 누적되며 AI 분석 시간이 추가될 수 있습니다. 모델 응답 확신도는 정확도가 보증된 확률이 아닙니다.

`Off` 모드는 기존처럼 관련 앱·단어와 최근 입력/화면 변화로만 판단합니다. 게임·디자인 영상 내용 구분에는 비전 AI 연결이 필요합니다.

## 중앙 알림·숨기기·완전 종료

알림은 사용 중인 모니터의 가운데에 최상위 창으로 표시됩니다. 자동으로 20초 뒤 사라지지 않으며 **지금 할게 / 10분만 쉴게 / X**로 닫습니다. 화면 잠금·정지·설정창·종료 시에는 알림을 닫습니다. 독점 전체화면 앱 위에는 Windows 동작에 따라 표시되지 않을 수 있으니 창 모드/테두리 없는 창 모드에서 사용하세요.

한국어 문구는 다음처럼 순환합니다. 영어 UI에서는 영어 문구가 나옵니다.

- 공부 안할거냐?
- 공부좀 하세요
- 공부 안함?
- 디자인하러 가라
- 꿈을 키워라
- 키프레임 하나만 더!

**Ctrl+Alt+H**: 메인 창 숨기기/다시 표시. **Ctrl+Alt+Q**: 완전 종료. 다른 앱이 단축키를 사용 중이면 버튼 또는 트레이 메뉴를 사용합니다. 숨기기는 감시를 계속하고, 완전 종료는 화면 분석·AI 요청·트레이 아이콘을 종료합니다. 메인 창 X도 완전 종료입니다.

기존의 10분 휴식·자동 재개, 일시 정지, 알림 테스트, 트레이 메뉴, 화면 허용 해제, 잠금/절전 대응과 세션 공부 시간 표시를 유지합니다.

## 설정과 캐시

설정은 `%LOCALAPPDATA%\Miareve\StudyGuard\settings.json`에 저장됩니다. 공부 단어, 언어, 시간, AI 설정이 저장되지만 **화면 접근 허용은 저장하지 않습니다**. 공부 시간은 현재 세션 기준입니다. 설정창을 열면 화면 분석을 중단하고 진행 중인 AI 요청을 취소합니다.

**설정 → 캐시·개인정보 → 캐시 전체 삭제**로 삭제할 수 있습니다.

- 기본 유효 기간 30분, 최대 500건. TTL은 1~1440분으로 변경 가능
- 캐시에는 해시·분류·확신도·만료 시각만 저장
- 창 제목 원문, 이미지, AI 설명, API 키는 캐시에 저장하지 않음
- 화면·창 제목·모델·공부 단어가 다르면 다른 캐시 키 사용
- 캐시 삭제 시 진행 중인 응답이 이전 캐시를 다시 채우지 않도록 취소·세대 검사를 수행
- 공부 단어와 설정은 유지. Ollama가 별도로 저장하는 **모델 파일은 삭제하지 않음**

이 앱은 화면 JPEG를 디스크에 저장하지 않습니다. 활성 창이 보이는 영역을 메모리에서 캡처하므로, 그 영역 위에 겹친 창이나 알림도 포함될 수 있습니다. 캡처를 AI에 전달하기 전에 최대 변 길이 960픽셀로 축소합니다.

API 키는 Windows 사용자 계정의 DPAPI로 암호화해서 저장합니다. 암호화할 수 없으면 평문으로 대신 저장하지 않고 저장 실패를 알려줍니다.

## 외부 API와 CLI

- **OpenAI** 모드: 이미지 입력을 지원하는 OpenAI 호환 API. 주소는 `/v1`까지 입력합니다. 예: 로컬 서버 `http://127.0.0.1:1234/v1`.
- 외부 서버는 **HTTPS + 외부 전송 허용 체크**가 필요합니다. 이미지·창 제목·공부 단어를 해당 서버에 전송하며, 서버의 요금과 개인정보 정책이 적용됩니다. 외부 API가 무료라고 보장하지 않습니다.
- **CLI** 모드: 신뢰하는 전용 JSON 브리지 `.exe` 경로 및 고정 인수를 설정합니다. CLI 실행 허용을 따로 체크합니다. 임의의 대화형 CLI를 바로 연결하는 기능은 아닙니다.
- 동봉된 `MiareveAiBridge.exe`는 CLI 입력을 로컬 Ollama HTTP API로 이어 줍니다. 모델을 포함하지 않습니다.

자세한 규격과 연결 방법은 [CLI-BRIDGE.md](CLI-BRIDGE.md)를 참고하세요. AI 결과는 정해진 판단 JSON으로만 처리하고 화면에 등장한 지시를 명령으로 실행하지 않습니다.

## 빌드와 검증

추가 개발 패키지 설치 없이 Windows의 .NET Framework C# 컴파일러를 사용합니다.

```powershell
.\build.ps1
.\test.ps1
```

빌드 결과: `MiareveStudyGuard.exe`, 선택용 `MiareveAiBridge.exe`. 화면 접근 DLL은 메인 EXE에 포함합니다.

베타 1.1 검증: 기본 규칙 15개 및 통합 검사 34개 통과. 연결 검사는 로컬 가짜 응답 서버와 합성 이미지를 사용하여 Ollama 형식, OpenAI 형식, CLI 왕복, 취소, 설정/캐시 저장과 중앙 알림의 20초 초과 유지 등을 확인했습니다. 테스트 환경에 Windows 사용자 프로필이 로드되지 않아 DPAPI 암호화 왕복 검사 1개는 생략했습니다. **실제 Qwen 모델 다운로드 및 영상 분류 정확도 검사는 수행하지 않았습니다.**

## License

**무료 사용 가능 · 유료 재배포 및 재판매 금지**

[Miareve 무료 사용 · 유료 재배포 금지 라이선스 1.0](LICENSE)을 적용합니다. 원본·수정본의 무료 사용·수정·무료 공유는 가능하며 제작자와 라이선스를 유지해야 합니다. 원본·수정본·일부 코드를 재포장해 유료로 판매하거나 결제·유료 구독·의무 후원 조건으로 배포할 수 없습니다. 이 앱을 사용하며 제작한 별도의 영상·디자인 결과물은 판매할 수 있습니다. 본 앱의 맞춤 라이선스는 OSI 승인 오픈소스 라이선스가 아닙니다.

## English quick start

Run `MiareveStudyGuard.exe` → Settings → General → English → Save. Configure seconds/minutes, edit persistent study words, then allow screen access for this session and press Start. Visual reminders appear in the center of your active monitor and remain until dismissed. Sound is optional.

For free local vision, install official Ollama, run it, click **Get default model** (~1.9GB), then **Test connection** and save. No runtime or model is silently installed. Design lessons, MV/BGA, animation, and game VFX references are eligible study content; unrelated entertainment is leisure. Ambiguous screens are held as unknown. Sampled screenshots cannot establish your actual intent; classification accuracy has not been measured with a real model in this release.

Ctrl+Alt+H hides/shows the app; Ctrl+Alt+Q exits. Settings → Cache · Privacy clears classification cache without deleting saved words or Ollama models. Local mode has no API usage fees. Optional remote APIs/third-party CLIs have their own costs and privacy policies.

Copyright © 2026 Miareve.
