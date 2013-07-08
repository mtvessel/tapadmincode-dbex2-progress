using System.Collections.Generic;
using System;
using System.Data;

public class GameObjectBase
{	
	#region static data and ctor	
	
	/// <summary>
	/// The valid state transitions a game object can undergo.  Format is Key = FromState, Value = List of valid ToStates.
	/// </summary>
	private static Dictionary<DataRowState, List<DataRowState>> validStateTransitions = 
		new Dictionary<DataRowState, List<DataRowState>>();
	
	static GameObjectBase()
	{
		List<DataRowState> validStates = new List<DataRowState>();
		validStates.Add(DataRowState.Modified);
		validStates.Add(DataRowState.Unchanged);
		validStateTransitions.Add(DataRowState.Deleted, validStates);
		
		validStates = new List<DataRowState>();
		validStates.Add(DataRowState.Modified);
		validStates.Add(DataRowState.Deleted);
		validStateTransitions.Add(DataRowState.Unchanged, validStates);
		
		validStates = new List<DataRowState>();
		validStates.Add(DataRowState.Unchanged);
		validStates.Add(DataRowState.Deleted);
		validStateTransitions.Add(DataRowState.Modified, validStates);
		
		// Technically, Detached should only be allowed to go to Added,
		// but since we're using it as a default, we'll allow it to go to anything.
		// This shouldn't hurt anything (famous last words).
		validStates = new List<DataRowState>();
		validStates.Add(DataRowState.Unchanged);
		validStates.Add(DataRowState.Deleted);
		validStates.Add(DataRowState.Added);
		validStates.Add(DataRowState.Modified);
		validStateTransitions.Add(DataRowState.Detached, validStates);
		
		
	}
	
	#endregion
		
	public GameObjectBase ()
	{			
	}
	
	DataRowState objectState = DataRowState.Detached;
	public DataRowState ObjectState
	{
		get { return objectState; }
	}
	
	/// <summary>
	/// Sets the object's state to the new state, if the state change is valid.
	/// </summary>
	/// <returns>
	/// The effective state of the object.
	/// </returns>
	/// <param name='newState'>
	/// The object state to attempt to change to.
	/// </param>
	public DataRowState SetObjectChanged(DataRowState newState)
	{		
		List<DataRowState> validTargetStates = null;
		if (validStateTransitions.ContainsKey(objectState)) {
			validTargetStates = validStateTransitions[objectState];
		}
		
		if (validTargetStates != null) {
			if (validTargetStates.Contains(newState)) {
				objectState = newState;
			}
		}
		
		return objectState;
	}
	
	DataRow tableRow = null;			
	public DataRow TableRow
	{
		get { return tableRow; }
		set { tableRow = value; }
	}
}
