CREATE PROCEDURE SqlPermissions_RemoveNodeFromUser(@prmUserName varchar(255), @prmNode varchar(255), @prmDeny bit)
as
BEGIN
	declare @vUserId int = 0;
	declare @vNodeId int = 0;

	select top 1 @vUserId = Id
	from tdsm_users u
	where u.Username = @prmUserName;
	
	select @vNodeId = Id
	from SqlPermissions_Permissions
	where Node = @prmNode
		and [Deny] = @prmDeny;

	if @vUserId is not null and @vNodeId is not null and @vUserId > 0 and @vNodeId > 0
		begin
			delete from SqlPermissions_UserPermissions
			where UserId = @vUserId
				and PermissionId = @vNodeId;

			select 1 Result; /* No fail required */
		end
	else
		begin
			select 0 Result;
		end
END