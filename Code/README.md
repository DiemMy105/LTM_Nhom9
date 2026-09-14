# Code

Kiến trúc dự án:

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
