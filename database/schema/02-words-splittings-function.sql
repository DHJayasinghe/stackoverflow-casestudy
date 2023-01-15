USE [StackOverflow2010]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
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