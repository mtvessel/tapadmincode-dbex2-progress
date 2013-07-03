using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System;

/// <summary>
/// Interface which specifies contract that all DTO Db logic instances must fulfill in order.  These methods are used
/// by the DTORepositories to access data in the db and translate between db data and game objects.
/// </summary>
public interface IDTODbLogic<T>
{
	/// <summary>
	/// Gets the SQL select statement needed to read all objects from a db table.
	/// </summary>
	/// <value>
	/// The select statement.
	/// </value>
	string SelectStatement { get; }
	
	/// <summary>
	/// Gets the SQL statement needed to update values in the database from a game object.
	/// </summary>
	/// <value>
	/// The update statement.
	/// </value>
	//string UpdateStatement { get; }
	
	/// <summary>
	/// Gets the SQL statement needed to delete objects from a db table.
	/// </summary>
	/// <value>
	/// The delete statement.
	/// </value>
	//string DeleteStatement { get; }
	
	/// <summary>
	/// Gets the SQL statement needed to insert objects into a db table.
	/// </summary>
	/// <value>
	/// The insert statement.
	/// </value>
	//string InsertStatement { get; }
	
	/// <summary>
	/// Creates a game object, and populates shallow fields only from a given data table row.
	/// </summary>
	/// <returns>
	/// The populated game object.
	/// </returns>
	/// <param name='row'>
	/// A DataRow containing the raw column data from the underlying db table.
	/// </param>
	T CreateShallowObjectFromDataRow(DataRow row);
	
	/// <summary>
	/// Populates the child game object properties for a given object.  The supplied object must already have
	/// its shallow properties populated.  This method should inspect all child property key values,
	/// and use them to populate all fields that hold child objects.
	/// </summary>
	/// <param name='objectToMap'>
	/// The game Object to populate.
	/// </param>
	/// <param name='gameDatabase'>
	/// The data source.
	/// </param>
	void PopulateChildObjects(T objectToMap, GameDatabase gameDatabase);
	
	void SaveChildObjects(T parentObject, GameDatabase gameDatabase);
	
	/// <summary>
	/// Populates the command parameters needed to issue an update query.
	/// </summary>
	/// <param name='cmd'>
	/// The DbCommand object to populate.
	/// </param>
	/// <param name='item'>
	/// The Game Item whose values are used to populate the query.
	/// </param>
	//void PopulateUpdateCommandParameters(DbCommand cmd, T item);
	
	/// <summary>
	/// Populates the command parameters needed to issue an insert query.
	/// </summary>
	/// <param name='cmd'>
	/// The DbCommand object to populate.
	/// </param>
	/// <param name='item'>
	/// The Game Item whose values are used to populate the query.
	/// </param>
	//void PopulateInsertCommandParameters(DbCommand cmd, T item);

	/// <summary>
	/// Populates the command parameters needed to issue an delete query.
	/// </summary>
	/// <param name='cmd'>
	/// The DbCommand object to populate.
	/// </param>
	/// <param name='item'>
	/// Item.
	/// The Game Item whose values are used to populate the query.
	//void PopulateDeleteCommandParameters(DbCommand cmd, T item);	
}
