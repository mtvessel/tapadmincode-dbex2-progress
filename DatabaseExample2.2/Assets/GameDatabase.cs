using System.Linq;
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
public class GameDatabase {
	
	private static readonly List<string> LOCAL_DB_FILE_EXTENSIONS = new List<string> { ".db", ".db3", ".sqlite" };
	
	private DbConnection currentDbConnection = null;
	public DbConnection CurrentDbConnection 
	{
		get { return this.currentDbConnection; }
	}	
	
	private Dictionary<Type, DbDataAdapter> cachedDataAdapters = new Dictionary<Type, DbDataAdapter>();
	private Dictionary<string, ForeignKeyCollection> dbFkCollection = new Dictionary<string, ForeignKeyCollection>();	
	public Dictionary<string, ForeignKeyCollection> DbFkCollection 
	{
		get	{ return this.dbFkCollection; }
	}	
	
	#region public utility methods
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
			
			BuildForeignKeyMappings();
			
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
	
	public void SetConnectionFromEmbeddedResource(string resourceName)
	{
		// extract in embedded resource into a TextAsset
		// (see Unity manual entry for TextAsset for more info)
		TextAsset asset = Resources.Load(resourceName) as TextAsset;
		string resourceFilename = Application.dataPath + "\\" + resourceName;
		
		// write out that embedded resource as a file back into its original form 
		using (FileStream fs = new FileStream(resourceFilename, FileMode.Create))
		{
			using (BinaryWriter wr = new BinaryWriter(fs))
			{
				wr.Write(asset.bytes);					
			}			
		}
		
		SetConnection(ConnectionStringFromLocalDBFilename(resourceFilename), SupportedDbType.SQLITE);
	}

	public void TurnOnSqliteForeignKeySupport()
	{
		// For historical reasons, foreing key support is not automatically turned on for sqlite.
		// If it is desired (and it is), it must be turned on manually.
		// NOTE: This must be done every time the connection is opened, so it is intentionally left open here.
		// For this reason, it's best to only call this method right before doing a save.
		if (currentDbConnection != null
			&& currentDbConnection is SqliteConnection
			)
		try 
		{
			if (currentDbConnection.State != ConnectionState.Open)
			{
				currentDbConnection.Open();
			}
			DbCommand cmd = currentDbConnection.CreateCommand();
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = "PRAGMA foreign_keys = ON";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "PRAGMA foreign_keys";
			object fkStatus = cmd.ExecuteScalar();
			Debug.Log("Turning SQLite foreign key support on. PRAGMA foreign keys returned: " + fkStatus.ToString());
			if (int.Parse(fkStatus.ToString()) != 1)
			{
				throw new InvalidDataException("Foreign key support for this SQLite database cannot be turned on.");
			}											
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			throw;
		}
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
		// can be used for debugging db commands, but throws exceptions on deletes, so uncomment at your own risk!
//#if DEBUG
//		LogRowUpdatingEvent(e);		
//#endif
	}

	void HandleSqldaRowUpdated (object sender, SqlRowUpdatedEventArgs e)
	{
		UnityEngine.Debug.Log("Row Updated.");
	}

	void HandleSqldaRowUpdating (object sender, SqlRowUpdatingEventArgs e)
	{	
		// can be used for debugging db commands, but throws exceptions on deletes, so uncomment at your own risk!
//#if DEBUG
//		LogRowUpdatingEvent(e);		
//#endif
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
		
		string dbTableName = DbNameFromDotNetName(gameObjectType.Name);
		DbDataAdapter adapter = GetDbDataAdapter();
		DataTable table = new DataTable(dbTableName);
		DbCommand cmd = currentDbConnection.CreateCommand(); //)
		cmd.CommandType = CommandType.Text;
		cmd.CommandText = "SELECT * FROM " + dbTableName;
		adapter.SelectCommand = cmd;
		
		// You're supposed to be able to declare a variable using the base type DbAdapter, assign a derived type to it, 
		// (i.e. SqlDataAdapter, SqliteDataAdapter, etc.), and then any calls to overriden methods are executed by the derived type. 
		// But I think some methods are not overriden properly.  That is, if they've been declared with the keyword "new" 
		// rather than "override", you have to cast to the derived type to get the correct method to execute.
		// I can't remember if this is one of those times, so I'm leaving it in for all the code below. -mt
		if (adapter is SqlDataAdapter)
		{
			((SqlDataAdapter)adapter).FillSchema(table, SchemaType.Source);
		}
		else if (adapter is SqliteDataAdapter)
		{
			((SqliteDataAdapter)adapter).MissingSchemaAction = MissingSchemaAction.AddWithKey;
			((SqliteDataAdapter)adapter).FillSchema(table, SchemaType.Source);	
		}
		
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
			SqlDataAdapter sqladapter = adapter as SqlDataAdapter;
			builder = new SqlCommandBuilder(sqladapter);
		}
		else if (currentConnectionType == typeof(SqliteConnection))
		{
			SqliteDataAdapter sqliteadapter = adapter as SqliteDataAdapter;
			builder = new SqliteCommandBuilder(sqliteadapter);
		}
		else
		{
			throw new InvalidOperationException("An appropriate command builder could not be created for this connection.");
		}
		
		return builder;
	}
	
	public string DbNameFromDotNetName (string name)
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
	
	public string DotNetNameFromDbName(string name)
	{
		if (string.IsNullOrEmpty(name)) { return ""; }
		
		string dotNetName = "";
		bool nextIsUpper = true;
		foreach (char c in name)
		{
			if (c == '_')
			{
				nextIsUpper = true;
			}
			else
			{
				dotNetName += nextIsUpper ? char.ToUpper(c) : c;
				nextIsUpper = false;
			}
		}
		
		return dotNetName;
	}
	
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
	
	private void BuildForeignKeyMappings()
	{
		if (this.currentDbConnection == null)
		{
			throw new InvalidOperationException("A SetConnection method must be called first before attempting to retrieve foreign key info.");
		}
		
		dbFkCollection.Clear();
		
		UnityEngine.Debug.Log("Retrieving foreign key information...");
		
		DataTable fkTable = null;		
		Type currentDbConnectionType = currentDbConnection.GetType();
		if (currentDbConnectionType == typeof(SqliteConnection)) 
		{
			ConnectionState origState = currentDbConnection.State;
			try {
				if (origState != ConnectionState.Open)
				{
					currentDbConnection.Open();
				}
				
				fkTable = currentDbConnection.GetSchema("ForeignKeys");
			} 
			finally
			{
				if (origState == ConnectionState.Closed)
				{
					currentDbConnection.Close();	
				}				
			}
			
		}
		else
		{
            DbCommand cmd = currentDbConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = 
                "select " +
                "        fk.Name AS FKEY_ID, " +
                "        OBJECT_NAME(fk.parent_object_id) as TABLE_NAME, " +
                "        OBJECT_NAME(fk.referenced_object_id) as FKEY_TO_TABLE, " +
                "        cpa.name as FKEY_FROM_COLUMN, " +
                "        cref.name as FKEY_TO_COLUMN " +
                "    FROM " +
                "        sys.foreign_keys fk " +
                "    INNER JOIN " +
                "        sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id " +
                "    INNER JOIN " +
                "        sys.columns cpa ON fkc.parent_object_id = cpa.object_id AND fkc.parent_column_id = cpa.column_id " +
                "    INNER JOIN  " +
                "        sys.columns cref ON fkc.referenced_object_id = cref.object_id AND fkc.referenced_column_id = cref.column_id ";
            DbDataAdapter fkadapter = GetDbDataAdapter();
            fkadapter.SelectCommand = cmd;
            fkTable = new DataTable();
            fkadapter.Fill(fkTable);
		}
		
		if (fkTable == null || fkTable.Rows.Count == 0)
		{
			UnityEngine.Debug.Log("...no foreign key information found.");		
			return;
		}

		var fkConstraintQuery = 
			from fkTableRow in fkTable.AsEnumerable()
		    group fkTableRow by new { ConstraintName = fkTableRow["CONSTRAINT_NAME"].ToString(), 
									  SourceTableName = fkTableRow["TABLE_NAME"].ToString(),
									  TargetTableName = fkTableRow["FKEY_TO_TABLE"].ToString()	
									} into constraintGroup
			select constraintGroup;
		// NOTE: I wanted to declare the actual columns from the detail row, and give them friendly names/types (like below),
		// just like I did in the group by clause above, but I just couldn't get it to work in linq. --mt
		//					select new { SourceColumnName = constraintGroup["FKEY_FROM_COLUMN"].ToString(),
		//						 		 TargetColumnName = constraintGroup["FKEY_TO_COLUMN"].ToString() };
		foreach (var constraint in fkConstraintQuery )
		{
			ForeignKeyCollection sourceObjectFkColl = null;
			
			string sourceTableName = constraint.Key.SourceTableName;
			string sourceTypeName = DotNetNameFromDbName(sourceTableName);
			string targetTableName = constraint.Key.TargetTableName;
			string targetTypeName = DotNetNameFromDbName(targetTableName);
			
			ForeignKeyPropertyMappingCollection mappings = new ForeignKeyPropertyMappingCollection(targetTypeName);
			foreach (var constraintRow in constraint)
			{
				string sourceColumnName = constraintRow["FKEY_FROM_COLUMN"].ToString();
				string sourcePropertyName = DotNetNameFromDbName(sourceColumnName);
				string targetColumnName = constraintRow["FKEY_TO_COLUMN"].ToString();
				string targetPropertyName = DotNetNameFromDbName(targetColumnName);
				mappings.Add(
					new ForeignKeyPropertyMapping(sourcePropertyName, targetPropertyName));
			}

			// is there an entry containing foreign key mappings for this object in the master list?
			if (dbFkCollection.ContainsKey(sourceTypeName))
			{
				// yes, this object already has a mapping collection, so just add the current mappings to it
				sourceObjectFkColl = dbFkCollection[sourceTypeName];
				sourceObjectFkColl.Add(targetTypeName, mappings);
			}
			else
			{
				// no, this is a new mapping collection, so create one and add the current set of mappings
				sourceObjectFkColl = new ForeignKeyCollection(targetTypeName, mappings);
				// and add it to the master list
				dbFkCollection.Add(sourceTypeName, sourceObjectFkColl);
			}
#if DEBUG			
			DumpFkCollection(dbFkCollection);
#endif
		}		
		
		UnityEngine.Debug.Log("...foreign key information mapped.");		
	}
	#endregion

	#region test
	public void DumpFkCollection(Dictionary<string, ForeignKeyCollection> fkcoll)
	{
		StringBuilder sb = new StringBuilder();
		foreach( string typeName in fkcoll.Keys )
		{
			sb.AppendFormat("ForeignKeyCollection for Key:{0}", typeName).AppendLine();
			ForeignKeyCollection coll = fkcoll[typeName];
			foreach (string propertyName in coll.PropertiesWithForeignKeysMappings)				
			{
				sb.AppendFormat("   Mappings for Key:{0}", propertyName).AppendLine();
				ForeignKeyPropertyMappingCollection mappings = coll[propertyName];
				foreach (ForeignKeyPropertyMapping mapping in mappings.Mappings)
				{				
					sb.AppendFormat("      SourceProperty:{0}, TargetTable{1}, TargetProperty{2}", 
						mapping.SourceObjectPropertyName, 
						mapping.TargetObjectKeyPropertyName,
						mappings.TargetTypeName).AppendLine();
				}
			}
		}
		
		UnityEngine.Debug.Log(sb.ToString());
	}
	
	public void PrintAllMetaData()
	{
		// TEST AREA!
		ConnectionState origConnectionState = currentDbConnection.State;
		if (origConnectionState != ConnectionState.Open) {
			currentDbConnection.Open();
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Metadata info for connection type" + currentDbConnection.GetType().ToString());
		
		try {
			DataTable metaDataTable = currentDbConnection.GetSchema("MetaDataCollections");
			foreach (DataRow metarow in metaDataTable.Rows)
			{
				string collectionName = metarow[0].ToString();
				if (collectionName == "StructuredTypeMembers" 
					|| collectionName == "Views"
					|| collectionName == "ViewColumns"
					|| collectionName == "UserDefinedTypes"
					)
				{
					continue; // throws an error
				}
				DataTable collectionTable = currentDbConnection.GetSchema(collectionName);
				// print header
				sb.AppendLine("Collection: " + collectionName);
				foreach (DataColumn col in collectionTable.Columns)
				{				
					sb.AppendFormat("{0}\t", col.ColumnName);
				}
				sb.AppendLine();
				
				foreach (DataRow collectionrow in collectionTable.Rows)
				{
					for(int i = 0; i < collectionTable.Columns.Count; i++)
					{
						sb.AppendFormat("{0}\t", collectionrow[i].ToString());
					}
					sb.AppendLine();
				}
				sb.AppendLine();
			}
			
			// try these explicitly:
			//ForeignKeys	Table	@Table	TABLE_NAME	3																		
			//ForeignKeys	Name	@Name	CONSTRAINT_NAME	4																		
//			if (currentDbConnection.GetType() == typeof(SqlConnection))
//			{
//				DataTable fkschema1 = currentDbConnection.GetSchema("ForeignKeys", new string[] {"Table", "player_item"});
//				DataTable fkschema2 = currentDbConnection.GetSchema("ForeignKeys", new string[] {"Name", "FK_player_item_game_item"});
//				int fuckYouMonoDevelopINeedToLookAtTheLastStatementBeforeYouGoOutOfScope = 1;
//			}

			
		} finally {
			using (StreamWriter writer = File.CreateText(Application.dataPath + "\\metatdata_info.tab"))
			{
				writer.Write(sb.ToString());
			}			
				
			if (origConnectionState == ConnectionState.Closed) {
				currentDbConnection.Close();
			}
			
			Debug.Log("Print metadata complete.");
		}
	}
	#endregion	
}


