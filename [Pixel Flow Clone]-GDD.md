### **KỊCH BẢN GAME PIXEL FLOW CLONE**

### **1\. Ý tưởng cốt lõi**

* Người chơi giải đố bằng cách điều phối các Đơn vị thu thập từ hàng chờ vào hệ thống Băng chuyền khép kín bao quanh các Khối gạch màu. Khi di chuyển trên băng chuyền, các Đơn vị này sẽ va chạm trực tiếp và dọn dẹp các Khối gạch cùng màu với chúng. Mục tiêu là dọn sạch toàn bộ các khối màu ở trung tâm mà không làm kẹt cứng băng chuyền.

### **2\. Các thành phần chính trong Gameplay (Core Entities)**

* **Khối Pixel trung tâm (Pixel Blocks):**  
  * Được xếp thành các cụm (Grid) hoặc hình thù cụ thể ở giữa màn hình. Mỗi khối mang một màu sắc tĩnh.  
  * **Cơ chế:** Đứng yên tại chỗ. Khi bị Đơn vị thu thập cùng màu áp sát và va chạm, khối gạch sẽ bị phá hủy.  
* **Đơn Vị Thu Thập (Collector Units):**  
  * Là các thực thể di chuyển (trong ảnh gốc là các chú heo/phương tiện) có màu sắc cụ thể và một Con số sức chứa (Capacity) hiển thị trên đầu (VD: 10, 17, 20).  
  * Cơ chế: Mỗi lần phá hủy 1 Khối Pixel cùng màu, con số sức chứa giảm đi 1\. Khi con số này về 0, Đơn vị đã "no", hoàn thành nhiệm vụ và bay ra khỏi màn chơi.  
* **Băng Chuyền & Quỹ Đạo (Path System):**  
  * Đường ray 2D bao quanh khu vực gạch trung tâm. Các Đơn vị khi lọt vào đây sẽ tự động di chuyển liên tục theo một chiều cố định (sử dụng hệ thống Waypoints đường thẳng và bo góc).  
* **Khu Vực Chờ (Waiting Slots):**  
  * **Khu vực chưa xuất phát:** Là hàng dọc các Đơn vị nằm ở dưới cùng màn hình, xếp đuôi nhau chờ người chơi kích hoạt.  
  * **Khu vực hàng chờ điều phối (0/5):** Gồm 5 ô ngang nằm ngay phía trên. Khi người chơi chạm (Tap) vào Đơn vị ở khu vực chưa xuất phát, Đơn vị đó sẽ tiến lên chiếm chỗ vào một trong 5 ô này để sẵn sàng rẽ vào băng chuyền chính.

### **3\. Vòng lặp Gameplay (Game Loop)**

* **Bắt đầu màn chơi:** GridManager load cấu trúc ma trận gạch từ ScriptableObject lên màn hình. Danh sách các Đơn vị thu thập được xếp sẵn ở Khu vực chưa xuất phát.  
* **Quá trình chơi (Active Phase):**  
  * **Xuất phát:** Người chơi Tap vào lợn ở dưới cùng. Nếu Băng chuyền chưa đạt giới hạn (VD: hiển thị 3/5 → 4/5), lợn sẽ tiến thẳng lên băng chuyền.  
  * **Trên băng chuyền:** Lợn chạy vòng quanh theo waypoint cố định, bắn tia quét gạch để ăn. Mỗi viên gạch ăn được, số trên đầu giảm 1\.  
  * **Hết 1 vòng:**   
    * Nếu số trên đầu \= 0: Lợn biến mất, giải phóng không gian.  
    * Nếu số trên đầu \> 0: Lợn rẽ vào khu vực Hàng chờ (Queue có 5 ô). Số đếm trên băng chuyền giảm 1 (VD: 4/5 → 3/5).  
  * **Tái xuất phát thủ công:** Người chơi phải tính toán và Tap trực tiếp vào con lợn đang nằm trong ô Queue để đẩy nó lên băng chuyền trở lại, miễn là băng chuyền chưa đạt mốc 5/5.  
* **Điều kiện thắng:** Toàn bộ Khối gạch trung tâm bị dọn sạch.  
* **Điều kiện thua:** Xảy ra khi cả 5 ô Queue đều đã bị lợn lấp đầy, đồng thời băng chuyền chính cũng đã max giới hạn (5/5), và không có con lợn nào trên băng chuyền quét được khối gạch hợp lệ → Hệ thống Deadlock.

### **4\. Tổng Quan Về Game (Overview)**

* **Tên game:** Pixel Flow Clone  
* **Đồ họa:** 2D Top-down (Góc nhìn từ trên xuống, sử dụng hệ thống Sprite Renderer, Rigidbody2D dạng Kinematic và các Collider2D để xử lý va chạm chính xác).  
* **Thể loại:** Puzzle (Giải đố logic / Sắp xếp phương tiện), Hyper-casual.  
* **Nền tảng tảng mục tiêu:** Mobile (Android / iOS) / PC / WebGL.

### **5\. Cấu Trúc Giao Diện & Trải Nghiệm (UI/UX Flow)**

* **Loading Screen:** Hiển thị tên game và thanh tải tiến trình.  
* **Main Menu:** Nút Play, màn hình chọn Level, cài đặt Âm thanh/Rung.  
* **Gameplay Screen:**  
  * **Khu vực trung tâm:** Hiển thị rõ ràng các Khối Pixel.  
  * **Khu vực viền:** Băng chuyền hiển thị trực quan không gian trống và các Đơn vị đang chạy.  
  * **Khu vực dưới cùng:** Hiển thị trực quan 5 ô hàng chờ ngang và hàng dọc chưa xuất phát.   
  * **HUD giữa:** Chỉ số trạng thái hàng chờ (Ví dụ: số hiển thị "3/5" để cảnh báo số ô đã bị chiếm dụng).  
  * **HUD trên cùng:** Nút Pause, Hiển thị cấp độ hiện tại (Level X).  
* **Pop-up:**  
  * **Victory:** Chúc mừng, hiệu ứng pháo hoa, nút "Next Level".  
  * **Defeat:** Thông báo kẹt xe (Out of moves / Jammed), nút "Retry".

### **6\. Xử lý các trường hợp ngoại lệ (Edge Cases)**

* **Tap Spamming:** Điều gì xảy ra nếu người chơi nhấp liên tục vào khu vực chưa xuất phát với tốc độ cực nhanh? Cần có một khoảng trễ (Cooldown) nhỏ giữa các lần Tap để hệ thống kịp xử lý Animation hoặc kiểm tra logic hàng đợi.

### **7\. Yêu cầu kỹ thuật**

* **Design Patterns:** Quy định rõ việc sử dụng Singleton cho các thành phần quản lý luồng tĩnh như `GameManager`, `UIManager` .  
* **Tối ưu bộ nhớ:** Áp dụng bắt buộc **Object Pooling** cho các Đơn vị thu thập và Khối gạch, vì chúng được sinh ra và phá hủy liên tục.   
* **Tổ chức dự án:** Đưa ra tiêu chuẩn về cấu trúc thư mục (Folder Hierarchy) tách biệt rõ ràng giữa `Scripts`, `Prefabs`, `ScriptableObjects` (dữ liệu màn chơi) và `Tests`. Việc phân rã các lớp đối tượng (OOP) theo chuẩn mực ngay từ đầu sẽ giúp quá trình viết Unit Test cho từng chức năng nhỏ (như logic trừ số lượng gạch, logic kiểm tra điều kiện kẹt xe) trở nên khả thi và chính xác. 

### **8\. Yêu cầu kỹ thuật**

* **Cơ chế Raycast "ăn" gạch:** Lợn chạy trên băng chuyền dùng Physics2D.Raycast để quét gạch. Hướng bắn tia phải luôn vuông góc với hướng di chuyển hiện tại của lợn (ví dụ: lợn đi từ trái sang phải thì tia bắn hướng lên trên), chứ không phải bắn xuyên chéo vào tâm bản đồ.  
* **Vật lý:** Sử dụng Rigidbody2D dạng Kinematic để code điều khiển quỹ đạo lợn chạy trên băng chuyền mượt mà (không dùng Dynamic để tránh ngoại lực làm trật đường ray), kết hợp Trigger/Raycast để xử lý logic "ăn".  
* **Luồng Queue (Rất quan trọng)**: \- Lợn đi hết 1 vòng băng chuyền, nếu chưa "no", nó sẽ tự động chui vào 1 trong 5 ô Queue.  
* Việc tái xuất phát từ Queue lên băng chuyền là THỦ CÔNG. Người chơi phải Tap vào con lợn đang nằm trong Queue thì nó mới nhảy lên băng chuyền trở lại.  
* áp dụng tốt các nguyên lý C\# OOP, Design Patterns (như Singleton, Object Pooling) và Unit Test.

