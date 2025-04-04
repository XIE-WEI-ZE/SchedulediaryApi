CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(50),
    Account NVARCHAR(50),
    Email NVARCHAR(100),
    PasswordHash NVARCHAR(100),
    Gender NVARCHAR(10),
    BirthDate DATE,
    CreatedAt DATETIME,
    AvatarPath NVARCHAR(100),
    DueDateTime NVARCHAR(50),
    Provider¡@NVARCHAR(200),
    EmailVerified BIT,
    AvatarUrl NVARCHAR(200)
);

CREATE TABLE ToDoEvents (
    UserId INT ,
    Title NVARCHAR(50),
    Description NVARCHAR(100),
    PriorityLevel INT,
    IsCompleted BIT,
    CreatedAt DATETIME,
    Category NVARCHAR(50),
);
