#pragma strict
import PartLoader;
import ShipTemplate;
import System.Collections.Generic;
/*
 * Static class for loading ships from a ship file.
 *
 */

private static var templates : Dictionary.<String,ShipTemplate>;

static function GetShip(shipname : String) : GameObject {
	var template : ShipTemplate;
	templates.TryGetValue(shipname,template);
	var obj : GameObject = new GameObject(shipname) as GameObject;
	obj.AddComponent(ShipBehavior);
	var shp : ShipBehavior = obj.GetComponent(ShipBehavior) as ShipBehavior;
	obj.name = template.name;
	shp.myName = template.myName;
	shp.myAuthor = template.myAuthor;
	var parts : Array = new Array();
	for (var i=0; i<template.myParts.Length; i+=1) {
		parts.Add(PartLoader.GetPart(template.myParts[i]));
		(parts[i] as GameObject).transform.parent = shp.transform;
	}
	shp.Initialize();
	for (i=0; i < template.coreParts.Length; i+=1) {
		var prt1 : PartBehavior = (parts[template.coreParts[i]] as GameObject).GetComponent(PartBehavior);
		var prt2 : PartBehavior = (parts[template.attachParts[i]] as GameObject).GetComponent(PartBehavior);
		prt1.ConnectToHardpoint(prt2,template.coreHPs[i],template.attachHPs[i]);
	}
	return obj;
}

static function PreloadShips() {
	templates = new Dictionary.<String,ShipTemplate>();
}

private static function LoadShip(shipname : String) {
	var file = ParseFile(Application.dataPath+"/mods/ships/"+shipname+"/def.txt");
	var line = 0;
	var pos = 0;
    var obj : ShipTemplate = new ShipTemplate();
	while (line < file.length) {
		// Scan the file
		var fl = (file[line] as Array).ToBuiltin(String);
		Debug.Log(fl[0]);
		if (fl[0] == "name") {
			var unTokenized = new Array(fl);
			unTokenized.RemoveAt(0);
			obj.myName = unTokenized.Join(" ");
		}
		if (fl[0] == "author") {
			unTokenized = new Array(fl);
			unTokenized.RemoveAt(0);
			obj.myAuthor = unTokenized.Join(" ");
		}
		if (fl[0] == "shortname") {
			obj.name = fl[1] as String;
		}
		
		/* Read the part list*/
		var partlist : Array = new Array();
		if (fl[0] == "PARTS:") {
			// Read the part list.
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				partlist.Add((file[line] as Array).Join(" "));
				line += 1;
			}
			fl = (file[line] as Array).ToBuiltin(String);
			obj.myParts = partlist.ToBuiltin(String);
		}
		
		/* Read the connection list*/
		var cores : Array = new Array();
		var attach : Array = new Array();
		var cHPs : Array = new Array();
		var aHPs : Array = new Array();
		if (fl[0] == "CONNECTIONS:") {
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "connect") {
					cores.Add(int.Parse(fl[1]));
					attach.Add(int.Parse(fl[2]));
					cHPs.Add(int.Parse(fl[3]));
					aHPs.Add(int.Parse(fl[4]));
				}
				line += 1;
			}
			obj.coreParts = cores.ToBuiltin(int);
			obj.attachParts = attach.ToBuiltin(int);
			obj.coreHPs = cHPs.ToBuiltin(int);
			obj.attachHPs = aHPs.ToBuiltin(int);
		}
	}
}

static function ParseFile(filename : String) : Array {
	var reader = File.OpenText(filename);
	var ret = new Array();
	var ln = reader.ReadLine();
	while (ln != null) {
		ret.Add(new Array(ln.Split()) as Array);
		ln = reader.ReadLine();
	}
	return ret;
}