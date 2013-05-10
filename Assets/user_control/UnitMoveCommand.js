/*
 * UnitMoveCommand.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Player move ordering for ships.
 * Creates a UnitMotion component for them if successful.
 *
 * 16 October 2012
 */
var moveGoal : Vector3;
// Use this for initialization
function Start () {
	moveGoal = transform.position;
}

// Update is called once per frame
function LateUpdate() {
	if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) {
		// Movement in X-Z plane
		var movePlane = new Plane(new Vector3(0,1,0),new Vector3(transform.position.x,moveGoal.y,transform.position.z));
		var ray : Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		var dist : float;
		movePlane.Raycast(ray,dist);
		//var tempy = moveGoal.y;
		moveGoal = Camera.main.ScreenPointToRay(Input.mousePosition).GetPoint(dist);
		//moveGoal.y = tempy;
	}
	else {
		moveGoal.y += Input.GetAxis("Mouse Y");
	}
	if (Input.GetKey("mouse 0")) {
		// Finalize the order.
		Finalize(GetComponent("ShipBehavior") as ShipBehavior,moveGoal);
		Component.Destroy(this);
	}
	//print(moveGoal);
}

static function Finalize (ship : ShipBehavior, goal : Vector3) { // Static so that the AI doesn't have to instantiate one to do this.
	if (ship.GetComponent("UnitMotion") == null) {
	ship.gameObject.AddComponent("UnitMotion");
	}
	var um : UnitMotion = ship.GetComponent("UnitMotion") as UnitMotion;
	um.moveGoal = goal;
	um.Initialize();
}

function OnDrawGizmos () {
	Debug.DrawLine(transform.position,moveGoal);
	Debug.DrawLine(transform.position, new Vector3(moveGoal.x,transform.position.y,moveGoal.z));
	Debug.DrawLine(new Vector3(moveGoal.x,transform.position.y,moveGoal.z),moveGoal);
	Gizmos.DrawWireSphere(moveGoal,1);
}

