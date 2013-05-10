/*
 * ShipBehavior.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Manages the behavior of ships.
 *
 * 12 October 2012
*/
#pragma strict
/*
 * There are several types of part to put in our ships
 * Command Modules -- One is required to run a ship
 * Crew Modules ----- Provide extra crew to help run the ship 
 *                   (command modules come with some crew)
 * Engine Modules -- Acceleration
 * Nav Modules ----- Rotation
 * Power Modules --- Provide power for the ship (or fuel)
 * Weapon Modules -- Shoot at stuff
 * Armor Plates ---- Take Damage
 * Special Modules - Special Things (shields, cloak, etc)
 * Struct Modules -- Structural stuff
 * Salvage Modules - Grab debris parts 
 * Cargo Modules --- Hold things
 * Build Modules --- Construct other ships
*/
var allModules : PartBehavior[] = null;
var buildModules : PartBehavior[] = null;
var crewModules : PartBehavior[] = null;
var engineModules : EnginePart[] = null;
var navModules : NavPart[] = null;
var weaponModules : WeaponPart[] = null;
var commandModule : CommandPart;
var initialized;
var maxTorque : double;
var maxThrust : double;
var mass : double;

function Start () {
	initialized = false;
}
function Initialize() {
	if (initialized) return null;
	allModules= GetComponentsInChildren.<PartBehavior>();
	engineModules = GetComponentsInChildren.<EnginePart>();
	navModules = GetComponentsInChildren.<NavPart>();
	weaponModules = GetComponentsInChildren.<WeaponPart>();
	commandModule = GetComponentInChildren.<CommandPart>();
	
//	print(engineModules[0]);
//	print(commandModule);
	
	// Make our mass the summation of the masses of the components.
	// Also release the components to the world!
	mass = 0;
	for (var m : PartBehavior in allModules) {
		//m.transform.parent = null;
		//m.transform.position += transform.position;
		//m.transform.rotation *= transform.rotation;
		m.ship = this;
		mass += m.mass;
		if (m.GetComponent("Rigidbody"))
		GameObject.DestroyImmediate(m.rigidbody);
	}
	
	// Compute our max thrust and max torque.
	maxThrust = 0.0;
	for (var m in engineModules) {
		maxThrust += m.thrust;
	}
	maxTorque = 0.0;
	for (var m in navModules) {
		maxTorque += m.torque;
	}
	if (!GetComponent("Rigidbody"));
	gameObject.AddComponent("Rigidbody");
	rigidbody.mass = mass;
	rigidbody.useGravity = false;
	rigidbody.isKinematic = false;
	rigidbody.angularDrag = 0; rigidbody.drag = 0;
	rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
	initialized = true;
	return null;
}
function Update() {
	if (!initialized) return null;
	
	//transform.position = commandModule.rigidbody.centerOfMass + commandModule.transform.position;
	//transform.rotation = commandModule.transform.rotation;
	return null;
}

function FixedUpdate () {
	if (!initialized) return null;
	// For the time being, use input to give the player control.
	// In the finished game maybe we'll want to give control over movement too.
	//return null;
	Thrust(Input.GetAxis("Throttle"));
	TurnRelative(new Vector3(Input.GetAxis("Vertical"),Input.GetAxis("Horizontal"),Input.GetAxis("Spin")));
	if (Input.GetAxis("Vertical") == 0 && Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Spin") == 0) {
		DampRotation();
	}
	return null;
}

function Thrust (amt : double) {
	if (!initialized) return false;
	if (amt == 0)
		return false;
	// tell all the engines to thrust.
	for (var e in engineModules) {
		e.Thrust(amt); // Maybe we'll need to tell it to thrust forward, maybe not.
	}
	// TODO: Add a drain on the fuel tanks?
	return true;
}

// We'll have to know which way to exert torque.
// I'll need to read up on how the engine does it to do this right.
function Turn (dir : Vector3) {
	if (!initialized) return false;
	if (dir.Equals(Vector3.zero))
		return false;
	// tell all the nav components to rotate.
	for (var n : NavPart in navModules) {
		n.Turn(dir); 
	}
	return true;
}

function TurnRelative (dir : Vector3) {
	if (!initialized) return false;
	if (dir.Equals(Vector3.zero))
		return false;
	// tell all the nav components to rotate.
	for (var n : NavPart in navModules) {
		n.TurnRelative(dir); 
	}
	return true;
}

function TurnToDir(dir : Vector3) {
		dir.Normalize();
		// start accelerating into a turn towards the target.
		var partAngle = Vector3.Angle(transform.forward,dir);
		// a = I/t is our max angular acceleration.
		var omega = rigidbody.angularVelocity;
		var torqueDirection = Vector3.Cross(transform.forward.normalized,dir.normalized);
		var alpha = omega.normalized * maxTorque/Mathf.Max([rigidbody.inertiaTensor.x,rigidbody.inertiaTensor.y,rigidbody.inertiaTensor.z]);
		//alpha = Vector3.one * Mathf.Min(rigidbody.inertiaTensor.x,rigidbody.inertiaTensor.y,rigidbody.inertiaTensor.z)/maxTorque;
		var decelTime = (omega.magnitude/alpha.magnitude);
		var decelPos = (Quaternion.AngleAxis(omega.magnitude*decelTime - 1/2*alpha.magnitude*decelTime*decelTime,torqueDirection) * transform.forward);
		decelPos.Normalize();
		
		torqueDirection.Normalize();

		if ((Vector3.Angle(decelPos,dir) < 0.5 || omega.magnitude > alpha.magnitude * decelTime) && omega != Vector3.zero) {
			//print("Braking!");
			KillRotation();
		} else {
			if (partAngle < 180)
			Turn(torqueDirection);
			else
			Turn(transform.up);
			//print("Decel Angle: "+decelAngle+" Angle: "+partAngle);
		}
		
		// Stop when we're close.
		if ((partAngle <= alpha.magnitude) || (omega == 0 && partAngle < alpha.magnitude)) {
			rigidbody.angularVelocity = Vector3.zero;
			rigidbody.isKinematic = true;
			//construct a final rotation.
			var rot : Quaternion = Quaternion.LookRotation(dir,transform.up);
			transform.rotation = rot;
			rigidbody.isKinematic = false;
			return 0;
		}
		partAngle = Vector3.Angle(transform.forward,dir);
		return partAngle + omega.magnitude;

}

function KillRotation () {
	if (!initialized) return false;
	for (var n : NavPart in navModules) {
		n.KillRotation(); 
	}
	return true;
}

function DampRotation () {
	if (!initialized) return false;
	for (var n : NavPart in navModules) {
		n.DampRotation(); 
	}
	return true;
}

function KillVelocity () {
	if (!initialized) return false;
	for (var e : EnginePart in engineModules) {
		e.KillVelocity(); 
	}
	return true;
}

// Remove a part from the ship.
function KillPart(p : PartBehavior) {
	// Check if the dead part is the command module.  If it is, then everything must go.
	if (p.GetComponent("CommandPart") && commandModule != null) {
		commandModule = null;
	}
	

	// free the dead part from here.
	//p.transform.position += transform.position;
	//p.transform.rotation *= transform.rotation;
	p.transform.parent = null;
	// Make the part a rigidbody.
	p.gameObject.AddComponent("Rigidbody");
	p.rigidbody.mass = p.mass;
	p.rigidbody.useGravity = false;
	p.rigidbody.isKinematic = false;
	p.rigidbody.angularDrag = 0; p.rigidbody.drag = 0;
	p.rigidbody.velocity = rigidbody.velocity; p.rigidbody.angularVelocity = rigidbody.angularVelocity;
	p.rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
	p.ship = null;
	mass -= p.mass;
	
	
	// Check and see what other parts we have to remove after this one comes off.
	var am : PartBehavior[]; 
	if (commandModule != null) {
		var cmd : PartBehavior = commandModule.gameObject.GetComponent("PartBehavior") as PartBehavior;
		print(cmd);
		am = GetAttachedParts(cmd, new Array());
	}
	else {
		am = new PartBehavior[0]; // If we lost the command module, everything must go.
		Destroy(gameObject); // This part can die too.
	}
	print("saving : "+am);
	for (var m : PartBehavior in allModules) {
		var stay : boolean = false;
		for (var x : PartBehavior in am)
			if (m==x) stay = true;
		if (!stay && m != p) {
				// free the dead part from here.
				//m.transform.position += transform.position;
				//m.transform.rotation *= transform.rotation;
				m.transform.parent = null;
				// Make the part a rigidbody.
				m.gameObject.AddComponent("Rigidbody");
				m.rigidbody.mass = p.mass;
				m.rigidbody.useGravity = false;
				m.rigidbody.isKinematic = false;
				m.rigidbody.angularDrag = 0; m.rigidbody.drag = 0;
				m.rigidbody.velocity = rigidbody.velocity; m.rigidbody.angularVelocity = rigidbody.angularVelocity;
				m.rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
				m.ship = null;
				m.UnconnectHardpoints();
				mass -= m.mass;
		}
	}
	
	// Reload the ship's stats now.
	if (commandModule != null) {
	allModules= null;
	engineModules = null;
	navModules = null;
	weaponModules = null;
	commandModule = null;
	initialized = false;
	Initialize();
	}
}

function GetAttachedParts(p : PartBehavior, arr : Array) : PartBehavior[] {
	arr.Push(p);
	print("keeping..."+p);
	for (var m : PartBehavior in p.connections) {
		if (m != null) {
		    print("Checking "+m);
			var ret : PartBehavior[] = arr.ToBuiltin(PartBehavior) as PartBehavior[];
			var doWork : boolean = true;
			for (var x : PartBehavior in ret)
				if (m==x) doWork = false;
			if (doWork)
			{
			arr.Concat(new Array(GetAttachedParts(m,arr)));
			}
		}
	}
	return arr.ToBuiltin(PartBehavior) as PartBehavior[];
}

// A collision function to pass on to the parts:
function OnCollisionEnter(other : Collision){
	for (var c : ContactPoint in other.contacts) {
		for (var p : PartBehavior in allModules) {
			if (p.collider.ClosestPointOnBounds(c.point) == c.point) {
				p.OnCollisionEnter(other);
			}
		}
	}
}

function OnExplosionHit(other : Vector3,f : double){
	for (var p : PartBehavior in allModules) {
		if (p.collider.ClosestPointOnBounds(other) == other) {
			p.OnExplosionHit(other,f);
		}
	}
}

@script RequireComponent(UnitController)