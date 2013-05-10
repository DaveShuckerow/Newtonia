var torque : double;
var damp : double;
// Use this for initialization
function Start () {
	damp = 0.1;
}

// Update is called once per frame
function Update () {
}
function FixedUpdate() {
}
function TurnRelative (dir : Vector3) {
	// Stuff like this:
	//dir.Normalize();
	transform.parent.rigidbody.AddRelativeTorque(new Vector3(torque*Mathf.Clamp(dir.x,-1,1),
											torque*Mathf.Clamp(dir.y,-1,1),
											torque*Mathf.Clamp(dir.z,-1,1)),
								UnityEngine.ForceMode.Impulse);
}

function Turn (dir : Vector3) {
	// Stuff like this:
	//dir.Normalize();
	transform.parent.rigidbody.AddTorque(new Vector3(torque*Mathf.Clamp(dir.x,-1,1),
									torque*Mathf.Clamp(dir.y,-1,1),
									torque*Mathf.Clamp(dir.z,-1,1)),
						UnityEngine.ForceMode.Impulse);
}

function KillRotation () {
	if (transform.parent.rigidbody.angularVelocity.magnitude != 0) {
		//Dampen our movement.
		var dir : Vector3 = new Vector3(-transform.parent.rigidbody.angularVelocity.x,
									    -transform.parent.rigidbody.angularVelocity.y,
									    -transform.parent.rigidbody.angularVelocity.z);
		if (dir.magnitude > 1)
			dir.Normalize();
		transform.parent.rigidbody.AddTorque(new Vector3(torque*dir.x,
										torque*dir.y,
										torque*dir.z),
							UnityEngine.ForceMode.Impulse);
	}
}
function DampRotation () {
	if (transform.parent.rigidbody.angularVelocity.magnitude != 0) {
		//Dampen our movement.
		var dir : Vector3 = new Vector3(-transform.parent.rigidbody.angularVelocity.x,
									    -transform.parent.rigidbody.angularVelocity.y,
									    -transform.parent.rigidbody.angularVelocity.z);
		if (dir.magnitude > 1)
			dir.Normalize();
		transform.parent.rigidbody.AddTorque(new Vector3(damp*torque*dir.x,
										damp*torque*dir.y,
										damp*torque*dir.z),
							UnityEngine.ForceMode.Impulse);
	}
}


// Implement parenting in some other way if possible.
@script RequireComponent(PartBehavior)