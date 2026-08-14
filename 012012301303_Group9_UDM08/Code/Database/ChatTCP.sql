
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ChatTCP')
BEGIN
    CREATE DATABASE ChatTCP;
END
GO

USE ChatTCP;
GO

-- Drop tables if they already exist to allow clean re-execution
-- IF OBJECT_ID('dbo.Messages', 'U') IS NOT NULL DROP TABLE dbo.Messages;
-- IF OBJECT_ID('dbo.GroupMembers', 'U') IS NOT NULL DROP TABLE dbo.GroupMembers;
-- IF OBJECT_ID('dbo.Groups', 'U') IS NOT NULL DROP TABLE dbo.Groups;
-- IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
-- GO

-- 1.Quản lí người dùng (Users)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(100) NOT NULL,
    Avatar NVARCHAR(255) NULL DEFAULT 'default.png',
    Status NVARCHAR(20) NOT NULL DEFAULT 'Offline',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- 2.Quản lí nhóm chat (Groups)
CREATE TABLE Groups (
    GroupId INT IDENTITY(1,1) PRIMARY KEY,
    GroupName NVARCHAR(100) NOT NULL,
    CreatedBy INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Groups_Users FOREIGN KEY (CreatedBy) REFERENCES Users(UserId) ON DELETE SET NULL
);
GO

-- 3.Quản lí thành viên trong nhóm chat (GroupMembers)
CREATE TABLE GroupMembers (
    GroupId INT NOT NULL,
    UserId INT NOT NULL,
    JoinedAt DATETIME NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (GroupId, UserId),
    CONSTRAINT FK_GroupMembers_Groups FOREIGN KEY (GroupId) REFERENCES Groups(GroupId) ON DELETE CASCADE,
    CONSTRAINT FK_GroupMembers_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- 4.Quản lí tin nhắn 1-1 và Chat nhóm (Messages)
CREATE TABLE Messages (
    MessageId INT IDENTITY(1,1) PRIMARY KEY,
    SenderId INT NOT NULL,
    ReceiverId INT NULL,
    GroupId INT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    MessageType NVARCHAR(20) NOT NULL DEFAULT 'Text',
    ReplyToMessageId INT NULL,
    IsForwarded BIT NOT NULL DEFAULT 0,
    SentAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Messages_Sender FOREIGN KEY (SenderId) REFERENCES Users(UserId),
    CONSTRAINT FK_Messages_Receiver FOREIGN KEY (ReceiverId) REFERENCES Users(UserId),
    CONSTRAINT FK_Messages_Group FOREIGN KEY (GroupId) REFERENCES Groups(GroupId) ON DELETE CASCADE,
    CONSTRAINT FK_Messages_ReplyTo FOREIGN KEY (ReplyToMessageId) REFERENCES Messages(MessageId),
    CONSTRAINT CHK_MessageTarget CHECK (
        (ReceiverId IS NOT NULL AND GroupId IS NULL) OR 
        (ReceiverId IS NULL AND GroupId IS NOT NULL)
    )
);
GO

-- Tối ưu cho truy vấn database
CREATE INDEX IX_Messages_DirectChat ON Messages(SenderId, ReceiverId, SentAt);
CREATE INDEX IX_Messages_GroupChat ON Messages(GroupId, SentAt);
CREATE INDEX IX_Users_Username ON Users(Username);
GO

