using System;
using UnityEngine;

/// <summary>
/// DTO class for the game object ItemType.
/// </summary>
public class GameItemType : GameObjectBase
{
	private string gameItemTypeCd;
	public string GameItemTypeCd {
		get {
			return this.gameItemTypeCd;
		}
		set {
			gameItemTypeCd = value;
			SetObjectChanged(System.Data.DataRowState.Modified);
		}
	}

	private string gameItemDescription;
	public string GameItemDescription {
		get {
			return this.gameItemDescription;
		}
		set {
			gameItemDescription = value;
			SetObjectChanged(System.Data.DataRowState.Modified);
		}
	}
	
	private int canHeal;
	public int CanHeal {
		get {
			return this.canHeal;
		}
		set {
			canHeal = value;
			SetObjectChanged(System.Data.DataRowState.Modified);
		}
	}
	
	private string bodyPartWornCd;	
	public string BodyPartWornCd {
		get {
			return this.bodyPartWornCd;
		}
		set {
			bodyPartWornCd = value;
			SetObjectChanged(System.Data.DataRowState.Modified);
		}
	}
}
