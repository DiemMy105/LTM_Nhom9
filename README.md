# [PROJECT_CODE] - [PROJECT_NAME]
UDM_08 — Chat TCP Client–Server

## Thành viên

| STT | MSSV | Họ và tên | Vai trò |
|---:|---|---|---|
| 1 | 075306001575 | Nguyễn Hồ Diễm My | Phân công, theo dõi tiến độ, lập trình tính năng chat 1-1, xây dựng module kết nối socket phía client, xử lý định tuyến tin nhắn cá nhân giữa 2 client, gửi/nhận tin nhắn 1-1, làm word, tổng hợp báo cáo word, powerpoint |
| 2 | 042205011735 | Võ Tùng Sơn | Cài đặt module Đăng nhập và Đăng ký, thiết kế Database, triển khai cơ chế băm mật khẩu và kiểm tra mật khẩu, xử lý lưu trữ tin nhắn/dữ liệu người dùng, làm word, powerpoint |
| 3 | 056206002399 | Võ Duy Thịnh | Lập trình, theo dõi luồng TCP Socket để cập nhật trạng thái online/offline theo thời gian thực, kiểm tra tính năng query tin nhắn cũ từ CSDL khi người dùng mở khung chat (lịch sử chat), hiển thị thông báo chấm đỏ ở giao diện khi có tin nhắn mới (chưa đọc), làm word, powerpoint |
| 4 | 082205013580 | Lưu Quốc Phú | Lập trình tính năng chat group, xử lý phát sóng tin nhắn nhóm tới các client trong nhóm, gửi/nhận tin nhắn nhóm, phối hợp hiện thị danh sách nhóm lên giao diện, làm word, powerpoint |
| 5 | 083206006476 | Nguyễn Hồ Hùng Phương | Thiết kế Giao diện Client & Server,hiển thị IP/Port, chức năng xử lý avatar mặc định và upload avatar, thực hiện test case, powerpoint. |
| 6 | 001206022589 | Nguyễn Đặng Thái Bình | Lập trình các tính năng tin nhắn mở rộng: Forward (Chuyển tiếp), Reply, Xử lý mã hóa & hiển thị Emoji, chat bubble, làm word, powerpoint |

## Giới thiệu

Dự án Chat TCP Client–Server là ứng dụng trò chuyện trực tuyến được xây dựng bằng ngôn ngữ C# trên nền tảng .NET và giao diện Windows Forms (WinForms).
Mục tiêu của đề tài là xây dựng một hệ thống Chat hoạt động theo mô hình Client–Server, trong đó các Client kết nối đến Server thông qua TCP Socket để trao đổi dữ liệu và tin nhắn theo thời gian thực.
Hệ thống hỗ trợ các chức năng chính như:
- Đăng ký và đăng nhập tài khoản.
- Chat 1-1 giữa các người dùng.
- Chat nhóm.
- Reply tin nhắn.
- Forward tin nhắn.
- Gửi Emoji.
- Hiển thị trạng thái Online/Offline.
- Hiển thị thông báo tin nhắn mới, chưa đọc.
- Quản lý và cập nhật Avatar.
- Lưu trữ và xem lại lịch sử trò chuyện.
- Quản lý dữ liệu người dùng, tin nhắn và nhóm thông qua Database.
Đối tượng sử dụng của hệ thống là người dùng cần trao đổi tin nhắn trong cùng một hệ thống mạng.
Ứng dụng được xây dựng chủ yếu nhằm phục vụ mục đích học tập, nghiên cứu và thực hành các kiến thức về lập trình mạng, TCP Socket, mô hình Client–Server, xử lý đa luồng và quản lý dữ liệu.
Phạm vi của đề tài tập trung vào việc xây dựng ứng dụng Chat Desktop bằng C# WinForms.
Server đóng vai trò tiếp nhận kết nối, quản lý Client và xử lý dữ liệu, trong khi Client cung cấp giao diện để người dùng đăng nhập, gửi và nhận tin nhắn.
Dữ liệu được truyền giữa Client và Server thông qua TCP Socket.
- Server có nhiệm vụ:
Lắng nghe các kết nối từ Client.
Quản lý danh sách Client đang kết nối.
Nhận và xử lý dữ liệu từ Client.
Điều phối tin nhắn đến đúng người nhận.
Xử lý Chat Group.
Lưu dữ liệu vào Database.
Cập nhật trạng thái Online/Offline.
- Client có nhiệm vụ:
Kết nối đến Server.
Gửi dữ liệu đến Server.
Nhận dữ liệu từ Server.
Hiển thị dữ liệu lên giao diện GUI.

## Kiến trúc hệ thống

- Mô hình: Client–Server

- Protocol: Hệ thống sử dụng giao thức TCP để truyền dữ liệu giữa Client và Server.
Dữ liệu Message được chuyển thành chuỗi JSON trước khi gửi.
Mỗi Message được kết thúc bằng ký tự xuống dòng để xác định điểm kết thúc của một Message: \n
Giúp xử lý trường hợp:
Nhiều Message được gửi cùng lúc.
Một Message bị chia thành nhiều lần nhận dữ liệu.
Tránh việc các Message bị ghép sai với nhau.

- Port mặc định: 
Port mặc định được cấu hình trong file: ChatTCP.Shared/Utils/NetworkConfig.cs
IP và Port có thể thay đổi tùy theo môi trường chạy.

- Cấu trúc message:
   public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int? ReceiverId { get; set; }
        public int? GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.DirectChat;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int? ReplyToMessageId { get; set; }
        public string? ReplyToSenderName { get; set; }
        public string? ReplyToContent { get; set; }
        public bool IsForward { get; set; } = false;
    }

## Yêu cầu môi trường

- Hệ điều hành: 
Ứng dụng có thể chạy trên:
Windows 10.
Windows 11.

- Ngôn ngữ và phiên bản:
C#
.NET

- Công cụ hỗ trợ:
Visual Studio.
SQL Server.
Git.
GitHub.

-Công nghệ sử dụng:
C#.
WinForms.
TCP Socket.
SQL Server.
System.Text.Json.

## Cài đặt

Bước 1: Clone Repository
Clone source code từ GitHub: git clone <https://github.com/DiemMy105/LTM_Nhom9.git>
Hoặc tải source code về máy.

Bước 2: Mở Solution
Mở file: Code/ChatTCP.sln bằng Visual Studio.

Bước 3: Cấu hình Database
Mở thư mục: Database/
Chạy file: ChatTCP.sql trên SQL Server để tạo Database và các bảng cần thiết.
Các bảng chính gồm:
User.
Message.
Group.
GroupMember.

Bước 4: Kiểm tra Connection String
Kiểm tra cấu hình kết nối Database trong: ChatTCP.Server/Services/DatabaseService.cs

Bước 5: Build Project
Trong Visual Studio chọn:
File → Project/Solution → ChatTCP.slnx
Hoặc nhấn 2 lần vào ChatTCP.slnx.
Đảm bảo Solution không có lỗi trước khi chạy.

## Hướng dẫn chạy

### Server

Mở Solution bằng Visual Studio.
Chọn project:
```text
ChatTCP.Server
```
- Chạy Server.
Server bắt đầu lắng nghe kết nối từ Client.
Kiểm tra trạng thái Server trên giao diện.
- Server hiển thị:
Trạng thái Server.
IP/Port đang sử dụng.
Danh sách Client kết nối.
Log hoạt động.

### Client

Chọn project:
```text
ChatTCP.Client
```
Chạy Client.
Nhập thông tin kết nối Server.
Đăng ký hoặc đăng nhập tài khoản.
Sau khi đăng nhập thành công, người dùng được chuyển đến giao diện Chat.
Có thể chạy nhiều Client cùng lúc để kiểm tra chức năng Chat.

## Cấu hình

IP và Port được cấu hình trong file: ChatTCP.Shared/Utils/NetworkConfig.cs
Khi chạy trên cùng một máy: 127.0.0.1
Khi chạy nhiều máy trong cùng mạng LAN: Sử dụng địa chỉ IP của máy chạy Server.

## Chức năng

- [ ] Chức năng 1: Đăng ký
- Người dùng có thể:
Tạo tài khoản mới.
Nhập Username.
Nhập Password.
Lưu thông tin tài khoản vào Database.
- Hệ thống kiểm tra:
Password không dưới 6 ký tự.
Không cho phép Username trùng.
Không cho phép bỏ trống thông tin bắt buộc.

- [ ] Chức năng 2: Đăng nhập
- Người dùng có thể đăng nhập bằng:
Username.
Password.
- Sau khi đăng nhập thành công:
Tạo Session người dùng.
Kết nối đến Server.
Chuyển đến giao diện Chat.

- [ ] Chức năng 3: Chat 1-1
- Người dùng có thể:
Chọn một người dùng.
Gửi tin nhắn.
Nhận tin nhắn.
Hiển thị tên người gửi.
Hiển thị thời gian gửi.

- [ ] Chức năng 4: Chat Group
- Người dùng có thể:
Tạo Group.
Thêm thành viên.
Xóa thành viên.
Xem nhóm trưởng/ thành viên
Gửi tin nhắn trong Group.
- Server sẽ gửi tin nhắn đến các thành viên thuộc Group.

- [ ] Chức năng 5: Reply tin nhắn
- Người dùng có thể:
Chọn một tin nhắn.
Reply tin nhắn đó.
Hiển thị nội dung tin nhắn gốc.
Hiển thị người gửi của tin nhắn gốc.

- [ ] Chức năng 6:Forward tin nhắn
- Người dùng có thể:
Chọn một tin nhắn.
Chọn người nhận khác.
Forward tin nhắn.
Forward tin nhắn đến Group.

- [ ] Chức năng 7: Emoji
- Người dùng có thể:
Mở Emoji Picker.
Chọn Emoji.
Chèn Emoji vào nội dung tin nhắn.
Gửi Emoji.

- [ ] Chức năng 8: Avatar
- Người dùng có thể:
Hiển thị Avatar.
Upload Avatar.
Hiển thị Avatar trong danh sách User.
Hiển thị Avatar trong giao diện Chat.
Hiển thị Avatar trong khu vực Chat.

- [ ] Chức năng 9: Trạng thái Online/Offline
Server quản lý trạng thái kết nối của các User.
Khi User kết nối: Online
Khi User ngắt kết nối: Offline
Trạng thái được cập nhật đến các Client liên quan.

- [ ] Chức năng 10: Lịch sử trò chuyện
- Người dùng có thể:
Mở cuộc trò chuyện cũ.
Xem các tin nhắn đã gửi và nhận.
Server lấy dữ liệu từ Database và gửi về Client.

## Kiểm thử

- Functional test:

Khởi động ứng dụng Client.
Khởi động ứng dụng Server.
Kiểm tra giao diện GUI của Client và Server.
Kết nối Client đến Server bằng IP và Port cấu hình.
Ngắt kết nối và kết nối lại Client.
Đăng ký tài khoản mới.
Đăng nhập hệ thống.
Kiểm tra không thể sử dụng chức năng Chat khi chưa đăng nhập.
Hiển thị và cập nhật thông tin người dùng.
Cập nhật Avatar.
Hiển thị trạng thái Online/Offline.
Chat 1-1 giữa hai người dùng.
Gửi và nhận nhiều tin nhắn liên tiếp.
Hiển thị đúng nội dung, người gửi và thời gian gửi tin nhắn.
Chat Group.
Tạo Group.
Thêm thành viên vào Group.
Xóa thành viên khỏi Group.
Xem nhóm trưởng/thành viên trong group.
Gửi và nhận tin nhắn trong Group.
Reply tin nhắn.
Hủy Reply trước khi gửi.
Forward tin nhắn đến người dùng khác.
Forward tin nhắn đến Group.
Chọn và gửi Emoji.
Gửi tin nhắn chỉ chứa Emoji.
Gửi tin nhắn kết hợp văn bản và Emoji.
Hiển thị lịch sử Chat.
Kiểm tra dữ liệu User, Message và Group được lưu và đọc đúng từ Database.
Kiểm tra Server hiển thị danh sách Client đang kết nối.
Kiểm tra Server ghi Log các sự kiện quan trọng.

- Test dữ liệu không hợp lệ:

Đăng ký Username đã tồn tại.
Bỏ trống Username.
Bỏ trống Password.
Bỏ trống các thông tin bắt buộc khi đăng ký.
Đăng nhập sai Username.
Đăng nhập sai Password.
Nhập IP không hợp lệ.
Nhập Port không hợp lệ.
Kết nối khi Server chưa được khởi động.
Gửi dữ liệu Message không đúng định dạng.
Gửi dữ liệu JSON không hợp lệ.
Gửi dữ liệu chưa đầy đủ.
Kiểm tra Server xử lý dữ liệu không hợp lệ mà không bị dừng.
Kiểm tra dữ liệu chưa hoàn chỉnh không được xử lý như một Message hoàn chỉnh.

- Test mất kết nối:

Client chủ động ngắt kết nối khỏi Server.
Client mất kết nối đột ngột.
Server ngắt kết nối đột ngột.
Client xử lý được khi Server không còn hoạt động.
Server cập nhật trạng thái Client sau khi ngắt kết nối.
Trạng thái Online/Offline được cập nhật trên các Client liên quan.
Kết nối lại Client sau khi mất kết nối.
Server không bị dừng khi một Client xảy ra lỗi.
Các Client khác vẫn hoạt động bình thường khi một Client bị mất kết nối.
Socket và tài nguyên mạng được đóng đúng khi kết thúc kết nối.
Server ghi Log khi xảy ra kết nối, ngắt kết nối hoặc lỗi.
Log không chứa Password.

- Stress test:

Nhiều Client kết nối đến Server cùng lúc.
Nhiều Client đăng nhập đồng thời.
Nhiều Client gửi tin nhắn cùng lúc.
Một Client gửi nhiều tin nhắn liên tiếp.
Nhiều tin nhắn được gửi trong thời gian ngắn.
Nhiều Message được nhận trong một lần đọc.
Message chia thành nhiều lần nhận.
Kiểm tra Server vẫn hoạt động khi số lượng Message tăng.
Kiểm tra các Client vẫn hoạt động ổn định khi có nhiều kết nối.

- Performance test:

Thời gian phản hồi khi Client gửi tin nhắn.
Thời gian nhận tin nhắn từ Server.
Thời gian gửi tin nhắn giữa hai Client.
Khả năng xử lý nhiều Client cùng lúc.
Mức sử dụng CPU của Client.
Mức sử dụng CPU của Server.
Mức sử dụng RAM của Client.
Mức sử dụng RAM của Server.
Độ ổn định của khi số lượng tin nhắn tăng.
Độ ổn định của sau thời gian hoạt động liên tục.

Bằng chứng kiểm thử chi tiết được lưu tại `Extra/Test Case Template`.

## Demo

- Video: [Public hoặc Unlisted URL]
- Slide: `PPTX/`
- Báo cáo: `DOCX/`

## Giới hạn

Chức năng chưa hỗ trợ và giới hạn hiện tại của sản phẩm:

- Ứng dụng được xây dựng dưới dạng Desktop WinForms.
- Chưa hỗ trợ phiên bản Web.
- Chưa hỗ trợ gọi thoại hoặc gọi video.
- Chưa hỗ trợ gửi File dung lượng lớn.
- Chưa hỗ trợ gửi ảnh trong tin nhắn.
- Chưa hỗ trợ thông báo trong chat group khi thêm, xóa hoặc giải tán nhóm.
- Chức năng Avatar có giới hạn về dung lượng và định dạng ảnh.
- Hệ thống hoạt động theo mô hình một Server trung tâm, chưa hỗ trợ Server dự phòng.
- Khi Server dừng hoạt động, các Client đang kết nối sẽ bị mất kết nối.
- Chưa hỗ trợ mã hóa đầu cuối cho nội dung tin nhắn.
- Hệ thống chủ yếu được kiểm thử trong môi trường mạng LAN hoặc trên cùng một máy tính.
- Hiệu năng hệ thống phụ thuộc vào cấu hình máy chạy Server và số lượng Client đang kết nối.
- Một số chức năng nâng cao về bảo mật và mở rộng hệ thống chưa được triển khai.
- Hiệu năng hệ thống mới được kiểm thử với số lượng Client ở mức phù hợp với phạm vi đồ án.
- Giao diện và chức năng hiện tập trung vào các yêu cầu chính của đề tài, chưa tối ưu đầy đủ cho môi trường sử dụng thực tế quy mô lớn.

## Kiến trúc dự án:

```text
Code/
├── ChatTCP.Server/
│   ├── Forms/
│   │   ├── ServerForm.cs
│   │   └── ServerForm.resx
│   ├── Network/
│   │   ├── ClientConnection.cs
│   │   └── TcpServer.cs
│   ├── Services/
│   │   ├── ClientManager.cs
│   │   ├── DatabaseService.cs
│   │   ├── GroupHistoryService.cs
│   │   ├── GroupManager.cs
│   │   ├── GroupMessageService.cs
│   │   └── MessageHandler.cs
│   ├── Utils/
│   │   ├── Logger.cs
│   │   └── SecurityUtils.cs
│   ├── ChatTCP.Server.csproj
│   └── Program.cs
├── ChatTCP.Client/
│   ├── Forms/
│   │   ├── ClientForm.cs
│   │   ├── CreateGroupForm.cs
│   │   ├── CreateGroupForm.resx
│   │   ├── EmojiPickerForm.cs
│   │   ├── ForwardDialog.cs
│   │   ├── GroupMembersForm.cs
│   │   ├── LoginForm.cs
│   │   ├── LoginForm.resx
│   │   ├── RegisterForm.cs
│   │   └── RegisterForm.resx
│   ├── Network/
│   │   └── TcpClientManager.cs
│   ├── Resources/
│   │   └── Avatars/
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── ChatService.cs
│   │   ├── EmojiService.cs
│   │   ├── GroupHistoryService.cs
│   │   └── GroupService.cs
│   ├── UserControls/
│   │   ├── ChatBubble.cs
│   │   ├── EmojiInputBar.cs
│   │   └── UserItem.cs
│   ├── Utils/
│   │   ├── ImageUtils.cs
│   │   ├── MessageHelper.cs
│   │   └── SessionManager.cs
│   ├── ChatTCP.Client.csproj
│   └── Program.cs
└── ChatTCP.Shared/
│   ├── Enums/
│   │   └── MessageType.cs
│   ├── Models/
│   │   ├── CreateGroupRequest.cs
│   │   ├── CreateGroupResponse.cs
│   │   ├── Group.cs
│   │   ├── GroupHistoryRequest.cs
│   │   ├── GroupHistoryResponse.cs
│   │   ├── GroupManagementRequest.cs
│   │   ├── GroupManagementResponse.cs
│   │   ├── GroupMessageResult.cs
│   │   ├── Message.cs
│   │   └── User.cs
│   ├── Network/
│   │   └── MessageParser.cs
│   ├── Utils/
│   │   └── NetworkConfig.cs
│   └── ChatTCP.Shared.csproj
├── Database/
│   └── ChatTCP.sql
├── ChatTCP.slnx
├── DOCX/
├── Extra/
├── PPTX/
└── README.md
```
