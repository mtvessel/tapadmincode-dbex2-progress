using UnityEngine;
using System.Collections;

public class GameItem : GameObjectBase {
	
	private string gameItemId;
	public string GameItemId 
	{
		get { return gameItemId; }
		set { gameItemId = value; }
	}
	
	private string gameItemTypeCd;
	public string GameItemTypeCd
	{
		get { return gameItemTypeCd; }
		set { gameItemTypeCd = value; }		
	}
	
	private GameItemType gameItemType;
	public GameItemType GameItemType
	{
		get { return gameItemType; }
		set { gameItemType = value; }
	}
	
}
