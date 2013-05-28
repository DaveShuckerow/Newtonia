#pragma strict

/*
 * Static class for loading ships from a ship file.
 *
 */
 
static function LoadShip(shipname : String) : GameObject {
	var file = ParseFile(Application.dataPath+"/mods/ships/"+shipname+"/def.txt");
	var line = 0;
	var pos = 0;
    var obj = new GameObject("New Ship") as GameObject;
    obj.AddComponent(ShipBehavior);
    var prt = obj.GetComponent(ShipBehavior) as ShipBehavior;
	while (line < file.length) {
		// Scan the file
		var fl = (file[line] as Array).ToBuiltin(String);
		Debug.Log(fl[0]);
		if (fl[0] == "name") {
			var unTokenized = new Array(fl);
			unTokenized.RemoveAt(0);
			prt.myName = unTokenized.Join(" ");
		}
		if (fl[0] == "author") {
			unTokenized = new Array(fl);
			unTokenized.RemoveAt(0);
			prt.myAuthor = unTokenized.Join(" ");
		}
		if (fl[0] == "shortname") {
			obj.name = fl[1] as String;
		}
		
		/* Read the part list*/
		
		if (fl[0] == "PARTS:") {
			// Read the part list.
			line += 1;
			var partlist : Array = new Array();
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				partlist.Add((file[line] as Array).Join(" "));
				line += 1;
			}
			
		}
		
		/* Read the connection list*/
		
		if (fl[0] == "CONNECTIONS:") {
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "connect") {
				
				}
				line += 1;
			}
			
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