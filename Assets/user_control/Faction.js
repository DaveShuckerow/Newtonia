/*
 * Faction.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * A faction in the game.
 * We'll use this class to store logistics about how it's performing
 * and also to tell units who can give them orders. For the Empire!
 *
 * 16 October 2012
 */
// Class Management variables
static var allFactions : Array;
static var playerFaction : Faction;

// Instance variables
var selectedUnits : UnitController[];

// Use this for initialization
function Start () {
	if (allFactions == null) {
		allFactions = new Array();
		playerFaction = this;
	}
	allFactions.Add(this);
}

function AddShip(shipname: String) {
	
}

// Update is called once per frame
function Update () {
}

