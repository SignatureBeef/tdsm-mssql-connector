//#define TESTING
using System;
using System.Linq;
using OTA.Logging;
using TDSM.Core.Data.Old;

namespace TDSM.Data.MSSQL
{
    public static class Procedures
    {
        public const String AddGroupNode = "AddGroupNode";
        public const String AddNodeToUser = "AddNodeToUser";
        public const String AddOrUpdateGroup = "AddOrUpdateGroup";
        public const String AddUserToGroup = "AddUserToGroup";
        public const String FindGroup = "FindGroup";
        public const String GroupNodes = "GroupNodes";
        public const String GroupList = "GroupList";
        public const String IsPermitted = "IsPermitted";
        public const String RemoveGroup = "RemoveGroup";
        public const String RemoveGroupNode = "RemoveGroupNode";
        public const String RemoveNodeFromUser = "RemoveNodeFromUser";
        public const String RemoveUserFromGroup = "RemoveUserFromGroup";
        public const String UserGroupList = "UserGroupList";
        public const String UserNodes = "UserNodes";

        static StoredProcedure[] _procedures = new StoredProcedure[]
        {
            new StoredProcedure(AddGroupNode),
            new StoredProcedure(AddNodeToUser),
            new StoredProcedure(AddOrUpdateGroup),
            new StoredProcedure(AddUserToGroup),
            new StoredProcedure(FindGroup),
            new StoredProcedure(GroupList),
            new StoredProcedure(GroupNodes),
            new StoredProcedure(IsPermitted),
            new StoredProcedure(RemoveGroup),
            new StoredProcedure(RemoveGroupNode),
            new StoredProcedure(RemoveNodeFromUser),
            new StoredProcedure(RemoveUserFromGroup),
            new StoredProcedure(UserGroupList),
            new StoredProcedure(UserNodes)
        };

        public static void Init(MSSQLConnector conn)
        {
            foreach (var proc in _procedures)
            {
                #if TESTING
                if (proc.Exists(conn))
                    proc.Drop(conn);
                #endif
                if (!proc.Exists(conn))
                {
                    ProgramLog.Admin.Log("{0} procedure does not exist and will now be created", proc.Name);
                    proc.Create(conn);
                }
            }
            _procedures = null;
        }
    }

    public class StoredProcedure
    {
        public string Name { get; set; }

        public StoredProcedure(string name)
        {
            this.Name = name;
        }

        public bool Drop(MSSQLConnector conn)
        {
            using (var sb = new MSSQLQueryBuilder(SqlPermissions.SQLSafeName))
            {
                sb.ProcedureDrop(Name);

                return ((IDataConnector)conn).Execute(sb);
            }
        }

        public bool Exists(MSSQLConnector conn)
        {
            using (var sb = new MSSQLQueryBuilder(SqlPermissions.SQLSafeName))
            {
                sb.ProcedureExists(Name);

                return ((IDataConnector)conn).Execute(sb);
            }
        }

        public bool Create(MSSQLConnector conn)
        {
            using (var sb = new MSSQLQueryBuilder(SqlPermissions.SQLSafeName))
            {
                var proc = PluginContent.GetResource("TDSM.Data.MSSQL.Procedures.Files." + Name + ".sql");

                sb.CommandType = System.Data.CommandType.Text;
                sb.CommandText = proc;

                return ((IDataConnector)conn).Execute(sb);
            }
        }
    }
}

