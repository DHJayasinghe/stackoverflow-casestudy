USE [StackOverflow2010]
GO

CREATE OR ALTER  PROCEDURE [dbo].[sp_posts_search](
	@Start int = 0,
	@PageSize int = 10,
	@SearchType varchar(100),
	@Tags varchar(1000)
)
AS
BEGIN
	declare @dynamic_sql nvarchar(2000)
	
	declare @param_definition nvarchar(500)='@Start int, @PageSize int';  
	declare @predicate nvarchar(1000)=CASE @Tags WHEN '' THEN '1=1' ELSE (SELECT STRING_AGG( CONCAT('Tags LIKE ''%',value,'%'''),' AND ') from string_split(@Tags,',')) END

	declare @total_count_dynamic_sql nvarchar(2000)
	declare @total_count int
	SET @total_count_dynamic_sql = CONCAT('SELECT @total_count=COUNT(1) FROM dbo.Posts WHERE ', @predicate)

	IF @SearchType = 'NEWEST'
	BEGIN
		SET @dynamic_sql = CONCAT('SELECT
			P.Id,
			P.Title,
			Substring(P.Body, 1, 200)[Description],
			P.Tags,
			P.CreationDate AskedDateTime,
			P.OwnerUserId AskedById,
			Own.DisplayName AskedByDisplayName,
			1 NoOfVotes
		FROM dbo.Posts P INNER JOIN(SELECT Id AS PostId
		FROM dbo.Posts 
		WHERE ', @predicate,' ',
		'ORDER BY CreationDate DESC
		OFFSET(@Start) ROWS FETCH NEXT(@PageSize) ROWS ONLY) PT
		ON P.Id = PT.PostId LEFT JOIN dbo.Users Own
		ON P.OwnerUserId = Own.Id')

		EXEC sp_executesql @dynamic_sql, @param_definition, @Start=@Start, @PageSize=@PageSize
		EXEC sp_executesql @total_count_dynamic_sql, N'@total_count int OUT', @total_count=@total_count OUT
	END
	ELSE IF @SearchType = 'ACTIVE'
	BEGIN
		SET @dynamic_sql = CONCAT('SELECT
			P.Id,
			P.Title,
			Substring(P.Body, 1, 200)[Description],
			P.Tags,
			P.CreationDate AskedDateTime,
			P.OwnerUserId AskedById,
			Own.DisplayName AskedByDisplayName,
			1 NoOfVotes
		FROM dbo.Posts P INNER JOIN(SELECT Id AS PostId
		FROM dbo.Posts 
		WHERE ', @predicate,' ',
		'ORDER BY LastActivityDate DESC
		OFFSET(@Start) ROWS FETCH NEXT(@PageSize) ROWS ONLY) PT
		ON P.Id = PT.PostId LEFT JOIN dbo.Users Own
		ON P.OwnerUserId = Own.Id')

		EXEC sp_executesql @dynamic_sql, @param_definition, @Start=@Start, @PageSize=@PageSize
		EXEC sp_executesql @total_count_dynamic_sql, N'@total_count int OUT', @total_count=@total_count OUT
	END
	ELSE IF @SearchType = 'UNANSWERED'
	BEGIN
		SET @total_count_dynamic_sql = CONCAT('SELECT @total_count=COUNT(1) FROM dbo.Posts WHERE ', @predicate,' AND AcceptedAnswerId IS NULL')
		SET @dynamic_sql = CONCAT('SELECT
			P.Id,
			P.Title,
			Substring(P.Body, 1, 200)[Description],
			P.Tags,
			P.CreationDate AskedDateTime,
			P.OwnerUserId AskedById,
			Own.DisplayName AskedByDisplayName,
			1 NoOfVotes
		FROM dbo.Posts P INNER JOIN (SELECT Id AS PostId
		FROM dbo.Posts 
		WHERE ', @predicate,' AND AcceptedAnswerId IS NULL ',
		'ORDER BY CreationDate DESC
		OFFSET(@Start) ROWS FETCH NEXT(@PageSize) ROWS ONLY) PT
		ON P.Id = PT.PostId LEFT JOIN dbo.Users Own
		ON P.OwnerUserId = Own.Id')

		EXEC sp_executesql @dynamic_sql, @param_definition, @Start=@Start, @PageSize=@PageSize
		EXEC sp_executesql @total_count_dynamic_sql, N'@total_count int OUT', @total_count=@total_count OUT
	END
	ELSE IF @SearchType = 'BOUNTIED'
	BEGIN
		SET @total_count_dynamic_sql = CONCAT('SELECT @total_count=COUNT(1) FROM dbo.vwPostTagsAndVoteTypes WHERE ', @predicate,' AND VoteTypeId=8')
		SET @dynamic_sql = CONCAT('SELECT
			P.Id,
			P.Title,
			Substring(P.Body, 1, 200)[Description],
			P.Tags,
			P.CreationDate AskedDateTime,
			P.OwnerUserId AskedById,
			Own.DisplayName AskedByDisplayName,
			1 NoOfVotes
		FROM dbo.Posts P INNER JOIN(SELECT Id AS PostId
		FROM dbo.vwPostTagsAndVoteTypes P 
		WHERE ', @predicate,' AND P.VoteTypeId=8 ',
		'ORDER BY LastActivityDate DESC
		OFFSET(@Start) ROWS FETCH NEXT(@PageSize) ROWS ONLY) PT
		ON P.Id = PT.PostId LEFT JOIN dbo.Users Own
		ON P.OwnerUserId = Own.Id')

		EXEC sp_executesql @dynamic_sql, @param_definition, @Start=@Start, @PageSize=@PageSize
		EXEC sp_executesql @total_count_dynamic_sql, N'@total_count int OUT', @total_count=@total_count OUT
	END

	SELECT @total_count
END
GO


CREATE OR ALTER VIEW [dbo].[vwPostTagsAndVoteTypes]
WITH SCHEMABINDING
AS
SELECT 
	P.Id,
	P.Tags,
	V.Id VoteId,
	V.VoteTypeId,
	P.LastActivityDate
FROM dbo.Posts P INNER JOIN dbo.Votes V
ON P.Id=V.PostId
GO


CREATE UNIQUE CLUSTERED INDEX [CIX_vwPostTagsAndVoteTypes] ON [dbo].[vwPostTagsAndVoteTypes]
(
	[Id] ASC,
	[VoteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


CREATE NONCLUSTERED INDEX [NIX_vwPostTagsAndVoteTypes_LastActivityDate] ON [dbo].[vwPostTagsAndVoteTypes]
(
	[LastActivityDate] DESC
)
INCLUDE([Tags],[VoteTypeId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


CREATE NONCLUSTERED INDEX [IX_Posts_LastActivityDate] ON [dbo].[Posts]
(
	[LastActivityDate] DESC
)
INCLUDE([Tags]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


CREATE NONCLUSTERED INDEX [IX_PostCreationDate] ON [dbo].[Posts]
(
	[CreationDate] DESC
)
INCLUDE([Tags],[AcceptedAnswerId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

