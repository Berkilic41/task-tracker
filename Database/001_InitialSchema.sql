-- =============================================================
-- Task Tracker Database Schema
-- Migration: 001_InitialSchema
-- Run this script against your SQL Server instance first.
-- =============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TaskTrackerDb')
BEGIN
    CREATE DATABASE TaskTrackerDb;
END
GO

USE TaskTrackerDb;
GO

-- ---------------------------------------------------------------
-- Users
-- ---------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id            INT            IDENTITY(1,1)  NOT NULL,
        Username      NVARCHAR(50)                  NOT NULL,
        Email         NVARCHAR(255)                 NOT NULL,
        PasswordHash  NVARCHAR(512)                 NOT NULL,
        CreatedAt     DATETIME2(0)                  NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Users          PRIMARY KEY (Id),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email    UNIQUE (Email)
    );
END
GO

-- ---------------------------------------------------------------
-- Tasks
-- Status:   0 = Todo | 1 = InProgress | 2 = Done
-- Priority: 0 = Low  | 1 = Medium     | 2 = High
-- ---------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tasks')
BEGIN
    CREATE TABLE Tasks (
        Id               INT            IDENTITY(1,1)  NOT NULL,
        Title            NVARCHAR(200)                 NOT NULL,
        Description      NVARCHAR(MAX)                 NULL,
        Status           TINYINT                       NOT NULL CONSTRAINT DF_Tasks_Status   DEFAULT 0,
        Priority         TINYINT                       NOT NULL CONSTRAINT DF_Tasks_Priority DEFAULT 1,
        DueDate          DATE                          NULL,
        CreatedAt        DATETIME2(0)                  NOT NULL CONSTRAINT DF_Tasks_CreatedAt  DEFAULT GETUTCDATE(),
        UpdatedAt        DATETIME2(0)                  NOT NULL CONSTRAINT DF_Tasks_UpdatedAt  DEFAULT GETUTCDATE(),
        CreatedByUserId  INT                           NOT NULL,
        AssignedToUserId INT                           NULL,

        CONSTRAINT PK_Tasks             PRIMARY KEY (Id),
        CONSTRAINT CK_Tasks_Status      CHECK (Status   IN (0, 1, 2)),
        CONSTRAINT CK_Tasks_Priority    CHECK (Priority IN (0, 1, 2)),
        CONSTRAINT FK_Tasks_CreatedBy   FOREIGN KEY (CreatedByUserId)  REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Tasks_AssignedTo  FOREIGN KEY (AssignedToUserId) REFERENCES Users(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_Tasks_Status           ON Tasks (Status);
    CREATE INDEX IX_Tasks_Priority         ON Tasks (Priority);
    CREATE INDEX IX_Tasks_AssignedToUserId ON Tasks (AssignedToUserId);
    CREATE INDEX IX_Tasks_CreatedByUserId  ON Tasks (CreatedByUserId);
END
GO

-- ---------------------------------------------------------------
-- Comments
-- ---------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Comments')
BEGIN
    CREATE TABLE Comments (
        Id        INT           IDENTITY(1,1)  NOT NULL,
        TaskId    INT                          NOT NULL,
        UserId    INT                          NOT NULL,
        Content   NVARCHAR(MAX)               NOT NULL,
        CreatedAt DATETIME2(0)                NOT NULL CONSTRAINT DF_Comments_CreatedAt DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Comments      PRIMARY KEY (Id),
        CONSTRAINT FK_Comments_Task FOREIGN KEY (TaskId) REFERENCES Tasks(Id)  ON DELETE CASCADE,
        CONSTRAINT FK_Comments_User FOREIGN KEY (UserId) REFERENCES Users(Id)  ON DELETE NO ACTION
    );

    CREATE INDEX IX_Comments_TaskId ON Comments (TaskId);
END
GO

PRINT 'Schema created successfully.';
GO
