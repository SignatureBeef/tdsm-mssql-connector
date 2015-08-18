CREATE PROCEDURE SqlPermissions_GroupList
as
BEGIN
	select Name
	from SqlPermissions_Groups
	order by Name;
END