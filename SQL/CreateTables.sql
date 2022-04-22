CREATE TABLE [dbo].[PhotoVisibilities] (
[Id] INT IDENTITY (1, 1) NOT NULL,
[Name] NVARCHAR (50) NULL,
PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE TABLE [dbo].[Photos] (
[Id] INT IDENTITY (1, 1) NOT NULL,
[UserId] INT NOT NULL,
[Title] NVARCHAR (50) NOT NULL,
[Description] NVARCHAR (MAX) NULL,
[VisibilityId] INT NOT NULL,
[GUID] NVARCHAR (36) NOT NULL,
[CreationDate] DATETIME NOT NULL,
[Ratings] FLOAT (53) NOT NULL,
[RatingsCount] INT NULL,
PRIMARY KEY CLUSTERED ([Id] ASC),
FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]),
FOREIGN KEY ([VisibilityId]) REFERENCES [dbo].[PhotoVisibilities] ([Id])
);

CREATE TABLE [dbo].[PhotoRatings] (
[Id] INT IDENTITY (1, 1) NOT NULL,
[PhotoId] INT NOT NULL,
[UserId] INT NOT NULL,
[Comment] NVARCHAR (MAX) NOT NULL,
[Rating] INT NOT NULL,
[CreationDate] DATETIME NOT NULL,
PRIMARY KEY CLUSTERED ([Id] ASC),
FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]),
FOREIGN KEY ([PhotoId]) REFERENCES [dbo].[Photos] ([Id])
);