#pragma strict

/*
 * Static class for loading ships from a ship file.
 *
 */
 
static function LoadShip(shipname : String) : GameObject {
	var file = ParseFile(Application.dataPath+"/mods/ships/"+shipname+"/def.txt");
	var line = 0;
	var pos = 0;
	while (line < file.length) {
	
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