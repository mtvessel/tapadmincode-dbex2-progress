using System.Data.Common;
using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Db logic for the game object Item
/// </summary>
public class GameItemDbLogic : IDTODbLogic<GameItem>
{
	#region IDTODbLogic[GameItem] implementation
	public GameItem CreateShallowObjectFromDataRow (System.Data.DataRow row)
	{
		GameItem newItem = new GameItem();
		newItem.GameItemId = row["game_item_id"].ToString();
		newItem.GameItemTypeCd = row["game_item_type_cd"].ToString();
		return newItem;
	}

	public string SelectStatement {
		get 
		{
			return "SELECT game_item_id, game_item_type_cd from game_item";
		}
	}

	public void PopulateChildObjects (GameItem objectToMap, GameDatabase gameDatabase)
	{
		if (objectToMap == null) { return; }
		
		DTORepository<GameItemType> itemTypeRepository = DTORepository<GameItemType>.GetInstance(); //new GameItemTypeDbLogic());
		List<GameItemType> itemTypeList = itemTypeRepository.FindAll(gameDatabase);
		foreach(GameItemType it in itemTypeList)
		{
			if (it.GameItemTypeCd == objectToMap.GameItemTypeCd) 
			{
				objectToMap.GameItemType = it;
				break;
			}
		}
	}

//	public string UpdateStatement {
//		get 
//		{
//			return "UPDATE game_item" +
//				   "   SET game_item_type_cd = ?" +
//				   " WHERE game_item_id = ?";
//		}
//	}
	
//	public string DeleteStatement 
//	{
//		get { return "DELETE game_item WHERE game_item_id = ?"; }
//	}

//	public string InsertStatement 
//	{
//		get 
//		{
//			return "INSERT INTO game_item (game_item_id, game_item_type_cd) VALUES (?, ?)";
//		}
//	}

//	public void PopulateUpdateCommandParameters (DbCommand cmd, GameItem item)
//	{
//		if (cmd == null) { return; }
//		
//		DbParameter param = cmd.CreateParameter();
//		param.Direction = System.Data.ParameterDirection.Input;
//		param.DbType = System.Data.DbType.String;
//		param.ParameterName = "game_item_type_cd";
//		cmd.Parameters.Add(param);
//
//		param = cmd.CreateParameter();
//		param.Direction = System.Data.ParameterDirection.Input;
//		param.DbType = System.Data.DbType.Int32;
//		param.ParameterName = "game_item_id";
//		cmd.Parameters.Add(param);
//		
//		if (item != null) 
//		{
//			cmd.Parameters["game_item_type_cd"].Value = item.GameItemTypeCd;
//			cmd.Parameters["game_item_id"].Value = item.GameItemId;
//		}
//	}

//	public void PopulateInsertCommandParameters (DbCommand cmd, GameItem item)
//	{
//		if (cmd == null) { return; }
//		
//		DbParameter param = cmd.CreateParameter();
//		param.Direction = System.Data.ParameterDirection.Input;
//		param.DbType = System.Data.DbType.Int32;
//		param.ParameterName = "game_item_id";
//		cmd.Parameters.Add(param);
//
//		param = cmd.CreateParameter();
//		param.Direction = System.Data.ParameterDirection.Input;
//		param.DbType = System.Data.DbType.String;
//		param.ParameterName = "game_item_type_cd";
//		cmd.Parameters.Add(param);
//		
//		if (item != null) 
//		{
//			cmd.Parameters["game_item_id"].Value = item.GameItemId;
//			cmd.Parameters["game_item_type_cd"].Value = item.GameItemTypeCd;
//		}
//	}

//	public void PopulateDeleteCommandParameters (DbCommand cmd, GameItem item)
//	{
//		if (cmd == null) { return; }
//		
//		DbParameter param = cmd.CreateParameter();
//		param.Direction = System.Data.ParameterDirection.Input;
//		param.DbType = System.Data.DbType.Int32;
//		param.ParameterName = "game_item_id";
//		cmd.Parameters.Add(param);
//		
//		if (item != null) 
//		{
//			cmd.Parameters["game_item_id"].Value = item.GameItemId;
//		}
//	}

	public void SaveChildObjects (GameItem parentObject, GameDatabase gameDatabase)
	{
		//GameItemTypeDbLogic gitLogic = new GameItemTypeDbLogic();
		DTORepository<GameItemType> gitRepo = DTORepository<GameItemType>.GetInstance(); //gitLogic);
		gitRepo.SaveAll(gameDatabase);
	}
	
	#endregion
}

