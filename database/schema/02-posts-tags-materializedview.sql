USE [StackOverflow2010]
GO

-- Creating Materialized View for Post Tags with Creation Date
CREATE OR ALTER VIEW [dbo].[vwPostTags]
WITH SCHEMABINDING
AS
SELECT
	PT.PostId,
	PT.TagId,
	T.TagName,
	P.CreationDate
FROM dbo.Posts P INNER JOIN dbo.PostTags PT
ON P.Id=PT.PostId INNER JOIN dbo.Tags T
ON PT.TagId=T.Id
GO

-- Creating Clustered and Non-Clustered Indexes
IF EXISTS(SELECT * FROM sys.indexes WHERE name = 'CIX_vwPostTags_PostId_TagId')
DROP INDEX CIX_vwPostTags_PostId_TagId ON [dbo].[vwPostTags]
CREATE UNIQUE CLUSTERED INDEX CIX_vwPostTags_PostId_TagId ON dbo.vwPostTags(PostId,TagId)

IF EXISTS(SELECT * FROM sys.indexes WHERE name = 'NIX_vwPostTags_CreationDate')
DROP INDEX NIX_vwPostTags_CreationDate ON [dbo].[vwPostTags]
CREATE NONCLUSTERED INDEX NIX_vwPostTags_CreationDate ON [dbo].[vwPostTags]
(
	[CreationDate] ASC
)
INCLUDE([TagName])

IF EXISTS(SELECT * FROM sys.indexes WHERE name = 'NIX_vwPostTags_TagName')
DROP INDEX NIX_vwPostTags_TagName ON [dbo].[vwPostTags]
CREATE NONCLUSTERED INDEX NIX_vwPostTags_TagName ON [dbo].[vwPostTags]
(
	[TagName] ASC
)
INCLUDE([CreationDate])
GO