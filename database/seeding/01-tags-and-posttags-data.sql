USE [StackOverflow2010]
GO

CREATE  OR ALTER FUNCTION [dbo].[splitstring](
	@stringToSplit nvarchar(max),
	@seperator varchar(8000)
)
RETURNS
 @returnList TABLE ([value] nvarchar(max))
AS
BEGIN
	declare @searchTerm varchar(8000)=@seperator
	declare @searchTermStartPos int
	declare @searchTermEndPos int
	declare @searchResult varchar(max)

	WHILE CHARINDEX(@searchTerm, @stringToSplit) > 0
	BEGIN
		SET @searchTermStartPos = (SELECT CHARINDEX(@searchTerm,@stringToSplit))
		SET @searchTermEndPos = @searchTermStartPos + LEN(@searchTerm)
		SET @searchResult = (SELECT SUBSTRING(@stringToSplit,1,@searchTermStartPos-1))
		SET @stringToSplit = (SELECT SUBSTRING(@stringToSplit,@searchTermEndPos,LEN(@stringToSplit)))

		INSERT INTO @returnList 
		SELECT @searchResult
	END

	INSERT INTO @returnList
	SELECT @stringToSplit

	RETURN
END
GO

DECLARE @postsTags TABLE(id int,val varchar(1000))
INSERT INTO @postsTags(id,val)

SELECT Posts.Id,REPLACE(REPLACE(Value,'<',''),'>','')
FROM dbo.Posts CROSS APPLY splitstring(Tags,'><')
WHERE Tags IS NOT NULL

INSERT INTO dbo.Tags(TagName)
SELECT DISTINCT val from @postsTags

INSERT INTO dbo.PostTags(PostId,TagId)
SELECT PT.id,T.Id
FROM @postsTags PT INNER JOIN dbo.Tags T
ON PT.val=T.TagName
GO