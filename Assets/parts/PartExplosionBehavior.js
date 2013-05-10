/*
 * PartExplosionBehavior.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Provides a camera that works a lot like the Homeworld camera.
 *
 * 18 October 2012
*/
#pragma strict
var time : double = 2;
var maxTime : double = 2;
var force : double = 0;
private var startTime;
function Start () {
	maxTime = time;
	collider.isTrigger = true;
}

function Update () {
	var light = GetComponent("Light") as Light;
	light.intensity = Mathf.Sin(time/maxTime * Mathf.PI);
	time -= Time.deltaTime;
	if (time <= 0) {
		GameObject.Destroy(gameObject);
	}
}

function OnTriggerEnter(other : Collider) {
	if (force == 0) return;
	print("Exploding!" + force);
	var col = GetComponent(SphereCollider);
	var pt : Vector3 = other.ClosestPointOnBounds(transform.position);
	var pb : PartBehavior = null;
	var sb : ShipBehavior = null;
	if (other.attachedRigidbody.gameObject.GetComponent("ShipBehavior"))
		sb = other.attachedRigidbody.gameObject.GetComponent("ShipBehavior") as ShipBehavior;
	if (other.attachedRigidbody.gameObject.GetComponent("PartBehavior"))
		pb = other.attachedRigidbody.gameObject.GetComponent("PartBehavior") as PartBehavior;
	if (pb != null)
		pb.OnExplosionHit(pt,force - force * (transform.position - pt).magnitude/col.radius);
	if (sb != null)
		sb.OnExplosionHit(pt,force - force * (transform.position - pt).magnitude/col.radius);
	other.attachedRigidbody.AddForceAtPosition((pt-transform.position).normalized * force * (1-(transform.position - pt).magnitude/col.radius),pt,ForceMode.Impulse);
}

function FixedUpdate() {
	if (time < maxTime - Time.deltaTime) {
	var col = GetComponent(SphereCollider);
	col.radius = 0;
	force = 0;
	}
}

@script RequireComponent(SphereCollider)