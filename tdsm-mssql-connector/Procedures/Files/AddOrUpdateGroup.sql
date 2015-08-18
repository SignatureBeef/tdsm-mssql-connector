CREATE PROCEDURE SqlPermissions_AddOrUpdateGroup(@prmName varchar(255), @prmApplyToGuests bit, @prmParent varchar(255), @prmR tinyint, @prmG tinyint, @prmB tinyint, @prmPrefix varchar(10), @prmSuffix varchar(10))
as
BEGIN
	if exists
	(
		select 1
		from SqlPermissions_Groups
		where Name = @prmName
	)
		begin
			update SqlPermissions_Groups
			set
				ApplyToGuests = @prmApplyToGuests,
				Parent = @prmParent,
				Chat_Red = @prmR,
				Chat_Green = @prmG,
				Chat_Blue = @prmB,
				Chat_Prefix = @prmPrefix,
				Chat_Suffix = @prmSuffix
			where Name = @prmName;

			select Id
			from SqlPermissions_Groups
			where Name = @prmName;
		end
	else
		begin
			insert SqlPermissions_Groups
			( Name, ApplyToGuests, Parent, Chat_Red, Chat_Green, Chat_Blue, Chat_Prefix, Chat_Suffix )
			select @prmName, @prmApplyToGuests, @prmParent, @prmR, @prmG, @prmB, @prmPrefix, @prmSuffix;
		
			select SCOPE_IDENTITY();
		end
END