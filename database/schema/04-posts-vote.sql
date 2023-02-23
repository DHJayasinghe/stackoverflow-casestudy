CREATE OR ALTER PROCEDURE [dbo].[sp_posts_vote](
	@Id int,
	@VoteTypeId int,
	@UserId int
)
AS
BEGIN
	DECLARE @existingVoteId INT =  0
	DECLARE @existingVoteTypeId INT = 0
	DECLARE @noOfChanges INT = 0

	BEGIN TRY
		BEGIN TRANSACTION

		-- We only consider UpVote or DownVote
		SELECT 
			@existingVoteId=Id,
			@existingVoteTypeId=VoteTypeId 
		FROM dbo.Votes 
		WHERE PostId=@Id 
			AND UserId=@UserId 
			AND VoteTypeId IN (2,3) 

		IF @existingVoteId = 0 
		BEGIN	
			-- Here, there was no previous Vote from this user, so we INSERT one
			INSERT INTO dbo.Votes(PostId,UserId,VoteTypeId,CreationDate)
			VALUES(@id,@userId,@voteTypeId,GETUTCDATE())

			SET @noOfChanges=1
		END

		IF @existingVoteId != 0 AND @existingVoteTypeId != @VoteTypeId
		BEGIN
			-- Here, we found a previous vote from this user, but the Vote has changed. So we UPDATE it
			-- And of course to make sure we don't ran out of int fast for Ids.
			UPDATE dbo.Votes SET 
				VoteTypeId=@VoteTypeId,
				CreationDate=GETUTCDATE()
			WHERE Id=@existingVoteId

			SET @noOfChanges=@@ROWCOUNT
		END

		IF @noOfChanges > 0 -- Here we found an update/insert, so we update the Posts table score 
		BEGIN
			-- Calculate the Score for this Post first
			declare @score int
			SELECT @score=SUM(CASE VoteTypeId WHEN 2 THEN 1 ELSE -1 END) FROM dbo.Votes WHERE PostId=@id AND VoteTypeId IN (2,3)

			UPDATE dbo.Posts SET 
				Score=@score,
				LastActivityDate=GETUTCDATE()
			WHERE Id=@Id
		END

		COMMIT
	END TRY
	BEGIN CATCH
		ROLLBACK
	END CATCH
END