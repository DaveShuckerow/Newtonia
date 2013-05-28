/*
 * PartLoader.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Loads parts from files.
 *
 * 16 October 2012
 */
#pragma strict
import System.IO;
import System;
import PartBehavior;
import OBJ;
// Load a Part and return it.


/*
 * Format for a part file:
 * No...go check the doc file...
 */

static function LoadPart(partname : String) : GameObject {
	var file = ParseFile(Application.dataPath+"/../Parts/"+partname+"/def.txt");
	var line = 0;
	var pos = 0;
	var obj = GameObject("new "+partname) as GameObject;
	obj.AddComponent("PartBehavior");
	var prt = obj.GetComponent("PartBehavior") as PartBehavior;
	obj.AddComponent("Rigidbody");
	obj.rigidbody.drag = 0;
	obj.rigidbody.useGravity = false;
	obj.rigidbody.angularDrag = 0;
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
		
		/* Read A Module*/
		
		if (fl[0] == "MODULE:") {
			// Read the module data.
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "mass") {
					prt.mass = Double.Parse(fl[1] as String);
					obj.rigidbody.mass = prt.mass;
				} 
				if (fl[0] == "health") {
					prt.health = Double.Parse(fl[1] as String);
					prt.maxHealth = prt.health;
				}
				if (fl[0] == "explosion") {
					prt.explosion = UnityEngine.Resources.Load("PartExplosion",GameObject) as GameObject;
					prt.explodeSize = Double.Parse(fl[1] as String);
					prt.explodeForce = Double.Parse(fl[2] as String);
				}
				if (fl[0] == "hardpoint") {
					pos = 1;
					var loc : Vector3; var fw : Vector3; var up : Vector3;
					var a1 : Array = new Array(prt.hardpointList);
					var a2 : Array = new Array(prt.forwardList);
					var a3 : Array = new Array(prt.upList);
					while (pos < fl.Length) {
						if (fl[pos] == "location") {
							loc = new Vector3(Double.Parse(fl[pos+1] as String),
							                  Double.Parse(fl[pos+2] as String),
							                  Double.Parse(fl[pos+3] as String));
							
						}
						if (fl[pos] == "forward") {
							fw =  new Vector3(Double.Parse(fl[pos+1] as String),
							                  Double.Parse(fl[pos+2] as String),
							                  Double.Parse(fl[pos+3] as String));
						}
						if (fl[pos] == "up") {
							up =  new Vector3(Double.Parse(fl[pos+1] as String),
							                  Double.Parse(fl[pos+2] as String),
							                  Double.Parse(fl[pos+3] as String));
						}
						pos += 1;
					}
					a1.Add(loc);
					a2.Add(fw);
					a3.Add(up);
					prt.hardpointList = a1.ToBuiltin(Vector3) as Vector3[];
					prt.forwardList = a2.ToBuiltin(Vector3) as Vector3[];
					prt.upList = a3.ToBuiltin(Vector3) as Vector3[];
					prt.connections = new PartBehavior[a1.length];
				}
				line += 1;
			}
		}
		
		/* Read Resources*/
		
		if (fl[0] == "RESOURCES:") {
			// Read the resource data.
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				// TODO: FILL ME IN WITH A DYNAMIC RESOURCE ALLOCATION SYSTEM LATER.
				line += 1;
			}
		}
		
		/* Read Model*/
		
		if (fl[0] == "MODEL:") {
			//prt.gameObject.AddComponent(MeshRenderer);
			//var objMesh : MeshFilter = prt.gameObject.AddComponent(MeshFilter);
			//var importer : ObjImporter = new ObjImporter();
			//objMesh.mesh = importer.ImportFile(Application.dataPath+"/../Parts/"+partname+"/model.obj");
			line += 1;
			var modelScale = 1.0;
			var shaderList = Array();
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "scale") {
					modelScale = Double.Parse(fl[1]);
				}
				if (fl[0] == "shader") {
					shaderList.Add(fl[1] as String);
				}
				line += 1;
			}
			obj.AddComponent(OBJ);
			var objLoader : OBJ = obj.GetComponent(OBJ) as OBJ;
			objLoader.MasterScale = modelScale;
			objLoader.Shaders = shaderList.ToBuiltin(String) as String[];
			objLoader.objPath = Application.dataPath+"/../Parts/"+partname+"/model.obj";
		}
		
		/* Read Colliders*/
		
		if (fl[0] == "BOXCOLLIDER:") {
			var center: Vector3;
			var size : Vector3;
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "center") {
					center = new Vector3(Double.Parse(fl[1] as String),
										 Double.Parse(fl[2] as String),
										 Double.Parse(fl[3] as String));
				}
				if (fl[0] == "size") {
					size = new Vector3(Double.Parse(fl[1] as String),
									   Double.Parse(fl[2] as String),
									   Double.Parse(fl[3] as String));
				}
				line += 1;
			}
			obj.AddComponent("BoxCollider");
			(obj.collider as BoxCollider).center = center;
			(obj.collider as BoxCollider).size = size;
		}
		
		if (fl[0] == "SPHERECOLLIDER:") {
			center = new Vector3(0,0,0);
			var radius: float;
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "center") {
					center = new Vector3(Double.Parse(fl[1] as String),
										 Double.Parse(fl[2] as String),
										 Double.Parse(fl[3] as String));
				}
				if (fl[0] == "radius") {
					radius = Double.Parse(fl[1] as String);
				}
				line += 1;
			}
			obj.AddComponent("SphereCollider");
			(obj.collider as SphereCollider).center = center;
			(obj.collider as SphereCollider).radius = radius;
		}
		
		if (fl[0] == "CAPSULECOLLIDER:") {
			center = new Vector3(0,0,0);
			radius = 0.0;
			var height : float;
			var direction : int;
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "center") {
					center = new Vector3(Double.Parse(fl[1] as String),
										 Double.Parse(fl[2] as String),
										 Double.Parse(fl[3] as String));
				}
				if (fl[0] == "radius") {
					radius = Double.Parse(fl[1] as String);
				}
				if (fl[0] == "height") {
					height = Double.Parse(fl[1] as String);
				}
				if (fl[0] == "direction") {
					direction = int.Parse(fl[1] as String);
				}
				line += 1;
			}
			obj.AddComponent("CapsuleCollider");
			(obj.collider as CapsuleCollider).center = center;
			(obj.collider as CapsuleCollider).radius = radius;
			(obj.collider as CapsuleCollider).height = height;
			(obj.collider as CapsuleCollider).direction = direction;
		}
		
		if (fl[0] == "STABILIZER:") {
			var torque : double;
			var damping : double;
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "torque") {
					torque = Double.Parse(fl[1] as String);
				}
				if (fl[0] == "damping") {
					damping = Double.Parse(fl[1] as String);
				}
				line += 1;
			}
			obj.AddComponent("NavPart");
			var nav : NavPart;
			nav = obj.GetComponent("NavPart") as NavPart;
			nav.torque = torque;
			nav.damp = damping;
		}
		
		if (fl[0] == "ENGINE:") {
			var thrust : double;
			var canReverse : int;
			damping = 0;
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "thrust") {
					thrust = Double.Parse(fl[1] as String);
				}
				if (fl[0] == "reverse") {
					canReverse = (int.Parse(fl[1] as String));
				}
				if (fl[0] == "damping") {
					damping = Double.Parse(fl[1] as String);
				}
				line += 1;
			}
			obj.AddComponent("EnginePart");
			var eng : EnginePart;
			eng = obj.GetComponent("EnginePart") as EnginePart;
			eng.thrust = thrust;
			eng.canReverseThrust = canReverse;
			eng.damp = damping;
		}
		
		if (fl[0] == "COMMAND:") {
			obj.AddComponent("CommandPart");
		}
		
		if (fl[0] == "WEAPON:") {
			var bullet : GameObject;
			var muzzle : Vector3;
			var force : double;
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "bullet") {
					//bullet = new GameObject(fl[1] as String);
					bullet = UnityEngine.Resources.Load("Weapons/"+fl[1] as String);
				}
				if (fl[0] == "muzzle") {
					muzzle = new Vector3(Double.Parse(fl[1] as String),
					                     Double.Parse(fl[2] as String),
					                     Double.Parse(fl[3] as String));
				}
				if (fl[0] == "force") {
					force = Double.Parse(fl[1] as String);
				}
				line += 1;
			}
			obj.AddComponent("WeaponPart");
			var wpn : WeaponPart = obj.GetComponent("WeaponPart") as WeaponPart;
			wpn.bullet = bullet;
			wpn.muzzle = muzzle;
			wpn.fireForce = force;
		}
		
		if (fl[0] == "CABIN:") {
			var crew : int;
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				fl = (file[line] as Array).ToBuiltin(String);
				Debug.Log(fl[0]);
				if (fl[0] == "crew") {
					crew = Double.Parse(fl[1] as String);
				}
				line += 1;
			}
			obj.AddComponent("CrewPart");
			var crw : CrewPart = obj.GetComponent("CrewPart") as CrewPart;
			crw.crew = crew;
			crw.maxCrew = crew;
		}
		
		if (fl[0] == "CONSTRUCTION:") {
			var minSize : int;
			var maxSize : int;
			var buildPos : Vector3;
			var exitPos : Vector3;
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				Debug.Log(fl[0]);
				fl = (file[line] as Array).ToBuiltin(String);
				if (fl[0] == "size") {
					minSize = int.Parse(fl[1] as String);
					maxSize = int.Parse(fl[2] as String);
				}
				if (fl[0] == "build") {
					buildPos = new Vector3(Double.Parse(fl[1] as String),
										   Double.Parse(fl[2] as String),
										   Double.Parse(fl[3] as String));
				}
				if (fl[0] == "exit") {
					exitPos = new Vector3(Double.Parse(fl[1] as String),
										  Double.Parse(fl[2] as String),
										  Double.Parse(fl[3] as String));
				}
				line += 1;
			}
			obj.AddComponent("BuildPart");
			var bld : BuildPart = obj.GetComponent("BuildPart") as BuildPart;
			bld.minSize = minSize;
			bld.maxSize = maxSize;
			bld.buildPos = buildPos;
			bld.exitPos = exitPos;
		}
		
		if (fl[0] == "DOCKING:") {
			var bays : int;
			minSize = 0; maxSize = 0;
			var enterPos : Vector3;
			var dockPos : Vector3;
			exitPos = new Vector3(0,0,0);
			line += 1;
			while (line < file.length && (file[line] as Array).length > 1) {
				Debug.Log(file[line]);
				while ((file[line] as Array).length > 1 && (file[line] as Array)[0] == "")
					(file[line] as Array).RemoveAt(0);
				Debug.Log(fl[0]);
				fl = (file[line] as Array).ToBuiltin(String);
				if (fl[0] == "size") {
					minSize = int.Parse(fl[1] as String);
					maxSize = int.Parse(fl[2] as String);
				}
				if (fl[0] == "exit") {
					enterPos = new Vector3(Double.Parse(fl[1] as String),
										   Double.Parse(fl[2] as String),
										   Double.Parse(fl[3] as String));
				}
				if (fl[0] == "dock") {
					dockPos = new Vector3(Double.Parse(fl[1] as String),
										  Double.Parse(fl[2] as String),
										  Double.Parse(fl[3] as String));
				}
				if (fl[0] == "exit") {
					exitPos = new Vector3(Double.Parse(fl[1] as String),
										  Double.Parse(fl[2] as String),
										  Double.Parse(fl[3] as String));
				}
				line += 1;
			}
			obj.AddComponent("DockPart");
			var dck : DockPart = obj.GetComponent("DockPart") as DockPart;
			dck.minSize = minSize;
			dck.maxSize = maxSize;
			dck.enterPos = enterPos;
			dck.dockPos = dockPos;
			dck.exitPos = exitPos;
		}
		line += 1;
	}
	return obj;
}
static function PreloadParts() {
    // Iterate over all folders from System.IO.Directory.GetDirectories(path)
    var pList : String[] = Directory.GetDirectories(Application.dataPath+"/../Parts");
    Debug.Log(new Array(pList));
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

/*
 * CommandPart file format:
 *
 * (none yet)
 */
static function LoadCommand(partname : String, obj : GameObject) {
	// Read through the command module file and append a command module to the object.
	obj.AddComponent("CommandPart");
}

/*
 * EnginePart file format:
 *
 * thrust
 * canReverseThrust
 * thrustDampening
 */
static function LoadEngine(partname : String, obj : GameObject) {
	// Basic setup.
	var fullname = Application.dataPath + "/parts/" + partname + "/engine.prt";
	var r = new StreamReader(fullname);
	var file = r.ReadToEnd();
	r.Close();
	var lines = file.Split('\n'[0]);
	obj.AddComponent("EnginePart");
	var part : EnginePart = obj.GetComponent("EnginePart") as EnginePart;
	
	// Reading stuff.
	part.thrust = System.Double.Parse(lines[0]);
	part.canReverseThrust = System.Int16.Parse(lines[1]);
	part.damp = System.Double.Parse(lines[2]);
	
	// And we're done!
}

/*
 * NavPart file format:
 *
 * torque
 * torqueDampening
 */
static function LoadNav(partname : String, obj : GameObject) {
	// Basic setup.
	var fullname = Application.dataPath + "/parts/" + partname + "/nav.prt";
	var r = new StreamReader(fullname);
	var file = r.ReadToEnd();
	r.Close();
	var lines = file.Split('\n'[0]);
	obj.AddComponent("NavPart");
	var part : NavPart = obj.GetComponent("NavPart") as NavPart;
	
	// Reading stuff.
	part.torque = System.Double.Parse(lines[0]);
	part.damp = System.Double.Parse(lines[1]);
	
	// And we're done!
}

/*
 * CrewPart file format:
 *
 * (none yet)
 */
static function LoadCrew(partname : String, obj : GameObject) {
	obj.AddComponent("CrewPart");
}

/*
 * BuildPart file format:
 *
 * (none yet)
 */
static function LoadBuild(partname : String, obj : GameObject) {
	obj.AddComponent("BuildPart");
}