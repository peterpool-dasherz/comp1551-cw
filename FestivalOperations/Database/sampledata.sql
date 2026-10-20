USE FestivalOperationsDB;
GO


INSERT INTO People (Name, Telephone, Email, Role)
SELECT N'Mai Nguyen', N'0123456789', N'mai@example.com', N'Performer'
WHERE NOT EXISTS (
    SELECT 1 FROM People WHERE Email = N'mai@example.com'
);

INSERT INTO People (Name, Telephone, Email, Role)
SELECT N'Alex Tran', N'0987654321', N'alex@example.com', N'Crew'
WHERE NOT EXISTS (
    SELECT 1 FROM People WHERE Email = N'alex@example.com'
);

INSERT INTO People (Name, Telephone, Email, Role)
SELECT N'Lan Pham', N'09123456789', N'lan@example.com', N'Vendor'
WHERE NOT EXISTS (
    SELECT 1 FROM People WHERE Email = N'lan@example.com'
);


INSERT INTO Performers (PersonID, Fee)
SELECT p.PersonID, 500.00
FROM People AS p
WHERE p.Email = N'mai@example.com'
  AND NOT EXISTS (
      SELECT 1 FROM Performers AS performer
      WHERE performer.PersonID = p.PersonID
  );

-- Add genres if they do not already exist.
INSERT INTO Genres (GenreName)
SELECT N'Pop'
WHERE NOT EXISTS (
    SELECT 1 FROM Genres WHERE GenreName = N'Pop'
);

INSERT INTO Genres (GenreName)
SELECT N'Indie'
WHERE NOT EXISTS (
    SELECT 1 FROM Genres WHERE GenreName = N'Indie'
);

-- Link the performer to their genres if the links are missing.
INSERT INTO PerformerGenres (PersonID, GenreID)
SELECT p.PersonID, g.GenreID
FROM People AS p
CROSS JOIN Genres AS g
WHERE p.Email = N'mai@example.com'
  AND g.GenreName IN (N'Pop', N'Indie')
  AND NOT EXISTS (
      SELECT 1
      FROM PerformerGenres AS pg
      WHERE pg.PersonID = p.PersonID
        AND pg.GenreID = g.GenreID
  );

-- Add the crew record if it does not already exist.
INSERT INTO Crew (PersonID, HourlyRate, EmploymentType, WeeklyHours)
SELECT p.PersonID, 15.00, N'PartTime', 20
FROM People AS p
WHERE p.Email = N'alex@example.com'
  AND NOT EXISTS (
      SELECT 1 FROM Crew AS c
      WHERE c.PersonID = p.PersonID
  );

-- Add the vendor record if it does not already exist.
INSERT INTO Vendors (PersonID)
SELECT p.PersonID
FROM People AS p
WHERE p.Email = N'lan@example.com'
  AND NOT EXISTS (
      SELECT 1 FROM Vendors AS v
      WHERE v.PersonID = p.PersonID
  );

-- Add product categories if they do not already exist.
INSERT INTO ProductCategories (CategoryName)
SELECT N'Food'
WHERE NOT EXISTS (
    SELECT 1 FROM ProductCategories WHERE CategoryName = N'Food'
);

INSERT INTO ProductCategories (CategoryName)
SELECT N'Drinks'
WHERE NOT EXISTS (
    SELECT 1 FROM ProductCategories WHERE CategoryName = N'Drinks'
);

-- Link the vendor to their product categories if the links are missing.
INSERT INTO VendorCategories (PersonID, CategoryID)
SELECT p.PersonID, pc.CategoryID
FROM People AS p
CROSS JOIN ProductCategories AS pc
WHERE p.Email = N'lan@example.com'
  AND pc.CategoryName IN (N'Food', N'Drinks')
  AND NOT EXISTS (
      SELECT 1
      FROM VendorCategories AS vc
      WHERE vc.PersonID = p.PersonID
        AND vc.CategoryID = pc.CategoryID
  );
GO