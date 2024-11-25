-- Tables

CREATE TABLE [dbo].[Companies](
	[Id] [uniqueidentifier] NOT NULL,
	[RowVersion] [timestamp] NOT NULL,
	[Content] [nvarchar](max) NULL,
 CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE TABLE [dbo].[Employees](
	[Id] [uniqueidentifier] NOT NULL,
	[RowVersion] [timestamp] NOT NULL,
	[CompanyId] [uniqueidentifier] NOT NULL,
	[Content] [nvarchar](max) NULL,
	[Age] [int] NOT NULL,
 CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_Employees_CompanyId] ON [dbo].[Employees]
(
	[CompanyId] ASC
) ON [PRIMARY]