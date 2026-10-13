# BÁO CÁO XÂY DỰNG TRÒ CHƠI BẮN SÚNG 3D GÓC NHÌN THỨ NHẤT

> Tên đề xuất: **Crimson Arena: Survival Protocol**  
> Thể loại: FPS 3D – sinh tồn đấu trường – khoa học viễn tưởng đen tối  
> Nền tảng: Windows và thiết bị di động  
> Công cụ phát triển: Unity 6.0, C#, Meshy AI, Unity NavMesh, UGUI

## 1. Tóm tắt đề tài

Đề tài xây dựng một trò chơi bắn súng 3D góc nhìn thứ nhất, trong đó người chơi chiến đấu với các sinh vật cơ sinh học có khả năng tuần tra, phát hiện, truy đuổi và bắn trả. Trò chơi gồm ba đấu trường với quy mô và độ khó tăng dần. Người chơi phải tận dụng khả năng di chuyển, chạy tăng tốc, nhảy, bò, thay đạn và chuyển đổi vũ khí để tiêu diệt toàn bộ kẻ địch trước khi bị hạ gục.

Điểm nổi bật của sản phẩm là kết hợp mô hình 3D tạo bằng Meshy với hệ thống điều khiển, AI và hoạt ảnh được xây dựng trong Unity. Các đấu trường sử dụng phong cách kiến trúc kim loại Gothic, ánh sáng đỏ và bầu trời u ám để tạo cảm giác nguy hiểm. Hệ thống được thiết kế theo hướng dữ liệu hóa: thông tin màn chơi, số lượng quái, vị trí vật cản, vũ khí, điểm số và tài nguyên được tách thành các thành phần riêng, giúp dễ bảo trì và mở rộng.

![Toàn cảnh đấu trường](images/01-toan-canh-dau-truong.png)

*Hình 1. Ảnh concept toàn cảnh đấu trường, được tạo dựa trên phong cách và tài sản của project.*

## 2. Lý do chọn đề tài

Trò chơi FPS là dạng sản phẩm phù hợp để vận dụng nhiều kiến thức quan trọng trong phát triển game: xử lý input, camera 3D, vật lý, va chạm, âm thanh, AI, NavMesh, Animator, giao diện và tối ưu tài nguyên. Đề tài cũng có tính trực quan cao, dễ đánh giá thông qua cảm giác điều khiển, phản hồi khi bắn và hành vi của kẻ địch.

Việc chọn bối cảnh đấu trường giúp giới hạn phạm vi hợp lý. Thay vì xây dựng một thế giới mở quá lớn, dự án tập trung vào ba không gian chiến đấu nhỏ nhưng có bố cục, vật cản và nhịp độ khác nhau. Cách tiếp cận này cho phép đầu tư nhiều hơn vào chất lượng điều khiển, AI và hiệu ứng.

## 3. Mục tiêu

Mục tiêu tổng quát là xây dựng một bản game FPS 3D có thể chơi hoàn chỉnh từ màn hình chọn cấp độ đến chiến thắng hoặc thất bại.

Các mục tiêu cụ thể gồm:

- Xây dựng góc nhìn thứ nhất với tay và súng 3D.
- Hỗ trợ bàn phím, chuột và điều khiển cảm ứng.
- Xây dựng ba cấp độ có độ khó tăng dần.
- Tạo AI quái có các trạng thái tuần tra, truy đuổi, tấn công và chết.
- Đồng bộ chuyển động AI với Animator.
- Xây dựng hệ thống máu, đạn, thay băng, sát thương và vật phẩm.
- Hiển thị HUD, tạm dừng, chọn màn và kết quả.
- Tạo hiệu ứng bắn, vệt đạn, trúng đạn, âm thanh bước chân và tiếng quái.
- Tổ chức mã nguồn theo từng nhóm chức năng để dễ nâng cấp.

## 4. Cốt truyện và bối cảnh

Trong tương lai, một hệ thống phòng thủ tự động mang tên Crimson Protocol mất quyền kiểm soát sau khi lõi năng lượng trung tâm bị nhiễm một dạng vật chất sinh học ngoài hành tinh. Robot bảo vệ bị biến đổi thành những thực thể cơ sinh học, kết hợp mô sống với giáp kim loại và vũ khí năng lượng.

Người chơi vào vai một chiến binh thuộc đội phản ứng cuối cùng. Nhiệm vụ là xâm nhập ba khu vực của pháo đài, tiêu diệt các thực thể bị nhiễm và phá hủy đơn vị chỉ huy. Mỗi đấu trường là một lớp phòng thủ:

- **Training Yard:** sân huấn luyện bị chiếm đóng, giúp người chơi làm quen với điều khiển và chiến đấu.
- **Warehouse:** khu kho chứa có nhiều tường, cột và vật cản, buộc người chơi tận dụng góc bắn và di chuyển chiến thuật.
- **Fortress:** khu pháo đài cuối cùng với mật độ địch cao và Boss cơ sinh học mang đại bác tích hợp ở phần đầu.

Mục tiêu cốt truyện phù hợp với vòng lặp gameplay: tiến vào đấu trường, sống sót, tiêu diệt toàn bộ quái và mở khóa khu vực tiếp theo.

## 5. Đối tượng người chơi và phạm vi

Trò chơi hướng tới người thích FPS nhịp độ nhanh, thời lượng mỗi màn ngắn và thao tác trực tiếp. Độ khó phù hợp cho người chơi phổ thông, đồng thời hệ thống chấm điểm và thời gian hoàn thành tạo động lực chơi lại.

Phạm vi phiên bản hiện tại:

- Chế độ một người chơi.
- Ba màn chơi độc lập.
- Hai nhóm kẻ địch chính: quái thường và Boss/quái đỏ.
- Hai vũ khí góc nhìn thứ nhất được quản lý bởi hệ thống vũ khí.
- Điều khiển desktop và mobile.
- Không có multiplayer, tài khoản trực tuyến hoặc lưu trữ đám mây.

## 6. Vòng lặp gameplay

Vòng lặp chính của trò chơi gồm:

1. Người chơi chọn màn đã mở khóa.
2. Hệ thống dựng đấu trường, NavMesh, vật cản, người chơi và quái.
3. Người chơi tìm mục tiêu, di chuyển và sử dụng vật cản.
4. Quái tuần tra; khi thấy hoặc bị người chơi tấn công, chúng truy đuổi và bắn trả.
5. Người chơi quản lý HP, số đạn trong băng và đạn dự trữ.
6. Quái chết có xác suất rơi máu hoặc đạn.
7. Khi toàn bộ quái bị tiêu diệt, hệ thống tính điểm, thưởng thời gian, thưởng máu còn lại và xếp sao.
8. Khi HP người chơi về 0, màn chơi thất bại và có thể chơi lại.

Vòng lặp này ngắn, rõ ràng và phù hợp với kiểu game đấu trường. Sự tăng dần về số quái và mật độ vật cản tạo ra độ khó mà không cần thay đổi luật chơi cốt lõi.

## 7. Cơ chế điều khiển

### 7.1. Desktop

- W/A/S/D hoặc phím mũi tên: di chuyển.
- Chuột: xoay góc nhìn.
- Chuột trái: bắn.
- R: thay băng đạn.
- Space: nhảy.
- C hoặc Ctrl: bò.
- Shift: chạy tăng tốc.
- Q hoặc con lăn chuột: chuyển vũ khí.
- Phím 1 và 2: chọn trực tiếp vũ khí.

### 7.2. Mobile

- Joystick tròn bên trái: di chuyển.
- Kéo trên vùng nhìn: xoay camera.
- Nút bắn, nhảy, bò và thay đạn ở phía phải.
- Nút có màu trắng bán trong suốt để người chơi vẫn quan sát được không gian phía sau.
- Người chơi vẫn có thể tiến, lùi và đổi hướng trong khi nhảy hoặc bò.

### 7.3. Chuyển động

Player sử dụng `CharacterController`. Tốc độ đi cơ bản hiện là 8,25 đơn vị/giây. Khi giữ chạy và di chuyển về phía trước, tốc độ tăng dần theo gia tốc tới hệ số 1,65. Nhảy sử dụng vận tốc tính từ độ cao nhảy và trọng lực. Trạng thái bò giảm chiều cao collider và tốc độ, đồng thời hạ camera để tạo phản hồi trực quan.

## 8. Hệ thống chiến đấu và vũ khí

Vũ khí sử dụng phương pháp hitscan: khi bóp cò, một tia ray được bắn từ camera theo hướng ngắm. Nếu ray chạm đối tượng có giao diện `IDamageable`, đối tượng nhận sát thương. Nếu ray chạm môi trường, hệ thống sinh hiệu ứng va chạm.

Mỗi vũ khí có các thuộc tính dữ liệu riêng: tên, sát thương, tốc độ bắn, độ giật, độ tản đạn, tầm bắn, kích thước băng, lượng đạn dự trữ và thời gian thay đạn. Cách tách dữ liệu này cho phép cân bằng súng mà không thay đổi logic.

Phản hồi khi bắn gồm:

- Giảm đạn trong băng.
- Muzzle flash và ánh sáng đầu nòng.
- Giật camera và giật mô hình súng.
- Âm thanh bắn.
- Dấu xác nhận trúng mục tiêu.
- Hiệu ứng máu với quái hoặc tia lửa với môi trường.
- Tự động thay đạn khi băng hết nếu còn đạn dự trữ.

![Chiến đấu góc nhìn thứ nhất](images/02-chien-dau-goc-nhin-thu-nhat.png)

*Hình 2. Ảnh concept mô tả chiến đấu góc nhìn thứ nhất và vệt đạn từ quái.*

## 9. Hệ thống kẻ địch và AI

AI sử dụng `NavMeshAgent` và máy trạng thái hữu hạn gồm bốn trạng thái:

- **Patrol:** chọn điểm ngẫu nhiên trong bán kính tuần tra và đi tới đó.
- **Chase:** cập nhật đường đi tới vị trí người chơi.
- **Attack:** dừng di chuyển, xoay về người chơi và bắn theo chu kỳ.
- **Dead:** dừng NavMesh, tắt collider, chạy animation chết và xóa xác sau một khoảng thời gian.

### 9.1. Nhận biết người chơi

Quái kiểm tra cảm nhận theo chu kỳ 0,2 giây nhằm giảm chi phí CPU. Điều kiện phát hiện gồm khoảng cách, góc nhìn và đường nhìn. Raycast từ mắt quái tới người chơi giúp vật cản chặn tầm nhìn, do đó quái không bắn xuyên tường.

Trong cấu hình runtime hiện tại, tầm phát hiện của quái thường được mở rộng tới 96 Unity units và tầm tấn công được đồng bộ với tầm phát hiện. Vì vậy, nếu quái nhìn thấy người chơi trong vùng phát hiện thì nó có thể chuyển sang tấn công.

### 9.2. Phản ứng khi bị bắn

Khi nhận sát thương, quái lập tức chuyển sang trạng thái cảnh giác:

- Nếu nhìn thấy người chơi, quái chuyển sang Attack và bắn trả gần như ngay lập tức.
- Nếu không nhìn thấy, quái chuyển sang Chase và tìm tới vị trí người chơi.
- Quái tiếp tục truy tìm trong thời gian ghi nhớ; khi mất dấu đủ lâu mới quay lại tuần tra.

### 9.3. Né vật cản

Trước khi nhận một điểm tuần tra, AI kiểm tra NavMesh path và kiểm tra vật lý theo hướng di chuyển. Nếu tuyến đường cắt vật cản, điểm đó bị loại. Khi collider của quái chạm vật thể, quái quay về vị trí an toàn gần nhất rồi thử chọn hướng vòng khác. Cơ chế này hạn chế tình trạng quái đi xuyên vật thể hoặc mắc kẹt tại một chướng ngại.

### 9.4. Tấn công của quái

Đòn bắn của quái gồm animation Attack, tiếng súng 3D, chớp sáng ở đầu, LineRenderer mô phỏng vệt đạn và xác suất trúng dựa theo khoảng cách. Boss có màu vệt đạn và hiệu ứng nổi bật hơn quái thường.

## 10. Animator và hoạt ảnh

Quái sử dụng Animator Controller riêng cho mẫu thường và mẫu đỏ. Các parameter gồm:

- `Speed`: mức tốc độ di chuyển.
- `MotionRate`: đồng bộ tốc độ phát clip với vận tốc NavMesh.
- `Combat`: xác định trạng thái chiến đấu.
- `Attack`: trigger tấn công.
- `Hit`: trigger trúng đạn.
- `Dead`: trạng thái chết.

`EnemyAnimatorBridge` làm lớp trung gian giữa logic AI và Animator. Lớp này cập nhật tốc độ, phát trigger và căn chân mesh theo root NavMesh. Animation chết được lấy mẫu chủ động để tránh tình trạng clip Generic ngừng cập nhật sau khi NavMeshAgent bị tắt.

Trong quá trình kiểm thử, prefab từng ghi đè `RuntimeAnimatorController` thành rỗng. Lỗi đã được sửa; kiểm tra runtime xác nhận các quái nhận đúng controller và đầy đủ parameter.

## 11. Thiết kế màn chơi

### 11.1. Map 1 – Training Yard

- Kích thước dữ liệu thiết kế: 30 × 30.
- Số lượng: 5 quái thường.
- Thời gian chuẩn: 90 giây.
- Vai trò: hướng dẫn không chính thức, ít địch, bố cục dễ đọc.
- Có các khối che chắn nhỏ để người chơi học cách di chuyển và ngắm bắn.

### 11.2. Map 2 – Warehouse

- Kích thước dữ liệu thiết kế: 50 × 50.
- Số lượng: 7 quái thường.
- Thời gian chuẩn: 180 giây.
- Có tường trong, thùng và cột tạo nhiều góc khuất.
- Yêu cầu người chơi quản lý vị trí, tránh bị nhiều quái bao vây.

### 11.3. Map 3 – Fortress

- Kích thước dữ liệu thiết kế: 70 × 70.
- Số lượng: 7 quái thường và 1 Boss, tổng cộng 8 kẻ địch.
- Thời gian chuẩn: 300 giây.
- Có khu đấu Boss ở phía bắc, tường ngoài, vật cản và cột.
- Đây là màn tổng hợp mọi cơ chế của trò chơi.

Hệ thống dựng màn chuẩn hóa mô hình Meshy theo kích thước mục tiêu, tạo NavigationFloor ẩn, ranh giới đấu trường, collider và vật cản. Spawn người chơi và quái được chiếu lên NavMesh, kiểm tra capsule trống và loại bỏ những điểm nằm trên vật trang trí cao. Nhờ đó nhân vật không rơi ngoài map hoặc xuất hiện trong lòng sàn.

## 12. Boss

Boss là phiên bản mạnh hơn của quái thường, có kích thước, máu, tầm nhận biết và hiệu ứng lớn hơn. Về mỹ thuật, Boss được định hướng là thực thể cơ sinh học màu đỏ với khẩu đại bác lớn tích hợp vào miệng/đầu, tạo điểm nhận diện khác biệt.

Boss xuất hiện ở Map 3 sau lớp phòng thủ của pháo đài. Trận đấu Boss đóng vai trò cao trào, yêu cầu người chơi duy trì di chuyển, tận dụng vật cản và quản lý đạn tốt hơn.

![Boss cuối](images/03-boss-cuoi.png)

*Hình 3. Ảnh concept Boss cuối với đại bác tích hợp ở đầu.*

## 13. Vật phẩm, máu và tài nguyên

Người chơi có thanh HP. Khi nhận sát thương, HUD phát hiệu ứng đỏ và cập nhật màu thanh máu từ xanh sang vàng rồi đỏ. Khi HP về 0, điều khiển bị khóa và camera tạo hiệu ứng ngã.

Quái thường có xác suất rơi vật phẩm; Boss rơi cả máu và đạn. Vật phẩm máu chỉ được tiêu thụ khi người chơi chưa đầy HP, tránh việc đi qua vật phẩm khi đầy máu nhưng vẫn làm mất vật phẩm. Vật phẩm đạn bổ sung đạn dự trữ trong giới hạn tối đa.

## 14. Điểm số, chiến thắng và mở khóa

Mỗi quái có giá trị điểm. Điểm cuối màn được tổng hợp từ:

- Điểm tiêu diệt quái.
- Thưởng hoàn thành nhanh so với thời gian chuẩn.
- Thưởng theo phần trăm máu còn lại.

Hệ thống quy đổi kết quả thành số sao. Kết quả được dùng để mở khóa màn tiếp theo và tạo động lực chơi lại để cải thiện thành tích.

Điều kiện thắng là tiêu diệt toàn bộ kẻ địch. Điều kiện thua là HP người chơi về 0. Trò chơi có các trạng thái Playing, Paused, Won và Lost để quản lý thời gian, input và giao diện.

## 15. Giao diện người dùng

HUD hiển thị:

- Thanh HP và giá trị máu.
- Tên vũ khí.
- Đạn trong băng và đạn dự trữ.
- Số quái còn lại.
- Điểm số và thời gian.
- Tâm ngắm và hit marker.
- Trạng thái thay đạn.
- Nút tạm dừng.

Thanh máu sử dụng nền tối, viền phát sáng, vạch chia và lớp highlight. Thanh máu quái được thu nhỏ để không che mô hình. Giao diện mobile dùng các nút tròn màu trắng với opacity thấp; icon rõ hơn nền nhưng vẫn đảm bảo quan sát được cảnh phía sau.

Ngoài HUD còn có màn hình chính, chọn level, cài đặt, pause và kết quả. Các màn hình sử dụng cùng ngôn ngữ thiết kế tối, kim loại và điểm nhấn đỏ/xanh để nhất quán với bối cảnh.

## 16. Âm thanh

Hệ thống âm thanh sử dụng một pool AudioSource để tránh tạo mới liên tục. Các âm thanh chính gồm tiếng súng lục, súng trường, súng quái, thay đạn, hết đạn, trúng mục tiêu, quái chết, người chơi bị thương, nhặt vật phẩm, thắng, thua, bước chân và tiếng gầm.

Tiếng bước chân dùng file thu âm giày đi trên sàn và được phát dạng loop. Khi chạy, pitch và volume tăng dần theo mức tốc độ. Tiếng gầm của quái là âm thanh 3D, chỉ phát khi người chơi đến gần, giúp người chơi nhận biết nguy hiểm bằng thính giác.

## 17. Đồ họa và hiệu ứng

Phong cách đồ họa là sci-fi Gothic với các đặc điểm:

- Bề mặt kim loại đen, xám và đỏ sẫm.
- Khe năng lượng đỏ phát sáng trên sàn và công trình.
- Bầu trời nhiều mây, ánh sáng lạnh và không khí u ám.
- Quái kết hợp cơ bắp sinh học với giáp và vũ khí máy móc.
- Tay và súng góc nhìn thứ nhất có vật liệu sắc nét, kích thước vừa phải.

Hiệu ứng chiến đấu gồm muzzle flash, ánh sáng đầu nòng, vệt đạn, tia lửa va chạm, hiệu ứng trúng quái, damage flash, hit marker và recoil. Object Pool được dùng cho hiệu ứng va chạm, máu và vật phẩm nhằm giảm cấp phát bộ nhớ.

Ba ảnh trong báo cáo là concept art được tạo mới dựa trên đặc trưng của project; chúng không phải ảnh chụp trực tiếp từ Unity và được dùng để minh họa định hướng chất lượng hình ảnh.

## 18. Kiến trúc phần mềm

Mã nguồn được chia thành các nhóm:

- `Core`: GameManager, AudioManager, SaveManager, GameEvents, input mode và pool.
- `Player`: input, chuyển động, camera và máu người chơi.
- `Weapons`: logic súng, dữ liệu súng và quản lý vũ khí.
- `Enemy`: AI, health bar và cầu nối Animator.
- `Level`: dữ liệu level, dựng map, spawn, vật phẩm và chấm điểm.
- `UI`: HUD, mobile controls, menu, pause, chọn level và cài đặt.
- `Data`: GameAssets, LevelDefinition và LevelDatabase.

Các hệ thống giao tiếp qua `GameEvents`, giảm phụ thuộc trực tiếp giữa gameplay và UI. `GameAssets` tập trung tham chiếu prefab, material và effect trong Resources. `LevelDatabase` mô tả ba cấp độ bằng dữ liệu, còn `LevelBuilder` chịu trách nhiệm dựng môi trường thực tế.

## 19. Quy trình xây dựng

Quy trình triển khai dự án gồm:

1. Xây dựng prototype bằng primitive để kiểm chứng di chuyển và bắn.
2. Thiết kế PlayerController, camera và hệ thống input.
3. Tạo hệ thống vũ khí hitscan và đạn.
4. Xây dựng AI NavMesh và máy trạng thái.
5. Tạo UI/HUD và luồng menu–chọn màn–kết quả.
6. Thiết kế mô hình nhân vật, quái, tay súng và đấu trường bằng Meshy.
7. Import FBX/GLB, tạo material, rig, animation clip và Animator Controller.
8. Thay thế placeholder bằng mô hình chính thức.
9. Thiết lập collider, NavMesh, spawn và căn chân.
10. Bổ sung âm thanh, hiệu ứng và tối ưu pool.
11. Kiểm thử từng map trên desktop và giao diện mobile.

## 20. Công nghệ sử dụng

- Unity Editor 6000.6.0f1.
- Ngôn ngữ C#.
- Unity Physics và CharacterController.
- AI Navigation/NavMesh 2.0.14.
- Unity UGUI 2.6.0.
- Animator Controller và animation Generic từ mô hình Meshy.
- Meshy AI để tạo mô hình 3D, texture và rig.
- MCP for Unity để hỗ trợ kiểm tra, tự động hóa và xác minh runtime.
- Git để quản lý thay đổi mã nguồn nếu triển khai kho mã.

## 21. Kiểm thử

Các nhóm kiểm thử quan trọng:

### 21.1. Điều khiển

- W/A/S/D hoạt động đồng thời khi UI mobile xuất hiện trong Editor.
- Joystick và các nút cảm ứng nhận đúng input.
- Có thể di chuyển khi nhảy và bò.
- Chạy tăng tốc dần, không thay đổi tốc độ đột ngột.

### 21.2. Va chạm và spawn

- Player và quái không đi xuyên tường/vật cản.
- Quái không chìm hoặc lơ lửng.
- Spawn không nằm trên vật trang trí hoặc ngoài sàn.
- Map 1 được xác minh có 5 quái và sai số căn chân gần bằng 0.

### 21.3. AI

- Quái tuần tra và đổi hướng khi gặp vật cản.
- Quái chỉ bắn khi có đường nhìn.
- Quái phản công khi bị bắn.
- Quái mất dấu rồi quay lại tuần tra.
- Tracer và muzzle flash xuất phát từ phần đầu quái.

### 21.4. Animator

- Tất cả prefab quái có RuntimeAnimatorController.
- Các parameter Speed, Combat, Attack, Hit, Dead và MotionRate tồn tại.
- Animation chết chạy trước khi xác biến mất.

### 21.5. Gameplay

- Đạn giảm, thay băng và nhặt đạn đúng.
- Vật phẩm máu không mất khi HP đã đầy.
- Số quái còn lại, điểm, thời gian và điều kiện thắng/thua chính xác.

## 22. Khó khăn và cách khắc phục

### Mô hình Meshy có pivot và kích thước không đồng nhất

Giải pháp là chuẩn hóa mô hình theo chiều cao mục tiêu, đặt root gameplay riêng và dùng `EnemyAnimatorBridge` căn đáy renderer với root NavMesh.

### Mesh đấu trường có số polygon rất lớn

Fast Midphase có thể bỏ sót va chạm. Collider được nấu lại với mesh cleaning và welding, đồng thời NavMesh sử dụng NavigationFloor nhẹ thay vì trực tiếp bake mesh nhiều triệu tam giác.

### Quái đi xuyên hoặc mắc vật cản

Ngoài NavMesh path, AI bổ sung kiểm tra SphereCast và OverlapCapsule. Khi bị chặn, quái quay lại vị trí an toàn và tìm hướng vòng.

### Animator bị thiếu controller

Prefab lồng nhau từng có override đặt controller thành rỗng. Override được loại bỏ và runtime được kiểm tra để chắc chắn toàn bộ quái nhận đúng controller.

### Sai tỉ lệ camera và mô hình tay

FOV, vị trí camera và kích thước view model được điều chỉnh độc lập để đấu trường không bị cảm giác quá nhỏ và súng không che quá nhiều màn hình.

## 23. Phiên bản dự án

Có thể trình bày lịch sử phiên bản theo các mốc chức năng sau:

- **v0.1 – Prototype:** di chuyển FPS, camera, bắn hitscan và map primitive.
- **v0.2 – Core Gameplay:** máu, đạn, thay băng, quái cơ bản và điều kiện thắng/thua.
- **v0.3 – Level System:** ba level, điểm số, thời gian, xếp sao và mở khóa.
- **v0.4 – 3D Asset Integration:** tích hợp quái, tay súng, đấu trường và vật cản từ Meshy.
- **v0.5 – Mobile Controls:** joystick, nút bắn, nhảy, bò và thay đạn.
- **v0.6 – AI & Animation:** NavMesh, né vật cản, phản công, Animator và animation chết.
- **v0.7 – Presentation Polish:** HUD mới, âm thanh bước chân, tiếng gầm, tracer, muzzle flash và bầu trời u ám.
- **v0.8 – Stability:** sửa spawn, collider, căn chân, controller Animator và kiểm thử ba map.

Phiên bản hiện tại phù hợp gọi là **Alpha 0.8** vì vòng chơi đã hoàn chỉnh nhưng vẫn cần tối ưu hiệu năng, cân bằng và đóng gói bản phát hành.

## 24. Kết quả đạt được

Dự án đã hình thành một game FPS 3D có thể chơi từ đầu đến cuối. Người chơi có thể chọn màn, chiến đấu bằng góc nhìn thứ nhất, quản lý máu và đạn, sử dụng kỹ năng di chuyển và đối đầu với AI có phản ứng. Ba đấu trường tạo lộ trình độ khó rõ ràng; hình ảnh, âm thanh và UI đã có phong cách thống nhất.

Về kỹ thuật, dự án vận dụng được nhiều thành phần quan trọng của Unity: CharacterController, Physics Raycast, NavMeshAgent, Animator, ScriptableObject, event, object pool, UGUI, âm thanh 3D và lưu tiến trình.

## 25. Hạn chế

- Chưa có hệ thống nhiệm vụ phụ hoặc cốt truyện thể hiện trực tiếp trong game.
- Số loại quái và kiểu tấn công còn ít.
- Boss hiện kế thừa phần lớn logic quái thường.
- Chưa có minimap, tùy biến đồ họa sâu hoặc remap phím.
- Mesh 3D tạo bằng AI có thể nặng và cần retopology/LOD.
- Chưa có benchmark chính thức trên nhiều thiết bị di động.
- Chưa có multiplayer, leaderboard hoặc cloud save.

## 26. Hướng phát triển

- Thêm nhiều loại quái: cận chiến, xe tăng robot, quái bay, sniper và healer.
- Xây dựng Boss nhiều phase, chiêu diện rộng và điểm yếu riêng.
- Thêm objective như giữ vị trí, hộ tống, phá lõi hoặc sống sót theo wave.
- Bổ sung minimap, nhiệm vụ, hội thoại và cinematic.
- Tạo hệ thống nâng cấp súng, perk và kỹ năng chủ động.
- Thêm LOD, occlusion culling, texture atlas và giảm polygon cho mobile.
- Sử dụng baked lighting, reflection probe và post-processing có cấu hình chất lượng.
- Thêm gamepad, rung, aim assist và tùy chỉnh độ nhạy riêng.
- Xây dựng hệ thống analytics và cân bằng dựa trên dữ liệu chơi.
- Đóng gói bản Windows/Android và thực hiện kiểm thử hiệu năng thực tế.

## 27. Kết luận

Đề tài đã giải quyết được mục tiêu xây dựng một trò chơi FPS 3D đấu trường với đầy đủ luồng chơi cơ bản, AI, hoạt ảnh, đồ họa, âm thanh và giao diện. Quá trình thực hiện cho thấy chất lượng game không chỉ phụ thuộc vào mô hình đẹp mà còn phụ thuộc mạnh vào collider, NavMesh, camera, phản hồi bắn và sự đồng bộ giữa logic với animation.

Kiến trúc hiện tại đủ linh hoạt để tiếp tục bổ sung quái, vũ khí, map và chế độ chơi. Đây là nền tảng phù hợp để phát triển thành một sản phẩm hoàn chỉnh hơn sau giai đoạn Alpha.

## 28. Gợi ý bố cục thuyết trình

Nếu chuyển nội dung thành slide, có thể dùng trình tự:

1. Tên đề tài và ảnh toàn cảnh.
2. Bài toán, mục tiêu và phạm vi.
3. Cốt truyện và gameplay loop.
4. Điều khiển và vũ khí.
5. AI và sơ đồ trạng thái.
6. Animator và mô hình 3D.
7. Thiết kế ba map.
8. UI, âm thanh và hiệu ứng.
9. Kiến trúc mã nguồn.
10. Khó khăn và giải pháp.
11. Kết quả kiểm thử.
12. Demo game.
13. Hạn chế và hướng phát triển.
14. Kết luận.

## 29. Chú thích sử dụng ảnh

Ba ảnh trong `BaoCao/images` là hình minh họa concept do AI tạo dựa trên thiết kế của game. Khi đưa vào báo cáo nên ghi: **“Hình ảnh concept minh họa định hướng mỹ thuật, không phải ảnh chụp gameplay thực tế.”** Ảnh gameplay thật nên được bổ sung ở phần kết quả triển khai để đối chiếu giữa concept và sản phẩm hiện tại.
