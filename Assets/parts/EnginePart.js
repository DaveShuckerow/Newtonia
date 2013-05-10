var thrust : double;
var canReverseThrust : int;
var damp : double;
// Use this for initialization
function Start () {
	var thruster : ParticleSystem = GetComponentInChildren(ParticleSystem) as ParticleSystem;
	if (thruster != null) {
		thruster.enableEmission = false;
	}
}

// Update is called once per frame

function LateUpdate() {
	var thruster : ParticleSystem = GetComponentInChildren(ParticleSystem) as ParticleSystem;
	if (thruster != null) {
		thruster.enableEmission = false;
	}
	var flare : Light = GetComponentInChildren(Light) as Light;
	if (flare != null) {
		flare.intensity = Mathf.Clamp(flare.intensity-0.1,0,2);
	}
}

function Thrust (amt : double) {
	// Stuff like this:
	amt = Mathf.Clamp(amt,-Mathf.Sign(canReverseThrust),1);
	transform.parent.rigidbody.AddForce(amt*thrust*transform.forward,UnityEngine.ForceMode.Impulse);
	
	var thruster : ParticleSystem = GetComponentInChildren(ParticleSystem) as ParticleSystem;
	if (thruster != null) {// && amt != 0) {
		thruster.enableEmission = true;
	}
	var flare : Light = GetComponentInChildren(Light) as Light;
	if (flare != null) {
		flare.intensity = Mathf.Clamp(flare.intensity+0.2,0,2);
	}
}

function KillVelocity () {
	if (transform.parent.rigidbody.velocity.magnitude != 0) {
		//Dampen our movement.
		var dir : Vector3 = new Vector3(-transform.parent.rigidbody.velocity.x,
									    -transform.parent.rigidbody.velocity.y,
									    -transform.parent.rigidbody.velocity.z);
		transform.parent.rigidbody.AddForce(new Vector3(thrust*Mathf.Clamp(dir.x,-1,1),
									   thrust*Mathf.Clamp(dir.y,-1,1),
									   thrust*Mathf.Clamp(dir.z,-1,1)),
						   UnityEngine.ForceMode.Impulse);
	}
}

function DampVelocity () {
	if (transform.parent.rigidbody.velocity.magnitude != 0) {
		//Dampen our movement.
		var dir : Vector3 = new Vector3(-transform.parent.rigidbody.velocity.x,
									    -transform.parent.rigidbody.velocity.y,
									    -transform.parent.rigidbody.velocity.z);
		transform.parent.rigidbody.AddForce(new Vector3(damp*thrust*Mathf.Clamp(dir.x,-1,1),
									   damp*thrust*Mathf.Clamp(dir.y,-1,1),
									   damp*thrust*Mathf.Clamp(dir.z,-1,1)),
						   UnityEngine.ForceMode.Impulse);
	}
}

// Implement parenting in some other way if possible.
@script RequireComponent(PartBehavior)