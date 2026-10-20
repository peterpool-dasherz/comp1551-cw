


CREATE DATABASE FestivalOperationsDB;
GO

USE FestivalOperationsDB;
GO

CREATE TABLE People
(
    PersonID INT IDENTITY(1, 1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Telephone NVARCHAR(30) NOT NULL,
    Email NVARCHAR(254) NOT NULL UNIQUE,
    Role NVARCHAR(20) NOT NULL,

    CONSTRAINT CK_People_Role
        CHECK (Role IN (N'Performer', N'Crew', N'Vendor'))
);
GO

CREATE TABLE Performers
(
    PersonID INT PRIMARY KEY,
    Fee DECIMAL(10, 2) NOT NULL,

    CONSTRAINT FK_Performers_People
        FOREIGN KEY (PersonID)
        REFERENCES People(PersonID)
        ON DELETE CASCADE,

    CONSTRAINT CK_Performers_Fee
        CHECK (Fee >= 0)
);
GO

CREATE TABLE Genres
(
    GenreID INT IDENTITY(1, 1) PRIMARY KEY,
    GenreName NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE PerformerGenres
(
    PersonID INT NOT NULL,
    GenreID INT NOT NULL,

    CONSTRAINT PK_PerformerGenres
        PRIMARY KEY (PersonID, GenreID),

    CONSTRAINT FK_PerformerGenres_Performers
        FOREIGN KEY (PersonID)
        REFERENCES Performers(PersonID)
        ON DELETE CASCADE,

    CONSTRAINT FK_PerformerGenres_Genres
        FOREIGN KEY (GenreID)
        REFERENCES Genres(GenreID)
        ON DELETE CASCADE
);
GO

CREATE TABLE Crew
(
    PersonID INT PRIMARY KEY,
    HourlyRate DECIMAL(8, 2) NOT NULL,
    EmploymentType NVARCHAR(10) NOT NULL,
    WeeklyHours INT NOT NULL,

    CONSTRAINT FK_Crew_People
        FOREIGN KEY (PersonID)
        REFERENCES People(PersonID)
        ON DELETE CASCADE,

    CONSTRAINT CK_Crew_HourlyRate
        CHECK (HourlyRate >= 0),

    CONSTRAINT CK_Crew_EmploymentType
        CHECK (EmploymentType IN (N'FullTime', N'PartTime')),

    CONSTRAINT CK_Crew_WeeklyHours
        CHECK (
            (EmploymentType = N'FullTime' AND WeeklyHours BETWEEN 25 AND 40)
            OR
            (EmploymentType = N'PartTime' AND WeeklyHours BETWEEN 1 AND 24)
        )
);
GO

CREATE TABLE Vendors
(
    PersonID INT PRIMARY KEY,

    CONSTRAINT FK_Vendors_People
        FOREIGN KEY (PersonID)
        REFERENCES People(PersonID)
        ON DELETE CASCADE
);
GO

CREATE TABLE ProductCategories
(
    CategoryID INT IDENTITY(1, 1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE VendorCategories
(
    PersonID INT NOT NULL,
    CategoryID INT NOT NULL,

    CONSTRAINT PK_VendorCategories
        PRIMARY KEY (PersonID, CategoryID),

    CONSTRAINT FK_VendorCategories_Vendors
        FOREIGN KEY (PersonID)
        REFERENCES Vendors(PersonID)
        ON DELETE CASCADE,

    CONSTRAINT FK_VendorCategories_ProductCategories
        FOREIGN KEY (CategoryID)
        REFERENCES ProductCategories(CategoryID)
        ON DELETE CASCADE
);
GO