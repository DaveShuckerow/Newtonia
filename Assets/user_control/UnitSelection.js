/*
 * UnitSelection.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Allow selection of units.
 * Must be applied to all PartBehaviors
 *
 * 16 October 2012
 */
import Faction;
// Use this for initialization
function Start () {
}

// Update is called once per frame
function Update () {
	var ray : Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    var hit : RaycastHit;
    if (collider.Raycast (ray, hit, Mathf.Infinity) && (GetComponent(PartBehavior) as PartBehavior).ship != null) {
    	// We've got something.  Highlight the ship.
    	var ship = (GetComponent(PartBehavior) as PartBehavior).ship;
    	var controller = ship.GetComponent("UnitController") as UnitController;
    	controller.highlighted = true;
    	if (Input.GetKey("mouse 0")) {
    		if (controller.faction  == Faction.playerFaction) {
    			controller.selected = true;
    		}
    		// do stuff depending on faction.
    		
    		print(ship.name + " is selected!");
    	}
        Debug.DrawLine (Camera.main.transform.position, hit.point);
    }
}

