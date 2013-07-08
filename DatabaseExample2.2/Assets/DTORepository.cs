using System.Reflection;
using System.Diagnostics;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;

//namespace AssemblyCSharp
//{
	/// <summary>
	/// Base class for repositories, implemented as a singleton pool.  Concrete repositories
	/// Repositories handle the basic operations of translating between database entities
	/// and domain model objects.  In order to interact with the database, each domain model class must have its own custom repository.
	/// Implementors must provide information about table and column names, must implement a function to map DataRows to objects, and optionally,
	/// a function to map child objects.
	/// </summary>
public class DTORepository<TGameObject>
	where TGameObject : GameObjectBase, new()
{
	#region fields/properties

	// allItems = cached copy of all DTO objects of a given type
	private List<TGameObject> allItems = new List<TGameObject>();
	public List<TGameObject> AllItems 
	{
		get { return allItems; }
	}
	
	// currentGameDatabase is the more recent database instance	used to access data
	private GameDatabase currentGameDatabase = null;		
	
	// currentDataAdapter is the last used fully-initialized data adapter
	private DbDataAdapter currentDataAdapter = null;
	
	// currentDataAdapter is the last data table used to transfer data to/from the db
	private DataTable currentDataTable = null;
	
	// mapping of GameObject PropertyInfo objects to data table column names, format = Key = column name, Value = PropertyInfo
	private Dictionary<string, PropertyInfo> propertyInfoForColumnName = new Dictionary<string, PropertyInfo>();

	// list of GameObject Property names that comprise the primary key for this object
	private List<string> primaryKeyPropertyNames = new List<string>();
	
	private Dictionary<DataRow, TGameObject> gameObjectForDataRow = new Dictionary<DataRow, TGameObject>();
	private Dictionary<SortedDictionary<string, object>, TGameObject> gameObjectForPrimaryKey = 
		new Dictionary<SortedDictionary<string, object>, TGameObject>(new PrimaryKeyComparer());

	
	#endregion fields
	
	#region singleton instance control 
	
	// singleton code based on documentation here: http://msdn.microsoft.com/en-us/library/ff650316.aspx
	
	// instancePool holds all previously instantiated instances of DTORepository
	// keyword "volatile" is used to ensure thread-safe locking
	private static volatile Dictionary<Type, DTORepository<TGameObject>> instancePool = new Dictionary<Type, DTORepository<TGameObject>>();
    private static object syncRoot = new Object();

	// singleton -- ctor must remain private to prevent direct instantiation
	private DTORepository()	{ }

	/// <summary>
	/// Gets an instance of a DTORepository for the game object type specified.
	/// </summary>
	/// <returns>
	/// The repository to be used with objects of that that game object type
	/// </returns>
	public static DTORepository<TGameObject> GetInstance()
    {
		Type dtoType = typeof(TGameObject);
        if (!instancePool.ContainsKey(dtoType))
        {
            lock (syncRoot)
            {
                if (!instancePool.ContainsKey(dtoType))
                    instancePool.Add(dtoType, new DTORepository<TGameObject>());
            }
        }

        return instancePool[dtoType];
    }

	#endregion singleton instance control 		

	#region public methods
	
	/// <summary>
	/// Finds all objects of a given type.
	/// </summary>
	/// <returns>
	/// A List containing all objects found in the game database.
	/// </returns>
	/// <param name='gameDatabase'>
	/// The game database where the data is stored.
	/// </param>
	/// <exception cref='System.ArgumentException'>
	/// An argument exception is thrown is the game database provided is not valid.
	/// </exception>
	public List<TGameObject> FindAll(GameDatabase gameDatabase) {
		Debug.WriteLine("FindAll starting...");

		if (allItems == null)
		{
			allItems = new List<TGameObject>();
		}
		else if (currentGameDatabase == gameDatabase)
		{
			// if we've already retrieved the collection previously from this database, return the cached collection
			return allItems;
		}

		DbDataAdapter dataAdapter = null;
		if (currentGameDatabase == gameDatabase
			&& currentDataAdapter != null)
		{
			dataAdapter = currentDataAdapter;
		}
		else
		{
			dataAdapter = gameDatabase.GetDbDataAdapter<TGameObject>();
		}
		
		DataTable dataTable = new DataTable();
        try
        {
            if (dataAdapter.SelectCommand.Connection.State != ConnectionState.Open)
            {
                dataAdapter.SelectCommand.Connection.Open();
            }
			
			dataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
			dataAdapter.FillSchema(dataTable, SchemaType.Source);
            dataAdapter.Fill(dataTable);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine(ex.Message);
            System.Diagnostics.Debug.WriteLine(ex);
            throw;
        }
        finally
        {
            if (dataAdapter.SelectCommand.Connection.State != ConnectionState.Closed)
            {
                dataAdapter.SelectCommand.Connection.Close();
            }
        }
		
		allItems = MapDataTable(dataTable, gameDatabase);
		currentGameDatabase = gameDatabase;
		currentDataAdapter = dataAdapter;
				
		// Return the populated collection of game items.		
		UnityEngine.Debug.Log("FindAll returning a collection of " + allItems.Count + " item(s).");
		return allItems;
	} // FindAll
	
	public TGameObject FindByKey(GameDatabase gameDatabase, SortedDictionary<string, object> keys)
	{
		TGameObject gameObjectForKey = null;
		
		if (AllItems == null || AllItems.Count == 0)
		{
			FindAll(gameDatabase);
		}
		
		if (gameObjectForPrimaryKey != null && gameObjectForPrimaryKey.Count > 0)
		{
			if (gameObjectForPrimaryKey.ContainsKey(keys))
			{
				gameObjectForKey = gameObjectForPrimaryKey[keys];
			}
			//TEST - since debugging collections is buggy, dump all values to console
#if DEBUG
			UnityEngine.Debug.Log("Looking for object where key is...");
			foreach(string key in keys.Keys)
			{
				UnityEngine.Debug.Log(string.Format("{0}={1}", key, keys[key]));
			}
			UnityEngine.Debug.Log("Key collection contains is...");
			int itemCnt = 0;
			foreach(SortedDictionary<string, object> primaryKey in gameObjectForPrimaryKey.Keys)
			{
				itemCnt++;
				UnityEngine.Debug.Log("Primary Keys for item #" + itemCnt);
				foreach(string keyProperty in primaryKey.Keys)
				{
					UnityEngine.Debug.Log(string.Format("property {0}={1}", keyProperty, primaryKey[keyProperty].ToString()));
				}
			}						
#endif
		}
		
		return gameObjectForKey;
	}
	
	public List<TGameObject> SaveAll(GameDatabase gameDatabase)
	{
		if (gameDatabase == null) { return this.allItems; }
				
		DbDataAdapter dataAdapter = null;
		if (currentGameDatabase == gameDatabase
			&& currentDataAdapter != null)
		{
			dataAdapter = currentDataAdapter;
		}
		else
		{
			dataAdapter = gameDatabase.GetDbDataAdapter<TGameObject>();
		}

		try 
		{
			bool wouldYouGiveUpTheCostOfACupOfCoffeeToSaveTheChildren = true;
			foreach (TGameObject item in this.allItems) 
			{
				if (wouldYouGiveUpTheCostOfACupOfCoffeeToSaveTheChildren)
				{						
					SaveChildObjects(gameDatabase, item);
					// children only need to be saved once (...for they are blameless in the eyes of the Lord)
					wouldYouGiveUpTheCostOfACupOfCoffeeToSaveTheChildren = false;
				}
				
				if (item.ObjectState == DataRowState.Unchanged)
				{
					continue;
				}				
				
				switch (item.ObjectState)
				{
				case DataRowState.Unchanged:
					break;
				case DataRowState.Added:
					item.TableRow = currentDataTable.NewRow();
					PopulateRowFromObject(item, item.TableRow);
					currentDataTable.Rows.Add(item.TableRow);
					break;
				case DataRowState.Deleted:
					item.TableRow.Delete();
					break;
				case DataRowState.Modified:
					PopulateRowFromObject(item, item.TableRow);
					break;
				default:
				break;
				}
			}
				
            if (dataAdapter.SelectCommand.Connection.State != ConnectionState.Open)
            {
                dataAdapter.SelectCommand.Connection.Open();
            }
			
			// Just like in GameDatabase, I'm not sure if calling .Update on the 
			// subtype is necessary, but since data adapters are flaky, we'll do it that way here, too.  -mt
            if (dataAdapter is SqlDataAdapter)
            {
                SqlDataAdapter sqlda = dataAdapter as SqlDataAdapter;
                sqlda.Update(currentDataTable);
            }
            else if (dataAdapter is SqliteDataAdapter )
            {
				SqliteDataAdapter sqliteda = dataAdapter as SqliteDataAdapter;
                sqliteda.Update(currentDataTable);
            }				
		}
		catch (System.Exception ex)
		{
			Debug.WriteLine("The following error occurred while updating the db: " + ex.Message );
			UnityEngine.Debug.LogException(ex);
			throw;
		}
        finally
        {
            if (dataAdapter.SelectCommand.Connection.State != ConnectionState.Closed)
            {
                dataAdapter.SelectCommand.Connection.Close();
            }
        }
		
		// refresh data
		allItems = null;
		FindAll(gameDatabase);
		
		return allItems;
	}

	public void SaveChildObjects (GameDatabase gameDatabase, TGameObject item)
	{
		if (gameDatabase == null) { return; }
	
		// get all child properties
		Type itemType = item.GetType();
		PropertyInfo[] itemProperties = itemType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo pi in itemProperties)
		{
			// only interested in properties whose type is derived from GameObjectBase
			if (pi.PropertyType.IsSubclassOf(typeof(GameObjectBase)))
			{
				// construct a repository for this type
				object constructedRepo = ConstructRepositoryforType(pi.PropertyType);
				Type constructedRepoType = constructedRepo.GetType();
				// get and invoke the SaveAll method
				MethodInfo saveAllMi = constructedRepoType.GetMethod("SaveAll");
				saveAllMi.Invoke(constructedRepo, new object[] {gameDatabase});
			}
		}			
	}
	
	private void PopulateObjectFromRow (TGameObject item, DataRow tableRow)
	{
		if (propertyInfoForColumnName == null || propertyInfoForColumnName.Count == 0)
		{
			BuildPropertyColumnMappings(tableRow);		
		}
		
		if (primaryKeyPropertyNames == null || primaryKeyPropertyNames.Count == 0)
		{
			BuildPrimaryKeyPropertyList(tableRow.Table);
		}

		SortedDictionary<string, object> primaryKeyValues = new SortedDictionary<string, object>();
		
		foreach (string colname in propertyInfoForColumnName.Keys) 
		{			
			PropertyInfo targetPropertyInfo = propertyInfoForColumnName[colname];
			object propertyValue = Convert.ChangeType(tableRow[colname], targetPropertyInfo.PropertyType);
			targetPropertyInfo.SetValue(item, propertyValue, null);
			if (primaryKeyPropertyNames.Contains(targetPropertyInfo.Name))
			{
				primaryKeyValues.Add(targetPropertyInfo.Name, propertyValue);
			}
		}
		
		if (primaryKeyValues.Count > 0)
		{
			gameObjectForPrimaryKey.Add(primaryKeyValues, item);
		}
	}

	private void PopulateRowFromObject (TGameObject item, DataRow tableRow)
	{
		if (propertyInfoForColumnName == null || propertyInfoForColumnName.Count == 0)
		{
			BuildPropertyColumnMappings(tableRow);		
		}
		
		foreach (string colname in propertyInfoForColumnName.Keys) 
		{			
			PropertyInfo targetPropertyInfo = propertyInfoForColumnName[colname];
			object propertyValue = targetPropertyInfo.GetValue(item, null);
			Type columnType = tableRow.Table.Columns[colname].DataType;
			tableRow[colname] = Convert.ChangeType(propertyValue, columnType);
		}
	}

	private void BuildPropertyColumnMappings (DataRow tableRow)
	{
		Type gameObjectType = typeof(TGameObject);
		foreach (DataColumn col in tableRow.Table.Columns) 
		{
			string targetPropertyName = PropertyNameFromColumnName(col.ColumnName);
			if (!string.IsNullOrEmpty(targetPropertyName))
			{
				PropertyInfo pi = gameObjectType.GetProperty(targetPropertyName, BindingFlags.Public | BindingFlags.Instance);
				if (pi != null && pi.CanWrite) 
				{
					propertyInfoForColumnName.Add(col.ColumnName, pi);
				}				
			}
		}
	}

	private string PropertyNameFromColumnName (string columnName)
	{
		string propertyName = "";

		if (string.IsNullOrEmpty(columnName))
		{
			return propertyName;
		}
		
		bool nextCharIsUpper = true;
		foreach (char c in columnName) 
		{
			if (c == '_')
			{
				nextCharIsUpper = true;
				continue;
			}
			
			propertyName += nextCharIsUpper ? Char.ToUpper(c) : Char.ToLower(c);
			nextCharIsUpper = false;
		}
		
		return propertyName;		
	}
	
	public TGameObject CreateNewObject()
	{
		if (allItems == null) 
		{
			allItems = new List<TGameObject>();
		}
		
		TGameObject newItem = new TGameObject();
		newItem.SetObjectChanged(DataRowState.Added);
		allItems.Add(newItem);
		return newItem;
	}
	
	public void DeleteObject(TGameObject item)
	{
		if (allItems == null || allItems.Count == 0)
		{
			return;
		}
		
		if (allItems.Contains(item))
		{
			item.SetObjectChanged(DataRowState.Deleted);
		}
	}
	
	#endregion public methods
	
	#region private methods
	
	private List<TGameObject> MapDataTable (DataTable table, GameDatabase gameDatabase)
	{
		allItems = new List<TGameObject>();
		gameObjectForDataRow = new Dictionary<DataRow, TGameObject>();
		gameObjectForPrimaryKey = new Dictionary<SortedDictionary<string, object>, TGameObject>(new PrimaryKeyComparer());
		
		if (primaryKeyPropertyNames.Count == 0)
		{
			BuildPrimaryKeyPropertyList (table);
		}
		
		// Iterate through the returned datatable and for each row returned, call the dbLogic component
		// to map each row to an object.  For each object, also map any child properties that require their own
		// repository calls.
		foreach (DataRow row in table.Rows)
		{
			TGameObject newItem = new TGameObject();
			PopulateObjectFromRow(newItem, row);
			newItem.TableRow = row;
			PopulateChildObjects(newItem, gameDatabase);
			newItem.SetObjectChanged(DataRowState.Unchanged);
			allItems.Add(newItem);
			gameObjectForDataRow.Add(row, newItem);
		}
		
		currentDataTable = table;
		return allItems;
	}

	void PopulateChildObjects (TGameObject newItem, GameDatabase gameDatabase)
	{
		if (newItem == null || gameDatabase == null) { return; }
		
		// get all the fks for this game object type
		Type gameObjectType = typeof(TGameObject);
		string gameObjectTypeName = gameObjectType.Name;
		
		ForeignKeyCollection typeFkcoll = null;
		if (gameDatabase.DbFkCollection.ContainsKey(gameObjectTypeName))
		{
			typeFkcoll = gameDatabase.DbFkCollection[gameObjectTypeName];
		}
		
		// if there are no fk mappings for this object type, just return
		if ( typeFkcoll == null ) { return; }			
		
		// get all the property names that have fk mappings
		foreach (string propertyToMap in typeFkcoll.PropertiesWithForeignKeysMappings)
		{
			// does the type have a writeable, public, instance property that matches the name?
			PropertyInfo piToPopulate = gameObjectType.GetProperty(propertyToMap, 
														 BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			if (piToPopulate == null || piToPopulate.CanWrite == false)
			{
				continue;
			}
			
			// We found a property we'd like to populate.
			// Get the appropriate repository for the property's type, and that repository's FindByKey method
			object constructedChildRepo = ConstructRepositoryforType(piToPopulate.PropertyType);
			Type constructedRepoType = constructedChildRepo.GetType();
			MethodInfo findByKeyMi = constructedRepoType.GetMethod("FindByKey");
			
			// next get all the mappings needed to get the right instance of the child object
			ForeignKeyPropertyMappingCollection mappingsForProperty = typeFkcoll[propertyToMap];
			SortedDictionary<string, object> childPrimaryKeys = new SortedDictionary<string, object>();
			bool doFind = true;
			foreach (ForeignKeyPropertyMapping mapping in mappingsForProperty.Mappings) 
			{
				PropertyInfo piKeyValue = gameObjectType.GetProperty(mapping.SourceObjectPropertyName,
														 BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
				// if we don't have a property to supply the value, don't continue with this mapping
				if (piKeyValue == null)
				{
					doFind = false;
					break;
				}
												
				object primaryKeyValue = piKeyValue.GetValue(newItem, null);
				
				// if we don't have a value for this property, don't continue
				if (primaryKeyValue == null) 
				{
					doFind = false;
					break;
				}
				
				// add the value for this foreign key property to the target's primary key collection
				childPrimaryKeys.Add(mapping.TargetObjectKeyPropertyName, primaryKeyValue);									 
			}
			
			if (doFind)
			{
				// get the object by calling the constructed version of
				//  FindByKey(GameDatabase gameDatabase, SortedDictionary<string, object> keys)
				object childObject = findByKeyMi.Invoke(constructedChildRepo, new object[] {gameDatabase, childPrimaryKeys} );
				// convert it to the correct type, and set the target property
				piToPopulate.SetValue(newItem, Convert.ChangeType(childObject, piToPopulate.PropertyType), null);				
			}			
		}
	}

	private object ConstructRepositoryforType (Type typeToGetRepositoryFor)
	{				
		// Since we don't know the child object type until run-time, we can't just create an instance of the repository, like this:
		// (don't uncomment, won't compile)
		// DTORepository<childObjectType> childObjectRepo = DTORepository<childObjectType>.GetInstance();
		// Instead, we have to use reflection to have the clr create an instance for us
		// see http://msdn.microsoft.com/en-us/library/b8ytshk6.aspx for more info

		Type genericRepoType = typeof(DTORepository<>);
		Type[] typeArgs = {typeToGetRepositoryFor};
		Type constructedRepoType = genericRepoType.MakeGenericType(typeArgs);
		MethodInfo getInstanceMi = constructedRepoType.GetMethod("GetInstance", BindingFlags.Static | BindingFlags.Public);
		return getInstanceMi.Invoke(null, null);
	}


	void BuildPrimaryKeyPropertyList (DataTable table)
	{
		primaryKeyPropertyNames = new List<string>();
		foreach (DataColumn keyCol in table.PrimaryKey)
		{
			string propertyName = PropertyNameFromColumnName(keyCol.ColumnName);
			primaryKeyPropertyNames.Add(propertyName);
		}
	}
	#endregion private methods

} // class DTORepository

