using System.Data.Common;
using System.Data;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // required for Dictionary<>, List<>, etc.

public class GUIController : MonoBehaviour {

	private GameDatabase gameDatabase = null;
	private DTORepository<GameItem> itemRepository = null;
	
	// Use this for initialization
	void Start () {		
				
		// Usually logic to set up a db connection and any associated objects is done in an application
		// startup routine, because you want it done once, and not during gameplay.
		// For this example, GUIController is the only class which the user interacts with, so
		// db initialization and setup is done here, and in the Start() method, so it's done prior to user interaction.
		
		// initialize database
		try
		{
			gameDatabase = new GameDatabase(); //ScriptableObject.CreateInstance<GameDatabase>();
			gameDatabase.SetDefaultConnection();
			
			if (gameDatabase.CurrentDbConnection == null)
			{
				gameDatabase.SetConnectionFromEmbeddedResource("atas.db");
			}
		}
		catch (UnityException uex)
		{
			Debug.LogException(uex);
		}
		catch (System.Exception ex)
		{
			Debug.LogException(ex);
		}
		
		
		// set up the item repository, which the gui will need to instantiate GameItems using db data
		IDTODbLogic<GameItem> gameItemDbLogic = new GameItemDbLogic();			
		itemRepository = DTORepository<GameItem>.GetInstance(gameItemDbLogic);
	}

	// Update is called once per frame
	void Update () {
	
	}
	
	void OnGUI () {
		int left = 10;
		int top = 10;
		int width = 640;
		int labelHeight = 20;
		int padding = 5;
		int buttonHeight = labelHeight * 2;
				
		if (GUI.Button (new Rect (left, top, width, buttonHeight), "Click to print connection metatdata")) {							
			gameDatabase.PrintAllMetaData();
		}
		
		top += buttonHeight + padding;
		if (GUI.Button (new Rect (left, top, width, buttonHeight), "Click to load inventory from sqlite")) {							
			itemRepository.FindAll(gameDatabase);
		}
		
		top += buttonHeight + padding;
		if (GUI.Button (new Rect (left, top, width, buttonHeight), "Click to load inventory from SQL Server")) {							
			gameDatabase.SetConnection("Server=(local);Initial Catalog=atas;User=atas_user;Pwd=atas_pwd!", SupportedDbType.SQL_SERVER);
			itemRepository.FindAll(gameDatabase);
		}		
		
		if (itemRepository.AllItems.Count == 0) { return; }

		//Debug.Log ("Making labels for " + itemRepository.AllItems.Count + " item(s).");
		
		// we've got some inventory, so display it
		top += buttonHeight + padding;		
		DisplayInventory (left, top, width, labelHeight, padding);		
		
		
		// logic for udpating
		top += ((buttonHeight + padding) * itemRepository.AllItems.Count);
		if (GUI.Button (new Rect (left, top, width, buttonHeight), "Click to update item type 'CORE'")) {			
						
			if (itemRepository == null || itemRepository.AllItems == null || itemRepository.AllItems.Count == 0)
			{
				Debug.Log("No items to update.");				
			}
			else
			{
				GameItemTypeDbLogic itemTypeLogic = new GameItemTypeDbLogic();
				DTORepository<GameItemType> itemTypeRepo = DTORepository<GameItemType>.GetInstance(itemTypeLogic);
				GameItemType myItemType = //itemRepository.AllItems[0].GameItemType;
					itemTypeRepo.FindByKey(gameDatabase,
										   new SortedDictionary<string, object>() { {"GameItemTypeCd","CORE"} });				
				
				if (myItemType != null)
				{
					// update it
					//coreItemType.BodyPartWornCd = "Nose";
					string oldval = string.IsNullOrEmpty(myItemType.BodyPartWornCd) ? "a" : myItemType.BodyPartWornCd;
					char c1 = oldval[0];
					char c2 = (char)((int)c1 + 1);
					string newval = oldval.Replace(c1, c2);
					myItemType.BodyPartWornCd = newval;
					Debug.Log(string.Format("Updating item type {0} body part worm from {1} to {2}", myItemType.GameItemTypeCd, oldval, newval));
					
					itemRepository.SaveAll(gameDatabase);
					//itemRepository.FindAll(gameDatabase);
					
					top += buttonHeight + padding;
					DisplayInventory(left, top, width, labelHeight, padding);
				}
				else
				{
					Debug.Log("No item with item type CORE found.");
				}
			}
		}		
	}

    void DisplayInventory (int left, int top, int width, int labelHeight, int padding)
	{
		GUI.Label(new Rect(left, top, width, labelHeight), "We have the following items:");		

		// iterate through the item collection, creating a label to display each one
		foreach (GameItem item in itemRepository.AllItems) {
		
			string inventoryDescr = 
				string.Format("Game Item ID:{0} is a(n): {1}, with properties: Can Heal = {2}, Body Part Worn = {3}",
							  item.GameItemId, 
							  item.GameItemType.GameItemDescription,
							  item.GameItemType.CanHeal,
							  string.IsNullOrEmpty(item.GameItemType.BodyPartWornCd) ? 
									"<none>" : item.GameItemType.BodyPartWornCd);
		
			top += labelHeight + padding;
			GUI.Label(new Rect(left, top, width, labelHeight), inventoryDescr);			
		}
	}
}
