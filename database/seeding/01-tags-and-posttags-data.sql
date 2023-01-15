USE [StackOverflow2010]
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