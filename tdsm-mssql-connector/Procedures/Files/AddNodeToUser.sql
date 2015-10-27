CREATE PROCEDURE SqlPermissions_AddNodeToUser(@prmUserName varchar(50), @prmNode varchar(50), @prmPermission int)
as
BEGIN
	declare @vUserId int = 0;
	declare @vNodeId int = 0;

	select top 1 @vUserId = Id
	from tdsm_users g
	where g.Username = @prmUserName;
	
	select @vNodeId = Id
	from SqlPermissions_Permissions
	where Node = @prmNode
		and Permission = @prmPermission;

	if @vNodeId is null or @vNodeId = 0
		begin
			insert into SqlPermissions_Permissions
			( Node, Permission )
			select @prmNode, @prmPermission;
			set @vNodeId = SCOPE_IDENTITY();
		end

	if @vNodeId > 0 and @vUserId > 0
		begin
			if not exists
			(
				select 1
				from SqlPermissions_UserPermissions
				where UserId = @vUserId
					and PermissionId = @vNodeId
			)
				begin
					insert into SqlPermissions_UserPermissions
					( UserId, PermissionId )
					select @vUserId, @vNodeId;
				end

			select 1 Result; /* No fail required */
		end
	else
		begin
			select 0 Result;
		end
END