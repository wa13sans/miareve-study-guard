# CLI bridge / CLI 연결

Study Guard 베타 1.1은 HTTP API 또는 전용 CLI 브리지를 연결할 수 있습니다. 임의의 대화형 CLI가 자동으로 호환되는 것은 아닙니다.

## 가장 쉬운 설정

Ollama를 실행하고 `qwen3-vl:2b-instruct` 모델을 설치한 다음, 앱 설정에서 **Ollama**를 선택하세요. Study Guard가 Ollama의 로컬 HTTP API로 이미지를 전달합니다. 터미널 창이 필요 없습니다.

CLI 경로를 쓰고 싶다면:

1. 동봉된 `MiareveAiBridge.exe`를 메인 EXE 옆에 둡니다.
2. **설정 → AI / API / CLI → CLI**를 선택합니다.
3. CLI 경로에 `MiareveAiBridge.exe`의 전체 경로를 입력합니다.
4. 고정 인수에 `--endpoint http://127.0.0.1:11434 --model qwen3-vl:2b-instruct`를 입력합니다.
5. 선택한 CLI 실행·이미지 전달 허용을 체크하고 **연결 테스트 → 저장**합니다.

동봉 브리지는 로컬 Ollama만 사용하며, 모델을 자체 포함하지 않습니다. 소스는 `src/AiBridge.cs`입니다.

## 다른 CLI 연결 규격

신뢰하는 `.exe`가 아래 규격을 구현해야 합니다. `.cmd`, `.bat`, PowerShell 및 명령 셸은 지원하지 않습니다. 다른 AI CLI가 자체 API 서버를 제공한다면 **OpenAI** 모드에서 해당 서버의 `/v1` 주소를 지정할 수 있습니다. 이름이 OpenAI여도 서버는 로컬일 수 있습니다.

- 표준입력: UTF-8 JSON 한 줄
- 표준출력: 아래 판단 JSON 객체만 출력
- 진단 메시지: 표준오류로 출력
- 종료 코드: 성공 0
- 타임아웃: 120초, 허용 해제·설정창·종료 시 실행 중인 직접 자식 프로세스 취소
- 화면 문자열은 명령행에 끼워 넣지 않습니다. 설정된 고정 인수와 JSON 표준입력을 분리합니다.

입력 예시:

```json
{"title":"Game VFX breakdown - YouTube","keywords":"VFX,MV,design","images":["BASE64_JPEG"],"instruction":"Classification policy supplied by Study Guard"}
```

실제 입력에는 저장하지 않은 활성 창 JPEG가 1~2장 들어갑니다. 이미지는 로컬 파일 경로가 아닙니다.

출력 예시:

```json
{"label":"study","confidence":0.93,"reason":"Game VFX reference / 게임 이펙트 참고 자료"}
```

`label`은 `study`, `leisure`, `unknown` 중 하나입니다. `confidence`는 0~1입니다. 0.75 미만이거나 JSON이 잘못되면 `unknown`으로 처리하며 이탈 시간을 누적하지 않습니다. 키·명령·도구 실행 요청은 처리하지 않습니다.

외부 CLI가 사용하는 네트워크·요금·로그 저장·자식 프로세스 동작은 해당 CLI의 설정에 따릅니다. 동봉 브리지를 제외한 임의 CLI의 보안이나 무료 사용을 보증하지 않습니다. API 키를 고정 CLI 인수에 넣지 마세요.

## English

Select **Ollama** for the simplest local HTTP connection. For a dedicated CLI, choose **CLI**, set the absolute path to `MiareveAiBridge.exe`, allow it, and test. The bundled bridge forwards UTF-8 stdin JSON to local Ollama and prints a decision JSON object to stdout. It requires an installed vision model.

Third-party interactive CLIs need an adapter implementing the JSON protocol above, or an OpenAI-compatible HTTP server with image support. Model responses are data only; Study Guard does not execute model-generated commands. Local Ollama has no API usage fees; remote services and third-party CLIs may charge separately.
