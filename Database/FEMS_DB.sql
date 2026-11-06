
-- =============================================
-- FAMILY EXPENSE MANAGEMENT SYSTEM (FEMS)
-- SQL SERVER
-- =============================================

CREATE DATABASE FEMS_DB;
GO

USE FEMS_DB;
GO

-- =============================================
-- 1. USER
-- =============================================
CREATE TABLE [User] (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Email VARCHAR(100) NOT NULL UNIQUE,
    [Password] VARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(15)
);
GO

-- =============================================
-- 2. HOUSEHOLD
-- =============================================
CREATE TABLE Household (
    HouseholdID INT IDENTITY(1,1) PRIMARY KEY,
    HouseholdName NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',

    CONSTRAINT CK_Household_Status
        CHECK (Status IN ('Active', 'Inactive'))
);
GO

-- =============================================
-- 3. HOUSEHOLD MEMBER
-- =============================================
CREATE TABLE HouseholdMember (
    HouseholdMemberID INT IDENTITY(1,1) PRIMARY KEY,
    HouseholdID INT NOT NULL,
    UserID INT NOT NULL,
    Role VARCHAR(20) NOT NULL DEFAULT 'Member',
    JoinedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    AlertAmount DECIMAL(18,2) NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',

    CONSTRAINT FK_HouseholdMember_Household
        FOREIGN KEY (HouseholdID)
        REFERENCES Household(HouseholdID),

    CONSTRAINT FK_HouseholdMember_User
        FOREIGN KEY (UserID)
        REFERENCES [User](UserID),

    CONSTRAINT UQ_HouseholdMember
        UNIQUE (HouseholdID, UserID),

    CONSTRAINT CK_HouseholdMember_Role
        CHECK (Role IN ('Manager', 'Member')),

    CONSTRAINT CK_HouseholdMember_Status
        CHECK (Status IN ('Active', 'Inactive')),

    CONSTRAINT CK_HouseholdMember_AlertAmount
        CHECK (AlertAmount IS NULL OR AlertAmount >= 0)
);
GO

-- =============================================
-- 4. NOTIFICATION
-- =============================================
CREATE TABLE Notification (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Notification_User
        FOREIGN KEY (UserID)
        REFERENCES [User](UserID)
);
GO

-- =============================================
-- 5. CATEGORY
-- =============================================
CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    HouseholdMemberID INT NOT NULL,
    Description NVARCHAR(500),
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',

    CONSTRAINT FK_Category_HouseholdMember
        FOREIGN KEY (HouseholdMemberID)
        REFERENCES HouseholdMember(HouseholdMemberID),

    CONSTRAINT CK_Category_Status
        CHECK (Status IN ('Active', 'Inactive'))
);
GO

-- =============================================
-- 6. WALLET
-- =============================================
CREATE TABLE Wallet (
    WalletID INT IDENTITY(1,1) PRIMARY KEY,
    HouseholdMemberID INT NOT NULL,
    WalletName NVARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    LimitAmount DECIMAL(18,2) NULL,

    CONSTRAINT FK_Wallet_HouseholdMember
        FOREIGN KEY (HouseholdMemberID)
        REFERENCES HouseholdMember(HouseholdMemberID),

    CONSTRAINT CK_Wallet_LimitAmount
        CHECK (LimitAmount IS NULL OR LimitAmount >= 0)
);
GO

-- =============================================
-- 7. INCOME
-- =============================================
CREATE TABLE Income (
    IncomeID INT IDENTITY(1,1) PRIMARY KEY,
    HouseholdMemberID INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    IncomeDate DATE NOT NULL,
    Description NVARCHAR(500),
    WalletID INT NULL,

    CONSTRAINT FK_Income_HouseholdMember
        FOREIGN KEY (HouseholdMemberID)
        REFERENCES HouseholdMember(HouseholdMemberID),

    CONSTRAINT FK_Income_Wallet
        FOREIGN KEY (WalletID)
        REFERENCES Wallet(WalletID),

    CONSTRAINT CK_Income_Amount
        CHECK (Amount > 0)
);
GO

-- =============================================
-- 8. EXPENSE
-- =============================================
CREATE TABLE Expense (
    ExpenseID INT IDENTITY(1,1) PRIMARY KEY,
    HouseholdMemberID INT NOT NULL,
    CategoryID INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    ExpenseDate DATE NOT NULL,
    Description NVARCHAR(500),
    WalletID INT NULL,

    CONSTRAINT FK_Expense_HouseholdMember
        FOREIGN KEY (HouseholdMemberID)
        REFERENCES HouseholdMember(HouseholdMemberID),

    CONSTRAINT FK_Expense_Category
        FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID),

    CONSTRAINT FK_Expense_Wallet
        FOREIGN KEY (WalletID)
        REFERENCES Wallet(WalletID),

    CONSTRAINT CK_Expense_Amount
        CHECK (Amount > 0)
);
GO


-- =============================================
-- 1. INSERT USERS
-- =============================================
INSERT INTO [User] (Email, [Password], FullName, Phone)
VALUES
('minh@gmail.com', '123456', N'Nguyễn Văn Minh', '0901000001'),
('lan@gmail.com',   '123456', N'Nguyễn Thị Lan', '0901000002'),
('nam@gmail.com',   '123456', N'Nguyễn Văn Nam',  '0901000003'),
('hoa@gmail.com',   '123456', N'Nguyễn Thị Hoa',  '0901000004'),
('an@gmail.com',    '123456', N'Nguyễn Văn An',   '0901000005');
GO

-- =============================================
-- 2. INSERT HOUSEHOLD
-- =============================================
INSERT INTO Household (HouseholdName, Status)
VALUES (N'Gia đình Nguyễn Văn Minh', 'Active');
GO

-- =============================================
-- 3. INSERT HOUSEHOLD MEMBERS
-- UserID 1: Manager
-- UserID 2-5: Members
-- =============================================
INSERT INTO HouseholdMember
    (HouseholdID, UserID, Role, AlertAmount, Status)
VALUES
(1, 1, 'Manager', 2000000, 'Active'),
(1, 2, 'Member',  1000000, 'Active'),
(1, 3, 'Member',  1500000, 'Active'),
(1, 4, 'Member',  1000000, 'Active'),
(1, 5, 'Member',   500000, 'Active');
GO

-- =============================================
-- 4. INSERT WALLETS
-- Amount = Current balance
-- LimitAmount = Spending limit
-- =============================================
INSERT INTO Wallet
    (HouseholdMemberID, WalletName, Amount, LimitAmount)
VALUES
(1, N'Ví chính - Minh',       15000000, 10000000),
(2, N'Ví cá nhân - Lan',       5000000,  3000000),
(3, N'Ví cá nhân - Nam',       3500000,  2500000),
(4, N'Ví cá nhân - Hoa',       4000000,  2000000),
(5, N'Ví cá nhân - An',        2000000,  1500000);
GO

-- =============================================
-- 5. INSERT CATEGORIES
-- Each member creates their own categories
-- =============================================
INSERT INTO Category
    (CategoryName, HouseholdMemberID, Description, Status)
VALUES
-- Manager
(N'Ăn uống',       1, N'Chi phí ăn uống gia đình', 'Active'),
(N'Sinh hoạt',     1, N'Điện, nước, internet',      'Active'),
(N'Giải trí',      1, N'Chi phí giải trí',         'Active'),

-- Member: Lan
(N'Ăn uống',       2, N'Chi phí ăn uống cá nhân',  'Active'),
(N'Mua sắm',       2, N'Chi phí mua sắm',         'Active'),

-- Member: Nam
(N'Ăn uống',       3, N'Chi phí ăn uống cá nhân',  'Active'),
(N'Di chuyển',     3, N'Chi phí đi lại',           'Active'),

-- Member: Hoa
(N'Ăn uống',       4, N'Chi phí ăn uống cá nhân',  'Active'),
(N'Học tập',       4, N'Chi phí học tập',          'Active'),

-- Member: An
(N'Ăn uống',       5, N'Chi phí ăn uống cá nhân',  'Active'),
(N'Giải trí',      5, N'Chi phí giải trí',         'Active');
GO

-- =============================================
-- 6. INSERT INCOMES
-- =============================================
INSERT INTO Income
    (HouseholdMemberID, Amount, IncomeDate, Description, WalletID)
VALUES
(1, 20000000, '2026-11-01', N'Lương tháng 11', 1),
(2,  8000000, '2026-11-01', N'Lương tháng 11', 2),
(3,  7000000, '2026-11-02', N'Lương tháng 11', 3),
(4,  6000000, '2026-11-02', N'Lương tháng 11', 4),
(5,  5000000, '2026-11-03', N'Lương tháng 11', 5);
GO

-- =============================================
-- 7. INSERT EXPENSES
-- =============================================
INSERT INTO Expense
    (HouseholdMemberID, CategoryID, Amount,
     ExpenseDate, Description, WalletID)
VALUES
-- Manager
(1, 1,  500000, '2026-11-04', N'Mua thực phẩm',       1),
(1, 2, 1200000, '2026-11-04', N'Thanh toán tiền điện', 1),

-- Lan
(2, 4,  150000, '2026-11-04', N'Ăn trưa',             2),
(2, 5,  300000, '2026-11-04', N'Mua quần áo',         2),

-- Nam
(3, 6,  100000, '2026-11-04', N'Ăn sáng',             3),
(3, 7,  200000, '2026-11-04', N'Đổ xăng',              3),

-- Hoa
(4, 8,  120000, '2026-11-04', N'Ăn trưa',             4),
(4, 9,  500000, '2026-11-04', N'Mua sách',             4),

-- An
(5,10,   80000, '2026-11-04', N'Ăn sáng',             5),
(5,11,  150000, '2026-11-04', N'Xem phim',              5);
GO

-- =============================================
-- 8. INSERT NOTIFICATIONS
-- =============================================
INSERT INTO Notification (UserID, Message, IsRead)
VALUES
(1, N'Nguyễn Thị Lan vừa tạo một khoản chi mới.', 0),
(1, N'Ví chính của bạn đã gần đạt hạn mức chi tiêu.', 0),
(2, N'Chào mừng bạn đến với FEMS!', 1),
(3, N'Chào mừng bạn đến với FEMS!', 1),
(4, N'Chào mừng bạn đến với FEMS!', 1),
(5, N'Chào mừng bạn đến với FEMS!', 1);
GO
