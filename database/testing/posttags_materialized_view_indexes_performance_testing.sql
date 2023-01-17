-- Enable Actual Execution Plan before running

SET STATISTICS IO ON
SET STATISTICS TIME ON
Declare @dbid int = db_ID() 

DBCC DROPCLEANBUFFERS
DBCC FLUSHPROCINDB(@dbid)

SELECT PostId,TagId,CreationDate 
FROM dbo.vwPostTags WITH(noexpand,INDEX(NIX_vwPostTags_CreationDate))
WHERE TagName = 'C#'
ORDER BY CreationDate DESC
OFFSET (100) ROWS FETCH NEXT (15) ROWS ONLY

DBCC DROPCLEANBUFFERS
DBCC FLUSHPROCINDB(@dbid)

SELECT PostId,TagId,CreationDate 
FROM dbo.vwPostTags WITH(noexpand,INDEX(NIX_vwPostTags_TagName))
WHERE TagName = 'C#'
ORDER BY CreationDate
OFFSET (100) ROWS FETCH NEXT (15) ROWS ONLY