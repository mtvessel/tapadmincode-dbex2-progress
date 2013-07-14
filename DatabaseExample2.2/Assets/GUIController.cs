using System.Data.Common;
using System.Data;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // required for Dictionary<>, List<>, etc.

public class GUIController : MonoBehaviour {

	private GameDatabase gameDatabase = null;
	private DTORepository<GameItem> itemRepository = null;
	private int padding = 15;
	
	
	// Use this for initialization
	void Start () {		
				
		// Usually logic to set up a db connection and any associated objects is done in an application
		// startup routine, because you want it done once, and not during gameplay.
		// For this example, GUIController is the only class which the user interacts with, so
		// db initialization and setup is done here, and in the Start() method, so it's done prior to user interaction.
		
		// initialize database
		try
		{
			gameDatabase = new GameDatabase();
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
		itemRepository = DTORepository<GameItem>.GetInstance();
	}

	// Update is called once per frame
	void Update () {
	
	}
	
	void OnGUI () {
		int padding = 15;
		float runningHeightTotal = 0;
				
		GUILayout.BeginArea(new Rect(0, 0, Screen.width - padding, Screen.height /4), new GUIStyle());

		string buttonText = "Click to print connection metatdata";
		runningHeightTotal += GUI.skin.button.CalcHeight(new GUIContent(buttonText), Screen.width - padding);
		if (GUILayout.Button (buttonText)) {							
			gameDatabase.PrintAllMetaData();
		}
		
		buttonText = "Click to load inventory from sqlite";
		runningHeightTotal += GUI.skin.button.CalcHeight(new GUIContent(buttonText), Screen.width - padding); // + padding;
		if (GUILayout.Button(buttonText)) {							
			itemRepository.FindAll(gameDatabase);
		}
		
		buttonText = "Click to load inventory from SQL Server";
		runningHeightTotal += GUI.skin.button.CalcHeight(new GUIContent(buttonText), Screen.width - padding); // + padding;
		if (GUILayout.Button (buttonText)) {							
			gameDatabase.SetConnection("Server=(local);Initial Catalog=atas;User=atas_user;Pwd=atas_pwd!", SupportedDbType.SQL_SERVER);
			itemRepository.FindAll(gameDatabase);
		}		
		
		GUILayout.Height(runningHeightTotal);
		GUILayout.EndArea();

		if (itemRepository.AllItems.Count == 0) { return; }

		// we've got some inventory, so display it
		runningHeightTotal += padding;
		float bottom = DisplayInventory(runningHeightTotal);
				
		GUILayout.Space(padding);
		GUILayout.BeginArea(new Rect(0, bottom + padding, Screen.width - padding, Screen.height / 4 ));
		if (GUILayout.Button ("Click to update item type 'CORE'")) {			
						
			if (itemRepository == null || itemRepository.AllItems == null || itemRepository.AllItems.Count == 0)
			{
				Debug.Log("No items to update.");				
			}
			else
			{
				DTORepository<GameItemType> itemTypeRepo = DTORepository<GameItemType>.GetInstance(); //itemTypeLogic);
				GameItemType myItemType =
					itemTypeRepo.FindByKey(gameDatabase,
										   new SortedDictionary<string, object>() { {"GameItemTypeCd","CORE"} });				
				
				if (myItemType != null)
				{
					string oldval = myItemType.BodyPartWornCd;
					string newval = ChangeValueToSomethingDifferent(oldval);
					myItemType.BodyPartWornCd = newval;
					Debug.Log(string.Format("Updating item type {0} body part worn from {1} to {2}", myItemType.GameItemTypeCd, oldval, newval));
					
					itemRepository.SaveAll(gameDatabase);
					
					DisplayInventory(runningHeightTotal);
				}
				else
				{
					Debug.Log("No item with item type CORE found.");
				}
			}			
		}
		
		//GUILayout.Space(padding);
		if (GUILayout.Button ("Click to add a whole new item type"))
		{
			if (itemRepository == null || itemRepository.AllItems == null || itemRepository.AllItems.Count == 0)
			{
				Debug.Log("No items to update.");				
			}
			else
			{
				DTORepository<GameItemType> itemTypeRepo = DTORepository<GameItemType>.GetInstance(); //itemTypeLogic);
				GameItemType myItemType =
					itemTypeRepo.FindByKey(gameDatabase,
										   new SortedDictionary<string, object>() { {"GameItemTypeCd","CORE"} });				
				
				if (myItemType != null)
				{
					GameItemType myNewItemType = null;
					string newval = myItemType.GameItemTypeCd;
					do
					{
						newval = ChangeValueToSomethingDifferent(newval);
						myNewItemType =
							itemTypeRepo.FindByKey(gameDatabase,
										   new SortedDictionary<string, object>() { {"GameItemTypeCd", newval} });
					}
					while (myNewItemType != null);
					
					GameItemType newItemType = itemTypeRepo.CreateNewObject();
					newItemType.GameItemTypeCd = newval;
					newItemType.GameItemDescription = "a dynamically constructed item type";
					newItemType.CanHeal = 1;
					newItemType.BodyPartWornCd = "toes";
					
					GameItem gameItemToChange = itemRepository.AllItems[0];
					gameItemToChange.GameItemType = newItemType;
					Debug.Log(string.Format("Updating item {0} with new item type {1}", gameItemToChange.GameItemTypeCd, newval));
					
					itemRepository.SaveAll(gameDatabase);
					
					DisplayInventory(runningHeightTotal);
				}
				else
				{
					Debug.Log("No item with item type CORE found.");
				}
			}			
		}
		
		if (GUILayout.Button ("Click to delete an unused type")) {
			if (itemRepository == null || itemRepository.AllItems == null || itemRepository.AllItems.Count == 0)
			{
				Debug.Log("Can't delete unless items were loaded.");				
			}
			
			GameItemType itemTypeToDelete = null;
			DTORepository<GameItemType> itemTypeRepo = DTORepository<GameItemType>.GetInstance();
			itemTypeRepo.FindAll(gameDatabase);
			foreach (GameItemType itemType in itemTypeRepo.AllItems)
			{
				bool inUse = false;
				foreach (GameItem item in itemRepository.AllItems) 
				{
					// is item type in use?
					bool itemsAreSame = item.GameItemType == itemType;
					bool itemsAreEqual = item.GameItemType.Equals(itemType);
					bool referenceEquals = System.Object.ReferenceEquals(item.GameItemType, itemType);
					if (item.GameItemType.Equals(itemType))
					{
						inUse = true;
						break;
					}
				}
				
				if (inUse) { continue; }

				// not used
				itemTypeToDelete = itemType;
				break;
			}
			
			if (itemTypeToDelete != null)
			{
				itemTypeRepo.DeleteObject(itemTypeToDelete);
				itemTypeRepo.SaveAll(gameDatabase);
				DisplayInventory(runningHeightTotal);
			}			
		}
		
		GUILayout.EndArea();
	}

	string ChangeValueToSomethingDifferent (string stringToChange)
	{
		// update it to something different, doesn't matter what -- we're just testing the update
		//string oldval = string.IsNullOrEmpty(stringToChange) ? "a" : stringToChange;
		string oldval = null;
		if (string.IsNullOrEmpty(stringToChange))
		{
			oldval = "a";
		}
		else
		{
			oldval = stringToChange;
		}
		
		char c1 = oldval[0];
		char c2;
		if (c1 >= 'z')
		{
			c2 = 'a';
		}
		else
		{
			c2 = (char)((int)c1 + 1);
		}		 
		return oldval.Replace(c1, c2);
		
	}

    float DisplayInventory (float top)
	{
		GUILayout.BeginArea(new Rect(top, Screen.height/2, Screen.width - padding , Screen.height / 2));
		float inventoryRunningHeight = 0;
		string labelText = "We have the following items:";
		GUILayout.Label(labelText);	
		inventoryRunningHeight += GUI.skin.button.CalcHeight(new GUIContent(labelText), Screen.width - padding); // + padding;
		
		// iterate through the item collection, creating a label to display each one
		foreach (GameItem item in itemRepository.AllItems) {
		
			string inventoryDescr = 
				string.Format("Game Item ID:{0} is a(n): {1} ({2}), with properties: Can Heal = {3}, Body Part Worn = {4}",
							  item.GameItemId,
							  item.GameItemType.GameItemTypeCd,
							  item.GameItemType.GameItemDescription,
							  item.GameItemType.CanHeal,
							  item.GameItemType.BodyPartWornCd == null ?
									 "<none>" : item.GameItemType.BodyPartWornCd);
		
			GUILayout.Label(inventoryDescr);			

			inventoryRunningHeight += GUI.skin.button.CalcHeight(new GUIContent(inventoryDescr), Screen.width - padding);
		}
		
		labelText = "We have the following item types:";
		GUILayout.Label(labelText);	
		inventoryRunningHeight += GUI.skin.button.CalcHeight(new GUIContent(labelText), Screen.width - padding); // + padding;
		
		// iterate through the item type collection, creating a label to display each one
		DTORepository<GameItemType> gameItemTypeRepo = DTORepository<GameItemType>.GetInstance();
		gameItemTypeRepo.FindAll(gameDatabase);
		foreach (GameItemType itemType in gameItemTypeRepo.AllItems) {
		
			string inventoryDescr = 
				string.Format("Game Item Type {0} ({1}) has properties Can Heal = {2}, Body Part Worn = {3}",
							  itemType.GameItemTypeCd,
							  itemType.GameItemDescription,
							  itemType.CanHeal,
							  itemType.BodyPartWornCd == null ?
									 "<none>" : itemType.BodyPartWornCd);
		
			GUILayout.Label(inventoryDescr);			

			inventoryRunningHeight += GUI.skin.button.CalcHeight(new GUIContent(inventoryDescr), Screen.width - padding);
		}
		GUILayout.EndArea();
		
		return inventoryRunningHeight;
	}
	
	void myWindowFunction(int id)
	{
		GUILayout.Label("window function");
	}
}
