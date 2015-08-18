CREATE PROCEDURE SqlPermissions_FindGroup(@prmName varchar(255))
as
BEGIN
	select Name
	from SqlPermissions_Groups
	where Name = @prmName
	order by Name;
END