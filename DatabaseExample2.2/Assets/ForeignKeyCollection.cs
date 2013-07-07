using System;
using System.Collections.Generic;
using System.Collections.Specialized;

public class ForeignKeyCollection : Dictionary<string, ForeignKeyPropertyMappingCollection>
{
	private ForeignKeyCollection ()	{ }
		
	public ForeignKeyCollection (string propertyToPopulate, ForeignKeyPropertyMappingCollection mappings)
	{
		this.Add(propertyToPopulate, mappings);
	}
	
	public ForeignKeyCollection (string propertyToPopulate, string targetTypeName, ForeignKeyPropertyMapping mapping)
	{
		this.Add(propertyToPopulate, targetTypeName, mapping);
	}
	
	public void Add (string propertyToPopulate, string targetTypeName, ForeignKeyPropertyMapping mapping)
	{
		ForeignKeyPropertyMappingCollection mappingColl = null;
		
		if (this.ContainsKey(propertyToPopulate))
		{
			mappingColl = this[propertyToPopulate];
			mappingColl.TargetTypeName = targetTypeName;
			mappingColl.Add(mapping);
		}
		else
		{
			mappingColl = new ForeignKeyPropertyMappingCollection(targetTypeName);
			mappingColl.Add(mapping);
			this.Add(propertyToPopulate, mappingColl);
		}
	}
	
	public void RemovePropertyMappings(string propertyName)
	{
		this.Remove(propertyName);
	}	
	
	public void RemovePropertyMapping(string propertyName, ForeignKeyPropertyMapping mapping)
	{
		this[propertyName].Remove(mapping);
	}
	
	public ForeignKeyPropertyMappingCollection GetForeignKeyMappingsForProperty (string propertyName)
	{
		return this[propertyName];
	}
	
	public List<string> PropertiesWithForeignKeysMappings
	{
		get { return new List<string>(this.Keys); }
	}
}
