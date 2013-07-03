using System;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Data.Common;
using Mono.Data.Sqlite;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// All valid supported db types must be included in this enum.
/// </summary>
public enum SupportedDbType { SQLITE, SQL_SERVER }

/// <summary>
/// Utility class for common database actions and information.
/// </summary>
public class GameDatabase : ScriptableObject {

	private static readonly List<string> LOCAL_DB_FILE_EXTENSIONS = new List<string> { ".db", ".db3", ".sqlite" };
	
	private DbConnection currentDbConnection = null;
	public DbConnection CurrentDbConnection 
	{
		get { return this.currentDbConnection; }
	}	
	
	private Dictionary<Type, DbDataAdapter> cachedDataAdapters = new Dictionary<Type, DbDataAdapter>();
	
	
	#region public static utility methods
	/// <summary>
	/// Creates a database connection based on the supplied connection string.
	/// If one already exists for this exact connection string the method returns without creating a new one,
	/// otherwise closes any open connection and creates a new one.
	/// </summary>
	/// <param name='connectionString'>
	/// A valid connection string for the db connection.
	/// </param>
	/// <param name="dbType">
	/// One of the supported database types.
	/// </param>
	public void SetConnection(string connectionString, SupportedDbType dbType)
	{		
		// don't bother if the connection string supplied isn't minimally valid
		if (string.IsNullOrEmpty(connectionString)) {
			return;
		}
		
		// Has a connection already been set?
		if (currentDbConnection != null)
		{
			// is it the same one being requested...?
			if (currentDbConnection.ConnectionString == connectionString)
			{
				// ...yes, so just return
				return;
			}
			else
			{
				// ...no, so close the existing connection
				currentDbConnection.Close ();
			}
		}
				
		try {
			// create a new connection
			switch (dbType) {
				case SupportedDbType.SQLITE:
					currentDbConnection = new SqliteConnection(connectionString);
					break;
				case SupportedDbType.SQL_SERVER:
					currentDbConnection = new SqlConnection(connectionString);
					break;
				default:
				break;						
			}
			
			currentDbConnection.StateChange += HandleCurrentDbConnectionStateChange;
			
			// clear out any cached data
			cachedDataAdapters.Clear();
		} catch (System.Exception ex) {
			
			Debug.LogException(ex);			
			throw;
		}
		
		return;
	}

	void HandleCurrentDbConnectionStateChange (object sender, StateChangeEventArgs e)
	{
		Debug.Log(string.Format("Db state changed from {0} to {1}", e.OriginalState.ToString(), e.CurrentState.ToString()));
		}

	/// <summary>
	/// Sets the default db connection, which is a sqlite connection based on any local database file found.
	/// </summary>
	public void SetDefaultConnection()
	{
		string connectionString = GetDefaultDbConnectionString();
		SetConnection(connectionString, SupportedDbType.SQLITE);
	}
		
	/// <summary>
	/// Gets the appropriate, empty DbDataAdaptor for the current connection.
	/// </summary>
	/// <returns>
	/// A db data adaptor.
	/// </returns>
	/// <exception cref='InvalidOperationException'>
	/// Thrown if this method is called before establishing a database connection.
	/// </exception>
	public DbDataAdapter GetDbDataAdapter()
	{
		if (currentDbConnection == null)
		{
			throw new InvalidOperationException("A SetConnection method must be called first before attempting to acquire a data adapter.");
		}
		
		DbDataAdapter adapter = null;
		Type connectionType = currentDbConnection.GetType();
		if (connectionType == typeof(SqlConnection)) 
		{
			adapter = new SqlDataAdapter();
			SqlDataAdapter sqlda = adapter as SqlDataAdapter;
			sqlda.RowUpdating += HandleSqldaRowUpdating;
			sqlda.RowUpdated += HandleSqldaRowUpdated;
		}
		else if (connectionType == typeof(SqliteConnection))
		{
			adapter = new SqliteDataAdapter();
			SqliteDataAdapter sqliteda = adapter as SqliteDataAdapter;
			sqliteda.RowUpdating += HandleSqlitedaRowUpdating;
			sqliteda.RowUpdated += HandleSqlitedaRowUpdated;
		}
		
		return adapter;
	}

	void HandleSqlitedaRowUpdated (object sender, RowUpdatedEventArgs e)
	{
		UnityEngine.Debug.Log("Row Updated.");		
	}

	void HandleSqlitedaRowUpdating (object sender, RowUpdatingEventArgs e)
	{
#if DEBUG
		LogRowUpdatingEvent(e);		
#endif
	}

	void HandleSqldaRowUpdated (object sender, SqlRowUpdatedEventArgs e)
	{
		UnityEngine.Debug.Log("Row Updated.");
	}

	void HandleSqldaRowUpdating (object sender, SqlRowUpdatingEventArgs e)
	{	
#if DEBUG
		LogRowUpdatingEvent(e);		
#endif
	}

	static void LogRowUpdatingEvent (RowUpdatingEventArgs e)
	{
		UnityEngine.Debug.Log(string.Format("Row Updating Event: {0}={1}", "e.Command.CommandText", e.Command.CommandText));
		if (e.Command.Parameters == null)
		{ 
			UnityEngine.Debug.Log(string.Format("Parameters collection is null."));			
		}
		else
		{
			foreach (DbParameter param in e.Command.Parameters)
			{
				string paramName = param.ParameterName == null ? "<null>" : param.ParameterName;
				string paramVal = param.Value == null ? "<null>" : param.Value.ToString();
				UnityEngine.Debug.Log(string.Format("Parameter: {0}={1}", paramName, paramVal));
				UnityEngine.Debug.Log(string.Format("Parameter: {0}: DbType={1} Size={2}", paramName, param.DbType, param.Size));			
			}				
		}
		UnityEngine.Debug.Log(string.Format("Row Updating Event: {0}={1}", "e.StatementType", e.StatementType));
		UnityEngine.Debug.Log(string.Format("Row Updating Event: {0}={1}", "e.Status", e.Status));
		UnityEngine.Debug.Log(string.Format("Row Updating Event: {0}={1}", "e.TableMapping.SourceTable", e.TableMapping.SourceTable));
		UnityEngine.Debug.Log(string.Format("Row Updating Event: {0}={1}", "e.TableMapping.DataSetTable", e.TableMapping.DataSetTable));
		UnityEngine.Debug.Log(string.Format("Row Updating Event: {0}={1}", "e.Row[0]", e.Row[0]));
	}

	/// <summary>
	/// Gets an appropriate DataAdapter for the current connection, initialized with default db commands for the GameObject Type parameter.
	/// </summary>
	/// <returns>
	/// The db data adapter.
	/// </returns>
	/// <typeparam name='T'>
	/// A GameObject that corresponds to a db table of the same name.
	/// </typeparam>
	public DbDataAdapter GetDbDataAdapter<T>() where T: GameObjectBase
	{
		if (currentDbConnection == null) 
		{
			throw new InvalidOperationException("A SetConnection method must be called first before attempting to acquire a data adapter.");
		}
		
		Type gameObjectType = typeof(T);
		
		if (cachedDataAdapters.ContainsKey(gameObjectType)) 
		{
			return cachedDataAdapters[gameObjectType];
		}
		
		string dbTableName = DBNameFromDotNetName(gameObjectType.Name);
		DbDataAdapter adapter = GetDbDataAdapter();
/*
		Type adapterType = adapter.GetType();
		if (adapterType == typeof(SqliteDataAdapter))
		{			
			//SqliteDataAdapter.FillSchema is broken, so we gotta roll our own
			
			//DataTable schemaTable = GetMinimalSchemaTable (dbTableName);
			
//			if (schemaTable == null
//				|| schemaTable.Rows.Count == 0)
//			{
//				return adapter;
//			}
			
			DataTable table = new DataTable(dbTableName);
			//DataTable schemaTable = null;
			//using (
			DbCommand cmd = currentDbConnection.CreateCommand(); //)
			//{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = "SELECT * FROM " + dbTableName;
				adapter.SelectCommand = cmd;
				adapter.FillSchema(table, SchemaType.Source);
				SqliteCommandBuilder builder = new SqliteCommandBuilder(adapter as SqliteDataAdapter);
				adapter.InsertCommand = builder.GetInsertCommand();
				adapter.UpdateCommand = builder.GetUpdateCommand();
				adapter.DeleteCommand = builder.GetDeleteCommand();
			//}
			//BuildAdapterCommands (dbTableName, schemaTable, adapter);
			
		}
		else if (adapterType == typeof(SqlDataAdapter))
		{
			DataTable table = new DataTable(dbTableName);
			using (DbCommand cmd = currentDbConnection.CreateCommand())
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = "SELECT * FROM " + dbTableName;
				adapter.SelectCommand = cmd;
				adapter.FillSchema(table, SchemaType.Source);
				
//				SqlDataAdapter sqlda = adapter as SqlDataAdapter;
//				if (sqlda != null)
//				{
					// this kinda, sorta works, but we get malformed sql when we call adapter.Update
					// comment out for now, and try rolling our own params
				
//					SqlCommandBuilder builder = new SqlCommandBuilder(adapter);  //sqlda);
//					adapter.InsertCommand = builder.GetInsertCommand();
//					adapter.DeleteCommand = builder.GetDeleteCommand();
//					adapter.UpdateCommand = builder.GetUpdateCommand();
//					FixupCommandBuilderGeneratedParameters(adapter, table);
//				}
//				else
//				{
//					throw new InvalidOperationException("Can't seem to cast our SQLDataAdapter as such.  Sorry, no clue.");
//				}
				
				
			}
		}
*/	
		DataTable table = new DataTable(dbTableName);
		DbCommand cmd = currentDbConnection.CreateCommand(); //)
		cmd.CommandType = CommandType.Text;
		cmd.CommandText = "SELECT * FROM " + dbTableName;
		adapter.SelectCommand = cmd;
		adapter.FillSchema(table, SchemaType.Source);
		DbCommandBuilder builder = GetCommandBuilder(adapter);
		
		SqlCommandBuilder sqlbuilder = builder as SqlCommandBuilder;
		if (sqlbuilder != null)
		{
			SqlDataAdapter sqladapter = adapter as SqlDataAdapter;
			sqladapter.InsertCommand = sqlbuilder.GetInsertCommand();
			sqladapter.UpdateCommand = sqlbuilder.GetUpdateCommand();
			sqladapter.DeleteCommand = sqlbuilder.GetDeleteCommand();
		}
		else
		{
			SqliteCommandBuilder sqlitebuilder = builder as SqliteCommandBuilder;
			if (sqlitebuilder != null) 
			{
				SqliteDataAdapter sqliteadapter = adapter as SqliteDataAdapter;
				sqliteadapter.InsertCommand = sqlitebuilder.GetInsertCommand();
				sqliteadapter.UpdateCommand = sqlitebuilder.GetUpdateCommand();
				sqliteadapter.DeleteCommand = sqlitebuilder.GetDeleteCommand();				
			}
		}		
		
		if (adapter.UpdateCommand == null)
		{
			throw new InvalidOperationException("The command builder failed for this connection type.");
		}
		
		cachedDataAdapters.Add(gameObjectType, adapter);
		return adapter;
	}

	public DbCommandBuilder GetCommandBuilder(DbDataAdapter adapter)
	{
		if (currentDbConnection == null) 
		{
			throw new InvalidOperationException("A SetConnection method must be called first before attempting to acquire a command builder.");
		}
		
		DbCommandBuilder builder = null;
		Type currentConnectionType = currentDbConnection.GetType();
		if (currentConnectionType == typeof(SqlConnection)) 
		{
			builder = new SqlCommandBuilder();
		}
		else if (currentConnectionType == typeof(SqliteConnection))
		{
			builder = new SqliteCommandBuilder();
		}
		else
		{
			throw new InvalidOperationException("An appropriate command builder could not be created for this connection.");
		}
		
		return builder;
	}
	
/*
	void BuildAdapterCommands (string dbTableName, DataTable schemaTable, DbDataAdapter adapter)
	{
		if (adapter == null || schemaTable == null || schemaTable.Rows.Count < 1 || string.IsNullOrEmpty(dbTableName))
		{
			return;
		}
		
		/*
		 * SELECT = SELECT x, y, z from t (WHERE keycol parameters)
		 * DELETE = DELETE FROM t (WHERE keycol parameters)
		 * UPDATE = UPDATE t SET x = ?, y = ?, z = ? WHERE keycol parameters
		 * INSERT = INSERT INTO t (x, y, z) VALUES (?, ?, ?)
		 * 
		 * keycol parameters = k1 = ?, k2 = ?
		 * 
		 /
		
		StringBuilder columnList = new StringBuilder();
		StringBuilder setColumnsClause = new StringBuilder();
		StringBuilder insertParamClause = new StringBuilder();
		StringBuilder concurrencyColumnsParamClause = new StringBuilder();
		
		List<DbParameter> insertParams = new List<DbParameter>();
		List<DbParameter> deleteParams = new List<DbParameter>();
		List<DbParameter> concurrencyParams = new List<DbParameter>();
		
		string parameterPlaceholder = "?";
		bool useColumnNameForParameter = false;
		Type adapterType = adapter.GetType();
		if (adapterType == typeof(SqlDataAdapter)) 
		{ 
			parameterPlaceholder = "@";
			useColumnNameForParameter = true;
		}
		
		foreach (DataRow row in schemaTable.Rows) 
		{
			string colname = row["name"].ToString();
//			string coltype = row["type"].ToString();
//			SqlDbType dbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), coltype);
//			DbParameter dbp = new SqliteParameter();
//			DbParameter dbp2 = new SqlParameter();
//			adapter.SelectCommand.Parameters.Add()
			
			
			
			string valueParameterName = useColumnNameForParameter ? colname : "";
			string concurrencyParameterName = useColumnNameForParameter ? colname + "_origval" : "";
			
			columnList.AppendFormat("{0}, ",colname);
			setColumnsClause.AppendFormat("{0} = {1}{2}, ", colname, parameterPlaceholder, valueParameterName);
			insertParamClause.AppendFormat("{0}{1}, ", parameterPlaceholder, valueParameterName);
			
			//if (row["pk"].ToString() != "0")
			//{
				concurrencyColumnsParamClause.AppendFormat("({0} = {1}{2}) AND ", colname, parameterPlaceholder, concurrencyParameterName);					
			//}				
//			DbParameter insertParam = adapterType == typeof(SqlParameter) ? 
//				new SqlParameter()
		}
		
		// get rid of trailing commas, spaces, etc.
		if (columnList.Length >= 2) { columnList.Remove(columnList.Length - 2, 2); }
		if (setColumnsClause.Length >= 2) { setColumnsClause.Remove(setColumnsClause.Length - 2, 2); }
		if (insertParamClause.Length >= 2) { insertParamClause.Remove(insertParamClause.Length - 2, 2); }
		if (concurrencyColumnsParamClause.Length >= 4) { concurrencyColumnsParamClause.Remove(concurrencyColumnsParamClause.Length - 4, 4); }
		
		DbCommand selectCmd = currentDbConnection.CreateCommand();
		DbCommand deleteCmd = currentDbConnection.CreateCommand();
		DbCommand updateCmd = currentDbConnection.CreateCommand();
		DbCommand insertCmd = currentDbConnection.CreateCommand();
		
		selectCmd.CommandText = "SELECT " + columnList.ToString() + " FROM " + dbTableName;
		deleteCmd.CommandText = "DELETE FROM " + dbTableName + " WHERE " + concurrencyColumnsParamClause.ToString();
		updateCmd.CommandText = "UPDATE " + dbTableName + " SET " + setColumnsClause.ToString() + " WHERE " + concurrencyColumnsParamClause.ToString();
		insertCmd.CommandText = "INSERT INTO " + dbTableName + " ( " + columnList.ToString() + " ) VALUES ( " + insertParamClause.ToString() + " )";
		
		adapter.SelectCommand = selectCmd;
		adapter.DeleteCommand = deleteCmd;
		adapter.UpdateCommand = updateCmd;
		adapter.InsertCommand = insertCmd;
	}

		public void FixupCommandBuilderGeneratedParameters (DbDataAdapter adapter, DataTable table)
	{
		if (adapter == null || table == null) {return;}
//		if (adapter.InsertCommand != null)
//		{
//			FixupCommandParameters (adapter.InsertCommand.Parameters, table);
//		}
		if (adapter.UpdateCommand != null)
		{
			FixupCommandParameters (adapter.UpdateCommand.Parameters, table);
		}
//		if (adapter.DeleteCommand != null)
//		{
//			FixupCommandParameters (adapter.DeleteCommand.Parameters, table);
//		}
	}

	private void FixupCommandParameters (DbParameterCollection parameters, DataTable table)
	{
		// note: there are 2x as many command parameters than table columns
		// because the commandbuilder generates one parameter for the new value,
		// and one to check the existing value
		// (...and one for the little girl who lives down the lane ;)
		
		if (parameters == null || table == null || parameters.Count != (table.Columns.Count * 2)) { return; }
		
		// right now, all I've discovered is that char column sizes are not getting set
		// there may be more to do if more errors are uncovered -mt 7/1/2013
		
		for (int i = 0; i < table.Columns.Count; i++) 
		{
			DbParameter param1 = parameters[i];
			DbParameter param2 = parameters[i + table.Columns.Count];
			DataColumn col = table.Columns[i];
			param1.Size = col.MaxLength;
			param2.Size = col.MaxLength;
		}
	}
	
	/// <summary>
	/// Gets the minimal schema table for a given db table.  Since sqlite returns the most minimal schema info, all queries are modeled on sqlite's schema table structure.
	/// </summary>
	/// <returns>
	/// The minimal schema table containing basic column info.
	/// </returns>
	/// <param name='dbTableName'>
	/// The db table name whose schema is to be fetched.
	/// </param>
	/// <exception cref='InvalidOperationException'>
	/// Thrown if this method is called before establishing a database connection.
	/// </exception>
	public DataTable GetMinimalSchemaTable(string dbTableName)
	{
		if (currentDbConnection == null)
		{
			throw new InvalidOperationException("A SetConnection method must be called first before attempting to acquire a data adapter.");
		}
		
		DataTable schemaTable = new DataTable(dbTableName + "_schema");
		
		using(DbDataAdapter adapter = GetDbDataAdapter())
		{
			Type adapterType = adapter.GetType();
			using(DbCommand cmd = currentDbConnection.CreateCommand())
			{
				cmd.CommandType = CommandType.Text;
				
				// columns returned are: cid (column id), name, type, notnull, dflt_value, pk
		
				if (adapterType == typeof(SqliteDataAdapter))
				{		
					cmd.CommandText = "pragma table_info([" + dbTableName + "])";
				}
				else if (adapterType == typeof(SqlDataAdapter))
				{
					cmd.CommandText = 
						"select c.ordinal_position as cid," +
						"	c.COLUMN_NAME as name, " +
						"	c.data_type as type, " +
						"	case c.IS_NULLABLE when 'NO' then 1 else 0 end as notnull, " +
						"	c.column_default as dflt_value, " +
						"	coalesce(kcu.ordinal_position, 0) as pk " +
						"from information_schema.columns c " +
						"left join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc " +
						"			on tc.TABLE_CATALOG = c.TABLE_CATALOG " +
						"		   and tc.TABLE_SCHEMA = c.TABLE_SCHEMA " +
						"		   and tc.TABLE_NAME = c.TABLE_NAME " +
						"		   and tc.CONSTRAINT_TYPE = 'PRIMARY KEY' " +
						"     left join INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu " +
						"			on kcu.CONSTRAINT_CATALOG = tc.CONSTRAINT_CATALOG " +
						"		   and kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA " +
						"		   and kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME " +
						"		   and kcu.COLUMN_NAME = c.COLUMN_NAME " +
						"where c.TABLE_NAME = ?";
					
					DbParameter param = cmd.CreateParameter();
					param.Direction = ParameterDirection.Input;
					param.ParameterName = "table_name";
					param.Value = dbTableName;
					
					cmd.Parameters.Add(param);
				}
				
				adapter.SelectCommand = cmd;
				adapter.Fill(schemaTable);
			} // using cmd
		} // using adapter
		return schemaTable;			
	}
*/
	public string DBNameFromDotNetName (string name)
	{
		if (string.IsNullOrEmpty(name)) { return ""; }
		
		string dbname = "";
		foreach (char c in name) 
		{
			if (dbname == "") 
			{
				dbname = Char.ToLower(c).ToString(); 
			} 
			else if (Char.IsUpper(c)) 
			{
				dbname += "_" + Char.ToLower(c);
			}
			else
			{
				dbname += Char.ToLower(c);
			}
		}
		
		return dbname;
	}
	
//	public DbCommandBuilder GetDbCommandBuilder()
//	{
//		if (currentDbConnection == null) 
//		{
//			throw new InvalidOperationException("A SetConnection method must be called first before attempting to acquire a command builder.");
//		}
//		
//		DbCommandBuilder builder = null;
//		
//		Type connectionType = currentDbConnection.GetType();
//		if (connectionType == typeof(SqlConnection))  
//		{
//			builder = new SqlCommandBuilder();
//		}
//		else if (connectionType == typeof(SqliteConnection))
//		{
//			builder = new SqliteCommandBuilder();
//		}
//		
//		return builder;
//	}
	
	#endregion
	
	#region private helper methods
	
	private string GetDefaultDbConnectionString()
	{
		string localDBFilename = FindLocalDBFile(Application.dataPath);
		if (string.IsNullOrEmpty(localDBFilename))
		{
			return "";
		}
		
		return ConnectionStringFromLocalDBFilename(localDBFilename);
	}
	
	private string ConnectionStringFromLocalDBFilename (string localDBFilename)
	{
		if (string.IsNullOrEmpty(localDBFilename))
		{
			return "";
		}
		
		return "URI=file:" + localDBFilename;
	}	
/*
	private void LogException(System.Exception ex)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("The following error has occurred:");
		while (ex != null) 
		{
			sb.AppendLine(ex.Message);
			ex = ex.InnerException;
		}
	}
*/	
	/// <summary>
	/// Finds the first local file, if any, that "looks like" a database file.
	/// </summary>
	/// <returns>
	/// The full pathname of a possible local DB file.
	/// </returns>
	/// <param name='searchDirectory'>
	/// Search directory.
	/// </param>
	/// 
	private string FindLocalDBFile(string searchDirectory) {		
		DirectoryInfo di = new DirectoryInfo(searchDirectory);		
		FileInfo[] allFiles = di.GetFiles("*", SearchOption.AllDirectories);
		foreach (FileInfo fi in allFiles)
		{
			if (LOCAL_DB_FILE_EXTENSIONS.Contains(fi.Extension.ToLower()))
			{
				return fi.FullName;
			}			
		}
		
		// if we get here, no file was found
		return null;
	}
	
	#endregion
	
}


