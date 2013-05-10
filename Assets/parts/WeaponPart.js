/*
 * WeaponPart.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * A weapon part of a ship.
 *
 * 17 October 2012
 */
#pragma strict
var bullet : GameObject; // This is just the type.
var muzzle : Vector3;
var fireForce : double;
function Start () {

}

function FixedUpdate () {
	if (Input.GetKeyDown("space")) {
		Fire();
	}
}

function Fire () {
	var shot : WeaponBehavior;
	var part : PartBehavior = gameObject.GetComponent("PartBehavior") as PartBehavior;
	if (part.ship != null) {
	var ship : UnitController = part.ship.GetComponent("UnitController") as UnitController;
	shot = Instantiate(bullet,transform.position+transform.rotation*muzzle,transform.rotation).GetComponent("WeaponBehavior") as WeaponBehavior;
	shot.faction = ship.faction;
	shot.transform.parent = null;
	shot.rigidbody.velocity = ship.rigidbody.velocity;
	shot.rigidbody.mass = shot.mass;
	shot.rigidbody.AddForce(transform.forward*fireForce,ForceMode.Impulse);
	ship.rigidbody.AddForce(-transform.forward*fireForce,ForceMode.Impulse);
	}
}

function OnDrawGizmos () {
	Gizmos.DrawWireSphere(transform.position+transform.rotation*muzzle,0.1);
}