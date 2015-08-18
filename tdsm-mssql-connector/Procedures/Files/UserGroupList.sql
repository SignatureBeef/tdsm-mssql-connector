CREATE PROCEDURE SqlPermissions_UserGroupList(@prmUserName varchar(255))
as
BEGIN
	select g.Name
	from tdsm_users u
		left join SqlPermissions_UserGroups ug on u.Id = ug.UserId
		left join SqlPermissions_Groups g on ug.GroupId = g.Id
	where u.Username = @prmUserName
	order by g.Name;
END