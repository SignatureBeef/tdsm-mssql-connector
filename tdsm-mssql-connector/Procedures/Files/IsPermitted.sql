CREATE PROCEDURE SqlPermissions_IsPermitted(@prmNode varchar(50), @prmIsGuest bit, @prmAuthentication varchar(50))
as
BEGIN
	declare @vPermissionValue int = 0;
	declare @vUserId int = 0;
	declare @vGroupId int = 0;
	declare @vPrevGroupId int = 0;
	declare @vNodeFound int = 0;
	/*
		PermissionEnum values:
			0	= Denied
			1	= Permitted
	*/

	if @prmIsGuest = 0 and @prmAuthentication is not null and LEN(@prmAuthentication) > 0
		begin
			select top 1 @vUserId = Id
			from tdsm_users u
			where u.Username = @prmAuthentication;

			if @vUserId > 0
				begin
					/*
						If the user has specific nodes then use them
						If not then search for a group
						If still none then try the guest permissions
					*/

					/*Do we have any nodes?*/
					if exists
					(
						select 1
						from tdsm_users u
							left join SqlPermissions_UserPermissions up on u.Id = up.UserId
							left join SqlPermissions_Permissions nd on up.PermissionId = nd.Id
						where u.Id = vUserId
							and (nd.Node = @prmNode or nd.Node = '*')
					)
						begin
							if exists
							(
								select 1
								from tdsm_users u
									left join SqlPermissions_UserPermissions up on u.Id = up.UserId
									left join SqlPermissions_Permissions nd on up.PermissionId = nd.Id
								where u.Id = @vUserId
									and (nd.Node = @prmNode or nd.Node = '*')
									and nd.Permission = 1 /*Permitted*/
							)
								begin
									set @vPermissionValue = 1;
									set @vNodeFound = 1;
								end
							else
								begin
									set @vPermissionValue = 0;
									set @vNodeFound = 1;
								end
						end
					else
						begin
							/*
								For each group, see if it has a permission
								Else, if it has a parent recheck.
								Else guestMode
							*/

							/* Get the first group */
							select top 1 @vGroupId = GroupId
							from SqlPermissions_UserGroups u
							where u.UserId = @vUserId;

							set @vPrevGroupId = @vGroupId;
							set @vNodeFound = 0;

							while (@vGroupId is not null and @vGroupId > 0 and @vNodeFound = 0)
								begin
									/* Check group permissions */
									select @vGroupId;
									if exists
									(
										select 1
										from SqlPermissions_GroupPermissions gp
											left join SqlPermissions_Permissions pm on gp.PermissionId = pm.Id
										where gp.GroupId = @vGroupId
											and (pm.Node = @prmNode or pm.Node = '*')
											and pm.Permission = 0 /*Denied*/
									)
										begin
											set @vPermissionValue = 0;
											set @vGroupId = 0;
											set @vNodeFound = 1;
										end
									else if exists
									(
										select 1
										from SqlPermissions_GroupPermissions gp
											left join SqlPermissions_Permissions pm on gp.PermissionId = pm.Id
										where gp.GroupId = @vGroupId
											and (pm.Node = @prmNode or pm.Node = '*')
											and pm.Permission = 1 /*Permitted*/
									)
										begin
											set @vPermissionValue = 1;
											set @vGroupId = 0;
											set @vNodeFound = 1;
										end
									else
										begin
											select top 1 @vGroupId = Id
											from SqlPermissions_Groups g
											where g.Name = (
												select top 1 c.Parent
												from SqlPermissions_Groups c
												where c.Id = @vGroupId
											);

											if @vPrevGroupId = @vGroupId
												begin
													set @vGroupId = 0;
												end

											set @vPrevGroupId = @vGroupId;
										end
								end

							if 1 <> @vNodeFound
								begin
									set @prmIsGuest = 1;
								end
						end
				end
			else
				begin
					/* Invalid user - try guest */
					set @prmIsGuest = 1;
				end
		end

	if @vNodeFound = 0 and @prmIsGuest = 1
		begin
			if exists
			(
				select 1
				from SqlPermissions_Groups gr
					left join SqlPermissions_GroupPermissions gp on gr.Id = gp.GroupId
					left join SqlPermissions_Permissions nd on gp.PermissionId = nd.Id
				where gr.ApplyToGuests = 1
					and (nd.Node = @prmNode or nd.Node = '*')
					and nd.Permission = 1 /*Permitted*/
			)
				begin
					set @vPermissionValue = 1;
					set @vNodeFound = 1;
				end
		end

	select @vPermissionValue PermissionEnum;
END