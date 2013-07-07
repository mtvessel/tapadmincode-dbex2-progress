using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class ForeignKeyPropertyMapping
{
	KeyValuePair<string, string> data;
	
	public ForeignKeyPropertyMapping ()
	{
	}
	
	public ForeignKeyPropertyMapping(string sourceObjectPropertyName, string targetObjectKeyPropertyName)
	{
		data = new KeyValuePair<string, string>(sourceObjectPropertyName, targetObjectKeyPropertyName);
	}	
	
	public string SourceObjectPropertyName 
	{
		get { return this.data.Key; } 
	}
	
	public string TargetObjectKeyPropertyName
	{
		get	{ return this.data.Value; }
	}
	
	public override string ToString ()
	{
		return string.Format ("[PropertyMapping]" + SourceObjectPropertyName + "->" + TargetObjectKeyPropertyName);
	}
}
