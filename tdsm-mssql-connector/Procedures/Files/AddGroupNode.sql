CREATE PROCEDURE SqlPermissions_AddGroupNode(@prmGroupName varchar(50), @prmNode varchar(50), @prmPermission int)
as
BEGIN
	declare @vGroupId int = 0;
	declare @vNodeId int = 0;

	select top 1 @vGroupId = Id
	from SqlPermissions_Groups g
	where g.Name = @prmGroupName;
	
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

	if @vGroupId > 0 and @vNodeId > 0
		begin
			if not exists
			(
				select 1
				from SqlPermissions_GroupPermissions
				where GroupId = @vGroupId
					and PermissionId = @vNodeId
			)
				begin
					insert into SqlPermissions_GroupPermissions
					( GroupId, PermissionId )
					select @vGroupId, @vNodeId;
				end

			select 1 Result; /* No fail required */
		end
	else
		begin
			select 0 Result;
		end
END