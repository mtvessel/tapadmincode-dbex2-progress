//using System.Text;
//using System.Data;
//using System.Data.SqlClient;
//using System.Data.Common;
//using Mono.Data.SqliteClient;
//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//
//public class ItemRepository : ScriptableObject {
//	
//	#region public methods
//	
//	/// <summary>
//	/// Finds all game items.
//	/// </summary>
//	/// <returns>
//	/// A List of game items.
//	/// </returns>
//	/// <param name='gameDatabase'>
//	/// The game database to retrieve items from.
//	/// </param>
//	/// <exception cref='System.ArgumentException'>
//	/// Thrown if the game database is not a valid database type.
//	/// </exception>
//	public List<GameItem> FindAll(GameDatabase gameDatabase) {
//		Debug.Log("FindAll starting...");
//		
//		// default to an empty collection, in case we find no data
//		List<GameItem> gameItems = new List<GameItem>(); 
//		
//		DbConnection dbConnection = gameDatabase.CurrentDbConnection;
//			
//		
//		try {
//			if (dbConnection.State != ConnectionState.Open)
//			{
//				dbConnection.Open();
//			}
//		} catch (System.Exception ex) {
//			Debug.LogException(ex);
//			return gameItems;
//		}		
//		
//		// Create a command object which we will use to issue a db query
//		DbCommand cmd = dbConnection.CreateCommand();
//		cmd.CommandType = CommandType.Text;
//		cmd.CommandText = "select ﻿item_id, item_name, item_type_cd from item";
//
//		// populate a DataTable with the results of the query using a DataAdapter		
//		DbDataAdapter inventoryAdapter = gameDatabase.GetDbDataAdaptor();
//		
//		if (inventoryAdapter == null)
//		{
//			throw new System.ArgumentException("Unknown database connection type.", "dbConnection");
//		}
//		
//		DataTable inventoryTable = new DataTable();
//		try
//		{
//			inventoryAdapter.SelectCommand = cmd;
//			inventoryAdapter.Fill(inventoryTable);						
//		}
//		catch (System.Exception ex)
//		{
//			Debug.LogException(ex);
//			return gameItems;
//		}	
//		
//		gameItems = MapDataTable(inventoryTable);
//				
//		// Return the populated collection of game items.
//		Debug.Log("LoadInventory returning a collection of " + gameItems.Count + " item(s).");
//		return gameItems;
//	}
//	
//	#endregion
//	
//	#region private methods
//
//	static List<GameItem> MapDataTable (DataTable inventoryTable)
//	{
//		List<GameItem> gameItems = new List<GameItem>();
//		
//		// Iterate through the returned datatable, and for each row returned, create a new GameItem,
//		// and populate it with the returned row data.
//		foreach (DataRow row in inventoryTable.Rows)
//		{
//			GameItem newItem = MapDataRow(row);
//			MapChildProperties(newItem);
//			gameItems.Add(newItem);
//		}
//		
//		return gameItems;
//	}
//	
//	static GameItem MapDataRow(DataRow row)
//	{
//			GameItem newItem = ScriptableObject.CreateInstance<GameItem>();
//			newItem.ItemId = row["item_id"].ToString();
//			newItem.ItemName = row["item_name"].ToString();
//			newItem.ItemTypeCd = row["item_type_cd"].ToString();
//			return newItem;
//	}
//
//	static void MapChildProperties(GameItem newItem)
//	{
//		//throw new System.NotImplementedException ();
//	}
//
//	#endregion	
//}
