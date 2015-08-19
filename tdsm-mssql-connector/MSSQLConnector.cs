using System;
using TDSM.API.Data;
using System.Data;
using System.Collections.Generic;
using TDSM.API.Logging;
using TDSM.API;
using System.Data.SqlClient;

namespace TDSM.Data.MSSQL
{
    public struct ProcedureParameter
    {
        public string Name { get; set; }

        public Type DataType { get; set; }

        public int? MinScale { get; set; }

        public int? MaxScale { get; set; }

        public ProcedureParameter(string name, Type dataType, int? minScale = null, int? maxScale = null) : this()
        {
            this.Name = name;

            this.DataType = dataType;

            this.MinScale = minScale;
            this.MaxScale = maxScale;
        }
    }

    public partial class MSSQLConnector : IDataConnector
    {
        private SqlConnection _connection;

        public QueryBuilder GetBuilder(string pluginName)
        {
            return new MSSQLQueryBuilder(pluginName);
        }

        public MSSQLConnector(string connectionString)
        {
            _connection = new SqlConnection();
            _connection.ConnectionString = connectionString;
        }

        public void Open()
        {
            _connection.Open();

            InitialisePermissions();
        }

        bool IDataConnector.Execute(QueryBuilder builder)
        {
            if (!(builder is MSSQLQueryBuilder))
                throw new InvalidOperationException("MSSQLQueryBuilder expected");

            var ms = builder as MSSQLQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand();
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.HasRows;
                }
            }
        }

        int IDataConnector.ExecuteNonQuery(QueryBuilder builder)
        {
            if (!(builder is MSSQLQueryBuilder))
                throw new InvalidOperationException("MSSQLQueryBuilder expected");

            var ms = builder as MSSQLQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand();
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.RecordsAffected;
                }
            }
        }

        long IDataConnector.ExecuteInsert(QueryBuilder builder)
        {
            if (!(builder is MSSQLQueryBuilder))
                throw new InvalidOperationException("MSSQLQueryBuilder expected");

            var ms = builder as MSSQLQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand() + " SELECT SCOPE_IDENTITY();";
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                //foreach (var prm in ms.Parameters.ToArray())
                //{
                //    ProgramLog.Plugin.Log("{0}\t- {1}", prm.ParameterName, prm.Value);
                //}
                //ProgramLog.Plugin.Log(cmd.CommandText);

                //cmd.ExecuteNonQuery ();

                //var scl = cmd.ExecuteScalar();

                //if(null!= scl)
                //{
                //    ProgramLog.Log("scl: " + scl.GetType());
                //}

                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        T IDataConnector.ExecuteScalar<T>(QueryBuilder builder)
        {
            if (!(builder is MSSQLQueryBuilder))
                throw new InvalidOperationException("MSSQLQueryBuilder expected");

            var ms = builder as MSSQLQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand();
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                var res = cmd.ExecuteScalar();
                if (null == res || Convert.IsDBNull(res)) return default(T);

                return (T)res;
            }
        }

        DataSet IDataConnector.ExecuteDataSet(QueryBuilder builder)
        {
            if (!(builder is MSSQLQueryBuilder))
                throw new InvalidOperationException("MSSQLQueryBuilder expected");

            var ms = builder as MSSQLQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand();
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                using (var da = new SqlDataAdapter(cmd))
                {
                    var ds = new DataSet();

                    da.Fill(ds);

                    return ds;
                }
            }
        }

        T[] IDataConnector.ExecuteArray<T>(QueryBuilder builder)
        {
            if (null == builder) throw new InvalidOperationException("builder was null");

            var ds = (this as IDataConnector).ExecuteDataSet(builder);

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                var records = new T[ds.Tables[0].Rows.Count];
                var tp = typeof(T);

                for (var x = 0; x < ds.Tables[0].Rows.Count; x++)
                {
                    object boxed = new T();
                    for (var cx = 0; cx < ds.Tables[0].Columns.Count; cx++)
                    {
                        var col = ds.Tables[0].Columns[cx];

                        var val = ds.Tables[0].Rows[x].ItemArray[cx];
                        if (DBNull.Value == val || null == val)
                        {
                            continue;
                        }

                        var fld = tp.GetField(col.ColumnName);
                        if (fld != null)
                        {
                            if (val != null && fld.FieldType != val.GetType())
                            {
                                //								ProgramLog.Log ("Converting type {0}->{1}", val.GetType ().Name, fld.FieldType.Name);
                                val = GetTypeValue(fld.FieldType, val);
                            }
                            fld.SetValue(boxed, val);
                        }
                        else
                        {
                            var prop = tp.GetProperty(col.ColumnName);
                            if (prop != null)
                            {
                                if (val != null && prop.PropertyType != val.GetType())
                                {
                                    //									ProgramLog.Log ("Converting type {0}->{1}", val.GetType ().Name, prop.PropertyType.Name);
                                    val = GetTypeValue(prop.PropertyType, val);
                                }
                                prop.SetValue(boxed, val, null);
                            }
                        }
                    }
                    records[x] = (T)boxed;
                }

                return records;
            }

            return null;
        }

        static object GetTypeValue(Type type, object value)
        {
            if (type == typeof(Microsoft.Xna.Framework.Color) || type.IsAssignableFrom(typeof(Microsoft.Xna.Framework.Color)))
            {
                if (value is UInt32) return Tools.Encoding.DecodeColor((uint)value);
            }
            else if (type == typeof(bool[]))
            {
                if (value is Byte) return Tools.Encoding.DecodeBits((byte)value);
                if (value is Int16) return Tools.Encoding.DecodeBits((short)value);
                if (value is Int32) return Tools.Encoding.DecodeBits((int)value);
            }
            else if (type == typeof(Byte))
                return Convert.ToByte(value);
            else if (type == typeof(Int16))
                return Convert.ToInt16(value);
            else if (type == typeof(Int32))
                return Convert.ToInt32(value);
            else if (type == typeof(Int64))
                return Convert.ToInt64(value);
            else if (type == typeof(UInt16))
                return Convert.ToUInt16(value);
            else if (type == typeof(UInt32))
                return Convert.ToUInt32(value);
            else if (type == typeof(Int64))
                return Convert.ToUInt64(value);

            return Convert.ChangeType(value, type);
        }

        public override string ToString()
        {
            return "[MSSQLConnector]";
        }
    }

    public class MSSQLQueryBuilder : QueryBuilder
    {
        private List<SqlParameter> _params;

        public List<SqlParameter> Parameters
        {
            get
            { return _params; }
        }

        public MSSQLQueryBuilder(string pluginName)
            : base(pluginName)
        {
            _params = new List<SqlParameter>();
        }

        public QueryBuilder ExecuteProcedure(string name, string prefix = "@", params DataParameter[] parameters)
        {
            Append("EXEC {0} ", base.GetObjectName(name));

            if (parameters != null && parameters.Length > 0)
            {
                for (var x = 0; x < parameters.Length; x++)
                {
                    var xp = parameters[x];

                    var paramKey = prefix + xp.Name;
                    _params.Add(new SqlParameter(xp.Name, xp.Value));
                    Append("@" + paramKey);

                    if (x + 1 < parameters.Length)
                        Append(",");
                }
            }

            Append(");");
            return this;
        }

        public override QueryBuilder AddParam(string name, object value, string prefix = "@")
        {
            //var paramKey = prefix + name;
            _params.Add(new SqlParameter(name, value));
            return this;
        }

        public override QueryBuilder TableExists(string name)
        {
            Append("SELECT * FROM information_schema.tables where table_name = '{0}'", base.GetObjectName(name));
            return this;
        }

        public override QueryBuilder TableCreate(string name, params TableColumn[] columns)
        {
            Append("CREATE TABLE {0} (", base.GetObjectName(name));

            if (columns != null && columns.Length > 0)
            {
                for (var x = 0; x < columns.Length; x++)
                {
                    var col = columns[x];

                    Append("[");
                    Append(col.Name);
                    Append("]");

                    if (col.DataType == typeof(Byte))
                    {
                        Append(" TINYINT");
                    }
                    else if (col.DataType == typeof(Int16))
                    {
                        Append(" SMALLINT");
                    }
                    else if (col.DataType == typeof(UInt16))
                    {
                        Append(" INT");
                    }
                    else if (col.DataType == typeof(Int32))
                    {
                        Append(" INT");
                    }
                    else if (col.DataType == typeof(UInt32))
                    {
                        Append(" BIGINT");
                    }
                    else if (col.DataType == typeof(Int64))
                    {
                        Append(" BIGINT");
                    }
                    else if (col.DataType == typeof(UInt64))
                    {
                        Append(" BIGINT");
                    }
                    else if (col.DataType == typeof(String))
                    {
                        var isVarChar = col.MinScale.HasValue && !col.MaxScale.HasValue;
                        if (isVarChar)
                        {
                            Append(" NVARCHAR(");
                            Append(col.MinScale.Value.ToString());
                            Append(")");
                        }
                        else
                        {
                            Append(" TEXT");
                        }
                    }
                    else if (col.DataType == typeof(DateTime))
                    {
                        Append(" TIMESTAMP");
                    }
                    else if (col.DataType == typeof(Boolean))
                    {
                        Append(" BIT");
                    }
                    else
                    {
                        throw new NotSupportedException(String.Format("Data type for column '{0}' is not supported", col.Name));
                    }

                    if (col.AutoIncrement) //TODO check for numerics
                    {
                        Append(" IDENTITY(1,1)");
                    }
                    if (col.PrimaryKey) //TODO check for numerics
                    {
                        Append(" PRIMARY KEY");
                    }
                    if (col.Nullable)
                    {
                        Append(" NULL");
                    }
                    else
                    {
                        Append(" NOT NULL");
                    }

                    if (x + 1 < columns.Length)
                        Append(",");
                }
            }
            Append(")");

            return this;
        }

        public override QueryBuilder TableDrop(string name)
        {
            Append("DROP TABLE {0}", base.GetObjectName(name));
            return this;
        }

        public QueryBuilder ProcedureExists(string name)
        {
            const String Fmt = "select 1 from information_schema.routines where routine_type='procedure' and routine_catalog = DB_NAME() and routine_name = '{0}';";
            return this.Append(Fmt, base.GetObjectName(name));
        }

        public QueryBuilder ProcedureCreate(string name, string contents, params ProcedureParameter[] parameters)
        {
            Append("CREATE PROCEDURE {0} (", base.GetObjectName(name));

            if (parameters != null && parameters.Length > 0)
            {
                for (var x = 0; x < parameters.Length; x++)
                {
                    var prm = parameters[x];

                    Append("@");
                    Append(prm.Name);

                    if (prm.DataType == typeof(Byte))
                    {
                        Append(" TINYINT");
                    }
                    else if (prm.DataType == typeof(Int16))
                    {
                        Append(" SMALLINT");
                    }
                    else if (prm.DataType == typeof(Int32))
                    {
                        Append(" INT");
                    }
                    else if (prm.DataType == typeof(Int64))
                    {
                        Append(" BIGINT");
                    }
                    else if (prm.DataType == typeof(String))
                    {
                        var isVarChar = prm.MinScale.HasValue && !prm.MaxScale.HasValue;
                        if (isVarChar)
                        {
                            Append(" NVARCHAR(");
                            Append(prm.MinScale.Value.ToString());
                            Append(")");
                        }
                        else
                        {
                            Append(" TEXT");
                        }
                    }
                    else if (prm.DataType == typeof(DateTime))
                    {
                        Append(" TIMESTAMP");
                    }
                    else if (prm.DataType == typeof(Boolean))
                    {
                        Append(" BIT");
                    }
                    else
                    {
                        throw new NotSupportedException(String.Format("Data type for parameter '{0}' is not supported", prm.Name));
                    }

                    if (x + 1 < parameters.Length)
                        Append(",");
                }
            }
            Append(")");

            Append(contents);

            return this;
        }

        public QueryBuilder ProcedureDrop(string name)
        {
            return this.Append("DROP PROCEDURE `{0}`", base.GetObjectName(name));
        }

        public override QueryBuilder Select(params string[] expression)
        {
            Append("SELECT ");

            if (expression != null && expression.Length > 0)
            {
                Append(String.Join(",", expression));

                return this.Append(" ");
            }

            return this;
        }

        public override QueryBuilder All()
        {
            Append("* ");
            return this;
        }

        public override QueryBuilder From(string tableName)
        {
            Append("FROM ");
            Append(base.GetObjectName(tableName));
            Append(" ");
            return this;
        }

        public override QueryBuilder Where(params WhereFilter[] clause)
        {
            Append("WHERE ");

            if (clause != null && clause.Length > 0)
            {
                for (var x = 0; x < clause.Length; x++)
                {
                    if (x > 0)
                        Append("AND ");

                    var xp = clause[x];

                    Append(xp.Column);

                    switch (xp.Expression)
                    {
                        case WhereExpression.EqualTo:
                            Append(" = ");
                            break;
                        case WhereExpression.NotEqualTo:
                            Append(" = ");
                            break;
                        case WhereExpression.Like:
                            Append(" LIKE ");
                            break;
                    }

                    var paramKey = "@" + xp.Column;
                    _params.Add(new SqlParameter(xp.Column, xp.Value));
                    Append(paramKey);
                    Append(" ");
                }
            }

            return this;
        }

        public override QueryBuilder Count(string expression = null)
        {
            Append("COUNT(");
            Append(expression ?? "*");
            return Append(") ");
            //return this.Append(fmt, String.Format("COUNT({0})", expression ?? "*"));
        }

        public override QueryBuilder Delete()
        {
            Append("DELETE ");
            return this;
        }

        public override QueryBuilder InsertInto(string tableName, params DataParameter[] values)
        {
            Append("INSERT INTO ");
            Append(base.GetObjectName(tableName));

            if (values != null && values.Length > 0)
            {
                //Columns
                Append(" ( ");
                for (var x = 0; x < values.Length; x++)
                {
                    Append("[");
                    Append(values[x].Name);
                    Append("]");

                    if (x + 1 < values.Length)
                        Append(",");
                }
                Append(" ) ");

                //Values
                Append(" VALUES ( ");
                for (var x = 0; x < values.Length; x++)
                {
                    var prm = values[x];
                    var paramKey = prm.Name;

                    if (null == prm.Value)
                    {
                        Append("NULL");
                    }
                    else
                    {
                        Append("@" + paramKey);
                        _params.Add(new SqlParameter(paramKey, prm.Value));
                    }

                    if (x + 1 < values.Length)
                        Append(",");

                    //SqlDbType type;
                    //if (TryGetDBType(prm.Value, out type))
                    //{
                    //    _params.Add(new SqlParameter(paramKey, type, prm.Value));
                    //}
                    //else
                    //{
                    //    _params.Add(new SqlParameter(paramKey, prm.Value));
                    //}
                }
                Append(" ); ");
            }
            return this;
        }

        static bool TryGetDBType(object value, out SqlDbType type)
        {
            type = SqlDbType.VarChar;
            if (value is String)
            {
                type = SqlDbType.NVarChar;
                return true;
            }
            return false;
        }

        public override QueryBuilder UpdateValues(string tableName, DataParameter[] values)
        {
            Append("UPDATE ");
            Append(base.GetObjectName(tableName));

            if (values != null && values.Length > 0)
            {
                Append(" SET ");

                for (var x = 0; x < values.Length; x++)
                {
                    var prm = values[x];
                    var paramKey = "@" + prm.Name;

                    Append(prm.Name);
                    Append("=");
                    Append(paramKey);
                    Append(" ");

                    if (x + 1 < values.Length)
                        Append(",");

                    _params.Add(new SqlParameter(prm.Name, prm.Value));
                }
            }

            return this;
        }
    }
}

