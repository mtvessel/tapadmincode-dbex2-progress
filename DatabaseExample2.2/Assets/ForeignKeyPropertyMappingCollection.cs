using System;
using System.Collections.Generic;

public class ForeignKeyPropertyMappingCollection : Dictionary<string, string>
{
	string targetTypeName;

	public string TargetTypeName {
		get { return this.targetTypeName; }
		set { targetTypeName = value; }
	}		
	
	public ForeignKeyPropertyMappingCollection (string targetTypeName)
	{
		this.targetTypeName = targetTypeName;
	}
	
	private ForeignKeyPropertyMappingCollection() {}
	
	public void Add (ForeignKeyPropertyMapping mapping)
	{
		this.Add(mapping.SourceObjectPropertyName, mapping.TargetObjectKeyPropertyName);
	}
	
	public void Remove(ForeignKeyPropertyMapping mapping)
	{
		this.Remove(mapping.SourceObjectPropertyName);
	}
	
	public List<ForeignKeyPropertyMapping> Mappings
	{
		get
		{ 
			List<ForeignKeyPropertyMapping> mappingList = new List<ForeignKeyPropertyMapping>();
			foreach (KeyValuePair<string, string> kvp in this)
			{
				mappingList.Add(new ForeignKeyPropertyMapping(kvp.Key, kvp.Value));
			}
			return mappingList;
		}
	}
}
