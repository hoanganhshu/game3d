# FPS Arena – Unity 6 (6000.6.4f1, Android)

Game FPS 3D cho Android được dựng **hoàn toàn bằng code**: không kéo thả, không chỉnh Inspector,
không tạo scene/prefab bằng tay. Built-in Render Pipeline, legacy Input/Touch, đồ họa primitive,
âm thanh tổng hợp bằng `AudioClip.Create`.

## Cấu trúc

```
Packages/manifest.json            # ai.navigation 2.0.14, ugui 2.6.0 (có sẵn TextMeshPro) + built-in modules
Assets/
  Editor/
    ProjectBootstrapper.cs        # Tools > Setup Everything  |  FPSGame.Editor.ProjectBootstrapper.SetupAll
    BuildScript.cs                # Tools > Build Android APK |  FPSGame.Editor.BuildScript.BuildAndroid
  Scripts/
    Core/     GameManager, SaveManager, AudioManager, SceneLoader, SceneBootstrap, BootController,
              GameEvents, BackButtonManager, InputMode, ObjectPool, PooledObject, PoolManager,
              FPSCounter, NumberStrings, SceneNames, IDamageable
    Data/     WeaponData (SO), GameAssets (SO), LevelDefinition, LevelDatabase, LevelResult
    Player/   PlayerInput, PlayerController, PlayerHealth
    Weapons/  Weapon, WeaponManager
    Enemy/    Enemy, EnemyHealthBar
    Level/    LevelManager, LevelBuilder, Pickup, ScoreCalculator
    UI/       UIFactory, SafeArea, TouchZone, VirtualJoystick, TouchButton, MobileControls,
              HUDController, InGameUI, SettingsPanel, MainMenuUI, LevelSelectUI
```

`SetupAll()` sinh ra: `Assets/Art` (texture tròn + caro), `Assets/Materials`, `Assets/Data`
(Pistol/Rifle), `Assets/Prefabs` (Player, Enemy, Boss, BulletImpact, BloodImpact, HealthPickup,
AmmoPickup), `Assets/Resources` (Managers.prefab, GameAssets.asset), `Assets/Scenes`
(Boot, MainMenu, LevelSelect, Level1–3) và Build Settings đúng thứ tự đó.

## Yêu cầu

* Unity **6000.6.4f1** + module **Android Build Support** (kèm Android SDK & NDK Tools, OpenJDK).
  SetupAll chạy được khi chưa có module Android; chỉ bước build APK là cần module này.
* Game dùng legacy Input. Nếu project đang để Active Input Handling = *Input System (New)*,
  SetupAll tự chuyển sang *Both*; khởi động lại Editor (hoặc chạy lệnh batchmode tiếp theo) để áp dụng.

Nếu bạn cài phiên bản Unity 6 khác, thay `6000.6.4f1` trong các lệnh dưới đây, và nếu cần thì sửa
version của `com.unity.ai.navigation` / `com.unity.ugui` trong manifest theo bản đi kèm editor.

---

## Windows (PowerShell)

```powershell
$UNITY   = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
$PROJECT = "D:\Projects\FPSGame"
$REPO    = "E:\Personal\Projects\game3d"   # thư mục chứa README này
```

`| Out-Null` bắt PowerShell chờ Unity chạy xong; `$LASTEXITCODE` là mã thoát (0 = thành công).

**0. (Nếu chưa có) cài module Android qua Unity Hub CLI**

```powershell
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install-modules --version 6000.6.4f1 -m android android-sdk-ndk-tools android-open-jdk-17.0.18+8 -cm
```

**(a) Tạo project mới bằng Unity CLI rồi chép mã nguồn vào**

```powershell
& $UNITY -batchmode -quit -createProject $PROJECT -logFile "$REPO\create.log" | Out-Null
Copy-Item -Recurse -Force "$REPO\Assets\*" "$PROJECT\Assets\"
Copy-Item -Force "$REPO\Packages\manifest.json" "$PROJECT\Packages\manifest.json"
Remove-Item -Force -ErrorAction SilentlyContinue "$PROJECT\Packages\packages-lock.json"
```

> Cách khác: dùng luôn thư mục repo làm project (`$PROJECT = $REPO`). Lần mở đầu Unity tự tạo
> `ProjectSettings/` và `Library/`.

**(b) Chạy SetupAll ở batchmode**

```powershell
& $UNITY -batchmode -quit -projectPath $PROJECT -buildTarget Android -executeMethod FPSGame.Editor.ProjectBootstrapper.SetupAll -logFile "$PROJECT\setup.log" | Out-Null; "Exit code: $LASTEXITCODE"
```

**(c) Build APK ở batchmode** (tự gọi SetupAll trước)

```powershell
& $UNITY -batchmode -quit -projectPath $PROJECT -buildTarget Android -executeMethod FPSGame.Editor.BuildScript.BuildAndroid -logFile "$PROJECT\build.log" | Out-Null; "Exit code: $LASTEXITCODE"
```

APK nằm ở `$PROJECT\Builds\Android\FPSGame.apk`.

**Cài lên máy** (bật USB debugging trên điện thoại):

```powershell
$ADB = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
& $ADB install -r "$PROJECT\Builds\Android\FPSGame.apk"
& $ADB shell monkey -p com.myname.fpsgame -c android.intent.category.LAUNCHER 1
```

---

## macOS (Terminal / zsh)

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$HOME/Projects/FPSGame"
REPO="$HOME/Projects/game3d"   # thư mục chứa README này
```

**0. (Nếu chưa có) cài module Android**

```bash
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install-modules --version 6000.6.4f1 -m android android-sdk-ndk-tools android-open-jdk-17.0.18+8 -cm
```

**(a) Tạo project mới rồi chép mã nguồn vào**

```bash
"$UNITY" -batchmode -quit -createProject "$PROJECT" -logFile "$REPO/create.log"
cp -R "$REPO/Assets/." "$PROJECT/Assets/"
cp "$REPO/Packages/manifest.json" "$PROJECT/Packages/manifest.json"
rm -f "$PROJECT/Packages/packages-lock.json"
```

**(b) Chạy SetupAll**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -buildTarget Android -executeMethod FPSGame.Editor.ProjectBootstrapper.SetupAll -logFile "$PROJECT/setup.log"; echo "Exit code: $?"
```

**(c) Build APK**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -buildTarget Android -executeMethod FPSGame.Editor.BuildScript.BuildAndroid -logFile "$PROJECT/build.log"; echo "Exit code: $?"
```

**Cài lên máy**

```bash
ADB="/Applications/Unity/Hub/Editor/6000.6.4f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
"$ADB" install -r "$PROJECT/Builds/Android/FPSGame.apk"
"$ADB" shell monkey -p com.myname.fpsgame -c android.intent.category.LAUNCHER 1
```

Lệnh rút gọn khi đứng trong thư mục project (giống ví dụ trong đề bài):

```bash
Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod FPSGame.Editor.BuildScript.BuildAndroid -logFile build.log
```

---

## Chạy trên giả lập Android Studio

APK chỉ có **ARM64** (Unity 6.6 không còn build x86_64 cho Android). Giả lập x86_64 từ
**Android 11 (API 30) trở lên** chạy được APK này nhờ bộ dịch ARM có sẵn trong system image.
Đã thử trên AVD `Medium_Phone_API_36.1` (Google Play, x86_64).

**1. Khởi động giả lập với GPU thật và tắt Vulkan** (bắt buộc – xem ghi chú bên dưới):

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\emulator\emulator.exe" -avd Medium_Phone_API_36.1 -gpu host -feature -Vulkan
```

macOS:

```bash
~/Library/Android/sdk/emulator/emulator -avd Medium_Phone_API_36.1 -gpu host -feature -Vulkan
```

Xem tên AVD có sẵn bằng `emulator -list-avds`.

**2. Cài và mở game** (khi giả lập đã boot xong):

```powershell
$ADB = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
& $ADB install -r "Builds\Android\FPSGame.apk"
& $ADB shell monkey -p com.myname.fpsgame -c android.intent.category.LAUNCHER 1
```

Hoặc kéo-thả file APK vào cửa sổ giả lập.

**3. Các hộp thoại có thể gặp lần đầu**

* *"Your device does not match the hardware requirements"* → bấm **CONTINUE**
  (Unity cảnh báo vì CPU giả lập là x86_64 chạy qua bộ dịch ARM).
* *"Bluetooth keeps stopping"* / *"System UI isn't responding"* → lỗi của chính system image,
  bấm **Close app** / **Wait**.

**Điều khiển trong giả lập:** chuột đóng vai ngón tay – kéo ở nửa trái để di chuyển (joystick),
kéo ở nửa phải để xoay camera, bấm các nút FIRE / RELOAD / JUMP / SWAP. Muốn hai ngón cùng lúc
(vừa chạy vừa xoay) thì phải dùng máy thật.

**Ghi chú – vì sao phải dùng các cờ trên:**

| Chế độ giả lập | Kết quả |
|---|---|
| `-gpu host` (mặc định, có Vulkan) | Unity khởi tạo Vulkan qua bộ dịch ARM → **cả giả lập bị crash và tự khởi động lại** |
| `-gpu swiftshader_indirect` | Không có Vulkan, OpenGL ES < 3.1 → *"Unable to initialize the Unity Engine Graphics API"* |
| `-gpu host -feature -Vulkan` | Unity chuyển sang OpenGL ES 3 trên GPU thật → **chạy được** |

Màu sắc trên giả lập đang bị lệch (trời xanh và sàn xám hiện ra ngả hồng/tím), nhiều khả năng do lớp
dịch đồ họa của giả lập – cần kiểm tra lại trên máy thật. Hiệu năng giả lập (dịch ARM + RAM 2 GB) thấp hơn nhiều so với điện thoại
thật, nên đừng dùng nó để đánh giá FPS.

---

## Chạy thử trong Editor

Sau SetupAll, scene **Boot** được mở sẵn – bấm Play. Scene nào cũng có `SceneBootstrap`, nên có thể
Play trực tiếp Level1/2/3; Managers sẽ tự được tạo.

| Hành động | Bàn phím + chuột (Editor) | Cảm ứng (Android) |
|---|---|---|
| Di chuyển | WASD / mũi tên | Joystick trái (nổi theo ngón tay) |
| Xoay camera | Chuột (con trỏ bị khóa) | Vuốt nửa phải màn hình / kéo nút FIRE |
| Bắn | Chuột trái (Rifle: giữ) | Nút FIRE |
| Nạp đạn | R | Nút RELOAD |
| Nhảy | Space | Nút JUMP |
| Đổi súng | Q / lăn chuột / 1, 2 | Nút SWAP |
| Pause / Back | Esc | Nút II / nút Back của Android |
| Bật/tắt giao diện cảm ứng | F2 (thử UI cảm ứng bằng chuột) | – |

Nút Back: đóng popup đang mở → trong game thì Pause/Resume → ở Level Select thì về Menu →
ở Main Menu thì hiện "Press BACK again to quit", bấm lần hai trong 2 giây để thoát.

## Luật chơi

* Level 1: 5 địch, bản đồ 30×30. Level 2: 10 địch, tường trong + thùng + cột, 50×50.
  Level 3: 15 địch + 1 boss trong khu pháo đài, 70×70.
* Thắng khi diệt hết địch, thua khi máu về 0. Thắng thì mở khóa level kế tiếp.
* Sao: hoàn thành = 1★. Điểm thời gian (≤ par: 2, ≤ 2×par: 1) + điểm máu (≥70%: 2, ≥35%: 1);
  tổng ≥2 → 2★, ≥3 → 3★. Điểm = điểm hạ địch + thưởng thời gian + thưởng máu.
* Địch chết có 50% rơi pickup máu (+35) hoặc đạn (+2 băng cho mỗi súng); boss rơi cả hai.
