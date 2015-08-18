CREATE PROCEDURE SqlPermissions_RemoveGroup(@prmName varchar(255))
as
BEGIN
	delete from SqlPermissions_Groups
	where Name = @prmName;
END