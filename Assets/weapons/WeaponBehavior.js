/*
 * WeaponBehavior.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Generic weapon behavior.
 *
 * 17 October 2012
 */
var faction : Faction;
var mass : double = 1;
var damage : double;
var damageRadius : double;
var time : double = 30.0;
// Use this for initialization
function Start () {
}

// Update is called once per frame
function Update () {
	time -= Time.deltaTime;
	if (time < 0) {
		GameObject.Destroy(gameObject);
	}
}

function Die() {
	// Make other behaviors die too.
	var bul = gameObject.GetComponent("BulletBehavior") as BulletBehavior;
	if (bul != null) {
		bul.Die();
	}
	
	GameObject.Destroy(this.gameObject);
}

@script RequireComponent(Rigidbody)