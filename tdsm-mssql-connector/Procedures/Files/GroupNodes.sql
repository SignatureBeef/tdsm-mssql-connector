CREATE PROCEDURE SqlPermissions_GroupNodes(@prmGroupName varchar(255))
as
BEGIN
	select nd.Node, nd.Permission
	from SqlPermissions_Groups g
		inner join SqlPermissions_GroupPermissions gp on g.Id = gp.GroupId
		inner join SqlPermissions_Permissions nd on gp.PermissionId = nd.Id
	where g.Name = @prmGroupName
	order by nd.Node;
END