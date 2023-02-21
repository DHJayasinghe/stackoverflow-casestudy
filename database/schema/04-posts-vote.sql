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

		SELECT 
			@existingVoteId=Id,
			@existingVoteTypeId=VoteTypeId 
		FROM dbo.Votes 
		WHERE PostId=@Id 
			AND UserId=@UserId 
			AND VoteTypeId IN (2,3)

		IF @existingVoteId = 0 -- Here there was no matching records, so we insert new
		BEGIN	
			INSERT INTO dbo.Votes(PostId,UserId,VoteTypeId,CreationDate)
			VALUES(@id,@userId,@voteTypeId,GETUTCDATE())

			SET @noOfChanges=@@ROWCOUNT
		END

		IF @existingVoteId != 0 AND @existingVoteTypeId != @VoteTypeId
		BEGIN
			-- Here we decided to update any existing Vote row for that specific user, for specific Post, with VoteType 1 or 2 as a fresh entry.
			-- Cause DELETE existing and INSERT new row each time voting has to regenerate a new ID
			-- This way we won't ran out of INT for our Primary Key

			UPDATE dbo.Votes SET 
				VoteTypeId=@VoteTypeId,
				CreationDate=GETUTCDATE()
			WHERE Id=@existingVoteId

			SET @noOfChanges=@@ROWCOUNT
		END

		declare @score int

		SELECT @score=SUM(CASE VoteTypeId WHEN 2 THEN 1 ELSE -1 END) FROM dbo.Votes WHERE PostId=@id AND VoteTypeId IN (2,3)

		IF @noOfChanges > 0 -- Here we found an update/insert, so we update the Posts table score 
		BEGIN
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