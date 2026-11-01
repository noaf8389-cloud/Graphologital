-- Graphologital Database
-- יצירת מסד נתונים וטבלאות

IF DB_ID('GraphologitalDB') IS NULL
BEGIN
    CREATE DATABASE GraphologitalDB;
END
GO

USE GraphologitalDB;
GO

-- מחיקה לפי סדר תלות, כדי שאפשר יהיה להריץ מחדש בזמן פיתוח
IF OBJECT_ID('CandidateTargetLinks', 'U') IS NOT NULL DROP TABLE CandidateTargetLinks;
IF OBJECT_ID('Analyses', 'U') IS NOT NULL DROP TABLE Analyses;
IF OBJECT_ID('TargetUsers', 'U') IS NOT NULL DROP TABLE TargetUsers;
IF OBJECT_ID('Candidates', 'U') IS NOT NULL DROP TABLE Candidates;
GO

-- 1. טבלת מועמדים

CREATE TABLE Candidates (
    Id            INT PRIMARY KEY IDENTITY(1,1),
    Username      NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(256) NOT NULL,
    FullName      NVARCHAR(100) NULL,
    Phone         NVARCHAR(20)  NULL,
    Email         NVARCHAR(100) NULL,
    CreatedAt     DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- 2. טבלת משתמשי יעד

CREATE TABLE TargetUsers (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    Username        NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(256) NOT NULL,
    FullName        NVARCHAR(100) NULL,
    Phone           NVARCHAR(20)  NULL,
    Email           NVARCHAR(100) NULL,
    IsActive        BIT           NOT NULL DEFAULT 0,
    RoleDescription NVARCHAR(200) NULL,
    CreatedAt       DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- 3. טבלת אבחונים ותוצאות

CREATE TABLE Analyses (
    Id                 INT PRIMARY KEY IDENTITY(1,1),
    CandidateId        INT NOT NULL,
    HandwritingImage   NVARCHAR(300) NULL,
    AnalysisDate       DATETIME      NOT NULL DEFAULT GETDATE(),

    TopPersonalityType NVARCHAR(10)  NULL,
    PersonalityName    NVARCHAR(50)  NULL,
    DiagnosisText      NVARCHAR(MAX) NULL,

    ScoreTypeA FLOAT NULL,
    ScoreTypeB FLOAT NULL,
    ScoreTypeC FLOAT NULL,
    ScoreTypeD FLOAT NULL,
    ScoreTypeE FLOAT NULL,
    ScoreTypeF FLOAT NULL,
    ScoreTypeG FLOAT NULL,
    ScoreTypeH FLOAT NULL,
    ScoreTypeI FLOAT NULL,

    CONSTRAINT FK_Analyses_Candidates
        FOREIGN KEY (CandidateId)
        REFERENCES Candidates(Id)
        ON DELETE CASCADE
);
GO

-- 4. טבלת הרשאות שיתוף

CREATE TABLE CandidateTargetLinks (
    Id            INT PRIMARY KEY IDENTITY(1,1),
    CandidateId   INT NOT NULL,
    TargetUserId  INT NOT NULL,
    SharedAt      DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Links_Candidates
        FOREIGN KEY (CandidateId)
        REFERENCES Candidates(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Links_TargetUsers
        FOREIGN KEY (TargetUserId)
        REFERENCES TargetUsers(Id)
        ON DELETE NO ACTION,

    CONSTRAINT UQ_Candidate_Target
        UNIQUE (CandidateId, TargetUserId)
);
GO

-- אינדקסים לשיפור שליפות

CREATE INDEX IX_Analyses_CandidateId
ON Analyses(CandidateId);
GO

CREATE INDEX IX_Links_CandidateId
ON CandidateTargetLinks(CandidateId);
GO

CREATE INDEX IX_Links_TargetUserId
ON CandidateTargetLinks(TargetUserId);
GO

PRINT 'GraphologitalDB tables created successfully.';
GO