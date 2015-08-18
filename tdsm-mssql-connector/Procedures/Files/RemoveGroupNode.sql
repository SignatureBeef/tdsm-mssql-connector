CREATE PROCEDURE SqlPermissions_RemoveGroupNode(@prmGroupName varchar(255), @prmNode varchar(255), @prmDeny bit)
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
		and [Deny] = @prmDeny;

	if @vGroupId is not null and @vNodeId is not null and @vGroupId > 0 and @vNodeId > 0
		begin
			delete from SqlPermissions_GroupPermissions
			where GroupId = @vGroupId
				and PermissionId = @vNodeId;

			select 1 Result; /* No fail required */
		end
	else
		begin
			select 0 Result;
		end
END