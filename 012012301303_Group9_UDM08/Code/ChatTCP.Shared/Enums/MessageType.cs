namespace ChatTCP.Shared.Enums
{
    public enum MessageType
    {
        // Authentication
        RegisterRequest,
        RegisterResponse,
        LoginRequest,
        LoginResponse,
        LogoutRequest,

        // Chat
        DirectChat,
        GroupChat,
        CreateGroupRequest,
        CreateGroupResponse,

        // History & Users
        GetChatHistoryRequest,
        GetChatHistoryResponse,
        GetUserListRequest,
        GetUserListResponse,
        GetGroupListRequest,
        GetGroupListResponse,

        // System & Status
        UserStatusUpdate,
        SystemNotification,
        Error
    }
}
