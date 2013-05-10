/*
 * UnitMotion.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Directs a unit to move to some given point.
 *
 * 16 October 2012
 */
var moveGoal : Vector3;
private var stage : int = 0; // stage 1: turn towards target, 2: move to target
private var startVelocity : Vector3;
private var startDirection : Vector3;
private var endDirection : Vector3;
private var distance : double;
private var ship : ShipBehavior;

private var angleTraveled = 0;
private var distanceTraveled = 0;

private var error = 5;

// Use this for initialization
function Start () {
}

// Update is called once per frame
function Update () {
}

function FixedUpdate() {
	var decelTime;		
	var partDistance = (moveGoal - transform.position).magnitude;
	var v = (rigidbody.velocity);
	var a = v.normalized * ship.maxThrust/ship.mass;//a = F/m
	// v = v0 + at
	decelTime = ship.mass * v.magnitude/ship.maxThrust;
	// Project where we'd stop if we started decelerating now.
	var decelPos = transform.position + (v * decelTime) - (a * decelTime/2 * decelTime);
	if (stage == 1) {
		if (ship.TurnToDir(moveGoal - transform.position) <= error/5) {
			ship.Thrust(1);
			stage = 2;
		}
		
	} else if (stage == 2) {
		//print("Stage 2!");
		// Thrust to the target!
		print(moveGoal + " " + decelPos + " " + (moveGoal - decelPos) + " " + transform.forward);
		if ((decelPos - moveGoal).magnitude > error*partDistance*Mathf.PI/180) {
			
			if (ship.TurnToDir(moveGoal - decelPos) < error) {
				ship.Thrust(Mathf.Clamp01((moveGoal-decelPos).magnitude/ship.maxThrust));
				//ship.KillRotation();
			}
			//ship.TurnToDir(moveGoal - decelPos);
		}
		
		// Stop.
		if ((partDistance < a.magnitude * error) && v.magnitude < a.magnitude) {
			rigidbody.velocity = Vector3.zero;
			rigidbody.isKinematic = true;
			//transform.position = moveGoal;
			rigidbody.isKinematic = false;
			stage = 3;
		}
	} else if (stage == 3) {
		if (ship.TurnToDir(endDirection - startDirection) == 0)
			GameObject.Destroy(this);
	}
	if ((decelPos - moveGoal).magnitude <= error*partDistance*Mathf.PI/180) {
		ship.KillVelocity();
		ship.KillRotation();
	}
}

// Start moving.
function Initialize () {
	stage = 1;
	//moveGoal.y = 0;
	ship = GetComponent("ShipBehavior") as ShipBehavior;	
	startVelocity = rigidbody.velocity;
	startDirection = transform.forward;
	endDirection = moveGoal - transform.position;
	print("Start: "+startDirection + "End: " + endDirection);
	print("Goal: "+moveGoal+"Start: "+transform.position);
	distance = (moveGoal - transform.position).magnitude;
	distanceTraveled = 0;
	angleTraveled = 0;
}