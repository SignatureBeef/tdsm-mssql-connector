using System;
using OTA;
using OTA.Logging;
using OTA.Plugin;
using TDSM.Core.Data;

namespace TDSM.Data.MSSQL
{
    [OTAVersion(1, 0)]
    public class SqlPermissions : BasePlugin
    {
        public const String SQLSafeName = "SqlPermissions";

        public SqlPermissions()
        {
            this.Version = "1";
            this.Author = "TDSM";
            this.Name = "MSSQL Connector";
            this.Description = "Adds MSSQL storage";
        }

        private MSSQLConnector _connector;

        protected override void Initialized(object state)
        {
            base.Initialized(state);
        }

        [Hook]
        void OnReadConfig(ref HookContext ctx, ref HookArgs.ConfigurationFileLineRead args)
        {
            switch (args.Key)
            {
                case "mssql":
                    if (!Storage.IsAvailable)
                    {
                        MSSQLConnector cn = null;

                        try
                        {
                            cn = new MSSQLConnector(args.Value);
                            cn.Open();
                        }
                        catch (Exception e)
                        {
                            ProgramLog.Error.Log("Exception connecting to MSSQL database: {0}", e);
                            return;
                        }
                        Storage.SetConnector(_connector = cn);
                    }
                    break;
            }
        }

        protected override void Enabled()
        {
            base.Enabled();
        }

        [Hook]
        void OnStateChange(ref HookContext ctx, ref HookArgs.ServerStateChange args)
        {
            if (args.ServerChangeState == ServerState.Initialising)
            {
                ProgramLog.Plugin.Log("MSSQL connector is: " + (_connector == null ? "disabled" : "enabled"));
            }
        }
    }
}

