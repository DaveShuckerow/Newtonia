/*
 * UnitController.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Control script for unit management.
 * Must be applied to all ShipBehaviors.
 *
 * 16 October 2012
 */
 
 
/*
 * How do we do this?
 * Unit management as a static class.
 *		-We need units to be controlled by their respective faction.
 *		-Let the static class manage 
 * We'll start off using raycasting to look for units to order.
 * 		-Multi-unit selection and groups will be done later.
 *		-AI control will have to be added in later too.
 */
import Faction;
import ShipBehavior;

var faction : Faction;
var selected : boolean; 
var highlighted : boolean;
// 
function Update () {
	if (selected) {
		if (Input.GetKey("m") && GetComponent(UnitMoveCommand) == null) {
			gameObject.AddComponent(UnitMoveCommand);
		}
	}
}

function LateUpdate () {
	if (highlighted == true) highlighted = false;
}