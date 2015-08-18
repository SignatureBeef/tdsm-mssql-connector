CREATE PROCEDURE SqlPermissions_RemoveUserFromGroup(@prmUserName varchar(255), @prmGroupName varchar(255))
as
BEGIN
	declare @vGroupId int = 0;
	declare @vUserId int = 0;

	select top 1 @vGroupId = Id
	from SqlPermissions_Groups g
	where g.Name = @prmGroupName;

	select top 1 @vUserId = Id
	from tdsm_users g
	where g.Username = @prmUserName;

	if @vUserId is not null and @vGroupId is not null and @vUserId > 0 and @vGroupId > 0
		begin
			delete from SqlPermissions_UserGroups
			where UserId = @vUserId
				and GroupId = @vGroupId;

			select 1 Result;
		end
	else
		begin
			select 0 Result;
		end
END