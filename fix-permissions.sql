-- Add configuration management permissions to Platform Admin role
DECLARE @PlatformAdminRoleId NVARCHAR(450);

-- Get the Platform Admin role ID
SELECT @PlatformAdminRoleId = Id
FROM AspNetRoles
WHERE Name = 'Platform Admin';

-- Add system:config:view permission if it doesn't exist
IF NOT EXISTS (
    SELECT 1 FROM AspNetRoleClaims
    WHERE RoleId = @PlatformAdminRoleId
    AND ClaimType = 'permission'
    AND ClaimValue = 'system:config:view'
)
BEGIN
    INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue)
    VALUES (@PlatformAdminRoleId, 'permission', 'system:config:view');
    PRINT 'Added system:config:view permission';
END

-- Add system:config:edit permission if it doesn't exist
IF NOT EXISTS (
    SELECT 1 FROM AspNetRoleClaims
    WHERE RoleId = @PlatformAdminRoleId
    AND ClaimType = 'permission'
    AND ClaimValue = 'system:config:edit'
)
BEGIN
    INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue)
    VALUES (@PlatformAdminRoleId, 'permission', 'system:config:edit');
    PRINT 'Added system:config:edit permission';
END

-- Add system:app:restart permission if it doesn't exist
IF NOT EXISTS (
    SELECT 1 FROM AspNetRoleClaims
    WHERE RoleId = @PlatformAdminRoleId
    AND ClaimType = 'permission'
    AND ClaimValue = 'system:app:restart'
)
BEGIN
    INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue)
    VALUES (@PlatformAdminRoleId, 'permission', 'system:app:restart');
    PRINT 'Added system:app:restart permission';
END

-- Verify permissions
SELECT r.Name AS RoleName, rc.ClaimValue AS Permission
FROM AspNetRoles r
JOIN AspNetRoleClaims rc ON r.Id = rc.RoleId
WHERE r.Name = 'Platform Admin'
AND rc.ClaimType = 'permission'
AND rc.ClaimValue LIKE '%config%' OR rc.ClaimValue LIKE '%restart%'
ORDER BY rc.ClaimValue;
