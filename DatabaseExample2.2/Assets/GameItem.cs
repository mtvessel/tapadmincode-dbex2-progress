using System;
using UnityEngine;
using System.Collections;

public class GameItem : GameObjectBase {
	
	private int gameItemId;
	public int GameItemId 
	{
		get { return gameItemId; }
		set
		{
			gameItemId = value; 
			SetObjectChanged(System.Data.DataRowState.Modified);
		}
	}

	private string gameItemTypeCd;
	public string GameItemTypeCd
	{
		get { return gameItemTypeCd; }
		set
		{ 
			if ( GameItemType == null)
			{
				gameItemTypeCd = value; 
				SetObjectChanged(System.Data.DataRowState.Modified);
			}
			else
			{
				throw new InvalidOperationException("GameItemTypeCode cannot be changed once GameItemType has been set.");
			}
		}		
	}
	
	private GameItemType gameItemType;
	public GameItemType GameItemType
	{
		get { return gameItemType; }
		set 
		{
			gameItemType = value; 
			SetObjectChanged(System.Data.DataRowState.Modified);
		}
	}
	
}
