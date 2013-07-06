using System;
using System.Collections.Generic;

public class PrimaryKeyComparer : EqualityComparer<SortedDictionary<string, object>>
{
	public PrimaryKeyComparer ()
	{
	}

	public override bool Equals (SortedDictionary<string, object> x, SortedDictionary<string, object> y)
	{
		//UnityEngine.Debug.Log("comparing two objects now");
		if (x == null && y == null) { return true; }
		
		if (
			   (x == null && y != null)
			|| (y == null && x != null)
		   )
		{
			return false;
		}
		
		if (x.Count != y.Count) { return false;}
		
		bool isEqual = true;
		foreach (KeyValuePair<string, object> xitem in x) 
		{
			if ( !y.ContainsKey(xitem.Key) )
			{
				isEqual = false;
				break;
			}
			
			if ( x[xitem.Key].ToString() != y[xitem.Key].ToString() )
			{
				isEqual = false;
				break;
			}
		}
		
		return isEqual;				
	}
	
	// see http://msdn.microsoft.com/en-us/library/system.object.gethashcode.aspx for more info on overriding GetHashCode
	public override int GetHashCode(SortedDictionary<string, object> obj)
	{
		int hash = 0;
		foreach (KeyValuePair<string, object> item in obj) 
		{
			hash = hash ^ item.Key.GetHashCode() ^ item.Value.ToString().GetHashCode();
		}
		//UnityEngine.Debug.Log("Calculated a hash code of " + hash.ToString ());
		return hash;
	}

}
