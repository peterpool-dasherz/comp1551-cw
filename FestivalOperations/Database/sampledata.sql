USE FestivalOperationsDB;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    
    INSERT INTO dbo.People (Name, Telephone, Email, Role)
    SELECT sample.Name, sample.Telephone, sample.Email, sample.Role
    FROM (VALUES
        (N'Mai Nguyen', N'0123456789', N'mai@example.com', N'Performer'),
        (N'Duc Pham', N'0901111222', N'duc@example.com', N'Performer'),
        (N'Alex Tran', N'0987654321', N'alex@example.com', N'Crew'),
        (N'Nhi Le', N'0903333444', N'nhi@example.com', N'Crew'),
        (N'Lan Pham', N'09123456789', N'lan@example.com', N'Vendor'),
        (N'Bao Vu', N'0905555666', N'bao@example.com', N'Vendor')
    ) AS sample (Name, Telephone, Email, Role)
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.People AS person
        WHERE person.Email = sample.Email
    );

    
    INSERT INTO dbo.Performers (PersonID, Fee)
    SELECT person.PersonID, sample.Fee
    FROM (VALUES
        (N'mai@example.com', CAST(500.00 AS DECIMAL(10, 2))),
        (N'duc@example.com', CAST(750.00 AS DECIMAL(10, 2)))
    ) AS sample (Email, Fee)
    INNER JOIN dbo.People AS person
        ON person.Email = sample.Email
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Performers AS performer
        WHERE performer.PersonID = person.PersonID
    );

   
    INSERT INTO dbo.Genres (GenreName)
    SELECT sample.GenreName
    FROM (VALUES
        (N'Pop'),
        (N'Indie'),
        (N'Rock'),
        (N'Electronic')
    ) AS sample (GenreName)
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Genres AS genre
        WHERE genre.GenreName = sample.GenreName
    );

    
    INSERT INTO dbo.PerformerGenres (PersonID, GenreID)
    SELECT person.PersonID, genre.GenreID
    FROM (VALUES
        (N'mai@example.com', N'Pop'),
        (N'mai@example.com', N'Indie'),
        (N'duc@example.com', N'Rock'),
        (N'duc@example.com', N'Electronic')
    ) AS sample (Email, GenreName)
    INNER JOIN dbo.People AS person
        ON person.Email = sample.Email
    INNER JOIN dbo.Genres AS genre
        ON genre.GenreName = sample.GenreName
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.PerformerGenres AS link
        WHERE link.PersonID = person.PersonID
          AND link.GenreID = genre.GenreID
    );

    
    INSERT INTO dbo.Crew (PersonID, HourlyRate, EmploymentType, WeeklyHours)
    SELECT person.PersonID, sample.HourlyRate,
           sample.EmploymentType, sample.WeeklyHours
    FROM (VALUES
        (N'alex@example.com', CAST(15.00 AS DECIMAL(8, 2)),
         N'PartTime', 20),
        (N'nhi@example.com', CAST(22.50 AS DECIMAL(8, 2)),
         N'FullTime', 35)
    ) AS sample (Email, HourlyRate, EmploymentType, WeeklyHours)
    INNER JOIN dbo.People AS person
        ON person.Email = sample.Email
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Crew AS crew
        WHERE crew.PersonID = person.PersonID
    );

   
    INSERT INTO dbo.Vendors (PersonID)
    SELECT person.PersonID
    FROM (VALUES
        (N'lan@example.com'),
        (N'bao@example.com')
    ) AS sample (Email)
    INNER JOIN dbo.People AS person
        ON person.Email = sample.Email
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Vendors AS vendor
        WHERE vendor.PersonID = person.PersonID
    );

    
    INSERT INTO dbo.ProductCategories (CategoryName)
    SELECT sample.CategoryName
    FROM (VALUES
        (N'Food'),
        (N'Drinks'),
        (N'Merch'),
        (N'Clothing'),
        (N'Art')
    ) AS sample (CategoryName)
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.ProductCategories AS category
        WHERE category.CategoryName = sample.CategoryName
    );

    
    INSERT INTO dbo.VendorCategories (PersonID, CategoryID)
    SELECT person.PersonID, category.CategoryID
    FROM (VALUES
        (N'lan@example.com', N'Food'),
        (N'lan@example.com', N'Drinks'),
        (N'lan@example.com', N'Merch'),
        (N'bao@example.com', N'Food'),
        (N'bao@example.com', N'Clothing'),
        (N'bao@example.com', N'Art')
    ) AS sample (Email, CategoryName)
    INNER JOIN dbo.People AS person
        ON person.Email = sample.Email
    INNER JOIN dbo.ProductCategories AS category
        ON category.CategoryName = sample.CategoryName
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.VendorCategories AS link
        WHERE link.PersonID = person.PersonID
          AND link.CategoryID = category.CategoryID
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO