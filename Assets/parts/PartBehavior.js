/*
 * PartBehavior.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Defines the behavior of individual ship parts.
 *
 * 11 October 2012
*/

#pragma strict
var hardpointList : Vector3[];
var forwardList : Vector3[];
//var radialMount : int; // Not sure if we'll implement this
var health : double;      // How much damage part can take before falling off
var maxHealth;  
var mass : double; 
var connections : PartBehavior[];
var explodeForce : double; // These guys determine the explosion on destruction
var explodeSize : double;
var explosion : GameObject; // Explosion effect to create on death.
var myName : String;
var myAuthor : String;
var ship : ShipBehavior;

function Start () {
	maxHealth = health;
}

function LateUpdate () {
	if (health < 0) {
		Die();
	}
}

function ConnectToHardpoint(that : PartBehavior, thisHP : int, thatHP : int) {
	if (gameObject.GetComponent("Rigidbody"))
	rigidbody.isKinematic = true;
	if (that.gameObject.GetComponent("Rigidbody"))
	that.rigidbody.isKinematic = true;
	
	// Move us to the right place with respect to the target.
	var x = that.transform.localPosition.x;
	var y = that.transform.localPosition.y;
	var z = that.transform.localPosition.z;
	transform.localPosition = Vector3(0,0,0);
	transform.localPosition = Vector3(x-hardpointList[thisHP].x+that.hardpointList[thatHP].x,
											     y-hardpointList[thisHP].y+that.hardpointList[thatHP].y,
											     z-hardpointList[thisHP].z+that.hardpointList[thatHP].z);
	// Orient us in the same way as the target.
	gameObject.transform.localRotation = (that.transform.localRotation);
	
	// Make it official.
	/*var joint = gameObject.AddComponent("FixedJoint") as FixedJoint;
	joint.connectedBody = that.rigidbody;
	joint.breakForce = Number.PositiveInfinity;
	joint.breakTorque = Number.PositiveInfinity;*/
	connections[thisHP] = that;
	that.connections[thatHP] = this;
	
	if (gameObject.GetComponent("Rigidbody"))
	Rigidbody.Destroy(rigidbody);
	if (that.gameObject.GetComponent("Rigidbody"))
	Rigidbody.Destroy(that.rigidbody);
}

// Handle collisions.
function OnCollisionEnter(other : Collision) {
	var p : double;
	if (other.gameObject.GetComponent("WeaponBehavior")) {
		// We hit a weapon.  Make it do stuff.
		var imp1 : WeaponBehavior = other.gameObject.GetComponent("WeaponBehavior") as WeaponBehavior;
		p = imp1.mass * other.relativeVelocity.magnitude;
		health = health - p;
		imp1.Die();
		
	} else if (other.gameObject.GetComponent("ShipBehavior")) {
		// We hit a ship.  Do stuff.
		// We hit a weapon.  Make it do stuff.
		var imp2 : ShipBehavior = other.gameObject.GetComponent("ShipBehavior") as ShipBehavior;
		if (imp2 != ship) {
		p = imp2.mass * other.relativeVelocity.magnitude;
		health -= p;

		}
		
	} else if (other.gameObject.GetComponent("PartBehavior")) {
		// We hit a ship.  Do stuff.
		// We hit a weapon.  Make it do stuff.
		var imp3 : PartBehavior = other.gameObject.GetComponent("PartBehavior") as PartBehavior;
		if (imp3.ship != ship) {
		p = imp3.mass * other.relativeVelocity.magnitude;
		health -= p;
		
		}
	}
}

function Die() {
	var exp : PartExplosionBehavior = Instantiate(explosion,transform.position,transform.rotation).GetComponent("PartExplosionBehavior") as PartExplosionBehavior;
	exp.GetComponent(SphereCollider).radius = explodeSize;
	print(exp.collider.bounds.extents+ " " +explodeSize);
	exp.force = explodeForce;
	UnconnectHardpoints();
	if (ship != null)
	ship.KillPart(this);
	Destroy(gameObject);
}

function UnconnectHardpoints() {
	for (var i=0; i<connections.length; i+=1) {
		var p:PartBehavior = connections[i];
		if (p != null)
		for (var j=0; j<p.connections.length; j+=1) {
			if (p.connections[j] == this) {
				p.connections[j] = null;
			}
		}
		connections[i] = null;
	}
}

function OnExplosionHit(pt : Vector3, f : double) {
	health -= f;
	print(name + " health : " +health);
	if (health < 0) {
		Die();
	}
}

@script RequireComponent(UnitSelection)