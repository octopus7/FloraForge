# Unity Visual Capture Workflow

이 문서는 FloraForge 식생 결과물을 Codex가 직접 확인하기 위한 캡쳐 절차를 기록한다.
여러 방식이 실패했으므로, 다음 작업에서는 아래의 성공 경로를 우선 사용한다.

## 결론

성공한 방식은 원본 Unity 프로젝트를 직접 캡쳐하지 않고, 캡쳐 전용 프로젝트 복사본을 만든 뒤 Unity batchmode에서 `FloraForgeCaptureUtility.CaptureDemoScene`을 실행하는 방식이다.

성공 산출물:

- `D:\github\FloraForge\captures\flora-overview.png`
- `D:\github\FloraForge\captures\flora-vines.png`
- `D:\github\FloraForge\captures\flora-ground.png`

관련 파일:

- `D:\github\FloraForge\FloraForgeUnity\Assets\FloraForge\Editor\FloraForgeCaptureUtility.cs`
- `D:\github\FloraForge\.gitignore`

로컬 산출물은 Git에 포함하지 않는다:

- `D:\github\FloraForge\captures\`
- `D:\github\FloraForge\FloraForgeCaptureProject\`

## 성공 절차

1. 캡쳐 전용 프로젝트 복사본을 만든다.

중요: `Copy-Item -LiteralPath (Join-Path $s '*')`는 와일드카드가 확장되지 않아 `Assets`가 비는 문제가 있었다. 반드시 `-Path`를 사용한다.

```powershell
$src = "D:\github\FloraForge\FloraForgeUnity"
$dst = "D:\github\FloraForge\FloraForgeCaptureProject"

foreach ($name in @("Assets", "Packages", "ProjectSettings")) {
    $s = Join-Path $src $name
    $d = Join-Path $dst $name
    New-Item -ItemType Directory -Force -Path $d | Out-Null
    Copy-Item -Path (Join-Path $s "*") -Destination $d -Recurse -Force
}
```

2. Unity batchmode로 캡쳐 유틸을 실행한다.

이 명령은 GUI/Unity 프로세스를 실행하므로 Codex sandbox에서는 `require_escalated`가 필요하다.

```powershell
$exe = "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe"
$args = @(
    "-batchmode",
    "-quit",
    "-projectPath", "D:\github\FloraForge\FloraForgeCaptureProject",
    "-executeMethod", "FloraForge.FloraForgeCaptureUtility.CaptureDemoScene",
    "-floraCaptureDir", "D:\github\FloraForge\captures",
    "-logFile", "D:\github\FloraForge\captures\unity-capture-copy.log"
)

$p = Start-Process -FilePath $exe -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
"EXIT=$($p.ExitCode)"
Get-ChildItem -LiteralPath "D:\github\FloraForge\captures" -Force |
    Select-Object Name,Mode,Length,LastWriteTime
```

정상 결과:

```text
EXIT=0
flora-ground.png
flora-overview.png
flora-vines.png
unity-capture-copy.log
```

3. 생성된 PNG를 `view_image`로 확인한다.

확인 대상:

- `D:\github\FloraForge\captures\flora-overview.png`
- `D:\github\FloraForge\captures\flora-vines.png`
- `D:\github\FloraForge\captures\flora-ground.png`

## 캡쳐 유틸 동작

`FloraForgeCaptureUtility.CaptureDemoScene`는 다음을 수행한다.

1. `Assets/FloraForge/Scenes/TavernVegetationDemo.unity`를 연다.
2. `FloraForgeVegetationGenerator`를 찾아 `Regenerate()`를 실행한다.
3. 임시 카메라를 생성한다.
4. 세 시점으로 `RenderTexture` 렌더링을 수행한다.
5. `Texture2D.ReadPixels()` 후 PNG로 저장한다.

현재 캡쳐 시점:

- `flora-overview.png`: 전체 배치 확인
- `flora-vines.png`: 덩굴/잎 회전 확인
- `flora-ground.png`: 꽃, 풀, 하단 식생 확인

## 실패했던 방식

다음 방식은 다시 우선 시도하지 않는다.

### Unity 창 직접 캡쳐

Win32 `GetWindowRect`와 `CopyFromScreen`으로 Unity 창을 캡쳐하려 했으나, Codex 세션에서 Unity 프로세스의 `MainWindowHandle`이 계속 `0`으로 나왔다.

증상:

```text
Unity window not found
MainWindowHandle : 0
MainWindowTitle  :
```

결론: 현재 환경에서는 Unity 창 핸들 기반 캡쳐가 불안정하다.

### 원본 프로젝트 batchmode 실행

원본 프로젝트 `D:\github\FloraForge\FloraForgeUnity`에 대해 batchmode 캡쳐를 실행했으나 return code `1`로 바로 종료됐다.

실패 명령 패턴:

```powershell
Unity.exe -batchmode -quit `
  -projectPath "D:\github\FloraForge\FloraForgeUnity" `
  -executeMethod FloraForge.FloraForgeCaptureUtility.CaptureDemoScene
```

로그에는 구체적인 컴파일 오류 없이 다음처럼 종료됐다.

```text
Successfully changed project path to: D:\github\FloraForge\FloraForgeUnity
Exiting without the bug reporter. Application will terminate with return code 1
```

추정 원인: 원본 프로젝트가 이미 열린 Unity 에디터/프로젝트 락 상태와 충돌할 수 있다.

결론: 원본 프로젝트에 직접 batchmode를 붙이지 말고 캡쳐 전용 복사본을 사용한다.

### `-nographics` batchmode

`-nographics`를 붙이면 그래픽 컨텍스트 없이 렌더링해야 하므로 캡쳐 목적에 맞지 않는다. 실제로 라이선스/그래픽 초기화 assertion 이후 return code `1`로 종료됐다.

실패 로그 일부:

```text
Assertion failed on expression: 'SUCCEEDED(hr)'
Application will terminate with return code 1
```

결론: 렌더 캡쳐에는 `-nographics`를 쓰지 않는다.

### 열린 에디터 요청 파일 감시

`Temp/flora-capture-request.txt`를 만들고 열린 에디터의 `InitializeOnLoad` watcher가 캡쳐하도록 시도했다.
현재 Codex 세션에서는 열린 에디터 이벤트 루프가 감지되지 않아 캡쳐 폴더가 생성되지 않았다.

증상:

```text
NO_CAPTURE_DIR
```

결론: 메뉴 실행이 가능한 수동 상황에서는 쓸 수 있지만, Codex 자동 루프에서는 batchmode 복사본 방식이 더 안정적이다.

### 잘못된 복사 명령

처음 캡쳐 전용 프로젝트를 만들 때 `Copy-Item -LiteralPath (Join-Path $s '*')`를 사용해서 `Assets`가 비었다.
그 결과 `executeMethod`의 클래스를 찾지 못했다.

실패 로그:

```text
executeMethod class 'FloraForgeCaptureUtility' could not be found.
Argument was -executeMethod FloraForge.FloraForgeCaptureUtility.CaptureDemoScene
```

결론: 캡쳐 프로젝트 복사에는 반드시 `Copy-Item -Path (Join-Path $s "*")`를 사용한다.

## 반복 개선 루프

다음 순서로 진행한다.

1. 원본 프로젝트 코드 수정
2. C# 컴파일 체크
3. 캡쳐 전용 프로젝트에 `Assets`, `Packages`, `ProjectSettings` 복사
4. batchmode 캡쳐 실행
5. `captures/*.png`를 `view_image`로 확인
6. 문제점 기록 후 다시 코드 수정

캡쳐 복사본은 원본이 아니므로, 코드 수정은 항상 `D:\github\FloraForge\FloraForgeUnity`에 한다.

## 주의 사항

- `FloraForgeCaptureProject`는 로컬 캡쳐용 복사본이다. 직접 수정하지 않는다.
- 캡쳐 PNG는 `captures/` 아래에 생성하며 Git에 포함하지 않는다.
- Unity 실행은 GUI 프로세스이므로 Codex에서는 escalation 승인이 필요할 수 있다.
- 캡쳐 유틸 수정 후에는 복사본을 다시 만들어야 최신 코드가 반영된다.
- 작업 종료 시 원본 Unity 에디터가 꺼져 있으면 다시 열어둔다.
