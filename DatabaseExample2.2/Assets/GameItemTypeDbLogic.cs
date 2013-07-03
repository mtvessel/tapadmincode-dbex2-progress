using System.Data.Common;
using System;

/// <summary>
/// Db logic for the game object ItemType
/// </summary>
public class GameItemTypeDbLogic : IDTODbLogic<GameItemType>
{
	#region IDTODbLogic[ItemType] implementation
	public GameItemType CreateShallowObjectFromDataRow (System.Data.DataRow row)
	{
		GameItemType newItemType = new GameItemType();
		newItemType.GameItemTypeCd = row["game_item_type_cd"].ToString();
		newItemType.GameItemDescription = row["game_item_description"].ToString();
		newItemType.CanHeal = //bool.Parse(
			(int)row["can_heal"]; //);
		newItemType.BodyPartWornCd = row["body_part_worn_cd"].ToString();
		return newItemType;
	}
	
	public void PopulateChildObjects (GameItemType objectToMap, GameDatabase gameDatabase)
	{
		return;
	}

	public string SelectStatement {
		get {
			return "select game_item_type_cd, game_item_description, can_heal, body_part_worn_cd from game_item_type";
		}
	}

	public string DeleteStatement 
	{
		get
		{
			return "DELETE game_item_type WHERE game_item_type_cd = ?";
		}
	}

	public string InsertStatement 
	{
		get
		{
			return "INSERT INTO game_item_type " +
				   "   (game_item_type_cd, game_item_description, can_heal, body_part_worn_cd) " +
				   "VALUES (?, ?, ?, ?)";
		}
	}

	public string UpdateStatement
	{
		get 
		{
			return "UPDATE game_item_type " +
				   "   SET 	game_item_description = ?, " +
				   "		can_heal = ?, " +
				   "		body_part_worn_cd = ?" +
				   "  WHERE game_item_type_cd = ?";
		}
	}
	
	public void PopulateUpdateCommandParameters(DbCommand cmd, GameItemType git)
	{
		if (cmd == null) { return ; }
		
		DbParameter param = cmd.CreateParameter();
		
		param.Direction = System.Data.ParameterDirection.Input;
		param.DbType = System.Data.DbType.String;
		param.ParameterName = "game_item_description";
		cmd.Parameters.Add(param);
				
		param = cmd.CreateParameter();
		param.Direction = System.Data.ParameterDirection.Input;
		param.DbType = System.Data.DbType.Boolean;
		param.ParameterName = "can_heal";
		cmd.Parameters.Add(param);
		
		param = cmd.CreateParameter();
		param.Direction = System.Data.ParameterDirection.Input;
		param.DbType = System.Data.DbType.String;
		param.ParameterName = "body_part_worn_cd";
		cmd.Parameters.Add(param);

		param = cmd.CreateParameter();
		param.Direction = System.Data.ParameterDirection.Input;
		param.DbType = System.Data.DbType.String;
		param.ParameterName = "game_item_type_cd";
		cmd.Parameters.Add(param);
				
		if (git != null)
		{
			cmd.Parameters["game_item_description"].Value = git.GameItemDescription;
			//cmd.Parameters["can_heal"].Value = git.CanHeal == true ? "True" : "False";
			cmd.Parameters["body_part_worn_cd"].Value = git.BodyPartWornCd;
			cmd.Parameters["game_item_type_cd"].Value = git.GameItemTypeCd;
		}
	}

	public void PopulateInsertCommandParameters (DbCommand cmd, GameItemType item)
	{
		if (cmd == null) { return ; }
		
		DbParameter param = cmd.CreateParameter();		
		param.Direction = System.Data.ParameterDirection.Input;
		param.DbType = System.Data.DbType.String;
		param.ParameterName = "game_item_type_cd";
		cmd.Parameters.Add(param);
				
		param = cmd.CreateParameter();
		param.Direction = System.Data.ParameterDirection.Input;
		param.DbType = System.Data.DbType.String;
		param.ParameterName = "game_item_description";
		cmd.Parameters.Add(param);
				
		param = cmd.CreateParameter();
		param.Direction = System.Data.ParameterDirection.Input;
		param.DbType = System.Data.DbType.Boolean;
		param.ParameterName = "can_heal";
		cmd.Parameters.Add(param);
		
		param = cmd.CreateParameter();
		param.Direction = System.Data.ParameterDirection.Input;
		param.DbType = System.Data.DbType.String;
		param.ParameterName = "body_part_worn_cd";
		cmd.Parameters.Add(param);

		if (item != null)
		{
			cmd.Parameters["game_item_description"].Value = item.GameItemDescription;
			//cmd.Parameters["can_heal"].Value = item.CanHeal == true ? "True" : "False";
			cmd.Parameters["body_part_worn_cd"].Value = item.BodyPartWornCd;
			cmd.Parameters["game_item_type_cd"].Value = item.GameItemTypeCd;
		}		
	}

	public void PopulateDeleteCommandParameters (DbCommand cmd, GameItemType item)
	{
		throw new NotImplementedException ();
	}

	public void SaveChildObjects (GameItemType parentObject, GameDatabase gameDatabase)
	{
		return;
	}
	#endregion
}
